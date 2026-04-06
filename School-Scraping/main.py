"""
main.py — Entry point / CLI for the School-Scraping application.

Usage
-----
    python main.py
    python main.py --input path/to/schools.csv --output path/to/output_dir
    python main.py --depth 3
    python main.py --verbose
"""

import argparse
import csv
import logging
import os
import sys

from config import (
    DEFAULT_CRAWL_DEPTH,
    DEFAULT_INPUT_FILE,
    DEFAULT_OUTPUT_DIR,
    DEFAULT_OUTPUT_STEM,
)
from detector import analyze_pages
from reporter import write_reports
from scraper import scrape_school


# ---------------------------------------------------------------------------
# Logging setup
# ---------------------------------------------------------------------------


def _configure_logging(verbose: bool) -> None:
    """Configure root logger level and format."""
    level = logging.DEBUG if verbose else logging.INFO
    logging.basicConfig(
        level=level,
        format="%(asctime)s [%(levelname)s] %(name)s: %(message)s",
        datefmt="%Y-%m-%d %H:%M:%S",
    )


# ---------------------------------------------------------------------------
# Input reading
# ---------------------------------------------------------------------------


def _read_schools(input_file: str) -> list[dict]:
    """
    Read school records from a CSV file.

    Expected columns: ``School Name``, ``School Website``.

    Parameters
    ----------
    input_file : str
        Path to the input CSV file.

    Returns
    -------
    list[dict] with keys ``name`` and ``url``.
    """
    if not os.path.isfile(input_file):
        logging.error("Input file not found: %s", input_file)
        sys.exit(1)

    schools: list[dict] = []
    with open(input_file, newline="", encoding="utf-8") as f:
        reader = csv.DictReader(f)
        for row in reader:
            name = row.get("School Name", "").strip()
            url = row.get("School Website", "").strip()
            if name and url:
                schools.append({"name": name, "url": url})
            else:
                logging.warning("Skipping incomplete row: %s", row)

    if not schools:
        logging.error("No valid school records found in %s", input_file)
        sys.exit(1)

    return schools


# ---------------------------------------------------------------------------
# Core processing
# ---------------------------------------------------------------------------


def process_school(school: dict, depth: int) -> dict:
    """
    Scrape and analyze a single school.

    Parameters
    ----------
    school : dict
        Must contain ``name`` and ``url`` keys.
    depth : int
        Crawl depth.

    Returns
    -------
    dict suitable for reporter.write_reports.
    """
    name = school["name"]
    url = school["url"]

    logging.info("Scraping: %s (%s)", name, url)

    try:
        pages = scrape_school(url, depth=depth)
    except Exception as exc:
        logging.error("Unexpected error scraping %s: %s", url, exc)
        pages = []

    if not pages:
        logging.warning("No pages retrieved for %s — marking as unknown.", name)
        return {
            "school_name": name,
            "school_website": url,
            "uses_our_product": False,
            "competitors": [],
            "competitor_url": "",
            "our_product_url": "",
        }

    analysis = analyze_pages(pages)

    return {
        "school_name": name,
        "school_website": url,
        "uses_our_product": analysis["uses_our_product"],
        "competitors": analysis["competitors"],
        "competitor_url": analysis.get("competitor_url", ""),
        "our_product_url": analysis.get("our_product_url", ""),
    }


# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------


def _build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        prog="main.py",
        description=(
            "Scrape school websites to determine whether they are still using "
            "Blackbaud's financial aid products or have switched to a competitor."
        ),
    )
    parser.add_argument(
        "--input",
        default=DEFAULT_INPUT_FILE,
        metavar="FILE",
        help=f"Path to the input CSV file (default: {DEFAULT_INPUT_FILE})",
    )
    parser.add_argument(
        "--output",
        default=DEFAULT_OUTPUT_DIR,
        metavar="DIR",
        help=f"Directory for output reports (default: {DEFAULT_OUTPUT_DIR})",
    )
    parser.add_argument(
        "--stem",
        default=DEFAULT_OUTPUT_STEM,
        metavar="NAME",
        help=f"Base filename for output reports without extension (default: {DEFAULT_OUTPUT_STEM})",
    )
    parser.add_argument(
        "--depth",
        type=int,
        default=DEFAULT_CRAWL_DEPTH,
        metavar="N",
        help=f"Crawl depth — levels to follow from the homepage (default: {DEFAULT_CRAWL_DEPTH})",
    )
    parser.add_argument(
        "--verbose",
        action="store_true",
        help="Enable DEBUG-level logging",
    )
    return parser


def main() -> None:
    """Main entry point."""
    parser = _build_parser()
    args = parser.parse_args()

    _configure_logging(args.verbose)

    schools = _read_schools(args.input)
    total = len(schools)
    logging.info("Loaded %d school(s) from %s", total, args.input)

    results: list[dict] = []

    for index, school in enumerate(schools, start=1):
        logging.info("--- Processing school %d of %d ---", index, total)
        result = process_school(school, depth=args.depth)
        results.append(result)

        # Print a quick summary line for the user
        status = "YES (still ours)" if result["uses_our_product"] else "NO"
        lost_to = ", ".join(result["competitors"]) if result["competitors"] else "Unknown"
        line = f"  [{index}/{total}] {school['name']}: Status={status}"
        if not result["uses_our_product"]:
            line += f", Lost To={lost_to}"
        print(line)

    logging.info("All schools processed. Writing reports...")
    write_reports(results, output_dir=args.output, stem=args.stem)
    logging.info("Done. Reports saved to: %s/", args.output)


if __name__ == "__main__":
    main()
