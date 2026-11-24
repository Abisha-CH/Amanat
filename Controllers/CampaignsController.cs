using Amanat.Models;
using Amanat.Models.Entities;
using Amanat.Repositories.Interfaces;
using Amanat.Services;
using Amanat.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Amanat.Controllers;

public sealed class CampaignsController : JsonControllerBase
{
    private readonly ICampaignService _campaigns;
    private readonly ICampaignRepository _campaignRepository;
    private readonly IDomainMutationService _mutations;
    private readonly UserManager<ApplicationUser> _userManager;

    public CampaignsController(
        ICampaignService campaigns,
        ICampaignRepository campaignRepository,
        IDomainMutationService mutations,
        UserManager<ApplicationUser> userManager)
    {
        _campaigns = campaigns;
        _campaignRepository = campaignRepository;
        _mutations = mutations;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Explore Campaigns | AMANAT (امانت) - Community Fund Transparency";
        ViewData["ActiveNav"] = "Campaigns";
        ViewData["FooterVariant"] = "Campaigns";
        return View(await _campaigns.GetAllAsync());
    }

    public async Task<IActionResult> Details(string id) => await RenderDetailsAsync(id, null);

    [HttpGet]
    public async Task<IActionResult> Summary(string id)
    {
        var campaign = await _campaigns.GetBySlugAsync(id);
        if (campaign is null)
        {
            return JsonFailure(404, "The campaign was not found.");
        }

        return JsonSuccess("Campaign summary loaded.", new CampaignSummaryJson
        {
            Slug = campaign.Slug,
            Title = campaign.Title,
            Status = campaign.Status,
            Raised = campaign.Raised,
            Goal = campaign.Goal,
            Balance = campaign.Balance,
            ApprovedExpenses = campaign.ApprovedExpenses,
            ProgressPercent = campaign.ProgressPercent,
            Contributors = campaign.Contributors,
            ApprovedCount = campaign.ApprovedCount,
            RaisedText = campaign.RaisedText,
            GoalText = campaign.GoalText,
            BalanceText = Campaign.AmountText(campaign.Balance),
            ApprovedExpensesText = Campaign.AmountText(campaign.ApprovedExpenses),
            RemainingText = campaign.Goal > campaign.Raised
                ? Campaign.AmountText(campaign.Goal - campaign.Raised)
                : Campaign.AmountText(0m),
            ContributorsText = campaign.Contributors + " verified backers",
            ApprovedCountText = campaign.ApprovedCount + " audited invoices"
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Details(PledgeInput input)
    {
        var campaign = await _campaigns.GetBySlugAsync(input.CampaignSlug);
        if (campaign is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return await RenderDetailsAsync(campaign.Slug, input);
        }

        try
        {
            var campaignEntity = await _campaignRepository.GetBySlugAsync(campaign.Slug)
                ?? throw new DomainValidationException("Campaign not found.");
            await _mutations.SubmitContributionAsync(new ContributionEntity
            {
                CampaignId = campaignEntity.CampaignId,
                FullName = input.FullName.Trim(),
                Amount = input.Amount,
                TransactionRef = input.TransactionRef.Trim()
            }, await _userManager.GetUserAsync(User));

            TempData["PledgeReceipt"] = $"Pledge of PKR {input.Amount:N0} recorded for audit. Reference: {input.TransactionRef}.";
            var detailsUrl = Url.Action("Details", new { id = input.CampaignSlug });
            return Redirect((detailsUrl ?? "/Campaigns/Index") + "#pledge-section");
        }
        catch (Exception exception) when (exception is DomainValidationException or DomainUnavailableException or DomainUnauthorizedException)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return await RenderDetailsAsync(campaign.Slug, input);
        }
    }

    private async Task<IActionResult> RenderDetailsAsync(string slug, PledgeInput? input)
    {
        var campaign = await _campaigns.GetBySlugAsync(slug);
        if (campaign is null)
        {
            return NotFound();
        }

        var entity = await _campaignRepository.GetBySlugAsync(campaign.Slug);
        var updates = entity is null
            ? []
            : (await _campaignRepository.GetUpdatesAsync(entity.CampaignId, publicOnly: true)).Select(DomainMapper.ToCampaignUpdate).ToList();

        ViewData["Title"] = $"{campaign.Title} - AMANAT (امانت) Campaign Details";
        ViewData["ActiveNav"] = "Campaigns";
        ViewData["FooterVariant"] = "Details";
        if (input is not null)
        {
            ViewData["PledgeInput"] = input;
        }

        return View(new CampaignDetailsViewModel
        {
            Campaign = campaign,
            Receipts = await _campaigns.GetReceiptsAsync(campaign.Slug),
            Updates = updates
        });
    }
}
