SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.Campaigns', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Campaigns
    (
        CampaignId int IDENTITY(1, 1) NOT NULL CONSTRAINT PK_Campaigns PRIMARY KEY,
        Slug nvarchar(100) NOT NULL,
        Title nvarchar(200) NOT NULL,
        Organization nvarchar(200) NOT NULL,
        Category nvarchar(100) NOT NULL,
        FocusKey nvarchar(50) NOT NULL,
        Thumbnail nvarchar(300) NULL,
        ThumbnailAlt nvarchar(300) NULL,
        ThumbnailLabel nvarchar(100) NULL,
        HomeThumbnail nvarchar(300) NULL,
        HomeThumbnailAlt nvarchar(300) NULL,
        DetailsImage nvarchar(300) NULL,
        DetailsImageAlt nvarchar(300) NULL,
        Summary nvarchar(max) NULL,
        HomeSummary nvarchar(max) NULL,
        LedgerNote nvarchar(max) NULL,
        Goal decimal(18, 2) NOT NULL,
        StartedOn date NOT NULL,
        Deadline date NOT NULL,
        Status nvarchar(20) NOT NULL,
        CreatedByUserId nvarchar(450) NOT NULL,
        UpdatedByUserId nvarchar(450) NULL,
        CreatedAtUtc datetime2(7) NOT NULL,
        UpdatedAtUtc datetime2(7) NULL,
        ClosedAtUtc datetime2(7) NULL,
        FinalSummary nvarchar(max) NULL,
        RowVersion rowversion NOT NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.Contributions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Contributions
    (
        ContributionId bigint IDENTITY(1, 1) NOT NULL CONSTRAINT PK_Contributions PRIMARY KEY,
        CampaignId int NOT NULL,
        ContributorUserId nvarchar(450) NULL,
        FullName nvarchar(100) NOT NULL,
        Amount decimal(18, 2) NOT NULL,
        TransactionRef nvarchar(100) NOT NULL,
        ExternalPaymentMethod nvarchar(50) NOT NULL,
        DateSent date NULL,
        Notes nvarchar(500) NULL,
        Status nvarchar(20) NOT NULL,
        SubmittedAtUtc datetime2(7) NOT NULL,
        VerifiedAtUtc datetime2(7) NULL,
        VerifiedByUserId nvarchar(450) NULL,
        VerificationNote nvarchar(500) NULL,
        RowVersion rowversion NOT NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.ContributionVerifications', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ContributionVerifications
    (
        ContributionVerificationId bigint IDENTITY(1, 1) NOT NULL CONSTRAINT PK_ContributionVerifications PRIMARY KEY,
        ContributionId bigint NOT NULL,
        Decision nvarchar(20) NOT NULL,
        ActorUserId nvarchar(450) NOT NULL,
        Note nvarchar(500) NULL,
        DecisionAtUtc datetime2(7) NOT NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.Expenses', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Expenses
    (
        ExpenseId bigint IDENTITY(1, 1) NOT NULL CONSTRAINT PK_Expenses PRIMARY KEY,
        CampaignId int NOT NULL,
        SubmittedByUserId nvarchar(450) NOT NULL,
        Reference nvarchar(50) NOT NULL,
        PayeeName nvarchar(150) NOT NULL,
        Amount decimal(18, 2) NOT NULL,
        Description nvarchar(500) NOT NULL,
        Status nvarchar(20) NOT NULL,
        SubmittedAtUtc datetime2(7) NOT NULL,
        DecisionAtUtc datetime2(7) NULL,
        DecisionByUserId nvarchar(450) NULL,
        DecisionNote nvarchar(500) NULL,
        RowVersion rowversion NOT NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.ExpenseApprovals', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ExpenseApprovals
    (
        ExpenseApprovalId bigint IDENTITY(1, 1) NOT NULL CONSTRAINT PK_ExpenseApprovals PRIMARY KEY,
        ExpenseId bigint NOT NULL,
        Decision nvarchar(20) NOT NULL,
        ActorUserId nvarchar(450) NOT NULL,
        Note nvarchar(500) NULL,
        DecisionAtUtc datetime2(7) NOT NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.ReceiptMetadata', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReceiptMetadata
    (
        ReceiptId bigint IDENTITY(1, 1) NOT NULL CONSTRAINT PK_ReceiptMetadata PRIMARY KEY,
        ExpenseId bigint NULL,
        ContributionId bigint NULL,
        ReceiptNumber nvarchar(100) NULL,
        Vendor nvarchar(150) NULL,
        FileName nvarchar(300) NOT NULL,
        OriginalFileName nvarchar(300) NOT NULL,
        ContentType nvarchar(100) NULL,
        StoragePath nvarchar(500) NULL,
        Amount decimal(18, 2) NULL,
        IssuedOn date NULL,
        IsVerified bit NOT NULL,
        UploadedByUserId nvarchar(450) NOT NULL,
        CreatedAtUtc datetime2(7) NOT NULL,
        CONSTRAINT CK_ReceiptMetadata_HasOwner CHECK (ExpenseId IS NOT NULL OR ContributionId IS NOT NULL)
    );
END;
GO

IF OBJECT_ID(N'dbo.CampaignUpdates', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CampaignUpdates
    (
        CampaignUpdateId bigint IDENTITY(1, 1) NOT NULL CONSTRAINT PK_CampaignUpdates PRIMARY KEY,
        CampaignId int NOT NULL,
        AuthorUserId nvarchar(450) NOT NULL,
        Title nvarchar(200) NOT NULL,
        Body nvarchar(max) NOT NULL,
        IsPublic bit NOT NULL,
        CreatedAtUtc datetime2(7) NOT NULL,
        PublishedAtUtc datetime2(7) NULL,
        UpdatedAtUtc datetime2(7) NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.AuditRecords', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditRecords
    (
        AuditRecordId bigint IDENTITY(1, 1) NOT NULL CONSTRAINT PK_AuditRecords PRIMARY KEY,
        EntityType nvarchar(50) NOT NULL,
        EntityId nvarchar(50) NOT NULL,
        Action nvarchar(50) NOT NULL,
        ActorUserId nvarchar(450) NULL,
        Details nvarchar(max) NULL,
        CorrelationId uniqueidentifier NOT NULL,
        OccurredAtUtc datetime2(7) NOT NULL
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Campaigns_Slug' AND object_id = OBJECT_ID(N'dbo.Campaigns'))
    CREATE UNIQUE INDEX UX_Campaigns_Slug ON dbo.Campaigns(Slug);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Contributions_Campaign_Status' AND object_id = OBJECT_ID(N'dbo.Contributions'))
    CREATE INDEX IX_Contributions_Campaign_Status ON dbo.Contributions(CampaignId, Status);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Contributions_Campaign_TransactionRef' AND object_id = OBJECT_ID(N'dbo.Contributions'))
    CREATE UNIQUE INDEX UX_Contributions_Campaign_TransactionRef ON dbo.Contributions(CampaignId, TransactionRef);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Contributions_Contributor' AND object_id = OBJECT_ID(N'dbo.Contributions'))
    CREATE INDEX IX_Contributions_Contributor ON dbo.Contributions(ContributorUserId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Contributions_Status' AND object_id = OBJECT_ID(N'dbo.Contributions'))
    CREATE INDEX IX_Contributions_Status ON dbo.Contributions(Status);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Expenses_Campaign_Status' AND object_id = OBJECT_ID(N'dbo.Expenses'))
    CREATE INDEX IX_Expenses_Campaign_Status ON dbo.Expenses(CampaignId, Status);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Expenses_Status' AND object_id = OBJECT_ID(N'dbo.Expenses'))
    CREATE INDEX IX_Expenses_Status ON dbo.Expenses(Status);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ReceiptMetadata_Expense' AND object_id = OBJECT_ID(N'dbo.ReceiptMetadata'))
    CREATE INDEX IX_ReceiptMetadata_Expense ON dbo.ReceiptMetadata(ExpenseId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ReceiptMetadata_Contribution' AND object_id = OBJECT_ID(N'dbo.ReceiptMetadata'))
    CREATE INDEX IX_ReceiptMetadata_Contribution ON dbo.ReceiptMetadata(ContributionId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CampaignUpdates_Campaign' AND object_id = OBJECT_ID(N'dbo.CampaignUpdates'))
    CREATE INDEX IX_CampaignUpdates_Campaign ON dbo.CampaignUpdates(CampaignId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AuditRecords_Entity' AND object_id = OBJECT_ID(N'dbo.AuditRecords'))
    CREATE INDEX IX_AuditRecords_Entity ON dbo.AuditRecords(EntityType, EntityId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = N'DF_Campaigns_Status' AND parent_object_id = OBJECT_ID(N'dbo.Campaigns'))
    ALTER TABLE dbo.Campaigns ADD CONSTRAINT DF_Campaigns_Status DEFAULT (N'Draft') FOR Status;
GO
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = N'DF_Campaigns_CreatedAtUtc' AND parent_object_id = OBJECT_ID(N'dbo.Campaigns'))
    ALTER TABLE dbo.Campaigns ADD CONSTRAINT DF_Campaigns_CreatedAtUtc DEFAULT (SYSUTCDATETIME()) FOR CreatedAtUtc;
GO
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = N'DF_Contributions_ExternalPaymentMethod' AND parent_object_id = OBJECT_ID(N'dbo.Contributions'))
    ALTER TABLE dbo.Contributions ADD CONSTRAINT DF_Contributions_ExternalPaymentMethod DEFAULT (N'External') FOR ExternalPaymentMethod;
GO
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = N'DF_Contributions_Status' AND parent_object_id = OBJECT_ID(N'dbo.Contributions'))
    ALTER TABLE dbo.Contributions ADD CONSTRAINT DF_Contributions_Status DEFAULT (N'Pending') FOR Status;
GO
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = N'DF_Contributions_SubmittedAtUtc' AND parent_object_id = OBJECT_ID(N'dbo.Contributions'))
    ALTER TABLE dbo.Contributions ADD CONSTRAINT DF_Contributions_SubmittedAtUtc DEFAULT (SYSUTCDATETIME()) FOR SubmittedAtUtc;
GO
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = N'DF_ContributionVerifications_DecisionAtUtc' AND parent_object_id = OBJECT_ID(N'dbo.ContributionVerifications'))
    ALTER TABLE dbo.ContributionVerifications ADD CONSTRAINT DF_ContributionVerifications_DecisionAtUtc DEFAULT (SYSUTCDATETIME()) FOR DecisionAtUtc;
GO
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = N'DF_Expenses_Status' AND parent_object_id = OBJECT_ID(N'dbo.Expenses'))
    ALTER TABLE dbo.Expenses ADD CONSTRAINT DF_Expenses_Status DEFAULT (N'Pending') FOR Status;
GO
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = N'DF_Expenses_SubmittedAtUtc' AND parent_object_id = OBJECT_ID(N'dbo.Expenses'))
    ALTER TABLE dbo.Expenses ADD CONSTRAINT DF_Expenses_SubmittedAtUtc DEFAULT (SYSUTCDATETIME()) FOR SubmittedAtUtc;
GO
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = N'DF_ExpenseApprovals_DecisionAtUtc' AND parent_object_id = OBJECT_ID(N'dbo.ExpenseApprovals'))
    ALTER TABLE dbo.ExpenseApprovals ADD CONSTRAINT DF_ExpenseApprovals_DecisionAtUtc DEFAULT (SYSUTCDATETIME()) FOR DecisionAtUtc;
GO
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = N'DF_ReceiptMetadata_IsVerified' AND parent_object_id = OBJECT_ID(N'dbo.ReceiptMetadata'))
    ALTER TABLE dbo.ReceiptMetadata ADD CONSTRAINT DF_ReceiptMetadata_IsVerified DEFAULT ((0)) FOR IsVerified;
GO
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = N'DF_ReceiptMetadata_CreatedAtUtc' AND parent_object_id = OBJECT_ID(N'dbo.ReceiptMetadata'))
    ALTER TABLE dbo.ReceiptMetadata ADD CONSTRAINT DF_ReceiptMetadata_CreatedAtUtc DEFAULT (SYSUTCDATETIME()) FOR CreatedAtUtc;
GO
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = N'DF_CampaignUpdates_IsPublic' AND parent_object_id = OBJECT_ID(N'dbo.CampaignUpdates'))
    ALTER TABLE dbo.CampaignUpdates ADD CONSTRAINT DF_CampaignUpdates_IsPublic DEFAULT ((1)) FOR IsPublic;
GO
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = N'DF_CampaignUpdates_CreatedAtUtc' AND parent_object_id = OBJECT_ID(N'dbo.CampaignUpdates'))
    ALTER TABLE dbo.CampaignUpdates ADD CONSTRAINT DF_CampaignUpdates_CreatedAtUtc DEFAULT (SYSUTCDATETIME()) FOR CreatedAtUtc;
GO
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = N'DF_AuditRecords_CorrelationId' AND parent_object_id = OBJECT_ID(N'dbo.AuditRecords'))
    ALTER TABLE dbo.AuditRecords ADD CONSTRAINT DF_AuditRecords_CorrelationId DEFAULT (NEWID()) FOR CorrelationId;
GO
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = N'DF_AuditRecords_OccurredAtUtc' AND parent_object_id = OBJECT_ID(N'dbo.AuditRecords'))
    ALTER TABLE dbo.AuditRecords ADD CONSTRAINT DF_AuditRecords_OccurredAtUtc DEFAULT (SYSUTCDATETIME()) FOR OccurredAtUtc;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Campaigns_GoalPositive' AND parent_object_id = OBJECT_ID(N'dbo.Campaigns'))
    ALTER TABLE dbo.Campaigns WITH CHECK ADD CONSTRAINT CK_Campaigns_GoalPositive CHECK (Goal > 0);
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Campaigns_Status' AND parent_object_id = OBJECT_ID(N'dbo.Campaigns'))
    ALTER TABLE dbo.Campaigns WITH CHECK ADD CONSTRAINT CK_Campaigns_Status CHECK (Status IN (N'Draft', N'Active', N'Closed', N'Archived'));
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Campaigns_Deadline' AND parent_object_id = OBJECT_ID(N'dbo.Campaigns'))
    ALTER TABLE dbo.Campaigns WITH CHECK ADD CONSTRAINT CK_Campaigns_Deadline CHECK (Deadline >= StartedOn);
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Contributions_AmountPositive' AND parent_object_id = OBJECT_ID(N'dbo.Contributions'))
    ALTER TABLE dbo.Contributions WITH CHECK ADD CONSTRAINT CK_Contributions_AmountPositive CHECK (Amount > 0);
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Contributions_Status' AND parent_object_id = OBJECT_ID(N'dbo.Contributions'))
    ALTER TABLE dbo.Contributions WITH CHECK ADD CONSTRAINT CK_Contributions_Status CHECK (Status IN (N'Pending', N'Verified', N'Rejected'));
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_ContributionVerifications_Decision' AND parent_object_id = OBJECT_ID(N'dbo.ContributionVerifications'))
    ALTER TABLE dbo.ContributionVerifications WITH CHECK ADD CONSTRAINT CK_ContributionVerifications_Decision CHECK (Decision IN (N'Verified', N'Rejected'));
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Expenses_AmountPositive' AND parent_object_id = OBJECT_ID(N'dbo.Expenses'))
    ALTER TABLE dbo.Expenses WITH CHECK ADD CONSTRAINT CK_Expenses_AmountPositive CHECK (Amount > 0);
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Expenses_Status' AND parent_object_id = OBJECT_ID(N'dbo.Expenses'))
    ALTER TABLE dbo.Expenses WITH CHECK ADD CONSTRAINT CK_Expenses_Status CHECK (Status IN (N'Pending', N'Approved', N'Rejected'));
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_ExpenseApprovals_Decision' AND parent_object_id = OBJECT_ID(N'dbo.ExpenseApprovals'))
    ALTER TABLE dbo.ExpenseApprovals WITH CHECK ADD CONSTRAINT CK_ExpenseApprovals_Decision CHECK (Decision IN (N'Approved', N'Rejected'));
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_ReceiptMetadata_AmountNonNegative' AND parent_object_id = OBJECT_ID(N'dbo.ReceiptMetadata'))
    ALTER TABLE dbo.ReceiptMetadata WITH CHECK ADD CONSTRAINT CK_ReceiptMetadata_AmountNonNegative CHECK (Amount IS NULL OR Amount >= 0);
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Contributions_Campaigns' AND parent_object_id = OBJECT_ID(N'dbo.Contributions'))
    ALTER TABLE dbo.Contributions WITH CHECK ADD CONSTRAINT FK_Contributions_Campaigns FOREIGN KEY (CampaignId) REFERENCES dbo.Campaigns(CampaignId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ContributionVerifications_Contributions' AND parent_object_id = OBJECT_ID(N'dbo.ContributionVerifications'))
    ALTER TABLE dbo.ContributionVerifications WITH CHECK ADD CONSTRAINT FK_ContributionVerifications_Contributions FOREIGN KEY (ContributionId) REFERENCES dbo.Contributions(ContributionId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Expenses_Campaigns' AND parent_object_id = OBJECT_ID(N'dbo.Expenses'))
    ALTER TABLE dbo.Expenses WITH CHECK ADD CONSTRAINT FK_Expenses_Campaigns FOREIGN KEY (CampaignId) REFERENCES dbo.Campaigns(CampaignId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ExpenseApprovals_Expenses' AND parent_object_id = OBJECT_ID(N'dbo.ExpenseApprovals'))
    ALTER TABLE dbo.ExpenseApprovals WITH CHECK ADD CONSTRAINT FK_ExpenseApprovals_Expenses FOREIGN KEY (ExpenseId) REFERENCES dbo.Expenses(ExpenseId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReceiptMetadata_Expenses' AND parent_object_id = OBJECT_ID(N'dbo.ReceiptMetadata'))
    ALTER TABLE dbo.ReceiptMetadata WITH CHECK ADD CONSTRAINT FK_ReceiptMetadata_Expenses FOREIGN KEY (ExpenseId) REFERENCES dbo.Expenses(ExpenseId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ReceiptMetadata_Contributions' AND parent_object_id = OBJECT_ID(N'dbo.ReceiptMetadata'))
    ALTER TABLE dbo.ReceiptMetadata WITH CHECK ADD CONSTRAINT FK_ReceiptMetadata_Contributions FOREIGN KEY (ContributionId) REFERENCES dbo.Contributions(ContributionId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_CampaignUpdates_Campaigns' AND parent_object_id = OBJECT_ID(N'dbo.CampaignUpdates'))
    ALTER TABLE dbo.CampaignUpdates WITH CHECK ADD CONSTRAINT FK_CampaignUpdates_Campaigns FOREIGN KEY (CampaignId) REFERENCES dbo.Campaigns(CampaignId);
GO

IF OBJECT_ID(N'dbo.vw_CampaignFinancialSummary', N'V') IS NOT NULL
    DROP VIEW dbo.vw_CampaignFinancialSummary;
GO
CREATE VIEW dbo.vw_CampaignFinancialSummary AS
SELECT
    c.CampaignId,
    c.Slug,
    c.Title,
    c.Status,
    c.Goal,
    CAST(ISNULL((SELECT SUM(x.Amount) FROM dbo.Contributions x WHERE x.CampaignId = c.CampaignId AND x.Status = N'Verified'), CAST(0 AS decimal(18, 2))) AS decimal(18, 2)) AS VerifiedContributions,
    CAST(ISNULL((SELECT SUM(x.Amount) FROM dbo.Expenses x WHERE x.CampaignId = c.CampaignId AND x.Status = N'Approved'), CAST(0 AS decimal(18, 2))) AS decimal(18, 2)) AS ApprovedExpenses,
    CAST(ISNULL((SELECT SUM(x.Amount) FROM dbo.Contributions x WHERE x.CampaignId = c.CampaignId AND x.Status = N'Verified'), CAST(0 AS decimal(18, 2)))
       - ISNULL((SELECT SUM(x.Amount) FROM dbo.Expenses x WHERE x.CampaignId = c.CampaignId AND x.Status = N'Approved'), CAST(0 AS decimal(18, 2))) AS decimal(18, 2)) AS Balance,
    (SELECT COUNT(*) FROM dbo.Contributions x WHERE x.CampaignId = c.CampaignId AND x.Status = N'Verified') AS VerifiedContributionCount,
    (SELECT COUNT(*) FROM dbo.Expenses x WHERE x.CampaignId = c.CampaignId AND x.Status = N'Approved') AS ApprovedExpenseCount
FROM dbo.Campaigns c;
GO

IF OBJECT_ID(N'dbo.vw_PublicLedgerEntries', N'V') IS NOT NULL
    DROP VIEW dbo.vw_PublicLedgerEntries;
GO
CREATE VIEW dbo.vw_PublicLedgerEntries AS
SELECT
    CAST(N'C' + CONVERT(nvarchar(20), cn.ContributionId) AS nvarchar(30)) AS LedgerEntryId,
    CAST(N'Contribution' AS nvarchar(20)) AS EntryType,
    cn.CampaignId,
    c.Slug AS CampaignSlug,
    c.Title AS CampaignTitle,
    cn.ContributionId AS EntityId,
    cn.TransactionRef AS Reference,
    cn.FullName AS PartyName,
    CAST(ISNULL(cn.Notes, N'') AS nvarchar(500)) AS Description,
    cn.Amount,
    CAST(N'In' AS nvarchar(3)) AS Direction,
    cn.VerifiedAtUtc AS OccurredAtUtc,
    CAST(N'Verified' AS nvarchar(20)) AS Status
FROM dbo.Contributions cn
INNER JOIN dbo.Campaigns c ON c.CampaignId = cn.CampaignId
WHERE cn.Status = N'Verified'
UNION ALL
SELECT
    CAST(N'E' + CONVERT(nvarchar(20), e.ExpenseId) AS nvarchar(30)) AS LedgerEntryId,
    CAST(N'Expense' AS nvarchar(20)) AS EntryType,
    e.CampaignId,
    c.Slug AS CampaignSlug,
    c.Title AS CampaignTitle,
    e.ExpenseId AS EntityId,
    e.Reference,
    e.PayeeName AS PartyName,
    e.Description,
    e.Amount,
    CAST(N'Out' AS nvarchar(3)) AS Direction,
    e.DecisionAtUtc AS OccurredAtUtc,
    CAST(N'Approved' AS nvarchar(20)) AS Status
FROM dbo.Expenses e
INNER JOIN dbo.Campaigns c ON c.CampaignId = e.CampaignId
WHERE e.Status = N'Approved';
GO

IF OBJECT_ID(N'dbo.vw_PendingContributions', N'V') IS NOT NULL
    DROP VIEW dbo.vw_PendingContributions;
GO
CREATE VIEW dbo.vw_PendingContributions AS
SELECT
    cn.ContributionId,
    cn.CampaignId,
    c.Slug AS CampaignSlug,
    c.Title AS CampaignTitle,
    cn.FullName,
    cn.Amount,
    cn.TransactionRef,
    cn.DateSent,
    cn.Notes,
    cn.Status,
    cn.SubmittedAtUtc,
    cn.RowVersion
FROM dbo.Contributions cn
INNER JOIN dbo.Campaigns c ON c.CampaignId = cn.CampaignId
WHERE cn.Status = N'Pending';
GO

IF OBJECT_ID(N'dbo.vw_PendingApprovals', N'V') IS NOT NULL
    DROP VIEW dbo.vw_PendingApprovals;
GO
CREATE VIEW dbo.vw_PendingApprovals AS
SELECT
    e.ExpenseId,
    e.CampaignId,
    c.Slug AS CampaignSlug,
    c.Title AS CampaignTitle,
    e.Reference,
    e.PayeeName,
    e.Amount,
    e.Description,
    e.Status,
    e.SubmittedAtUtc,
    e.RowVersion
FROM dbo.Expenses e
INNER JOIN dbo.Campaigns c ON c.CampaignId = e.CampaignId
WHERE e.Status = N'Pending';
GO

IF OBJECT_ID(N'dbo.vw_FundSummary', N'V') IS NOT NULL
    DROP VIEW dbo.vw_FundSummary;
GO
CREATE VIEW dbo.vw_FundSummary AS
SELECT
    CAST(N'AllCampaigns' AS nvarchar(50)) AS FundKey,
    COUNT(*) AS CampaignCount,
    CAST(ISNULL(SUM(VerifiedContributions), CAST(0 AS decimal(18, 2))) AS decimal(18, 2)) AS VerifiedContributions,
    CAST(ISNULL(SUM(ApprovedExpenses), CAST(0 AS decimal(18, 2))) AS decimal(18, 2)) AS ApprovedExpenses,
    CAST(ISNULL(SUM(Balance), CAST(0 AS decimal(18, 2))) AS decimal(18, 2)) AS Balance,
    CAST(NULL AS datetime2(7)) AS UpdatedAtUtc
FROM dbo.vw_CampaignFinancialSummary
UNION ALL
SELECT
    CAST(N'ActiveCampaigns' AS nvarchar(50)) AS FundKey,
    COUNT(*) AS CampaignCount,
    CAST(ISNULL(SUM(VerifiedContributions), CAST(0 AS decimal(18, 2))) AS decimal(18, 2)) AS VerifiedContributions,
    CAST(ISNULL(SUM(ApprovedExpenses), CAST(0 AS decimal(18, 2))) AS decimal(18, 2)) AS ApprovedExpenses,
    CAST(ISNULL(SUM(Balance), CAST(0 AS decimal(18, 2))) AS decimal(18, 2)) AS Balance,
    CAST(NULL AS datetime2(7)) AS UpdatedAtUtc
FROM dbo.vw_CampaignFinancialSummary
WHERE Status = N'Active';
GO
