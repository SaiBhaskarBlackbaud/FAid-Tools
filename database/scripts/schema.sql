-- FAid Database Schema
-- SQL Server

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'FAidDb')
BEGIN
    CREATE DATABASE FAidDb;
END
GO

USE FAidDb;
GO

-- Users table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Users' AND xtype='U')
CREATE TABLE Users (
    UserId          UNIQUEIDENTIFIER    PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    Username        NVARCHAR(100)       NOT NULL,
    EmailAddress    NVARCHAR(255)       NOT NULL,
    PasswordHash    NVARCHAR(500)       NOT NULL,
    FullName        NVARCHAR(200)       NOT NULL,
    Role            NVARCHAR(50)        NOT NULL,   -- 'Reviewer', 'Admin', 'Supervisor'
    IsActive        BIT                 NOT NULL DEFAULT 1,
    CreatedDate     DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    LastLoginDate   DATETIME2           NULL,
    CONSTRAINT UQ_Users_Username UNIQUE (Username)
);
GO

-- Applications table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Applications' AND xtype='U')
CREATE TABLE Applications (
    ApplicationId       UNIQUEIDENTIFIER    PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    FamilyName          NVARCHAR(200)       NOT NULL,
    Grade               INT                 NOT NULL,
    DependentCount      INT                 NOT NULL,
    SubmissionDate      DATETIME2           NOT NULL,
    ApplicationNumber   NVARCHAR(50)        NOT NULL,
    Status              NVARCHAR(50)        NOT NULL DEFAULT 'Pending',  -- Pending, Processing, Verified, DocsRequired, Completed
    CreatedDate         DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    LastModifiedDate    DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT UQ_Applications_Number UNIQUE (ApplicationNumber)
);
GO

-- Applicants table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Applicants' AND xtype='U')
CREATE TABLE Applicants (
    ApplicantId             UNIQUEIDENTIFIER    PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    ApplicationId           UNIQUEIDENTIFIER    NOT NULL REFERENCES Applications(ApplicationId),
    FirstName               NVARCHAR(100)       NOT NULL,
    LastName                NVARCHAR(100)       NOT NULL,
    EmailAddress            NVARCHAR(255)       NOT NULL,
    RelationshipToFamily    NVARCHAR(100)       NOT NULL,
    CreatedDate             DATETIME2           NOT NULL DEFAULT GETUTCDATE()
);
GO

-- Documents table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Documents' AND xtype='U')
CREATE TABLE Documents (
    DocumentId      UNIQUEIDENTIFIER    PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    ApplicationId   UNIQUEIDENTIFIER    NOT NULL REFERENCES Applications(ApplicationId),
    DocumentType    NVARCHAR(50)        NOT NULL,   -- 'ApplicationForm', 'Form1040', 'IRSTranscript'
    FileName        NVARCHAR(500)       NOT NULL,
    FileSize        BIGINT              NOT NULL,
    UploadedDate    DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    S3KeyOrPath     NVARCHAR(1000)      NULL,
    DocumentStatus  NVARCHAR(50)        NOT NULL DEFAULT 'Uploaded',  -- Uploaded, Processing, Processed, FailedProcessing
    CreatedDate     DATETIME2           NOT NULL DEFAULT GETUTCDATE()
);
GO

-- FinancialData table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='FinancialData' AND xtype='U')
CREATE TABLE FinancialData (
    FinancialDataId UNIQUEIDENTIFIER    PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    ApplicationId   UNIQUEIDENTIFIER    NOT NULL REFERENCES Applications(ApplicationId),
    DataSource      NVARCHAR(50)        NOT NULL,   -- 'ApplicationForm', 'Form1040', 'IRSTranscript'
    HouseholdIncome DECIMAL(18,2)       NULL,
    AGI             DECIMAL(18,2)       NULL,
    FilingStatus    NVARCHAR(50)        NULL,
    DependentCount  INT                 NULL,
    W2Employer1     NVARCHAR(200)       NULL,
    W2Employer2     NVARCHAR(200)       NULL,
    OtherIncome     DECIMAL(18,2)       NULL,
    CreatedDate     DATETIME2           NOT NULL DEFAULT GETUTCDATE()
);
GO

-- AIReviewResults table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AIReviewResults' AND xtype='U')
CREATE TABLE AIReviewResults (
    ReviewId            UNIQUEIDENTIFIER    PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    ApplicationId       UNIQUEIDENTIFIER    NOT NULL REFERENCES Applications(ApplicationId),
    ReviewStatus        NVARCHAR(50)        NOT NULL DEFAULT 'Pending',  -- Pending, InProgress, Completed, Failed
    ComparisonSummary   NVARCHAR(MAX)       NULL,   -- JSON
    IdentifiedFlags     NVARCHAR(MAX)       NULL,   -- JSON
    GeneratedEmailDraft NVARCHAR(MAX)       NULL,
    ReviewedDate        DATETIME2           NULL,
    ReviewedBy          NVARCHAR(200)       NULL,
    CreatedDate         DATETIME2           NOT NULL DEFAULT GETUTCDATE()
);
GO

-- Flags table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Flags' AND xtype='U')
CREATE TABLE Flags (
    FlagId              UNIQUEIDENTIFIER    PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    ReviewId            UNIQUEIDENTIFIER    NOT NULL REFERENCES AIReviewResults(ReviewId),
    FlagType            NVARCHAR(50)        NOT NULL,   -- 'IncomeDiscrepancy', 'MissingDocument', 'DataMismatch', 'Warning', 'Info'
    Title               NVARCHAR(200)       NOT NULL,
    Description         NVARCHAR(MAX)       NOT NULL,
    Severity            NVARCHAR(50)        NOT NULL,   -- 'Critical', 'Warning', 'Info'
    SourceField         NVARCHAR(200)       NULL,
    DiscrepancyAmount   DECIMAL(18,2)       NULL,
    CreatedDate         DATETIME2           NOT NULL DEFAULT GETUTCDATE()
);
GO

-- ReviewQueue table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ReviewQueue' AND xtype='U')
CREATE TABLE ReviewQueue (
    QueueId         UNIQUEIDENTIFIER    PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
    ApplicationId   UNIQUEIDENTIFIER    NOT NULL REFERENCES Applications(ApplicationId),
    QueueStatus     NVARCHAR(50)        NOT NULL DEFAULT 'Pending',  -- Pending, InProgress, Completed, OnHold
    AssignedTo      NVARCHAR(200)       NULL,
    DaysInQueue     INT                 NOT NULL DEFAULT 0,
    Priority        INT                 NOT NULL DEFAULT 0,
    CreatedDate     DATETIME2           NOT NULL DEFAULT GETUTCDATE(),
    CompletedDate   DATETIME2           NULL
);
GO

-- Indexes
CREATE INDEX IX_Applications_Status ON Applications(Status);
CREATE INDEX IX_Applications_SubmissionDate ON Applications(SubmissionDate);
CREATE INDEX IX_Applicants_ApplicationId ON Applicants(ApplicationId);
CREATE INDEX IX_Documents_ApplicationId ON Documents(ApplicationId);
CREATE INDEX IX_FinancialData_ApplicationId ON FinancialData(ApplicationId);
CREATE INDEX IX_AIReviewResults_ApplicationId ON AIReviewResults(ApplicationId);
CREATE INDEX IX_Flags_ReviewId ON Flags(ReviewId);
CREATE INDEX IX_ReviewQueue_ApplicationId ON ReviewQueue(ApplicationId);
CREATE INDEX IX_ReviewQueue_QueueStatus ON ReviewQueue(QueueStatus);
GO
