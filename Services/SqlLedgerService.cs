using Amanat.Models;
using Amanat.Repositories.Interfaces;

namespace Amanat.Services;

public sealed class SqlLedgerService : ILedgerService
{
    private const string UnavailableMessage = "Ledger information is temporarily unavailable. Please try again.";
    private readonly ILedgerRepository _ledger;
    private string _digest = "pending...";

    public SqlLedgerService(ILedgerRepository ledger)
    {
        _ledger = ledger;
    }

    public string PublicSealDigest => _digest;

    public Task<IReadOnlyList<LedgerFund>> GetInstitutionalFundsAsync() => SqlServiceGuard.RunAsync(async () =>
    {
        var summary = await _ledger.GetPublicSummaryAsync();
        _digest = DomainMapper.PublicDigest(summary);
        var funds = await _ledger.GetInstitutionalFundsAsync();
        return (IReadOnlyList<LedgerFund>)funds.Select(fund => DomainMapper.ToFund(fund, _digest)).ToList();
    }, UnavailableMessage);

    public Task<IReadOnlyList<CashbookEntry>> GetCashbookEntriesAsync() => SqlServiceGuard.RunAsync(async () =>
    {
        var entries = await _ledger.GetEntriesAsync();
        return (IReadOnlyList<CashbookEntry>)entries.Select(DomainMapper.ToCashbook).ToList();
    }, UnavailableMessage);

    public Task<IReadOnlyList<PendingApproval>> GetPendingApprovalsAsync() => SqlServiceGuard.RunAsync(async () =>
    {
        var pending = await _ledger.GetTreasurerQueueAsync();
        var result = new List<PendingApproval>(pending.Count);
        foreach (var item in pending)
        {
            var receipt = await _ledger.GetReceiptForExpenseAsync(item.ExpenseId);
            result.Add(DomainMapper.ToPendingApproval(item, receipt));
        }

        return (IReadOnlyList<PendingApproval>)result;
    }, UnavailableMessage);
}
