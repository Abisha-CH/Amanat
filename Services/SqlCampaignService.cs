using Amanat.Models;
using Amanat.Repositories.Interfaces;

namespace Amanat.Services;

public sealed class SqlCampaignService : ICampaignService
{
    private const string UnavailableMessage = "Campaign information is temporarily unavailable. Please try again.";
    private readonly ICampaignRepository _campaigns;
    private readonly ILedgerRepository _ledger;

    public SqlCampaignService(ICampaignRepository campaigns, ILedgerRepository ledger)
    {
        _campaigns = campaigns;
        _ledger = ledger;
    }

    public Task<IReadOnlyList<Campaign>> GetAllAsync() => SqlServiceGuard.RunAsync(async () =>
    {
        var entities = await _campaigns.GetAllAsync(activeOnly: false);
        var result = new List<Campaign>(entities.Count);
        foreach (var entity in entities)
        {
            var balance = await _ledger.GetCampaignBalanceAsync(entity.CampaignId);
            result.Add(DomainMapper.ToCampaign(entity, balance));
        }

        return (IReadOnlyList<Campaign>)result;
    }, UnavailableMessage);

    public Task<Campaign?> GetBySlugAsync(string slug) => SqlServiceGuard.RunAsync(async () =>
    {
        var entity = await _campaigns.GetBySlugAsync(slug);
        if (entity is null)
        {
            return null;
        }

        var balance = await _ledger.GetCampaignBalanceAsync(entity.CampaignId);
        return DomainMapper.ToCampaign(entity, balance);
    }, UnavailableMessage);

    public Task<IReadOnlyList<Receipt>> GetReceiptsAsync(string slug) => SqlServiceGuard.RunAsync(async () =>
    {
        var receipts = await _ledger.GetReceiptsByCampaignSlugAsync(slug);
        return (IReadOnlyList<Receipt>)receipts.Select(DomainMapper.ToReceipt).ToList();
    }, UnavailableMessage);
}
