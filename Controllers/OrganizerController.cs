using Amanat.Models;
using Amanat.Repositories.Interfaces;
using Amanat.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Amanat.Controllers;

[Authorize(Roles = AmanatRoles.Organizer)]
public sealed class OrganizerController : JsonControllerBase
{
    private readonly IDashboardService _dashboards;
    private readonly ICampaignRepository _campaignRepository;
    private readonly IDomainMutationService _mutations;
    private readonly UserManager<ApplicationUser> _userManager;

    public OrganizerController(
        IDashboardService dashboards,
        ICampaignRepository campaignRepository,
        IDomainMutationService mutations,
        UserManager<ApplicationUser> userManager)
    {
        _dashboards = dashboards;
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

        ViewData["Title"] = "Amanat - Organizer Dashboard";
        return View(await _dashboards.GetOrganizerAsync(user));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCampaign(CampaignCreateInput input)
    {
        var user = await CurrentUserAsync();
        if (user is null) return Challenge();

        if (!ModelState.IsValid)
        {
            return await RenderDashboardAsync(user, model => model.NewCampaign = input);
        }

        try
        {
            await _mutations.CreateCampaignAsync(input, user);
            TempData["CampaignMessage"] = $"Campaign '{input.Title}' created and ready for updates.";
            return RedirectToAction("Dashboard");
        }
        catch (Exception exception) when (exception is DomainValidationException or DomainUnavailableException or DomainUnauthorizedException)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return await RenderDashboardAsync(user, model => model.NewCampaign = input);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddUpdate(CampaignUpdateInput input)
    {
        var user = await CurrentUserAsync();
        if (user is null) return Challenge();

        if (!ModelState.IsValid)
        {
            return await RenderDashboardAsync(user, model => model.NewUpdate = input);
        }

        try
        {
            await _mutations.AddCampaignUpdateAsync(input, user);
            TempData["CampaignMessage"] = "Public campaign update published.";
            return RedirectToAction("Dashboard");
        }
        catch (Exception exception) when (exception is DomainValidationException or DomainUnavailableException or DomainUnauthorizedException)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return await RenderDashboardAsync(user, model => model.NewUpdate = input);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ArchiveCampaign(int campaignId, string rowVersion)
    {
        var user = await CurrentUserAsync();
        if (user is null) return Challenge();

        try
        {
            var campaign = await _campaignRepository.GetByIdAsync(campaignId);
            if (campaign is null)
            {
                throw new DomainValidationException("The selected campaign was not found.");
            }

            var version = string.IsNullOrWhiteSpace(rowVersion)
                ? Convert.ToBase64String(campaign.RowVersion)
                : rowVersion;
            await _mutations.ArchiveCampaignAsync(campaignId, version, user);
            TempData["CampaignMessage"] = "Campaign archived. Its public ledger remains sealed.";
            return RedirectToAction("Dashboard");
        }
        catch (Exception exception) when (exception is DomainValidationException or DomainUnavailableException or DomainUnauthorizedException)
        {
            TempData["CampaignError"] = exception.Message;
            return RedirectToAction("Dashboard");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveExpense(long expenseId, string rowVersion, string? note)
    {
        return await DecideExpenseAsync(expenseId, "Approved", rowVersion, note);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectExpense(long expenseId, string rowVersion, string? note)
    {
        return await DecideExpenseAsync(expenseId, "Rejected", rowVersion, note);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DecideExpenseJson([FromBody] DecisionJsonRequest request)
    {
        var authorization = JsonAuthorization(AmanatRoles.Organizer);
        if (authorization is not null)
        {
            return authorization;
        }

        var user = await JsonUserAsync(_userManager);
        if (user is null)
        {
            return JsonFailure(401, "Please sign in to continue.");
        }

        if (request.ExpenseId <= 0 || string.IsNullOrWhiteSpace(request.RowVersion)
            || request.Decision is not ("Approved" or "Rejected"))
        {
            return JsonFailure(400, "The expense decision could not be read.");
        }

        try
        {
            await _mutations.DecideExpenseAsync(
                request.ExpenseId,
                request.Decision,
                request.Note,
                request.RowVersion,
                user);

            return JsonSuccess(
                request.Decision == "Approved"
                    ? "Expense approved and forwarded to the ledger."
                    : "Expense rejected and recorded in the audit trail.",
                new DecisionJson
                {
                    RecordId = request.ExpenseId,
                    Status = request.Decision,
                    StatusClass = request.Decision == "Approved" ? "badge-verified" : "badge-danger"
                });
        }
        catch (Exception exception) when (exception is DomainValidationException or DomainUnavailableException or DomainUnauthorizedException)
        {
            return DomainFailure(exception);
        }
    }

    private async Task<IActionResult> DecideExpenseAsync(long expenseId, string decision, string rowVersion, string? note)
    {
        var user = await CurrentUserAsync();
        if (user is null) return Challenge();

        try
        {
            await _mutations.DecideExpenseAsync(expenseId, decision, note, rowVersion, user);
            TempData["ApprovalMessage"] = decision == "Approved"
                ? "Expense approved and forwarded to the ledger."
                : "Expense rejected and recorded in the audit trail.";
            return RedirectToAction("Dashboard");
        }
        catch (Exception exception) when (exception is DomainValidationException or DomainUnavailableException or DomainUnauthorizedException)
        {
            TempData["ApprovalError"] = exception.Message;
            return RedirectToAction("Dashboard");
        }
    }

    private async Task<IActionResult> RenderDashboardAsync(ApplicationUser user, Action<ViewModels.OrganizerDashboardViewModel> adjust)
    {
        var model = await _dashboards.GetOrganizerAsync(user);
        adjust(model);
        ViewData["Title"] = "Amanat - Organizer Dashboard";
        return View("Dashboard", model);
    }

    private async Task<ApplicationUser?> CurrentUserAsync() => await _userManager.GetUserAsync(User);
}
