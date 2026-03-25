# School-Scraping

A Python web scraping application that checks whether schools are still using **Blackbaud's financial aid products** or have switched to a competitor.

---

## Contents

- [Description](#description)
- [Project Structure](#project-structure)
- [Installation](#installation)
- [Usage](#usage)
- [Configuration](#configuration)
- [Sample Output](#sample-output)
- [Notes on Rate Limiting](#notes-on-rate-limiting)

---

## Description

This tool reads a list of school websites from a CSV file, crawls each site up to a configurable depth (default: 2 levels), and searches for mentions of Blackbaud financial aid products or known competitor products.

**Our products detected:**
- Blackbaud Tuition Management
- Smart Aid / SmartAid
- Blackbaud Financial Aid
- TADS / TADS Financial Aid / TADS Tuition Management
- Domains: `smartaidforstudents.com`, `tads.com`

**Competitors detected (25+):**
FACTS Management, Nelnet, SSS by NAIS, Clarity Financial Aid, FAST Financial Aid, Tuition Exchange, MySchoolBucks, PaySchools, RenWeb, Finalsite, SchoolAdmin, Ravenna, Veracross, and more.

---

## Project Structure

```
School-Scraping/
├── main.py             # Entry point / CLI
├── config.py           # All keywords, competitor lists, and crawl settings
├── scraper.py          # Web crawling engine (BFS, 2-3 levels deep)
├── detector.py         # Product detection logic (our product + competitors)
├── reporter.py         # CSV and Excel report generation
├── requirements.txt    # Python dependencies
├── input/
│   └── schools.csv     # Sample input file
└── output/
    └── .gitkeep        # Output directory placeholder
```

---

## Installation

**Requirements:** Python 3.10 or higher

```bash
cd School-Scraping

# (Optional) Create a virtual environment
python -m venv venv
source venv/bin/activate   # On Windows: venv\Scripts\activate

# Install dependencies
pip install -r requirements.txt
```

---

## Usage

Run the scraper from the `School-Scraping/` directory:

```bash
# Basic usage — reads from input/schools.csv, writes to output/
python main.py

# Custom input file and output directory
python main.py --input path/to/schools.csv --output path/to/output_dir

# Adjust crawl depth (default: 2; higher = more pages, slower)
python main.py --depth 3

# Enable verbose / debug logging
python main.py --verbose

# Combine options
python main.py --input input/schools.csv --output output --depth 2 --verbose
```

### Input CSV format

The input file must be a CSV with the following columns:

| School Name | School Website |
|---|---|
| Mount Saint Mary Academy | https://www.mountsaintmary.org/ |
| The Dalton School | https://www.dalton.org/ |

### Output files

Two files are written to the output directory:

| File | Description |
|---|---|
| `output/results.csv` | Plain CSV with all results |
| `output/results.xlsx` | Formatted Excel workbook (color-coded rows) |

---

## Configuration

All detection keywords, competitor names, and crawl settings live in `config.py`.

| Setting | Default | Description |
|---|---|---|
| `DEFAULT_CRAWL_DEPTH` | `2` | How many link-hops to follow from the homepage |
| `REQUEST_DELAY_SECONDS` | `2` | Seconds to wait between requests |
| `REQUEST_TIMEOUT_SECONDS` | `15` | HTTP timeout per request |
| `MAX_PAGES_PER_SCHOOL` | `50` | Maximum pages visited per school |
| `OUR_PRODUCT_KEYWORDS` | (list) | Keywords indicating Blackbaud products |
| `OUR_PRODUCT_DOMAINS` | (list) | Domains belonging to our products |
| `COMPETITORS` | (list of dicts) | Competitor names, keywords, and domains |

To add a new competitor, append an entry to `COMPETITORS` in `config.py`:

```python
{
    "name": "My Competitor",
    "keywords": ["My Competitor", "MC Platform"],
    "domains": ["mycompetitor.com"],
},
```

---

## Sample Output

**Console:**
```
[1/6] Mount Saint Mary Academy: Status=YES (still ours)
[2/6] Saint Ann's School: Status=NO, Lost To=FACTS Management
[3/6] The Chapin School: Status=NO, Lost To=Unknown
```

**results.csv:**

| School Name | School Website | Status (Yes/No) | Lost To (Competitor) |
|---|---|---|---|
| Mount Saint Mary Academy | https://www.mountsaintmary.org/ | Yes | |
| Saint Ann's School | https://www.saintannsny.org/ | No | FACTS Management |
| The Chapin School | https://www.chapin.edu/ | No | Unknown |

**results.xlsx:** Same data with color-coded rows — green for Yes, red for No.

---

## Notes on Rate Limiting

The scraper is configured to wait **2 seconds between requests** (`REQUEST_DELAY_SECONDS` in `config.py`) to avoid overloading school servers.  Please be respectful and do not set this value to 0.

The default crawl depth is **2 levels**, which balances coverage with request volume.  Increasing `--depth` will visit more pages and produce more accurate results, but will also make more HTTP requests per school.

SSL certificate errors are handled gracefully — the scraper will still fetch the page and log a warning.
