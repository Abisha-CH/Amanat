using Amanat.Models;
using Amanat.Models.Entities;

namespace Amanat.Services;

public interface IDomainMutationService
{
    Task<int> CreateCampaignAsync(CampaignCreateInput input, ApplicationUser organizer);
    Task<int> AddCampaignUpdateAsync(CampaignUpdateInput input, ApplicationUser organizer);
    Task ArchiveCampaignAsync(int campaignId, string rowVersion, ApplicationUser organizer);
    Task<long> SubmitContributionAsync(ContributionEntity contribution, ApplicationUser? contributor);
    Task DecideContributionAsync(long contributionId, string decision, string? note, string rowVersion, ApplicationUser reviewer);
    Task<long> SubmitExpenseAsync(ExpenseEntity expense, ApplicationUser treasurer);
    Task DecideExpenseAsync(long expenseId, string decision, string? note, string rowVersion, ApplicationUser organizer);
}
