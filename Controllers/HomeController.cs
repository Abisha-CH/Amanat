using Amanat.Services;
using Microsoft.AspNetCore.Mvc;

namespace Amanat.Controllers;

public sealed class HomeController : Controller
{
    private readonly ICampaignService _campaigns;

    public HomeController(ICampaignService campaigns)
    {
        _campaigns = campaigns;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "AMANAT | Community Fund Transparency Platform";
        ViewData["ActiveNav"] = "Home";
        ViewData["FooterVariant"] = "Home";
        return View(await _campaigns.GetAllAsync());
    }

    public IActionResult About()
    {
        ViewData["Title"] = "About Us | AMANAT - Community Fund Transparency";
        ViewData["ActiveNav"] = "About";
        ViewData["FooterVariant"] = "About";
        return View();
    }

    public IActionResult AccessDenied()
    {
        ViewData["Title"] = "Access Denied | AMANAT";
        return View();
    }

    public IActionResult Error()
    {
        ViewData["Title"] = "Error | AMANAT";
        return View();
    }
}
