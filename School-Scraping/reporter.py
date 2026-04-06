"""
reporter.py — CSV and Excel report generation.

Writes results to output/results.csv and output/results.xlsx.
"""

import csv
import logging
import os
from typing import Any

import openpyxl
from openpyxl.styles import Font, PatternFill, Alignment
from openpyxl.utils import get_column_letter

logger = logging.getLogger(__name__)

# ---------------------------------------------------------------------------
# Column definitions
# ---------------------------------------------------------------------------

COLUMNS = [
    "School Name",
    "School Website",
    "Status (Yes/No)",
    "Lost To (Competitor)",
    "Competitor Found On (URL)",
    "Our Product Found On (URL)",
]


def _format_row(result: dict) -> list[str]:
    """
    Convert a result dict to an ordered list of cell values matching COLUMNS.

    Expected keys in *result*:
        school_name, school_website, uses_our_product, competitors (list)
    """
    uses_our = result.get("uses_our_product", False)
    competitors: list[str] = result.get("competitors", [])
    competitor_url = result.get("competitor_url", "")
    our_product_url = result.get("our_product_url", "")

    status = "Yes" if uses_our and not competitors else "No"
    lost_to = ", ".join(competitors) if competitors else ("" if uses_our else "Unknown")

    return [
        result.get("school_name", ""),
        result.get("school_website", ""),
        status,
        lost_to,
        competitor_url,
        our_product_url,
    ]


# ---------------------------------------------------------------------------
# CSV output
# ---------------------------------------------------------------------------


def write_csv(results: list[dict], filepath: str) -> None:
    """
    Write *results* to a CSV file at *filepath*.

    Parameters
    ----------
    results : list[dict]
        List of result dicts (see _format_row for expected keys).
    filepath : str
        Destination file path (directories must already exist).
    """
    os.makedirs(os.path.dirname(filepath) or ".", exist_ok=True)
    with open(filepath, "w", newline="", encoding="utf-8") as f:
        writer = csv.writer(f)
        writer.writerow(COLUMNS)
        for result in results:
            writer.writerow(_format_row(result))

    logger.info("CSV report written to: %s", filepath)


# ---------------------------------------------------------------------------
# Excel output
# ---------------------------------------------------------------------------

_HEADER_FILL = PatternFill("solid", fgColor="1F4E79")   # dark blue
_HEADER_FONT = Font(bold=True, color="FFFFFF", size=11)
_YES_FILL = PatternFill("solid", fgColor="C6EFCE")      # light green
_NO_FILL = PatternFill("solid", fgColor="FFCCCC")        # light red


def _auto_fit_column(ws: Any, col_index: int, header: str, values: list[str]) -> None:
    """Set column width based on the maximum content length."""
    max_len = max(
        (len(str(v)) for v in [header] + values if v),
        default=10,
    )
    # Add a small padding; cap at 60 characters
    ws.column_dimensions[get_column_letter(col_index)].width = min(max_len + 4, 60)


def write_excel(results: list[dict], filepath: str) -> None:
    """
    Write *results* to an Excel (.xlsx) file at *filepath*.

    The worksheet has:
    - Bold, dark-blue header row with white text
    - Light-green rows for schools still using our product (Status = Yes)
    - Light-red rows for schools no longer using our product (Status = No)
    - Auto-fitted column widths

    Parameters
    ----------
    results : list[dict]
        List of result dicts.
    filepath : str
        Destination file path.
    """
    os.makedirs(os.path.dirname(filepath) or ".", exist_ok=True)

    wb = openpyxl.Workbook()
    ws = wb.active
    ws.title = "School Aid Status"

    # --- Header row ---
    ws.append(COLUMNS)
    for col_idx, _ in enumerate(COLUMNS, start=1):
        cell = ws.cell(row=1, column=col_idx)
        cell.font = _HEADER_FONT
        cell.fill = _HEADER_FILL
        cell.alignment = Alignment(horizontal="center", vertical="center")

    # --- Data rows ---
    rows_by_column: dict[int, list[str]] = {i: [] for i in range(1, len(COLUMNS) + 1)}

    for result in results:
        row_data = _format_row(result)
        ws.append(row_data)
        row_num = ws.max_row

        # Highlight row based on status
        status = row_data[2]
        fill = _YES_FILL if status == "Yes" else _NO_FILL
        for col_idx in range(1, len(COLUMNS) + 1):
            cell = ws.cell(row=row_num, column=col_idx)
            cell.fill = fill
            cell.alignment = Alignment(vertical="center")

        # Accumulate values for auto-fit
        for col_idx, value in enumerate(row_data, start=1):
            rows_by_column[col_idx].append(value)

    # --- Auto-fit column widths ---
    for col_idx, header in enumerate(COLUMNS, start=1):
        _auto_fit_column(ws, col_idx, header, rows_by_column[col_idx])

    # Freeze the header row
    ws.freeze_panes = "A2"

    wb.save(filepath)
    logger.info("Excel report written to: %s", filepath)


# ---------------------------------------------------------------------------
# Convenience: write both formats
# ---------------------------------------------------------------------------


def write_reports(results: list[dict], output_dir: str, stem: str = "results") -> None:
    """
    Write both CSV and Excel reports.

    Parameters
    ----------
    results : list[dict]
        List of result dicts.
    output_dir : str
        Directory to write output files into.
    stem : str
        Base filename (without extension). Defaults to "results".
    """
    csv_path = os.path.join(output_dir, f"{stem}.csv")
    xlsx_path = os.path.join(output_dir, f"{stem}.xlsx")
    write_csv(results, csv_path)
    write_excel(results, xlsx_path)
