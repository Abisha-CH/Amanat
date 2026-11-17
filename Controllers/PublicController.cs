using Amanat.Services;
using Amanat.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Amanat.Controllers;

public sealed class PublicController : Controller
{
    private readonly ILedgerService _ledger;

    public PublicController(ILedgerService ledger)
    {
        _ledger = ledger;
    }

    public async Task<IActionResult> Ledger()
    {
        ViewData["Title"] = "Institutional Ledger | AMANAT (امانت) - Community Fund Transparency";
        ViewData["ActiveNav"] = "Ledger";
        ViewData["FooterVariant"] = "Campaigns";

        var path = Url.Action("Ledger", "Public") ?? "/Public/Ledger";
        return View(new PublicLedgerViewModel
        {
            Funds = await _ledger.GetInstitutionalFundsAsync(),
            Digest = _ledger.PublicSealDigest,
            LedgerLink = $"{Request.Scheme}://{Request.Host}{path}"
        });
    }
}
