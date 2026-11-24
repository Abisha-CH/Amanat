using Amanat.Data;
using Amanat.Models;
using Amanat.Models.Entities;
using Amanat.Repositories;
using Amanat.Repositories.Interfaces;
using Amanat.Services;
using Dapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.RegularExpressions;

var masterConnectionString = Environment.GetEnvironmentVariable("AMANAT_TEST_MASTER_CONNECTION")
    ?? "Server=(localdb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=30";
var projectRoot = FindProjectRoot();
var schemaPath = Path.Combine(projectRoot, "Sql", "001_CreateDomainSchema.sql");

if (!File.Exists(schemaPath))
{
    throw new FileNotFoundException("Domain schema script was not found.", schemaPath);
}

var databaseName = "AmanatMvcTest_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + "_" + Guid.NewGuid().ToString("N")[..6];
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
var domainConnection = testBuilder.ConnectionString;

await using (var setup = new SqlConnection(domainConnection))
{
    await setup.OpenAsync();
    await SqlScriptRunner.ExecuteAsync(setup, schemaPath);
}

var tempRoot = Path.Combine(Path.GetTempPath(), "opencode");
Directory.CreateDirectory(tempRoot);
var identityConnection = "Data Source=" + Path.Combine(tempRoot, databaseName + ".db").Replace('\\', '/');
if (File.Exists(Path.Combine(tempRoot, databaseName + ".db")))
{
    File.Delete(Path.Combine(tempRoot, databaseName + ".db"));
}

var configuration = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["ConnectionStrings:DomainConnection"] = domainConnection,
        ["ConnectionStrings:IdentityConnection"] = identityConnection,
        ["SeedUsers:0:Email"] = "organizer.seed@amanat.test",
        ["SeedUsers:0:Password"] = "SeedOrganizer@2026",
        ["SeedUsers:0:FullName"] = "Seed Organizer",
        ["SeedUsers:0:Role"] = AmanatRoles.Organizer,
        ["SeedUsers:1:Email"] = "treasurer.seed@amanat.test",
        ["SeedUsers:1:Password"] = "SeedTreasurer@2026",
        ["SeedUsers:1:FullName"] = "Seed Treasurer",
        ["SeedUsers:1:Role"] = AmanatRoles.Treasurer
    })
    .Build();

var services = new ServiceCollection();
services.AddLogging();
services.AddSingleton<IConfiguration>(configuration);
services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(identityConnection));
services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
services.AddSingleton<IDbConnectionFactory, SqlConnectionFactory>();
services.AddScoped<ICampaignRepository, CampaignRepository>();
services.AddScoped<IContributionRepository, ContributionRepository>();
services.AddScoped<IExpenseRepository, ExpenseRepository>();
services.AddScoped<ILedgerRepository, LedgerRepository>();
services.AddScoped<IDomainMutationService, SqlDomainMutationService>();

using var provider = services.BuildServiceProvider();
using (var scope = provider.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.EnsureCreatedAsync();
    await RoleSeeder.SeedAsync(scope.ServiceProvider, configuration);
}

await DomainSeeder.SeedAsync(provider);

var totalChecks = 0;
var failedChecks = 0;
var stamp = DateTime.UtcNow.ToString("HHmmssfff");

ApplicationUser organizer;
ApplicationUser treasurer;
ApplicationUser contributor;
ApplicationUser otherOrganizer;

using (var scope = provider.CreateScope())
{
    var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    organizer = (await users.FindByEmailAsync("organizer.seed@amanat.test"))!;
    treasurer = (await users.FindByEmailAsync("treasurer.seed@amanat.test"))!;
    contributor = await CreateUserAsync(users, $"mvc-contributor-{stamp}@amanat.test", AmanatRoles.Contributor, "Mvc Contributor");
    otherOrganizer = await CreateUserAsync(users, $"mvc-organizer-{stamp}@amanat.test", AmanatRoles.Organizer, "Other Organizer");
}

using var appScope = provider.CreateScope();
var scoped = appScope.ServiceProvider;
var mutations = scoped.GetRequiredService<IDomainMutationService>();
var campaigns = scoped.GetRequiredService<ICampaignRepository>();
var contributions = scoped.GetRequiredService<IContributionRepository>();
var expenses = scoped.GetRequiredService<IExpenseRepository>();
var ledger = scoped.GetRequiredService<ILedgerRepository>();

var education = (await campaigns.GetBySlugAsync("education-for-all"))!;
var otherCampaignId = await mutations.CreateCampaignAsync(new CampaignCreateInput
{
    Slug = "mvc-other-" + stamp,
    Title = "MVC Other Campaign",
    Organization = "Other Organization",
    Category = "Education",
    FocusKey = "education",
    Goal = 50000m,
    StartedOn = new DateTime(2026, 1, 1),
    Deadline = new DateTime(2026, 12, 31),
    Summary = "Ownership test campaign"
}, otherOrganizer);
Check("Organizer creates campaign through mutation service", otherCampaignId > 0, otherCampaignId.ToString());

var updateFailure = await CaptureAsync(() => mutations.AddCampaignUpdateAsync(new CampaignUpdateInput
{
    CampaignId = education.CampaignId,
    Title = "Unauthorized update",
    Body = "Should be rejected"
}, otherOrganizer));
Check("Non-owner cannot post campaign update", updateFailure is DomainUnauthorizedException, updateFailure?.Message ?? "no exception");

var archiveFailure = await CaptureAsync(() => mutations.ArchiveCampaignAsync(education.CampaignId, Convert.ToBase64String(new byte[8]), otherOrganizer));
Check("Non-owner cannot archive campaign", archiveFailure is DomainUnauthorizedException, archiveFailure?.Message ?? "no exception");

var authorizedUpdateId = await mutations.AddCampaignUpdateAsync(new CampaignUpdateInput
{
    CampaignId = education.CampaignId,
    Title = "MVC update",
    Body = "Persisted through the domain mutation service."
}, organizer);
Check("Owner can post campaign update", authorizedUpdateId > 0, authorizedUpdateId.ToString());

var contributionRef = "MVC-CONTRIB-" + stamp;
var contributionId = await mutations.SubmitContributionAsync(new ContributionEntity
{
    CampaignId = education.CampaignId,
    FullName = "Mvc Contributor",
    Amount = 4321m,
    TransactionRef = contributionRef,
    DateSent = new DateTime(2023, 10, 24)
}, contributor);
var contribution = (await contributions.GetByIdAsync(contributionId))!;
Check("Contribution id is returned", contributionId > 0, contributionId.ToString());
Check("Contribution is forced to Pending", contribution.Status == "Pending", contribution.Status);
Check("Contribution is linked to signed-in contributor", contribution.ContributorUserId == contributor.Id, contribution.ContributorUserId ?? "null");

var duplicateContribution = await CaptureAsync(() => mutations.SubmitContributionAsync(new ContributionEntity
{
    CampaignId = education.CampaignId,
    FullName = "Duplicate Contributor",
    Amount = 999m,
    TransactionRef = contributionRef,
    DateSent = new DateTime(2023, 10, 24)
}, contributor));
Check("Duplicate contribution reference is rejected", duplicateContribution is DomainValidationException, duplicateContribution?.Message ?? "no exception");
Check("Duplicate error is user-safe", IsUserSafe(duplicateContribution?.Message), duplicateContribution?.Message ?? "no exception");
Check("Duplicate contribution rolls back", await CountAsync(domainConnection, "SELECT COUNT(*) FROM dbo.Contributions WHERE TransactionRef = @Reference", new { Reference = contributionRef }) == 1);

var selfReview = await CaptureAsync(() => mutations.DecideContributionAsync(contributionId, "Verified", null, Convert.ToBase64String(contribution.RowVersion), contributor));
Check("Contributor cannot verify own contribution", selfReview is DomainUnauthorizedException, selfReview?.Message ?? "no exception");
Check("Self-review leaves contribution pending", (await contributions.GetByIdAsync(contributionId))?.Status == "Pending");

var beforeVerification = await ledger.GetPublicSummaryAsync();
await mutations.DecideContributionAsync(contributionId, "Verified", "Mvc verification", Convert.ToBase64String(contribution.RowVersion), treasurer);
var afterVerification = await ledger.GetPublicSummaryAsync();
Check("Verified contribution increases public ledger inflow",
    afterVerification.TotalVerifiedContributions == beforeVerification.TotalVerifiedContributions + 4321m
    && afterVerification.VerifiedContributionCount == beforeVerification.VerifiedContributionCount + 1,
    $"{beforeVerification.TotalVerifiedContributions} -> {afterVerification.TotalVerifiedContributions}");

var repeatedVerification = await CaptureAsync(() => mutations.DecideContributionAsync(contributionId, "Rejected", null, Convert.ToBase64String(contribution.RowVersion), treasurer));
Check("Already reviewed contribution is rejected", repeatedVerification is DomainValidationException, repeatedVerification?.Message ?? "no exception");
Check("Repeat decision does not alter public ledger", (await ledger.GetPublicSummaryAsync()).TotalVerifiedContributions == afterVerification.TotalVerifiedContributions);

var expenseRef = "MVC-EXPENSE-" + stamp;
var expenseId = await mutations.SubmitExpenseAsync(new ExpenseEntity
{
    CampaignId = education.CampaignId,
    Reference = expenseRef,
    PayeeName = "Mvc Vendor",
    Amount = 2500m,
    Description = "Mvc service test"
}, treasurer);
var expense = (await expenses.GetByIdAsync(expenseId))!;
Check("Expense id is returned", expenseId > 0, expenseId.ToString());
Check("Expense is forced to Pending", expense.Status == "Pending", expense.Status);
Check("Expense is linked to submitting treasurer", expense.SubmittedByUserId == treasurer.Id, expense.SubmittedByUserId);

var duplicateExpense = await CaptureAsync(() => mutations.SubmitExpenseAsync(new ExpenseEntity
{
    CampaignId = education.CampaignId,
    Reference = expenseRef,
    PayeeName = "Duplicate Vendor",
    Amount = 999m,
    Description = "Duplicate"
}, treasurer));
Check("Duplicate expense reference is rejected", duplicateExpense is DomainValidationException, duplicateExpense?.Message ?? "no exception");
Check("Duplicate expense error is user-safe", IsUserSafe(duplicateExpense?.Message), duplicateExpense?.Message ?? "no exception");
Check("Duplicate expense rolls back", await CountAsync(domainConnection, "SELECT COUNT(*) FROM dbo.Expenses WHERE Reference = @Reference", new { Reference = expenseRef }) == 1);

var unauthorizedApproval = await CaptureAsync(() => mutations.DecideExpenseAsync(expenseId, "Approved", null, Convert.ToBase64String(expense.RowVersion), otherOrganizer));
Check("Non-owner organizer cannot approve expense", unauthorizedApproval is DomainUnauthorizedException, unauthorizedApproval?.Message ?? "no exception");
Check("Unauthorized approval leaves expense pending", (await expenses.GetByIdAsync(expenseId))?.Status == "Pending");

var beforeApproval = await ledger.GetPublicSummaryAsync();
await mutations.DecideExpenseAsync(expenseId, "Approved", "Mvc approval", Convert.ToBase64String(expense.RowVersion), organizer);
var afterApproval = await ledger.GetPublicSummaryAsync();
Check("Approved expense increases public ledger outflow",
    afterApproval.TotalApprovedExpenses == beforeApproval.TotalApprovedExpenses + 2500m
    && afterApproval.ApprovedExpenseCount == beforeApproval.ApprovedExpenseCount + 1,
    $"{beforeApproval.TotalApprovedExpenses} -> {afterApproval.TotalApprovedExpenses}");

var repeatedApproval = await CaptureAsync(() => mutations.DecideExpenseAsync(expenseId, "Rejected", null, Convert.ToBase64String(expense.RowVersion), organizer));
Check("Already reviewed expense is rejected", repeatedApproval is DomainValidationException, repeatedApproval?.Message ?? "no exception");
Check("Repeat expense decision does not alter public ledger", (await ledger.GetPublicSummaryAsync()).TotalApprovedExpenses == afterApproval.TotalApprovedExpenses);

var ownExpenseId = await mutations.SubmitExpenseAsync(new ExpenseEntity
{
    CampaignId = education.CampaignId,
    Reference = "MVC-OWN-" + stamp,
    PayeeName = "Organizer Vendor",
    Amount = 500m,
    Description = "Self approval test"
}, organizer);
var ownExpense = (await expenses.GetByIdAsync(ownExpenseId))!;
var selfApproval = await CaptureAsync(() => mutations.DecideExpenseAsync(ownExpenseId, "Approved", null, Convert.ToBase64String(ownExpense.RowVersion), organizer));
Check("Organizer cannot approve own submitted expense", selfApproval is DomainUnauthorizedException, selfApproval?.Message ?? "no exception");
Check("Self-submitted expense stays pending", (await expenses.GetByIdAsync(ownExpenseId))?.Status == "Pending");

var publicEntries = await ledger.GetEntriesAsync();
Check("Verified contribution appears in public ledger", publicEntries.Any(entry => entry.EntityId == contributionId && entry.Direction == "In"));
Check("Approved expense appears in public ledger", publicEntries.Any(entry => entry.EntityId == expenseId && entry.Direction == "Out"));
Check("Pending expense stays out of public ledger", publicEntries.All(entry => entry.EntityId != ownExpenseId));

Console.WriteLine($"RESULT total={totalChecks} passed={totalChecks - failedChecks} failed={failedChecks}");
Console.WriteLine($"Database kept for inspection: {databaseName}");
return failedChecks == 0 ? 0 : 1;

void Check(string name, bool passed, string? detail = null)
{
    totalChecks++;
    if (!passed)
    {
        failedChecks++;
    }

    Console.WriteLine($"{(passed ? "PASS" : "FAIL")} {name}{(detail is null ? string.Empty : " :: " + detail)}");
}

static async Task<Exception?> CaptureAsync(Func<Task> operation)
{
    try
    {
        await operation();
        return null;
    }
    catch (Exception exception)
    {
        return exception;
    }
}

static bool IsUserSafe(string? message) =>
    !string.IsNullOrWhiteSpace(message)
    && !message.Contains("SqlException", StringComparison.OrdinalIgnoreCase)
    && !message.Contains("SQL", StringComparison.OrdinalIgnoreCase)
    && !message.Contains("dbo.", StringComparison.OrdinalIgnoreCase);

static async Task<int> CountAsync(string connectionString, string sql, object parameters)
{
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    return await connection.ExecuteScalarAsync<int>(sql, parameters);
}

static async Task<ApplicationUser> CreateUserAsync(UserManager<ApplicationUser> users, string email, string role, string fullName)
{
    var user = new ApplicationUser
    {
        UserName = email,
        Email = email,
        FullName = fullName,
        EmailConfirmed = true
    };

    var result = await users.CreateAsync(user, "TestUser@2026");
    if (!result.Succeeded)
    {
        throw new InvalidOperationException($"Unable to create test user '{email}': {string.Join("; ", result.Errors.Select(error => error.Description))}");
    }

    var roleResult = await users.AddToRoleAsync(user, role);
    if (!roleResult.Succeeded)
    {
        throw new InvalidOperationException($"Unable to assign role '{role}' to '{email}'.");
    }

    return user;
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
