using Amanat.Repositories.Interfaces;
using Amanat.Services;
using Amanat.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Amanat.Controllers;

public sealed class PublicController : Controller
{
    private readonly ILedgerService _ledger;
    private readonly ILedgerRepository _ledgerRepository;

    public PublicController(ILedgerService ledger, ILedgerRepository ledgerRepository)
    {
        _ledger = ledger;
        _ledgerRepository = ledgerRepository;
    }

    public async Task<IActionResult> Ledger()
    {
        ViewData["Title"] = "Institutional Ledger | AMANAT (امانت) - Community Fund Transparency";
        ViewData["ActiveNav"] = "Ledger";
        ViewData["FooterVariant"] = "Campaigns";

        var path = Url.Action("Ledger", "Public") ?? "/Public/Ledger";
        var summary = await _ledgerRepository.GetPublicSummaryAsync();
        return View(new PublicLedgerViewModel
        {
            Funds = await _ledger.GetInstitutionalFundsAsync(),
            Summary = summary,
            ReceiptCoveragePercent = summary.ApprovedExpenseCount == 0 ? 0 : 100,
            Digest = _ledger.PublicSealDigest,
            LedgerLink = $"{Request.Scheme}://{Request.Host}{path}"
        });
    }
}
