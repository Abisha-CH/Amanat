using Amanat.Models.Entities;

namespace Amanat.Repositories.Interfaces;

public interface ILedgerRepository
{
    Task<PublicSummaryEntity> GetPublicSummaryAsync(int? campaignId = null);
    Task<IReadOnlyList<LedgerEntryEntity>> GetEntriesAsync(int? campaignId = null, string? statusFilter = null);
    Task<CampaignBalanceEntity?> GetCampaignBalanceAsync(int campaignId);
    Task<IReadOnlyList<PendingApprovalEntity>> GetTreasurerQueueAsync();
    Task<IReadOnlyList<FundSummaryEntity>> GetInstitutionalFundsAsync();
    Task<IReadOnlyList<ReceiptMetadataEntity>> GetReceiptsByCampaignSlugAsync(string slug);
    Task<ReceiptMetadataEntity?> GetReceiptForExpenseAsync(long expenseId);
}
