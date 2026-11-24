using Amanat.Data;
using Amanat.Models.Entities;
using Amanat.Repositories;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.RegularExpressions;

var masterConnectionString = Environment.GetEnvironmentVariable("AMANAT_TEST_MASTER_CONNECTION")
    ?? "Server=(localdb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=30";
var projectRoot = FindProjectRoot();
var schemaPath = Path.Combine(projectRoot, "Sql", "001_CreateDomainSchema.sql");

if (!File.Exists(schemaPath))
{
    throw new FileNotFoundException("Domain schema script was not found.", schemaPath);
}

var databaseName = "AmanatDomainTest_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + "_" + Guid.NewGuid().ToString("N")[..6];
Console.WriteLine($"TestDatabase: {databaseName}");

var masterBuilder = new SqlConnectionStringBuilder(masterConnectionString)
{
    InitialCatalog = "master"
};

await using (var master = new SqlConnection(masterBuilder.ConnectionString))
{
    await master.OpenAsync();
    await master.ExecuteAsync($"CREATE DATABASE [{databaseName}];");
}

var testBuilder = new SqlConnectionStringBuilder(masterConnectionString)
{
    InitialCatalog = databaseName
};
var testConnectionString = testBuilder.ConnectionString;

await using (var setup = new SqlConnection(testConnectionString))
{
    await setup.OpenAsync();
    await SqlScriptRunner.ExecuteAsync(setup, schemaPath);
}

var factory = new TestDbConnectionFactory(testConnectionString);
var campaigns = new CampaignRepository(factory);
var contributions = new ContributionRepository(factory);
var expenses = new ExpenseRepository(factory);
var ledger = new LedgerRepository(factory);

var totalChecks = 0;
var failedChecks = 0;

var mainCampaign = NewCampaign("Repository Main");
mainCampaign.Title = "x'; DROP TABLE dbo.Campaigns;--";
var mainCampaignId = await campaigns.CreateAsync(mainCampaign);
Check("Campaign create returns id", mainCampaignId > 0, mainCampaignId.ToString());

var mainById = await campaigns.GetByIdAsync(mainCampaignId);
Check("Campaign retrieve by id", mainById?.CampaignId == mainCampaignId);
Check("Campaign parameterization preserves literal value", mainById?.Title == mainCampaign.Title, mainById?.Title ?? "null");
Check("Campaign retrieve by slug", (await campaigns.GetBySlugAsync(mainCampaign.Slug))?.CampaignId == mainCampaignId);

await using (var checkConnection = await OpenTestConnectionAsync())
{
    var tableCount = await checkConnection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM sys.tables WHERE name = N'Campaigns';");
    Check("Parameterized campaign value did not drop table", tableCount == 1, tableCount.ToString());
}

var editableCampaign = mainById!;
editableCampaign.Title = "Repository Test Campaign Updated";
var updateResult = await campaigns.UpdateAsync(editableCampaign, editableCampaign.RowVersion);
await ThrowsAsync<DBConcurrencyException>(
    "Campaign update rejects stale rowversion",
    () => campaigns.UpdateAsync(editableCampaign, editableCampaign.RowVersion));
var updatedCampaign = (await campaigns.GetByIdAsync(mainCampaignId))!;
Check("Campaign update succeeds", updateResult);
Check("Campaign update persists", updatedCampaign.Title == "Repository Test Campaign Updated", updatedCampaign.Title);
Check("Campaign rowversion changes after update", !updatedCampaign.RowVersion.SequenceEqual(editableCampaign.RowVersion));

await ThrowsAsync<InvalidOperationException>(
    "Campaign create rejects non-positive goal",
    () =>
    {
        var invalid = NewCampaign("Invalid Goal");
        invalid.Goal = 0;
        return campaigns.CreateAsync(invalid);
    });

var archiveCampaign = NewCampaign("Archive Test");
archiveCampaign.Status = "Draft";
var archiveCampaignId = await campaigns.CreateAsync(archiveCampaign);
var archiveEntity = (await campaigns.GetByIdAsync(archiveCampaignId))!;
var archiveResult = await campaigns.ArchiveAsync(archiveCampaignId, archiveEntity.RowVersion);
var archivedCampaign = (await campaigns.GetByIdAsync(archiveCampaignId))!;
var activeCampaigns = await campaigns.GetAllAsync(true);
var allCampaigns = await campaigns.GetAllAsync(false);
Check("Campaign archive succeeds", archiveResult);
Check("Campaign archive status persists", archivedCampaign.Status == "Archived", archivedCampaign.Status);
Check("Campaign archive excluded from active list", !activeCampaigns.Any(c => c.CampaignId == archiveCampaignId));
Check("Campaign archive included in full list", allCampaigns.Any(c => c.CampaignId == archiveCampaignId));

var campaignUpdateId = await campaigns.AddUpdateAsync(new CampaignUpdateEntity
{
    CampaignId = mainCampaignId,
    AuthorUserId = "test-organizer",
    Title = "Repository update",
    Body = "Repository update body",
    IsPublic = true
});
var campaignUpdates = await campaigns.GetUpdatesAsync(mainCampaignId, true);
Check("Campaign update create and retrieve", campaignUpdates.Any(u => u.CampaignUpdateId == campaignUpdateId));

var rejectedContributionRef = "TXN-REJECTED-" + Guid.NewGuid().ToString("N")[..8];
var pendingContributionRef = "TXN-PENDING-" + Guid.NewGuid().ToString("N")[..8];
long verifiedContributionId;
long rejectedContributionId;
long pendingContributionId;

await using (var connection = await OpenTestConnectionAsync())
{
    await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
    verifiedContributionId = await contributions.SubmitAsync(
        NewContribution(mainCampaignId, "TXN-VERIFIED-" + Guid.NewGuid().ToString("N")[..8], 1000m, "Verified Contributor'; --"),
        transaction);
    rejectedContributionId = await contributions.SubmitAsync(
        NewContribution(mainCampaignId, rejectedContributionRef, 250m, "Rejected Contributor"),
        transaction);
    pendingContributionId = await contributions.SubmitAsync(
        NewContribution(mainCampaignId, pendingContributionRef, 700m, "Pending Contributor"),
        transaction);
    await transaction.CommitAsync();
}

Check("Contribution submissions persist", verifiedContributionId > 0 && rejectedContributionId > 0 && pendingContributionId > 0);
Check("Contribution submissions forced to pending", (await contributions.GetByIdAsync(verifiedContributionId))?.Status == "Pending");
Check("Contribution parameterization preserves literal value", (await contributions.GetByIdAsync(verifiedContributionId))?.FullName == "Verified Contributor'; --");

var duplicateReference = (await contributions.GetByIdAsync(verifiedContributionId))!.TransactionRef;
await using (var connection = await OpenTestConnectionAsync())
{
    await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
    try
    {
        await contributions.SubmitAsync(NewContribution(mainCampaignId, duplicateReference, 10m, "Duplicate Contributor"), transaction);
        Check("Contribution duplicate transaction reference rejected", false, "no exception thrown");
    }
    catch (InvalidOperationException exception)
    {
        Check("Contribution duplicate transaction reference rejected", true, exception.Message);
    }
    finally
    {
        await transaction.RollbackAsync();
    }
}

long rolledBackContributionId;
await using (var connection = await OpenTestConnectionAsync())
{
    await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
    rolledBackContributionId = await contributions.SubmitAsync(
        NewContribution(mainCampaignId, "TXN-ROLLBACK-" + Guid.NewGuid().ToString("N")[..8], 50m, "Rolled Back Contributor"),
        transaction);
    await transaction.RollbackAsync();
}
Check("Contribution rollback prevents persistence", await contributions.GetByIdAsync(rolledBackContributionId) is null);

var verifiedContribution = (await contributions.GetByIdAsync(verifiedContributionId))!;
bool verifyResult;
await using (var connection = await OpenTestConnectionAsync())
{
    await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
    verifyResult = await contributions.VerifyAsync(verifiedContributionId, "test-treasurer", "Verified in test", verifiedContribution.RowVersion, transaction);
    await transaction.CommitAsync();
}
Check("Contribution verify succeeds", verifyResult);
Check("Contribution verify status persists", (await contributions.GetByIdAsync(verifiedContributionId))?.Status == "Verified");

var rejectedContribution = (await contributions.GetByIdAsync(rejectedContributionId))!;
bool rejectContributionResult;
await using (var connection = await OpenTestConnectionAsync())
{
    await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
    rejectContributionResult = await contributions.RejectAsync(rejectedContributionId, "test-treasurer", "Rejected in test", rejectedContribution.RowVersion, transaction);
    await transaction.CommitAsync();
}
Check("Contribution reject succeeds", rejectContributionResult);
Check("Contribution reject status persists", (await contributions.GetByIdAsync(rejectedContributionId))?.Status == "Rejected");

var verificationHistory = await contributions.GetVerificationHistoryAsync(verifiedContributionId);
var rejectionHistory = await contributions.GetVerificationHistoryAsync(rejectedContributionId);
Check("Contribution verification history recorded", verificationHistory.Count == 1 && verificationHistory[0].Decision == "Verified");
Check("Contribution rejection history recorded", rejectionHistory.Count == 1 && rejectionHistory[0].Decision == "Rejected");

var processedContribution = (await contributions.GetByIdAsync(verifiedContributionId))!;
await using (var connection = await OpenTestConnectionAsync())
{
    await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
    try
    {
        await contributions.VerifyAsync(verifiedContributionId, "test-treasurer", "Second attempt", processedContribution.RowVersion, transaction);
        Check("Contribution double verification prevented", false, "no exception thrown");
    }
    catch (InvalidOperationException exception)
    {
        Check("Contribution double verification prevented", true, exception.Message);
    }
    finally
    {
        await transaction.RollbackAsync();
    }
}

var pendingContribution = (await contributions.GetByIdAsync(pendingContributionId))!;
await using (var connection = await OpenTestConnectionAsync())
{
    await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
    await connection.ExecuteAsync(
        "UPDATE dbo.Contributions SET Notes = @Notes WHERE ContributionId = @ContributionId;",
        new { Notes = "external change", ContributionId = pendingContributionId },
        transaction);
    try
    {
        await contributions.VerifyAsync(pendingContributionId, "test-treasurer", "Stale attempt", pendingContribution.RowVersion, transaction);
        Check("Contribution stale rowversion prevented", false, "no exception thrown");
    }
    catch (DBConcurrencyException exception)
    {
        Check("Contribution stale rowversion prevented", true, exception.Message);
    }
    finally
    {
        await transaction.RollbackAsync();
    }
}

await ThrowsAsync<InvalidOperationException>(
    "Contribution submit rejects non-positive amount",
    async () =>
    {
        await using var connection = await OpenTestConnectionAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
        var invalid = NewContribution(mainCampaignId, "TXN-INVALID-" + Guid.NewGuid().ToString("N")[..8], 0m, "Invalid Amount");
        await contributions.SubmitAsync(invalid, transaction);
    });

var rejectedExpenseRef = "EXP-REJECTED-" + Guid.NewGuid().ToString("N")[..8];
var pendingExpenseRef = "EXP-PENDING-" + Guid.NewGuid().ToString("N")[..8];
long approvedExpenseId;
long rejectedExpenseId;
long pendingExpenseId;

await using (var connection = await OpenTestConnectionAsync())
{
    await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
    approvedExpenseId = await expenses.SubmitAsync(NewExpense(mainCampaignId, "EXP-APPROVED-" + Guid.NewGuid().ToString("N")[..8], 200m), transaction);
    await connection.ExecuteAsync(
        "INSERT dbo.ReceiptMetadata (ExpenseId, FileName, OriginalFileName, UploadedByUserId, CreatedAtUtc, Amount) VALUES (@ExpenseId, @FileName, @OriginalFileName, @UploadedByUserId, @CreatedAtUtc, @Amount);",
        new
        {
            ExpenseId = approvedExpenseId,
            FileName = "receipt.pdf",
            OriginalFileName = "receipt.pdf",
            UploadedByUserId = "test-treasurer",
            CreatedAtUtc = DateTime.UtcNow,
            Amount = 200m
        },
        transaction);
    rejectedExpenseId = await expenses.SubmitAsync(NewExpense(mainCampaignId, rejectedExpenseRef, 75m), transaction);
    pendingExpenseId = await expenses.SubmitAsync(NewExpense(mainCampaignId, pendingExpenseRef, 900m), transaction);
    await transaction.CommitAsync();
}

Check("Expense submissions persist", approvedExpenseId > 0 && rejectedExpenseId > 0 && pendingExpenseId > 0);
Check("Expense submissions forced to pending", (await expenses.GetByIdAsync(approvedExpenseId))?.Status == "Pending");
Check("Expense receipt metadata retrieved", (await expenses.GetReceiptsAsync(approvedExpenseId)).Count == 1);

var duplicateExpenseReference = (await expenses.GetByIdAsync(approvedExpenseId))!.Reference;
await using (var connection = await OpenTestConnectionAsync())
{
    await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
    try
    {
        await expenses.SubmitAsync(NewExpense(mainCampaignId, duplicateExpenseReference, 10m), transaction);
        Check("Expense duplicate reference rejected", false, "no exception thrown");
    }
    catch (InvalidOperationException exception)
    {
        Check("Expense duplicate reference rejected", true, exception.Message);
    }
    finally
    {
        await transaction.RollbackAsync();
    }
}

long rolledBackExpenseId;
await using (var connection = await OpenTestConnectionAsync())
{
    await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
    rolledBackExpenseId = await expenses.SubmitAsync(NewExpense(mainCampaignId, "EXP-ROLLBACK-" + Guid.NewGuid().ToString("N")[..8], 40m), transaction);
    await transaction.RollbackAsync();
}
Check("Expense rollback prevents persistence", await expenses.GetByIdAsync(rolledBackExpenseId) is null);

var approvedExpense = (await expenses.GetByIdAsync(approvedExpenseId))!;
bool approveResult;
await using (var connection = await OpenTestConnectionAsync())
{
    await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
    approveResult = await expenses.ApproveAsync(approvedExpenseId, "test-organizer", "Approved in test", approvedExpense.RowVersion, transaction);
    await transaction.CommitAsync();
}
Check("Expense approve succeeds", approveResult);
Check("Expense approve status persists", (await expenses.GetByIdAsync(approvedExpenseId))?.Status == "Approved");

var rejectedExpense = (await expenses.GetByIdAsync(rejectedExpenseId))!;
bool rejectExpenseResult;
await using (var connection = await OpenTestConnectionAsync())
{
    await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
    rejectExpenseResult = await expenses.RejectAsync(rejectedExpenseId, "test-organizer", "Rejected in test", rejectedExpense.RowVersion, transaction);
    await transaction.CommitAsync();
}
Check("Expense reject succeeds", rejectExpenseResult);
Check("Expense reject status persists", (await expenses.GetByIdAsync(rejectedExpenseId))?.Status == "Rejected");

var approvalHistory = await expenses.GetApprovalHistoryAsync(approvedExpenseId);
var expenseRejectionHistory = await expenses.GetApprovalHistoryAsync(rejectedExpenseId);
Check("Expense approval history recorded", approvalHistory.Count == 1 && approvalHistory[0].Decision == "Approved");
Check("Expense rejection history recorded", expenseRejectionHistory.Count == 1 && expenseRejectionHistory[0].Decision == "Rejected");

var processedExpense = (await expenses.GetByIdAsync(approvedExpenseId))!;
await using (var connection = await OpenTestConnectionAsync())
{
    await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
    try
    {
        await expenses.ApproveAsync(approvedExpenseId, "test-organizer", "Second attempt", processedExpense.RowVersion, transaction);
        Check("Expense double approval prevented", false, "no exception thrown");
    }
    catch (InvalidOperationException exception)
    {
        Check("Expense double approval prevented", true, exception.Message);
    }
    finally
    {
        await transaction.RollbackAsync();
    }
}

var pendingExpense = (await expenses.GetByIdAsync(pendingExpenseId))!;
await using (var connection = await OpenTestConnectionAsync())
{
    await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
    await connection.ExecuteAsync(
        "UPDATE dbo.Expenses SET DecisionNote = @Note WHERE ExpenseId = @ExpenseId;",
        new { Note = "external change", ExpenseId = pendingExpenseId },
        transaction);
    try
    {
        await expenses.ApproveAsync(pendingExpenseId, "test-organizer", "Stale attempt", pendingExpense.RowVersion, transaction);
        Check("Expense stale rowversion prevented", false, "no exception thrown");
    }
    catch (DBConcurrencyException exception)
    {
        Check("Expense stale rowversion prevented", true, exception.Message);
    }
    finally
    {
        await transaction.RollbackAsync();
    }
}

await ThrowsAsync<InvalidOperationException>(
    "Expense submit rejects empty reference",
    async () =>
    {
        await using var connection = await OpenTestConnectionAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
        var invalid = NewExpense(mainCampaignId, string.Empty, 10m);
        await expenses.SubmitAsync(invalid, transaction);
    });

var summary = await ledger.GetPublicSummaryAsync();
var campaignBalance = await ledger.GetCampaignBalanceAsync(mainCampaignId);
var publicEntries = await ledger.GetEntriesAsync(mainCampaignId);
var verifiedEntries = await ledger.GetEntriesAsync(mainCampaignId, "Verified");
var approvedEntries = await ledger.GetEntriesAsync(mainCampaignId, "Approved");
var treasurerQueue = await ledger.GetTreasurerQueueAsync();
var funds = await ledger.GetInstitutionalFundsAsync();

Check("Ledger summary counts only verified contributions", summary.TotalVerifiedContributions == 1000m, summary.TotalVerifiedContributions.ToString());
Check("Ledger summary counts only approved expenses", summary.TotalApprovedExpenses == 200m, summary.TotalApprovedExpenses.ToString());
Check("Ledger summary balance", summary.Balance == 800m, summary.Balance.ToString());
Check("Ledger summary counts", summary.VerifiedContributionCount == 1 && summary.ApprovedExpenseCount == 1);
Check("Ledger summary campaign count", summary.CampaignCount == 2, summary.CampaignCount.ToString());
Check("Ledger campaign balance", campaignBalance?.Balance == 800m, campaignBalance?.Balance.ToString() ?? "null");
Check("Ledger public entries exclude pending and rejected", publicEntries.Count == 2 && publicEntries.All(e => e.Status is "Verified" or "Approved"), publicEntries.Count.ToString());
Check("Ledger verified entry filter", verifiedEntries.Count == 1);
Check("Ledger approved entry filter", approvedEntries.Count == 1);
Check("Ledger rejected contribution excluded", !publicEntries.Any(e => e.Reference == rejectedContributionRef));
Check("Ledger rejected expense excluded", !publicEntries.Any(e => e.Reference == rejectedExpenseRef));
Check("Ledger pending contribution excluded", !publicEntries.Any(e => e.Reference == pendingContributionRef));
Check("Ledger pending expense excluded", !publicEntries.Any(e => e.Reference == pendingExpenseRef));
Check("Ledger treasurer queue contains pending expense", treasurerQueue.Any(e => e.ExpenseId == pendingExpenseId));
Check("Ledger institutional funds views", funds.Count == 2 && funds.Any(f => f.FundKey == "AllCampaigns" && f.VerifiedContributions == 1000m && f.ApprovedExpenses == 200m && f.Balance == 800m));

await using (var connection = await OpenTestConnectionAsync())
{
    var auditCount = await connection.ExecuteScalarAsync<int>(
        "SELECT COUNT(*) FROM dbo.AuditRecords WHERE EntityType IN (N'Contribution', N'Expense') AND Action IN (N'Verified', N'Rejected', N'Approved');");
    Check("Financial decisions written to audit records", auditCount >= 4, auditCount.ToString());
}

var passedChecks = totalChecks - failedChecks;
Console.WriteLine($"RESULT total={totalChecks} passed={passedChecks} failed={failedChecks}");
Console.WriteLine($"Database kept for inspection: {databaseName}");
return failedChecks == 0 ? 0 : 1;

void Check(string name, bool pass, string detail = "")
{
    totalChecks++;
    if (!pass)
    {
        failedChecks++;
    }

    var suffix = string.IsNullOrWhiteSpace(detail) ? string.Empty : $" :: {detail}";
    Console.WriteLine($"{(pass ? "PASS" : "FAIL")} {name}{suffix}");
}

async Task ThrowsAsync<TException>(string name, Func<Task> action) where TException : Exception
{
    try
    {
        await action();
        Check(name, false, "no exception thrown");
    }
    catch (TException)
    {
        Check(name, true);
    }
    catch (Exception exception)
    {
        Check(name, false, $"expected {typeof(TException).Name}, got {exception.GetType().Name}: {exception.Message}");
    }
}

async Task<SqlConnection> OpenTestConnectionAsync()
{
    var connection = new SqlConnection(testConnectionString);
    await connection.OpenAsync();
    return connection;
}

static CampaignEntity NewCampaign(string prefix)
{
    return new CampaignEntity
    {
        Slug = $"{prefix.ToLowerInvariant().Replace(" ", "-")}-{Guid.NewGuid():N}",
        Title = prefix,
        Organization = "Amanat Test",
        Category = "Test",
        FocusKey = "repo",
        Goal = 10000m,
        StartedOn = new DateTime(2026, 1, 1),
        Deadline = new DateTime(2026, 12, 31),
        Status = "Active",
        CreatedByUserId = "test-organizer"
    };
}

static ContributionEntity NewContribution(int campaignId, string reference, decimal amount, string fullName)
{
    return new ContributionEntity
    {
        CampaignId = campaignId,
        ContributorUserId = "test-contributor",
        FullName = fullName,
        Amount = amount,
        TransactionRef = reference,
        Notes = "Repository integration test",
        Status = "Verified"
    };
}

static ExpenseEntity NewExpense(int campaignId, string reference, decimal amount)
{
    return new ExpenseEntity
    {
        CampaignId = campaignId,
        SubmittedByUserId = "test-contributor",
        Reference = reference,
        PayeeName = "Test Vendor",
        Amount = amount,
        Description = "Repository integration test",
        Status = "Approved"
    };
}

static string FindProjectRoot()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "Amanat.csproj")))
        {
            return directory.FullName;
        }

        directory = directory.Parent;
    }

    throw new DirectoryNotFoundException("Could not locate the Amanat project root.");
}

internal sealed class TestDbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public TestDbConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public SqlConnection CreateConnection() => new(_connectionString);
}

internal static class SqlScriptRunner
{
    public static async Task ExecuteAsync(SqlConnection connection, string path)
    {
        var script = await File.ReadAllTextAsync(path);
        var batches = Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

        foreach (var batch in batches)
        {
            if (string.IsNullOrWhiteSpace(batch))
            {
                continue;
            }

            using var command = connection.CreateCommand();
            command.CommandText = batch;
            command.CommandTimeout = 120;
            await command.ExecuteNonQueryAsync();
        }
    }
}
