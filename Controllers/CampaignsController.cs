using Amanat.Models;
using Amanat.Services;
using Amanat.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Amanat.Controllers;

public sealed class CampaignsController : Controller
{
    private readonly ICampaignService _campaigns;

    public CampaignsController(ICampaignService campaigns)
    {
        _campaigns = campaigns;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Explore Campaigns | AMANAT (امانت) - Community Fund Transparency";
        ViewData["ActiveNav"] = "Campaigns";
        ViewData["FooterVariant"] = "Campaigns";
        return View(await _campaigns.GetAllAsync());
    }

    public async Task<IActionResult> Details(string id)
    {
        var campaign = await _campaigns.GetBySlugAsync(id);
        if (campaign is null)
        {
            return NotFound();
        }

        ViewData["Title"] = $"{campaign.Title} - AMANAT (امانت) Campaign Details";
        ViewData["ActiveNav"] = "Campaigns";
        ViewData["FooterVariant"] = "Details";
        return View(new CampaignDetailsViewModel
        {
            Campaign = campaign,
            Receipts = await _campaigns.GetReceiptsAsync(campaign.Slug)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Details(PledgeInput input)
    {
        if (!ModelState.IsValid)
        {
            var campaign = await _campaigns.GetBySlugAsync(input.CampaignSlug);
            if (campaign is null)
            {
                return NotFound();
            }

            ViewData["Title"] = $"{campaign.Title} - AMANAT (امانت) Campaign Details";
            ViewData["ActiveNav"] = "Campaigns";
            ViewData["FooterVariant"] = "Details";
            return View(new CampaignDetailsViewModel
            {
                Campaign = campaign,
                Receipts = await _campaigns.GetReceiptsAsync(campaign.Slug)
            });
        }

        TempData["PledgeReceipt"] = $"Pledge of PKR {input.Amount:N0} recorded for audit. Reference: {input.TransactionRef}.";
        var detailsUrl = Url.Action("Details", new { id = input.CampaignSlug });
        return Redirect((detailsUrl ?? "/Campaigns/Index") + "#pledge-section");
    }
}
