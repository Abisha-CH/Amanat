using Amanat.Models;
using Amanat.Models.Entities;

namespace Amanat.ViewModels;

public sealed class PublicLedgerViewModel
{
    public IReadOnlyList<LedgerFund> Funds { get; init; } = [];
    public PublicSummaryEntity Summary { get; init; } = new();
    public int ReceiptCoveragePercent { get; init; }
    public string LedgerLink { get; init; } = string.Empty;
    public string Digest { get; init; } = string.Empty;
}

public sealed class TreasurerLedgerViewModel
{
    public IReadOnlyList<PendingApproval> PendingApprovals { get; init; } = [];
    public IReadOnlyList<CashbookEntry> Entries { get; init; } = [];
    public PublicSummaryEntity Summary { get; init; } = new();
    public int PendingCount { get; init; }
    public decimal PendingTotal { get; init; }
}
