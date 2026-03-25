# FAid-Tools — Financial Aid Tools

A collection of tools for managing and analyzing financial aid product usage across schools.

## Solutions

### [School-Scraping](./School-Scraping/README.md)

A Python web scraping application that checks whether schools are still using **Blackbaud's financial aid products** (Smart Aid, Blackbaud Tuition Management, TADS) or have switched to a competitor.

**Key features:**
- Crawls school websites up to 3 levels deep, focusing on financial aid, tuition, and admissions pages
- Detects Blackbaud products and 25+ competitor financial aid platforms
- Outputs results to **CSV** and **Excel (.xlsx)** with columns: `School Name`, `School Website`, `Status (Yes/No)`, `Lost To (Competitor)`
- Configurable crawl depth, rate limiting, and verbose logging

**Quick start:**
```bash
cd School-Scraping
pip install -r requirements.txt
python main.py
```

See [School-Scraping/README.md](./School-Scraping/README.md) for full documentation.
