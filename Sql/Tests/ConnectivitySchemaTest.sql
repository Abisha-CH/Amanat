:on error exit
SET NOCOUNT ON;
GO

SELECT @@VERSION AS ServerVersion, DB_NAME() AS CurrentDatabase;
GO

IF DB_ID(N'$(TestDatabaseName)') IS NULL
BEGIN
    DECLARE @createStatement nvarchar(max) = N'CREATE DATABASE ' + QUOTENAME(N'$(TestDatabaseName)');
    EXEC sys.sp_executesql @createStatement;
END;
GO

USE [$(TestDatabaseName)];
GO

:r Sql/001_CreateDomainSchema.sql
GO

SET NOCOUNT ON;
DECLARE @Failures int = 0;

SELECT @Failures = @Failures + COUNT(*)
FROM (VALUES
    (N'dbo.Campaigns'),
    (N'dbo.Contributions'),
    (N'dbo.ContributionVerifications'),
    (N'dbo.Expenses'),
    (N'dbo.ExpenseApprovals'),
    (N'dbo.ReceiptMetadata'),
    (N'dbo.CampaignUpdates'),
    (N'dbo.AuditRecords')
) AS expectedTables(name)
WHERE OBJECT_ID(expectedTables.name, N'U') IS NULL;

SELECT @Failures = @Failures + COUNT(*)
FROM (VALUES
    (N'dbo.vw_CampaignFinancialSummary'),
    (N'dbo.vw_PublicLedgerEntries'),
    (N'dbo.vw_PendingContributions'),
    (N'dbo.vw_PendingApprovals'),
    (N'dbo.vw_FundSummary')
) AS expectedViews(name)
WHERE OBJECT_ID(expectedViews.name, N'V') IS NULL;

SELECT @Failures = @Failures + COUNT(*)
FROM (VALUES
    (N'CK_Campaigns_GoalPositive'),
    (N'CK_Campaigns_Status'),
    (N'CK_Contributions_AmountPositive'),
    (N'CK_Contributions_Status'),
    (N'CK_ContributionVerifications_Decision'),
    (N'CK_Expenses_AmountPositive'),
    (N'CK_Expenses_Status'),
    (N'CK_ExpenseApprovals_Decision'),
    (N'CK_ReceiptMetadata_AmountNonNegative'),
    (N'CK_ReceiptMetadata_HasOwner'),
    (N'FK_Contributions_Campaigns'),
    (N'FK_ContributionVerifications_Contributions'),
    (N'FK_Expenses_Campaigns'),
    (N'FK_ExpenseApprovals_Expenses'),
    (N'FK_ReceiptMetadata_Expenses'),
    (N'FK_ReceiptMetadata_Contributions'),
    (N'FK_CampaignUpdates_Campaigns')
) AS expectedConstraints(name)
WHERE NOT EXISTS (
    SELECT 1
    FROM sys.objects
    WHERE name = expectedConstraints.name
      AND type IN (N'C', N'F')
);

IF (SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Campaigns') AND name = N'Goal' AND TYPE_NAME(user_type_id) = N'decimal' AND [precision] = 18 AND [scale] = 2) <> 1
    SET @Failures = @Failures + 1;
IF (SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Contributions') AND name = N'Amount' AND TYPE_NAME(user_type_id) = N'decimal' AND [precision] = 18 AND [scale] = 2) <> 1
    SET @Failures = @Failures + 1;
IF (SELECT COUNT(*) FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Expenses') AND name = N'Amount' AND TYPE_NAME(user_type_id) = N'decimal' AND [precision] = 18 AND [scale] = 2) <> 1
    SET @Failures = @Failures + 1;

DECLARE @CampaignId int = NULL;
DECLARE @TestSlug nvarchar(100) = N'connectivity-test-' + CONVERT(nvarchar(36), NEWID());
DECLARE @TestGoal decimal(18, 2) = CAST(1000 AS decimal(18, 2));
DECLARE @TestStartedOn date = CAST('2026-01-01' AS date);
DECLARE @TestDeadline date = CAST('2026-12-31' AS date);
DECLARE @ContributionAmount decimal(18, 2) = CAST(100 AS decimal(18, 2));
DECLARE @ExpenseAmount decimal(18, 2) = CAST(25 AS decimal(18, 2));
DECLARE @InvalidAmount decimal(18, 2) = CAST(10 AS decimal(18, 2));
DECLARE @InvalidCampaignId int = 2147483647;

BEGIN TRY
    EXEC sys.sp_executesql
        N'INSERT dbo.Campaigns (Slug, Title, Organization, Category, FocusKey, Goal, StartedOn, Deadline, Status, CreatedByUserId) VALUES (@Slug, @Title, @Organization, @Category, @FocusKey, @Goal, @StartedOn, @Deadline, @Status, @CreatedByUserId); SET @CampaignId = SCOPE_IDENTITY();',
        N'@Slug nvarchar(100), @Title nvarchar(200), @Organization nvarchar(200), @Category nvarchar(100), @FocusKey nvarchar(50), @Goal decimal(18,2), @StartedOn date, @Deadline date, @Status nvarchar(20), @CreatedByUserId nvarchar(450), @CampaignId int OUTPUT',
        @Slug = @TestSlug,
        @Title = N'Connectivity Test Campaign',
        @Organization = N'Amanat Test',
        @Category = N'Test',
        @FocusKey = N'test',
        @Goal = @TestGoal,
        @StartedOn = @TestStartedOn,
        @Deadline = @TestDeadline,
        @Status = N'Active',
        @CreatedByUserId = N'connectivity-test-user',
        @CampaignId = @CampaignId OUTPUT;
END TRY
BEGIN CATCH
    SET @Failures = @Failures + 1;
    SELECT ERROR_MESSAGE() AS CampaignInsertError;
END CATCH;

IF @CampaignId IS NULL
    SET @Failures = @Failures + 1;

IF @CampaignId IS NOT NULL
BEGIN
    BEGIN TRY
        EXEC sys.sp_executesql
            N'INSERT dbo.Contributions (CampaignId, FullName, Amount, TransactionRef, Status) VALUES (@CampaignId, @FullName, @Amount, @TransactionRef, @Status);',
            N'@CampaignId int, @FullName nvarchar(100), @Amount decimal(18,2), @TransactionRef nvarchar(100), @Status nvarchar(20)',
            @CampaignId = @CampaignId,
            @FullName = N'Test Contributor',
            @Amount = @ContributionAmount,
            @TransactionRef = N'TEST-CONTRIBUTION-1',
            @Status = N'Verified';

        EXEC sys.sp_executesql
            N'INSERT dbo.Expenses (CampaignId, SubmittedByUserId, Reference, PayeeName, Amount, Description, Status) VALUES (@CampaignId, @SubmittedByUserId, @Reference, @PayeeName, @Amount, @Description, @Status);',
            N'@CampaignId int, @SubmittedByUserId nvarchar(450), @Reference nvarchar(50), @PayeeName nvarchar(150), @Amount decimal(18,2), @Description nvarchar(500), @Status nvarchar(20)',
            @CampaignId = @CampaignId,
            @SubmittedByUserId = N'connectivity-test-user',
            @Reference = N'TEST-EXPENSE-1',
            @PayeeName = N'Test Vendor',
            @Amount = @ExpenseAmount,
            @Description = N'Connectivity test expense',
            @Status = N'Approved';
    END TRY
    BEGIN CATCH
        SET @Failures = @Failures + 1;
        SELECT ERROR_MESSAGE() AS FinanceInsertError;
    END CATCH;

    BEGIN TRY
        EXEC sys.sp_executesql
            N'INSERT dbo.Contributions (CampaignId, FullName, Amount, TransactionRef, Status) VALUES (@CampaignId, @FullName, @Amount, @TransactionRef, @Status);',
            N'@CampaignId int, @FullName nvarchar(100), @Amount decimal(18,2), @TransactionRef nvarchar(100), @Status nvarchar(20)',
            @CampaignId = @CampaignId,
            @FullName = N'Invalid Status',
            @Amount = @InvalidAmount,
            @TransactionRef = N'TEST-INVALID-STATUS',
            @Status = N'Unknown';
        SET @Failures = @Failures + 1;
    END TRY
    BEGIN CATCH
    END CATCH;

    BEGIN TRY
        EXEC sys.sp_executesql
            N'INSERT dbo.Contributions (CampaignId, FullName, Amount, TransactionRef, Status) VALUES (@CampaignId, @FullName, @Amount, @TransactionRef, @Status);',
            N'@CampaignId int, @FullName nvarchar(100), @Amount decimal(18,2), @TransactionRef nvarchar(100), @Status nvarchar(20)',
            @CampaignId = @InvalidCampaignId,
            @FullName = N'Invalid Foreign Key',
            @Amount = @InvalidAmount,
            @TransactionRef = N'TEST-INVALID-FK',
            @Status = N'Pending';
        SET @Failures = @Failures + 1;
    END TRY
    BEGIN CATCH
    END CATCH;

    DECLARE @Verified decimal(18, 2) = CAST(0 AS decimal(18, 2));
    DECLARE @Approved decimal(18, 2) = CAST(0 AS decimal(18, 2));
    DECLARE @Balance decimal(18, 2) = CAST(0 AS decimal(18, 2));

    SELECT
        @Verified = VerifiedContributions,
        @Approved = ApprovedExpenses,
        @Balance = Balance
    FROM dbo.vw_CampaignFinancialSummary
    WHERE CampaignId = @CampaignId;

    IF @Verified <> CAST(100 AS decimal(18, 2))
        OR @Approved <> CAST(25 AS decimal(18, 2))
        OR @Balance <> CAST(75 AS decimal(18, 2))
        SET @Failures = @Failures + 1;
END;

IF @Failures = 0
    PRINT 'PASS: LocalDB connectivity, isolated database creation, schema, constraints, parameterized writes, and financial view totals verified.';
ELSE
BEGIN
    PRINT 'FAIL: database verification failed.';
    RAISERROR('Database verification failed with %d failure(s).', 16, 1, @Failures);
END;
GO
