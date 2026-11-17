using Amanat.Models;

namespace Amanat.Services;

public interface ILedgerService
{
    Task<IReadOnlyList<LedgerFund>> GetInstitutionalFundsAsync();
    Task<IReadOnlyList<CashbookEntry>> GetCashbookEntriesAsync();
    Task<IReadOnlyList<PendingApproval>> GetPendingApprovalsAsync();
    string PublicSealDigest { get; }
}
