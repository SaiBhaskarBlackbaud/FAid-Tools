"""
config.py — Central configuration for the School-Scraping application.

Defines:
- Keywords and domains that indicate Blackbaud product usage
- Competitor product keywords and domains
- Crawl and request settings
"""

# ---------------------------------------------------------------------------
# Blackbaud / Our Product Detection
# ---------------------------------------------------------------------------

OUR_PRODUCT_KEYWORDS = [
    "Blackbaud Tuition Management",
    "Smart Aid",
    "SmartAid",
    "Blackbaud Financial Aid",
    "Blackbaud",          # broad match — only counted when on a financial-aid page
    "TADS",
    "TADS Financial Aid",
    "TADS Tuition Management",
    "Tuition Aid Data Services",
]

OUR_PRODUCT_DOMAINS = [
    "smartaidforstudents.com",
    "tads.com",
]

# ---------------------------------------------------------------------------
# Competitor Detection
# ---------------------------------------------------------------------------

# Each competitor entry is a dict with:
#   "name"     — human-readable product name used in the report
#   "keywords" — list of text strings to match (case-insensitive)
#   "domains"  — list of URL domains to look for in <a href="..."> attributes

COMPETITORS = [
    {
        "name": "FACTS Management",
        "keywords": [
            "FACTS Management",
            "FACTS Aid",
            "FACTS Grant & Aid",
            "FACTS Grant and Aid",
            "FACTS Grant &amp; Aid Assessment",
            "FACTS Tuition Management",
            "FACTS Financial Aid",
            "RenWeb/FACTS",
        ],
        "domains": [
            "factsmgt.com",
            "factstuitionaid.com",
            "online.factsmgt.com",
        ],
    },
    {
        "name": "Nelnet",
        "keywords": [
            "Nelnet",
        ],
        "domains": [
            "nelnet.com",
            "myschoolaccount.com",
        ],
    },
    {
        "name": "SSS by NAIS",
        "keywords": [
            "SSS by NAIS",
            "SSS Financial Aid",
            "School and Student Services",
            "School & Student Services",
        ],
        "domains": [
            "sss.nais.org",
        ],
    },
    {
        "name": "Clarity Financial Aid",
        "keywords": [
            "Clarity by Embark",
            "Clarity Financial Aid",
            "Clarity Aid",
        ],
        "domains": [
            "clarityapp.com",
        ],
    },
    {
        "name": "FAST Financial Aid",
        "keywords": [
            "FAST Financial Aid",
            "Financial Aid for School Tuition",
            "ISM FAST",
            "FAST by ISM",
        ],
        "domains": [],
    },
    {
        "name": "Tuition Exchange",
        "keywords": [
            "Tuition Exchange",
        ],
        "domains": [],
    },
    {
        "name": "MySchoolBucks",
        "keywords": [
            "MySchoolBucks",
        ],
        "domains": [
            "myschoolbucks.com",
        ],
    },
    {
        "name": "PaySchools",
        "keywords": [
            "PaySchools",
            "PSAS",
        ],
        "domains": [
            "payschools.com",
        ],
    },
    {
        "name": "RenWeb",
        "keywords": [
            "RenWeb",
        ],
        "domains": [
            "renweb.com",
        ],
    },
    # {
    #     "name": "Finalsite",
    #     "keywords": [
    #         "Finalsite",
    #     ],
    #     "domains": [
    #         "finalsite.com",
    #     ],
    # },
    {
        "name": "SchoolAdmin",
        "keywords": [
            "SchoolAdmin",
        ],
        "domains": [
            "schooladmin.com",
        ],
    },
    {
        "name": "Ravenna",
        "keywords": [
            "Ravenna",
            "Ravenna Solutions",
        ],
        "domains": [
            "ravenna-hub.com",
        ],
    },
    {
        "name": "Frontier Aid",
        "keywords": [
            "Frontier Aid",
        ],
        "domains": [],
    },
    {
        "name": "NetPrice",
        "keywords": [
            "NetPrice",
            "Net Price",
        ],
        "domains": [],
    },
    {
        "name": "Tuition Aid",
        "keywords": [
            "Tuition Aid Platform",
            "Tuition Aid Solution",
            "Tuition Aid System",
        ],
        "domains": [],
    },
    {
        "name": "CommunityBrands",
        "keywords": [
            "CommunityBrands",
            "Community Brands",
        ],
        "domains": [],
    },
    {
        "name": "Veracross",
        "keywords": [
            "Veracross",
        ],
        "domains": [
            "veracross.com",
        ],
    },
    {
        "name": "Magnus Health",
        "keywords": [
            "Magnus Health",
        ],
        "domains": [],
    },
    {
        "name": "Diamond Mind",
        "keywords": [
            "Diamond Mind",
        ],
        "domains": [],
    },
    {
        "name": "SchoolCues",
        "keywords": [
            "SchoolCues",
        ],
        "domains": [],
    },
    {
        "name": "Gradelink",
        "keywords": [
            "Gradelink",
        ],
        "domains": [],
    },
]

# ---------------------------------------------------------------------------
# Page-relevance keywords
# Used to decide whether a page is worth scanning deeply.
# ---------------------------------------------------------------------------

RELEVANT_PAGE_KEYWORDS = [
    "financial aid",
    "financialaid",
    "tuition",
    "enrollment",
    "admissions",
    "affordability",
    "apply",
    "application",
    "paying for school",
    "tuition assistance",
    "aid",
    "scholarship",
    "grant",
]

# ---------------------------------------------------------------------------
# Scraping settings
# ---------------------------------------------------------------------------

# Maximum number of levels to crawl below the homepage (0 = homepage only)
DEFAULT_CRAWL_DEPTH = 3

# Seconds to wait between HTTP requests (be respectful to school servers)
REQUEST_DELAY_SECONDS = 1

# HTTP request timeout in seconds
REQUEST_TIMEOUT_SECONDS = 15

# User-Agent header — identifies the scraper politely
USER_AGENT = (
    "Mozilla/5.0 (compatible; FAid-Tools-Scraper/1.0; "
    "+https://github.com/SaiBhaskarBlackbaud/FAid-Tools)"
)

# Maximum number of pages to visit per school (safety cap)
MAX_PAGES_PER_SCHOOL = 50

# When True, SSL certificate errors are ignored (allows scraping sites with
# self-signed or expired certs).  Set to False in environments that require
# strict certificate validation.
VERIFY_SSL = False

# ---------------------------------------------------------------------------
# File paths
# ---------------------------------------------------------------------------

DEFAULT_INPUT_FILE = "input/schools.csv"
DEFAULT_OUTPUT_DIR = "output"
DEFAULT_OUTPUT_STEM = "results"
