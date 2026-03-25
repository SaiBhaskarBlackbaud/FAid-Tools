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

    Parameters
    ----------
    url : str
        The school's homepage URL.
    depth : int
        Maximum crawl depth (0 = homepage only, 2 = default).

    Returns
    -------
    list[dict]
        Each element: ``{"url": str, "text": str, "hrefs": list[str]}``.
    """
    session = _make_session()
    pages: list[dict] = []
    visited: set[str] = set()

    # BFS queue: (url, current_depth)
    # Seed with the homepage at depth 0
    queue: deque[tuple[str, int]] = deque()
    queue.append((_normalize_url(url), 0))

    # Collect relevant-looking URLs first so we visit them with priority;
    # non-relevant URLs are deferred to the end of the queue.
    deferred: deque[tuple[str, int]] = deque()

    while (queue or deferred) and len(pages) < MAX_PAGES_PER_SCHOOL:
        # Prefer relevant URLs; fall back to deferred once queue is empty
        if queue:
            current_url, current_depth = queue.popleft()
        else:
            current_url, current_depth = deferred.popleft()

        if current_url in visited:
            continue
        visited.add(current_url)

        logger.debug("Fetching (%d/%d): %s", len(pages) + 1, MAX_PAGES_PER_SCHOOL, current_url)

        text, hrefs, internal_links = _fetch_page(session, current_url)

        if not text:
            # Skip pages we couldn't fetch
            continue

        pages.append({"url": current_url, "text": text, "hrefs": hrefs})

        # Respect rate limiting
        time.sleep(REQUEST_DELAY_SECONDS)

        # Enqueue child links if we haven't reached max depth
        if current_depth < depth:
            for link in internal_links:
                if link not in visited:
                    if _is_relevant_url(link):
                        queue.append((link, current_depth + 1))
                    else:
                        deferred.append((link, current_depth + 1))

    logger.info(
        "Crawled %d page(s) for %s (depth=%d)",
        len(pages),
        url,
        depth,
    )
    return pages
