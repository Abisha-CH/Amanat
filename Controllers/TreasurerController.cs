using Amanat.Models;
using Amanat.Services;
using Amanat.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Amanat.Controllers;

[Authorize(Roles = AmanatRoles.Treasurer)]
public sealed class TreasurerController : Controller
{
    private readonly IDashboardService _dashboards;
    private readonly ILedgerService _ledger;
    private readonly UserManager<ApplicationUser> _userManager;

    public TreasurerController(IDashboardService dashboards, ILedgerService ledger, UserManager<ApplicationUser> userManager)
    {
        _dashboards = dashboards;
        _ledger = ledger;
        _userManager = userManager;
    }

    public async Task<IActionResult> Dashboard()
    {
        ViewData["Title"] = "Amanat - Treasurer Dashboard";
        return View(await _dashboards.GetTreasurerAsync(await CurrentUserAsync()));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitExpense(ExpenseInput input)
    {
        var user = await CurrentUserAsync();

        if (!ModelState.IsValid)
        {
            var model = await _dashboards.GetTreasurerAsync(user);
            model.Expense = input;
            ViewData["Title"] = "Amanat - Treasurer Dashboard";
            return View("Dashboard", model);
        }

        TempData["ExpenseMessage"] = $"Expense of PKR {input.Amount:N0} for {input.PayeeName} submitted for organizer approval.";
        return RedirectToAction("Dashboard");
    }

    public async Task<IActionResult> Ledger()
    {
        ViewData["Title"] = "Amanat - Treasurer Cashbook & Disbursement Ledger";
        return View(new TreasurerLedgerViewModel
        {
            PendingApprovals = await _ledger.GetPendingApprovalsAsync(),
            Entries = await _ledger.GetCashbookEntriesAsync()
        });
    }

    private async Task<ApplicationUser> CurrentUserAsync() =>
        await _userManager.GetUserAsync(User) ?? new ApplicationUser { FullName = User.Identity?.Name ?? string.Empty };
}
