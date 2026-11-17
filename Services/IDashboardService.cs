using Amanat.Models;
using Amanat.ViewModels;

namespace Amanat.Services;

public interface IDashboardService
{
    Task<ContributorDashboardViewModel> GetContributorAsync(ApplicationUser user);
    Task<OrganizerDashboardViewModel> GetOrganizerAsync(ApplicationUser user);
    Task<TreasurerDashboardViewModel> GetTreasurerAsync(ApplicationUser user);
}
