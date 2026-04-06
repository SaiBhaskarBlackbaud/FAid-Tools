"""
scraper.py — Web crawling engine.

Crawls a school's website starting from the homepage and collects the
text content + link hrefs from pages that are likely to mention financial
aid products.  Respects rate limiting and handles errors gracefully.
"""

import logging
import time
from collections import deque
from urllib.parse import urljoin, urlparse
from detector import detect_competitors, detect_our_product

import requests
import urllib3
from bs4 import BeautifulSoup

from config import (
    DEFAULT_CRAWL_DEPTH,
    MAX_PAGES_PER_SCHOOL,
    REQUEST_DELAY_SECONDS,
    REQUEST_TIMEOUT_SECONDS,
    RELEVANT_PAGE_KEYWORDS,
    USER_AGENT,
    VERIFY_SSL,
)

# Suppress InsecureRequestWarning only when SSL verification is disabled
if not VERIFY_SSL:
    urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

logger = logging.getLogger(__name__)

_SESSION_HEADERS = {
    "User-Agent": USER_AGENT,
    "Accept-Language": "en-US,en;q=0.9",
}


# ---------------------------------------------------------------------------
# Internal helpers
# ---------------------------------------------------------------------------


def _make_session() -> requests.Session:
    """Create a requests Session with the project's default headers."""
    session = requests.Session()
    session.headers.update(_SESSION_HEADERS)
    return session


def _is_same_origin(base_url: str, url: str) -> bool:
    """Return True if *url* is on the same hostname as *base_url*."""
    base_host = urlparse(base_url).netloc.lower()
    url_host = urlparse(url).netloc.lower()
    return base_host == url_host


def _normalize_url(url: str) -> str:
    """Strip fragment identifiers so we don't visit the same page twice."""
    parsed = urlparse(url)
    return parsed._replace(fragment="").geturl()


def _is_relevant_url(url: str) -> bool:
    """
    Return True if the URL path looks like it could contain financial-aid info.
    All pages are visited; this just boosts priority for well-named paths.
    """
    path = urlparse(url).path.lower()
    return any(kw.replace(" ", "-") in path or kw.replace(" ", "") in path
               for kw in RELEVANT_PAGE_KEYWORDS)


def _is_relevant_page(text: str, url: str) -> bool:
    """
    Return True if the page's text or URL contains any relevant keyword.
    Used to decide whether to continue crawling sub-links from this page.
    """
    combined = (url + " " + text).lower()
    return any(kw in combined for kw in RELEVANT_PAGE_KEYWORDS)


def _fetch_page(session: requests.Session, url: str) -> tuple[str, list[str], list[str]]:
    """
    Fetch *url* and return (visible_text, hrefs, internal_links).

    Returns empty strings/lists on any error.
    """
    try:
        response = session.get(
            url,
            timeout=REQUEST_TIMEOUT_SECONDS,
            verify=VERIFY_SSL,   # configurable via config.VERIFY_SSL
            allow_redirects=True,
        )
        response.raise_for_status()
    except requests.exceptions.RequestException as exc:
        logger.warning("Failed to fetch %s: %s", url, exc)
        return "", [], []

    soup = BeautifulSoup(response.text, "lxml")

    # Extract visible text
    visible_text = soup.get_text(separator=" ", strip=True)

    # Collect all href values (for domain detection)
    hrefs = [a.get("href", "") for a in soup.find_all("a", href=True)]

    # Build list of absolute internal links for further crawling
    internal_links: list[str] = []
    for a_tag in soup.find_all("a", href=True):
        href = a_tag["href"].strip()
        abs_url = urljoin(url, href)
        abs_url = _normalize_url(abs_url)
        parsed = urlparse(abs_url)
        # Keep only http(s) links on the same origin
        if parsed.scheme in ("http", "https") and _is_same_origin(url, abs_url):
            internal_links.append(abs_url)

    return visible_text, hrefs, internal_links


# ---------------------------------------------------------------------------
# Public API
# ---------------------------------------------------------------------------


def scrape_school(
    url: str,
    depth: int = DEFAULT_CRAWL_DEPTH,
) -> list[dict]:
    """
    Crawl a school website up to *depth* levels and return scraped page data.

    Phase 1 — visits the homepage and all pages whose URL path matches
    financial-aid / admissions keywords.  Stops as soon as a product keyword
    or competitor is detected.

    Phase 2 — only entered if Phase 1 found nothing.  Visits the remaining
    (non-relevant-URL) pages collected during Phase 1 link discovery.

    Parameters
    ----------
    url : str
        The school's homepage URL.
    depth : int
        Maximum crawl depth (0 = homepage only).

    Returns
    -------
    list[dict]
        Each element: ``{"url": str, "text": str, "hrefs": list[str]}``.
    """
    session = _make_session()
    pages: list[dict] = []
    visited: set[str] = set()
    non_relevant_urls: set[str] = set()  # collected during Phase 1 for Phase 2

    # ------------------------------------------------------------------
    # Phase 1 — homepage + relevant-URL pages only
    # ------------------------------------------------------------------
    phase1_queue: deque[tuple[str, int]] = deque()
    phase1_queue.append((_normalize_url(url), 0))

    found_match = False

    while phase1_queue and len(pages) < MAX_PAGES_PER_SCHOOL:
        current_url, current_depth = phase1_queue.popleft()

        if current_url in visited:
            continue
        visited.add(current_url)

        logger.debug("[Phase 1] Fetching (%d/%d): %s", len(pages) + 1, MAX_PAGES_PER_SCHOOL, current_url)

        text, hrefs, internal_links = _fetch_page(session, current_url)
        if not text:
            continue

        pages.append({"url": current_url, "text": text, "hrefs": hrefs})

        if detect_our_product(text, hrefs) or detect_competitors(text, hrefs, current_url):
            logger.info("Match detected on %s — stopping crawl early.", current_url)
            found_match = True
            break

        time.sleep(REQUEST_DELAY_SECONDS)

        if current_depth < depth:
            for link in internal_links:
                if link not in visited:
                    if _is_relevant_url(link):
                        phase1_queue.append((link, current_depth + 1))
                    else:
                        non_relevant_urls.add(link)

    if found_match:
        logger.info("Crawled %d page(s) for %s (depth=%d)", len(pages), url, depth)
        return pages

    # ------------------------------------------------------------------
    # Phase 2 — non-relevant pages collected during Phase 1
    # ------------------------------------------------------------------
    logger.info(
        "No match in relevant pages — checking %d non-relevant page(s).",
        len(non_relevant_urls),
    )

    phase2_queue: deque[str] = deque(u for u in non_relevant_urls if u not in visited)

    while phase2_queue and len(pages) < MAX_PAGES_PER_SCHOOL:
        current_url = phase2_queue.popleft()

        if current_url in visited:
            continue
        visited.add(current_url)

        logger.debug("[Phase 2] Fetching (%d/%d): %s", len(pages) + 1, MAX_PAGES_PER_SCHOOL, current_url)

        text, hrefs, internal_links = _fetch_page(session, current_url)
        if not text:
            continue

        pages.append({"url": current_url, "text": text, "hrefs": hrefs})

        if detect_our_product(text, hrefs) or detect_competitors(text, hrefs, current_url):
            logger.info("Match detected on %s — stopping crawl early.", current_url)
            break

        time.sleep(REQUEST_DELAY_SECONDS)

    logger.info("Crawled %d page(s) for %s (depth=%d)", len(pages), url, depth)
    return pages
