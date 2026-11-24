using Amanat.Models;
using Amanat.Models.Entities;
using Amanat.Repositories.Interfaces;
using Amanat.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Amanat.Controllers;

[Authorize(Roles = AmanatRoles.Contributor)]
public sealed class ContributorController : JsonControllerBase
{
    private readonly IDashboardService _dashboards;
    private readonly ICampaignRepository _campaignRepository;
    private readonly IDomainMutationService _mutations;
    private readonly UserManager<ApplicationUser> _userManager;

    public ContributorController(
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

        ViewData["Title"] = "Amanat - Contributor Dashboard";
        return View(await _dashboards.GetContributorAsync(user));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordReference(PaymentReferenceInput input)
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

            DateTime? dateSent = DateTime.TryParse(input.DateSent, out var parsedDate) ? parsedDate : null;
            await _mutations.SubmitContributionAsync(new ContributionEntity
            {
                CampaignId = campaign.CampaignId,
                FullName = string.IsNullOrWhiteSpace(user.FullName) ? user.UserName ?? "Contributor" : user.FullName,
                Amount = input.Amount,
                TransactionRef = input.TransactionId.Trim(),
                DateSent = dateSent,
                Notes = input.Notes
            }, user);

            TempData["ReferenceMessage"] = $"Transaction {input.TransactionId} submitted for treasurer verification.";
            return RedirectToAction("Dashboard");
        }
        catch (Exception exception) when (exception is DomainValidationException or DomainUnavailableException or DomainUnauthorizedException)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return await RenderDashboardAsync(user, input);
        }
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordReferenceJson([FromBody] PaymentReferenceInput input)
    {
        var authorization = JsonAuthorization(AmanatRoles.Contributor);
        if (authorization is not null)
        {
            return authorization;
        }

        var user = await JsonUserAsync(_userManager);
        if (user is null)
        {
            return JsonFailure(401, "Please sign in to continue.");
        }

        if (!ModelState.IsValid)
        {
            return InvalidModelState();
        }

        var campaign = (await _campaignRepository.GetAllAsync(false))
            .FirstOrDefault(c => c.Title.Equals(input.Campaign, StringComparison.OrdinalIgnoreCase)
                || c.Slug.Equals(input.Campaign, StringComparison.OrdinalIgnoreCase));
        if (campaign is null)
        {
            return JsonFailure(400, "Select a valid campaign.", new Dictionary<string, string[]>
            {
                [nameof(input.Campaign)] = ["Select a valid campaign."]
            });
        }

        DateTime? dateSent = DateTime.TryParse(input.DateSent, out var parsedDate) ? parsedDate : null;
        try
        {
            var contributionId = await _mutations.SubmitContributionAsync(new ContributionEntity
            {
                CampaignId = campaign.CampaignId,
                FullName = string.IsNullOrWhiteSpace(user.FullName) ? user.UserName ?? "Contributor" : user.FullName,
                Amount = input.Amount,
                TransactionRef = input.TransactionId.Trim(),
                DateSent = dateSent,
                Notes = input.Notes
            }, user);

            return JsonSuccess($"Transaction {input.TransactionId} submitted for treasurer verification.", new ReferenceSubmissionJson
            {
                ContributionId = contributionId,
                Reference = input.TransactionId.Trim(),
                AmountText = Campaign.AmountText(input.Amount),
                DateText = (dateSent ?? DateTime.UtcNow).ToString("MMM dd, yyyy", System.Globalization.CultureInfo.InvariantCulture),
                Campaign = campaign.Title,
                Status = "Under Review"
            });
        }
        catch (Exception exception) when (exception is DomainValidationException or DomainUnavailableException or DomainUnauthorizedException)
        {
            return DomainFailure(exception, nameof(input.TransactionId));
        }
    }

    private async Task<IActionResult> RenderDashboardAsync(ApplicationUser user, PaymentReferenceInput? input)
    {
        var model = await _dashboards.GetContributorAsync(user);
        if (input is not null)
        {
            model.Reference = input;
        }

        ViewData["Title"] = "Amanat - Contributor Dashboard";
        return View("Dashboard", model);
    }

    private async Task<ApplicationUser?> CurrentUserAsync() => await _userManager.GetUserAsync(User);
}
