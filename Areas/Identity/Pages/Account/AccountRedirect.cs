using Amanat.Models;
using Microsoft.AspNetCore.Identity;

namespace Amanat.Areas.Identity.Pages.Account;

internal static class AccountRedirect
{
    public static async Task<string> DashboardPathAsync(UserManager<ApplicationUser> userManager, ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);

        if (roles.Contains(AmanatRoles.Treasurer))
        {
            return "/Treasurer/Dashboard";
        }

        if (roles.Contains(AmanatRoles.Organizer))
        {
            return "/Organizer/Dashboard";
        }

        return "/Contributor/Dashboard";
    }
}
