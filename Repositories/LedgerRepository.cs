using Amanat.Data;
using Amanat.Models.Entities;
using Amanat.Repositories.Interfaces;
using Dapper;

namespace Amanat.Repositories;

public sealed class LedgerRepository : ILedgerRepository
{
    private static readonly string[] PublicEntryStatuses = { "Verified", "Approved" };
    private readonly IDbConnectionFactory _connectionFactory;

    public LedgerRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PublicSummaryEntity> GetPublicSummaryAsync(int? campaignId = null)
    {
        if (campaignId.HasValue)
        {
            RepositoryGuard.Positive(campaignId.Value, nameof(campaignId));
        }

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
SELECT
    CAST(ISNULL(SUM(VerifiedContributions), CAST(0 AS decimal(18, 2))) AS decimal(18, 2)) AS TotalVerifiedContributions,
    CAST(ISNULL(SUM(ApprovedExpenses), CAST(0 AS decimal(18, 2))) AS decimal(18, 2)) AS TotalApprovedExpenses,
    CAST(ISNULL(SUM(Balance), CAST(0 AS decimal(18, 2))) AS decimal(18, 2)) AS Balance,
    ISNULL(SUM(VerifiedContributionCount), 0) AS VerifiedContributionCount,
    ISNULL(SUM(ApprovedExpenseCount), 0) AS ApprovedExpenseCount,
    COUNT(*) AS CampaignCount,
    CAST(NULL AS datetime2(7)) AS UpdatedAtUtc
FROM dbo.vw_CampaignFinancialSummary
WHERE @CampaignId IS NULL OR CampaignId = @CampaignId;";

        return await connection.QuerySingleAsync<PublicSummaryEntity>(sql, new { CampaignId = campaignId });
    }

    public async Task<IReadOnlyList<LedgerEntryEntity>> GetEntriesAsync(int? campaignId = null, string? statusFilter = null)
    {
        if (campaignId.HasValue)
        {
            RepositoryGuard.Positive(campaignId.Value, nameof(campaignId));
        }

        if (statusFilter is not null)
        {
            RepositoryGuard.AllowedStatus(statusFilter, nameof(statusFilter), PublicEntryStatuses);
        }

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
SELECT
    LedgerEntryId,
    EntryType,
    CampaignId,
    CampaignSlug,
    CampaignTitle,
    EntityId,
    Reference,
    PartyName,
    Description,
    Amount,
    Direction,
    ISNULL(OccurredAtUtc, CAST('1900-01-01' AS datetime2(7))) AS OccurredAtUtc,
    Status
FROM dbo.vw_PublicLedgerEntries
WHERE (@CampaignId IS NULL OR CampaignId = @CampaignId)
  AND (@StatusFilter IS NULL OR Status = @StatusFilter)
ORDER BY OccurredAtUtc DESC, LedgerEntryId DESC;";

        var rows = await connection.QueryAsync<LedgerEntryEntity>(sql, new
        {
            CampaignId = campaignId,
            StatusFilter = statusFilter
        });
        return rows.AsList();
    }

    public async Task<CampaignBalanceEntity?> GetCampaignBalanceAsync(int campaignId)
    {
        RepositoryGuard.Positive(campaignId, nameof(campaignId));
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = "SELECT * FROM dbo.vw_CampaignFinancialSummary WHERE CampaignId = @CampaignId;";
        return await connection.QuerySingleOrDefaultAsync<CampaignBalanceEntity>(sql, new { CampaignId = campaignId });
    }

    public async Task<IReadOnlyList<PendingApprovalEntity>> GetTreasurerQueueAsync()
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = "SELECT * FROM dbo.vw_PendingApprovals ORDER BY SubmittedAtUtc, ExpenseId;";
        var rows = await connection.QueryAsync<PendingApprovalEntity>(sql);
        return rows.AsList();
    }

    public async Task<IReadOnlyList<FundSummaryEntity>> GetInstitutionalFundsAsync()
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = "SELECT * FROM dbo.vw_FundSummary ORDER BY FundKey;";
        var rows = await connection.QueryAsync<FundSummaryEntity>(sql);
        return rows.AsList();
    }

    public async Task<IReadOnlyList<ReceiptMetadataEntity>> GetReceiptsByCampaignSlugAsync(string slug)
    {
        RepositoryGuard.RequiredText(slug, nameof(slug), 200);
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
SELECT r.*
FROM dbo.ReceiptMetadata r
LEFT JOIN dbo.Expenses e ON e.ExpenseId = r.ExpenseId
LEFT JOIN dbo.Contributions c ON c.ContributionId = r.ContributionId
LEFT JOIN dbo.Campaigns cam ON cam.CampaignId = COALESCE(e.CampaignId, c.CampaignId)
WHERE cam.Slug = @Slug
  AND ((e.ExpenseId IS NOT NULL AND e.Status = 'Approved')
    OR (c.ContributionId IS NOT NULL AND c.Status = 'Verified')
    OR (e.ExpenseId IS NULL AND c.ContributionId IS NULL))
ORDER BY r.IssuedOn DESC, r.ReceiptId DESC;";

        var rows = await connection.QueryAsync<ReceiptMetadataEntity>(sql, new { Slug = slug });
        return rows.AsList();
    }

    public async Task<ReceiptMetadataEntity?> GetReceiptForExpenseAsync(long expenseId)
    {
        RepositoryGuard.Positive(expenseId, nameof(expenseId));
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = "SELECT TOP (1) * FROM dbo.ReceiptMetadata WHERE ExpenseId = @ExpenseId ORDER BY ReceiptId;";
        return await connection.QuerySingleOrDefaultAsync<ReceiptMetadataEntity>(sql, new { ExpenseId = expenseId });
    }
}
