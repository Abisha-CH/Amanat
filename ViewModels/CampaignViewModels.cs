using Amanat.Models;

namespace Amanat.ViewModels;

public sealed class CampaignDetailsViewModel
{
    public required Campaign Campaign { get; init; }
    public IReadOnlyList<Receipt> Receipts { get; init; } = [];
    public IReadOnlyList<CampaignUpdateView> Updates { get; init; } = [];
}
