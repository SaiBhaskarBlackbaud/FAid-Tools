"""
detector.py — Product detection logic.

Provides functions to decide, given scraped page content and link URLs:
- Whether the school is still using our Blackbaud product
- Which competitor(s) are detected (if any)
"""

import re
import logging
from typing import Optional
from urllib.parse import urlparse

from config import (
    OUR_PRODUCT_KEYWORDS,
    OUR_PRODUCT_DOMAINS,
    COMPETITORS,
)

logger = logging.getLogger(__name__)


def _domain_from_url(url: str) -> str:
    """Extract the registered domain (hostname) from a URL string."""
    try:
        return urlparse(url).netloc.lower()
    except Exception:
        return ""


def _text_contains_keyword(text: str, keyword: str) -> bool:
    """Return True if *text* contains *keyword* (case-insensitive, whole-word match).

    Word boundaries ensure e.g. "TADS" does not match inside "GRADS", and
    "Smart Aid" does not match "SmartAider".
    """
    pattern = r"\b" + re.escape(keyword) + r"\b"
    return bool(re.search(pattern, text, flags=re.IGNORECASE))


def _domain_matches(href: str, domain: str) -> bool:
    """Return True if the href's hostname ends with *domain*."""
    host = _domain_from_url(href)
    return host == domain or host.endswith("." + domain)


# ---------------------------------------------------------------------------
# Public API
# ---------------------------------------------------------------------------


def detect_our_product(page_text: str, hrefs: list[str]) -> bool:
    """
    Return True if any Blackbaud/our-product keyword or domain is found.

    Parameters
    ----------
    page_text : str
        The full visible text of the page.
    hrefs : list[str]
        All href attribute values collected from <a> tags on the page.
    """
    # Check text keywords
    for keyword in OUR_PRODUCT_KEYWORDS:
        if _text_contains_keyword(page_text, keyword):
            logger.debug("Our product keyword found: %r", keyword)
            return True

    # Check link domains
    for href in hrefs:
        for domain in OUR_PRODUCT_DOMAINS:
            if _domain_matches(href, domain):
                logger.debug("Our product domain found in href: %r (domain: %s)", href, domain)
                return True

    return False


def detect_competitors(page_text: str, hrefs: list[str]) -> list[str]:
    """
    Return a list of detected competitor names (may be empty).

    Parameters
    ----------
    page_text : str
        The full visible text of the page.
    hrefs : list[str]
        All href attribute values collected from <a> tags on the page.
    """
    found: set[str] = set()

    for competitor in COMPETITORS:
        name = competitor["name"]

        # Check text keywords
        for keyword in competitor.get("keywords", []):
            if _text_contains_keyword(page_text, keyword):
                logger.debug("Competitor keyword found: %r → %s", keyword, name)
                found.add(name)
                break  # no need to check more keywords for this competitor

        # Check link domains (even if we already matched via text)
        for href in hrefs:
            for domain in competitor.get("domains", []):
                if _domain_matches(href, domain):
                    logger.debug(
                        "Competitor domain found in href: %r (domain: %s) → %s",
                        href,
                        domain,
                        name,
                    )
                    found.add(name)

    return sorted(found)


def analyze_pages(pages: list[dict]) -> dict:
    """
    Analyze a collection of scraped pages and return a consolidated result.

    Parameters
    ----------
    pages : list[dict]
        Each element has keys: ``url`` (str), ``text`` (str), ``hrefs`` (list[str]).

    Returns
    -------
    dict with keys:
        ``uses_our_product`` (bool),
        ``competitors`` (list[str])
    """
    uses_our_product = False
    all_competitors: set[str] = set()

    for page in pages:
        text = page.get("text", "")
        hrefs = page.get("hrefs", [])

        if not uses_our_product and detect_our_product(text, hrefs):
            uses_our_product = True

        competitors = detect_competitors(text, hrefs)
        all_competitors.update(competitors)

    return {
        "uses_our_product": uses_our_product,
        "competitors": sorted(all_competitors),
    }
