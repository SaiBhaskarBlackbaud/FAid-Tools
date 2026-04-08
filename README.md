# FAid-Tools — Financial Aid Tools

A collection of tools for managing and analyzing financial aid product usage across schools.

## Solutions

### [Financial Aid AI Review System](./backend/)

A full-stack application for processing, validating, and AI-reviewing financial aid applications.

**Architecture:**
- **Frontend**: HTML/CSS/JavaScript single-page app (`frontend/`)
- **Backend**: C# .NET 8 REST API (`backend/FAid.API/`)
- **Database**: SQL Server (scripts in `database/scripts/`)
- **AI Integration**: Claude API for document review and email drafting

**Key features:**
- Upload financial aid documents (Application Form, IRS Form 1040, IRS Transcript)
- AI-powered comparison of financial data across documents
- Automated flag generation for income discrepancies and missing documents
- AI-generated email drafts for requesting additional information
- Review queue management with dashboard metrics
- JWT authentication for reviewers and admins

**Quick start (backend):**
```bash
cd backend
dotnet restore
# Update connection string in FAid.API/appsettings.json
dotnet run --project FAid.API
# API available at http://localhost:5000, Swagger at http://localhost:5000/swagger
```

**Quick start (frontend):**
```bash
# Open frontend/index.html in a browser
# Or serve with any static file server, e.g.:
cd frontend
python -m http.server 8080
```

**Run tests:**
```bash
cd backend
dotnet test FAid.Tests
```

See `database/scripts/schema.sql` for the full database schema and `database/scripts/seed-data.sql` for sample data.

---

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
