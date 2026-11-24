using Amanat.Models;
using Amanat.Models.Entities;
using Amanat.Repositories.Interfaces;
using Amanat.Services;
using Amanat.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Amanat.Controllers;

[Authorize(Roles = AmanatRoles.Treasurer)]
public sealed class TreasurerController : JsonControllerBase
{
    private readonly IDashboardService _dashboards;
    private readonly ILedgerService _ledger;
    private readonly ILedgerRepository _ledgerRepository;
    private readonly ICampaignRepository _campaignRepository;
    private readonly IDomainMutationService _mutations;
    private readonly UserManager<ApplicationUser> _userManager;

    public TreasurerController(
        IDashboardService dashboards,
        ILedgerService ledger,
        ILedgerRepository ledgerRepository,
        ICampaignRepository campaignRepository,
        IDomainMutationService mutations,
        UserManager<ApplicationUser> userManager)
    {
        _dashboards = dashboards;
        _ledger = ledger;
        _ledgerRepository = ledgerRepository;
        _campaignRepository = campaignRepository;
        _mutations = mutations;
        _userManager = userManager;
    }

    public async Task<IActionResult> Dashboard()
    {
        var user = await CurrentUserAsync();
        if (user is null)
        {
            return Challenge();
        }

        ViewData["Title"] = "Amanat - Treasurer Dashboard";
        return View(await _dashboards.GetTreasurerAsync(user));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitExpense(ExpenseInput input)
    {
        var user = await CurrentUserAsync();
        if (user is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            return await RenderDashboardAsync(user, input);
        }

        try
        {
            var campaign = (await _campaignRepository.GetAllAsync(false))
                .FirstOrDefault(c => c.Title.Equals(input.Campaign, StringComparison.OrdinalIgnoreCase)
                    || c.Slug.Equals(input.Campaign, StringComparison.OrdinalIgnoreCase));
            if (campaign is null)
            {
                ModelState.AddModelError(nameof(input.Campaign), "Select a valid campaign.");
                return await RenderDashboardAsync(user, input);
            }

            await _mutations.SubmitExpenseAsync(new ExpenseEntity
            {
                CampaignId = campaign.CampaignId,
                Reference = "EXP-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"),
                PayeeName = input.PayeeName.Trim(),
                Amount = input.Amount,
                Description = input.Description.Trim()
            }, user);

            TempData["ExpenseMessage"] = $"Expense of PKR {input.Amount:N0} for {input.PayeeName} submitted for organizer approval.";
            return RedirectToAction("Dashboard");
        }
        catch (Exception exception) when (exception is DomainValidationException or DomainUnavailableException or DomainUnauthorizedException)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return await RenderDashboardAsync(user, input);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyContribution(long contributionId, string rowVersion, string? note)
    {
        return await DecideContributionAsync(contributionId, "Verified", rowVersion, note);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectContribution(long contributionId, string rowVersion, string? note)
    {
        return await DecideContributionAsync(contributionId, "Rejected", rowVersion, note);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DecideContributionJson([FromBody] DecisionJsonRequest request)
    {
        var authorization = JsonAuthorization(AmanatRoles.Treasurer);
        if (authorization is not null)
        {
            return authorization;
        }

        var user = await JsonUserAsync(_userManager);
        if (user is null)
        {
            return JsonFailure(401, "Please sign in to continue.");
        }

        if (request.ContributionId <= 0 || string.IsNullOrWhiteSpace(request.RowVersion)
            || request.Decision is not ("Verified" or "Rejected"))
        {
            return JsonFailure(400, "The contribution decision could not be read.");
        }

        try
        {
            await _mutations.DecideContributionAsync(
                request.ContributionId,
                request.Decision,
                request.Note,
                request.RowVersion,
                user);

            return JsonSuccess(
                request.Decision == "Verified"
                    ? "Contribution verified and added to the public ledger."
                    : "Contribution rejected and recorded in the audit trail.",
                new DecisionJson
                {
                    RecordId = request.ContributionId,
                    Status = request.Decision,
                    StatusClass = request.Decision == "Verified" ? "badge-verified" : "badge-danger"
                });
        }
        catch (Exception exception) when (exception is DomainValidationException or DomainUnavailableException or DomainUnauthorizedException)
        {
            return DomainFailure(exception);
        }
    }

    public async Task<IActionResult> Ledger()
    {
        ViewData["Title"] = "Amanat - Treasurer Cashbook & Disbursement Ledger";
        var approvals = await _ledger.GetPendingApprovalsAsync();
        var queue = await _ledgerRepository.GetTreasurerQueueAsync();
        return View(new TreasurerLedgerViewModel
        {
            PendingApprovals = approvals,
            Entries = await _ledger.GetCashbookEntriesAsync(),
            Summary = await _ledgerRepository.GetPublicSummaryAsync(),
            PendingCount = queue.Count,
            PendingTotal = queue.Sum(item => item.Amount)
        });
    }

    private async Task<IActionResult> DecideContributionAsync(long contributionId, string decision, string rowVersion, string? note)
    {
        var user = await CurrentUserAsync();
        if (user is null)
        {
            return Challenge();
        }

        try
        {
            await _mutations.DecideContributionAsync(contributionId, decision, note, rowVersion, user);
            TempData["VerificationMessage"] = decision == "Verified"
                ? "Contribution verified and added to the public ledger."
                : "Contribution rejected and recorded in the audit trail.";
            return RedirectToAction("Dashboard");
        }
        catch (Exception exception) when (exception is DomainValidationException or DomainUnavailableException or DomainUnauthorizedException)
        {
            TempData["VerificationError"] = exception.Message;
            return RedirectToAction("Dashboard");
        }
    }

    private async Task<IActionResult> RenderDashboardAsync(ApplicationUser user, ExpenseInput? input)
    {
        var model = await _dashboards.GetTreasurerAsync(user);
        if (input is not null)
        {
            model.Expense = input;
        }

        ViewData["Title"] = "Amanat - Treasurer Dashboard";
        return View("Dashboard", model);
    }

    private async Task<ApplicationUser?> CurrentUserAsync() => await _userManager.GetUserAsync(User);
}
