-- FAid Seed Data
USE FAidDb;
GO

-- Admin user (password: Admin@123)
-- BCrypt hash generated for 'Admin@123'
IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'admin')
BEGIN
    INSERT INTO Users (UserId, Username, EmailAddress, PasswordHash, FullName, Role, IsActive, CreatedDate)
    VALUES (
        '00000000-0000-0000-0000-000000000001',
        'admin',
        'admin@faid.example.com',
        '$2a$11$HJpSqJ0dR3Wm.pxdJjqJD.8KfM1MZ0r8W7r1Vq.vxLSqWaKj.g3i',
        'System Administrator',
        'Admin',
        1,
        '2024-01-01T00:00:00Z'
    );
END
GO

-- Reviewer user (password: Reviewer@123)
IF NOT EXISTS (SELECT 1 FROM Users WHERE Username = 'reviewer1')
BEGIN
    INSERT INTO Users (Username, EmailAddress, PasswordHash, FullName, Role, IsActive)
    VALUES (
        'reviewer1',
        'reviewer1@faid.example.com',
        '$2a$11$HJpSqJ0dR3Wm.pxdJjqJD.8KfM1MZ0r8W7r1Vq.vxLSqWaKj.g3i',
        'Jane Reviewer',
        'Reviewer',
        1
    );
END
GO

-- Sample Application 1
DECLARE @AppId1 UNIQUEIDENTIFIER = '10000000-0000-0000-0000-000000000001';

IF NOT EXISTS (SELECT 1 FROM Applications WHERE ApplicationNumber = 'FA-2024-001')
BEGIN
    INSERT INTO Applications (ApplicationId, FamilyName, Grade, DependentCount, SubmissionDate, ApplicationNumber, Status)
    VALUES (@AppId1, 'Smith', 5, 3, '2024-03-01T10:00:00Z', 'FA-2024-001', 'Pending');

    INSERT INTO Applicants (ApplicationId, FirstName, LastName, EmailAddress, RelationshipToFamily)
    VALUES (@AppId1, 'John', 'Smith', 'john.smith@example.com', 'Parent');

    INSERT INTO FinancialData (ApplicationId, DataSource, HouseholdIncome, AGI, FilingStatus, DependentCount)
    VALUES (@AppId1, 'ApplicationForm', 75000.00, 70000.00, 'MarriedFilingJointly', 3);

    INSERT INTO FinancialData (ApplicationId, DataSource, HouseholdIncome, AGI, FilingStatus, DependentCount)
    VALUES (@AppId1, 'Form1040', 78500.00, 72000.00, 'MarriedFilingJointly', 3);

    INSERT INTO ReviewQueue (ApplicationId, QueueStatus, Priority, DaysInQueue)
    VALUES (@AppId1, 'Pending', 0, 5);
END
GO

-- Sample Application 2
DECLARE @AppId2 UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000002';

IF NOT EXISTS (SELECT 1 FROM Applications WHERE ApplicationNumber = 'FA-2024-002')
BEGIN
    INSERT INTO Applications (ApplicationId, FamilyName, Grade, DependentCount, SubmissionDate, ApplicationNumber, Status)
    VALUES (@AppId2, 'Johnson', 3, 2, '2024-03-05T14:30:00Z', 'FA-2024-002', 'Processing');

    INSERT INTO Applicants (ApplicationId, FirstName, LastName, EmailAddress, RelationshipToFamily)
    VALUES (@AppId2, 'Mary', 'Johnson', 'mary.johnson@example.com', 'Parent');

    INSERT INTO FinancialData (ApplicationId, DataSource, HouseholdIncome, AGI, FilingStatus, DependentCount)
    VALUES (@AppId2, 'ApplicationForm', 55000.00, 52000.00, 'Single', 2);

    INSERT INTO FinancialData (ApplicationId, DataSource, HouseholdIncome, AGI, FilingStatus, DependentCount)
    VALUES (@AppId2, 'Form1040', 55000.00, 52000.00, 'Single', 2);

    INSERT INTO FinancialData (ApplicationId, DataSource, HouseholdIncome, AGI, FilingStatus, DependentCount)
    VALUES (@AppId2, 'IRSTranscript', 55200.00, 52100.00, 'Single', 2);

    INSERT INTO ReviewQueue (ApplicationId, QueueStatus, Priority, DaysInQueue, AssignedTo)
    VALUES (@AppId2, 'InProgress', 1, 2, 'reviewer1');
END
GO

-- Sample Application 3 (with discrepancy - will trigger flags)
DECLARE @AppId3 UNIQUEIDENTIFIER = '30000000-0000-0000-0000-000000000003';

IF NOT EXISTS (SELECT 1 FROM Applications WHERE ApplicationNumber = 'FA-2024-003')
BEGIN
    INSERT INTO Applications (ApplicationId, FamilyName, Grade, DependentCount, SubmissionDate, ApplicationNumber, Status)
    VALUES (@AppId3, 'Williams', 8, 4, '2024-03-10T09:00:00Z', 'FA-2024-003', 'DocsRequired');

    INSERT INTO Applicants (ApplicationId, FirstName, LastName, EmailAddress, RelationshipToFamily)
    VALUES (@AppId3, 'Robert', 'Williams', 'robert.williams@example.com', 'Parent');

    INSERT INTO FinancialData (ApplicationId, DataSource, HouseholdIncome, AGI, FilingStatus, DependentCount)
    VALUES (@AppId3, 'ApplicationForm', 45000.00, 42000.00, 'MarriedFilingJointly', 4);

    INSERT INTO FinancialData (ApplicationId, DataSource, HouseholdIncome, AGI, FilingStatus, DependentCount)
    VALUES (@AppId3, 'Form1040', 62000.00, 58000.00, 'MarriedFilingJointly', 2);

    DECLARE @ReviewId3 UNIQUEIDENTIFIER = NEWID();
    INSERT INTO AIReviewResults (ReviewId, ApplicationId, ReviewStatus, ComparisonSummary, ReviewedDate)
    VALUES (@ReviewId3, @AppId3, 'Completed', '{"IncomeMatch_AppVs1040":false}', '2024-03-11T10:00:00Z');

    INSERT INTO Flags (ReviewId, FlagType, Title, Description, Severity, SourceField, DiscrepancyAmount)
    VALUES (
        @ReviewId3,
        'IncomeDiscrepancy',
        'Income Discrepancy: Application vs Form 1040',
        'Reported household income ($45,000) differs from Form 1040 ($62,000) by 27.4%.',
        'Critical',
        'HouseholdIncome',
        17000.00
    );

    INSERT INTO Flags (ReviewId, FlagType, Title, Description, Severity, SourceField, DiscrepancyAmount)
    VALUES (
        @ReviewId3,
        'DataMismatch',
        'Dependent Count Mismatch',
        'Application reports 4 dependents, Form 1040 shows 2.',
        'Warning',
        'DependentCount',
        NULL
    );

    INSERT INTO ReviewQueue (ApplicationId, QueueStatus, Priority, DaysInQueue)
    VALUES (@AppId3, 'OnHold', 2, 8);
END
GO
