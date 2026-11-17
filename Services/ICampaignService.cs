using Amanat.Models;

namespace Amanat.Services;

public interface ICampaignService
{
    Task<IReadOnlyList<Campaign>> GetAllAsync();
    Task<Campaign?> GetBySlugAsync(string slug);
    Task<IReadOnlyList<Receipt>> GetReceiptsAsync(string slug);
}
