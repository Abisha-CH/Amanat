using Amanat.Models;
using Amanat.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Amanat.Controllers;

[Authorize(Roles = AmanatRoles.Contributor)]
public sealed class ContributorController : Controller
{
    private readonly IDashboardService _dashboards;
    private readonly UserManager<ApplicationUser> _userManager;

    public ContributorController(IDashboardService dashboards, UserManager<ApplicationUser> userManager)
    {
        _dashboards = dashboards;
        _userManager = userManager;
    }

    public async Task<IActionResult> Dashboard()
    {
        ViewData["Title"] = "Amanat - Contributor Dashboard";
        return View(await _dashboards.GetContributorAsync(await CurrentUserAsync()));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordReference(PaymentReferenceInput input)
    {
        var user = await CurrentUserAsync();

        if (!ModelState.IsValid)
        {
            var model = await _dashboards.GetContributorAsync(user);
            model.Reference = input;
            ViewData["Title"] = "Amanat - Contributor Dashboard";
            return View("Dashboard", model);
        }

        TempData["ReferenceMessage"] = $"Transaction {input.TransactionId} submitted for treasurer verification.";
        return RedirectToAction("Dashboard");
    }

    private async Task<ApplicationUser> CurrentUserAsync() =>
        await _userManager.GetUserAsync(User) ?? new ApplicationUser { FullName = User.Identity?.Name ?? string.Empty };
}
