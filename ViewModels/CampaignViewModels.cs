using Amanat.Models;

namespace Amanat.ViewModels;

public sealed class CampaignListViewModel
{
    public IReadOnlyList<Campaign> Campaigns { get; init; } = [];
}

public sealed class CampaignDetailsViewModel
{
    public required Campaign Campaign { get; init; }
    public IReadOnlyList<Receipt> Receipts { get; init; } = [];
}
