namespace Amanat.Models.Entities;

public sealed class PublicSummaryEntity
{
    public decimal TotalVerifiedContributions { get; set; }
    public decimal TotalApprovedExpenses { get; set; }
    public decimal Balance { get; set; }
    public int VerifiedContributionCount { get; set; }
    public int ApprovedExpenseCount { get; set; }
    public int CampaignCount { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class CampaignBalanceEntity
{
    public int CampaignId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal Goal { get; set; }
    public decimal VerifiedContributions { get; set; }
    public decimal ApprovedExpenses { get; set; }
    public decimal Balance { get; set; }
    public int VerifiedContributionCount { get; set; }
    public int ApprovedExpenseCount { get; set; }
}

public sealed class LedgerEntryEntity
{
    public string LedgerEntryId { get; set; } = string.Empty;
    public string EntryType { get; set; } = string.Empty;
    public int CampaignId { get; set; }
    public string CampaignSlug { get; set; } = string.Empty;
    public string CampaignTitle { get; set; } = string.Empty;
    public long EntityId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string PartyName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Direction { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class PendingApprovalEntity
{
    public long ExpenseId { get; set; }
    public int CampaignId { get; set; }
    public string CampaignSlug { get; set; } = string.Empty;
    public string CampaignTitle { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string PayeeName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime SubmittedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public sealed class FundSummaryEntity
{
    public string FundKey { get; set; } = string.Empty;
    public int CampaignCount { get; set; }
    public decimal VerifiedContributions { get; set; }
    public decimal ApprovedExpenses { get; set; }
    public decimal Balance { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
