using Amanat.Models;
using Amanat.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Amanat.Controllers;

[Authorize(Roles = AmanatRoles.Organizer)]
public sealed class OrganizerController : Controller
{
    private readonly IDashboardService _dashboards;
    private readonly UserManager<ApplicationUser> _userManager;

    public OrganizerController(IDashboardService dashboards, UserManager<ApplicationUser> userManager)
    {
        _dashboards = dashboards;
        _userManager = userManager;
    }

    public async Task<IActionResult> Dashboard()
    {
        ViewData["Title"] = "Amanat - Organizer Dashboard";
        return View(await _dashboards.GetOrganizerAsync(await CurrentUserAsync()));
    }

    private async Task<ApplicationUser> CurrentUserAsync() =>
        await _userManager.GetUserAsync(User) ?? new ApplicationUser { FullName = User.Identity?.Name ?? string.Empty };
}
