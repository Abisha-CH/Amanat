using Amanat.Models.Entities;

namespace Amanat.Repositories.Interfaces;

public interface ICampaignRepository
{
    Task<IReadOnlyList<CampaignEntity>> GetAllAsync(bool activeOnly = true);
    Task<CampaignEntity?> GetBySlugAsync(string slug);
    Task<CampaignEntity?> GetByIdAsync(int campaignId);
    Task<int> CreateAsync(CampaignEntity campaign);
    Task<bool> UpdateAsync(CampaignEntity campaign, byte[] expectedRowVersion);
    Task<bool> ArchiveAsync(int campaignId, byte[] expectedRowVersion);
    Task<IReadOnlyList<CampaignUpdateEntity>> GetUpdatesAsync(int campaignId, bool publicOnly);
    Task<int> AddUpdateAsync(CampaignUpdateEntity update);
}
