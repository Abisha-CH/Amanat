using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Amanat.Models;
using Amanat.Models.Entities;

namespace Amanat.Services;

internal static class DomainMapper
{
    public static Campaign ToCampaign(CampaignEntity entity, CampaignBalanceEntity? balance)
    {
        return new Campaign
        {
            CampaignId = entity.CampaignId,
            RowVersion = Convert.ToBase64String(entity.RowVersion),
            Slug = entity.Slug,
            Title = entity.Title,
            Organization = entity.Organization,
            Category = entity.Category,
            FocusKey = entity.FocusKey,
            Thumbnail = entity.Thumbnail ?? string.Empty,
            ThumbnailAlt = entity.ThumbnailAlt ?? string.Empty,
            ThumbLabel = entity.ThumbnailLabel ?? string.Empty,
            HomeThumbnail = entity.HomeThumbnail ?? string.Empty,
            HomeThumbnailAlt = entity.HomeThumbnailAlt ?? string.Empty,
            Summary = entity.Summary ?? string.Empty,
            HomeSummary = entity.HomeSummary ?? string.Empty,
            DetailsImage = entity.DetailsImage ?? string.Empty,
            DetailsImageAlt = entity.DetailsImageAlt ?? string.Empty,
            Raised = balance?.VerifiedContributions ?? 0m,
            ApprovedExpenses = balance?.ApprovedExpenses ?? 0m,
            Balance = balance?.Balance ?? 0m,
            Goal = entity.Goal,
            Status = entity.Status,
            Contributors = balance?.VerifiedContributionCount ?? 0,
            ApprovedCount = balance?.ApprovedExpenseCount ?? 0,
            ApprovedLabel = ApprovedLabelFor(entity.Category),
            LedgerNote = entity.LedgerNote ?? string.Empty,
            StartedOn = entity.StartedOn,
            Deadline = entity.Deadline
        };
    }

    public static CampaignUpdateView ToCampaignUpdate(CampaignUpdateEntity entity)
    {
        return new CampaignUpdateView
        {
            Title = entity.Title,
            Body = entity.Body,
            Author = string.IsNullOrWhiteSpace(entity.AuthorUserId) ? "Campaign Team" : entity.AuthorUserId,
            CreatedText = TimeText(entity.CreatedAtUtc)
        };
    }

    public static Receipt ToReceipt(ReceiptMetadataEntity entity)
    {
        var originalName = entity.OriginalFileName;
        var looksLikeFile = originalName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
            || originalName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
            || originalName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
            || originalName.EndsWith(".png", StringComparison.OrdinalIgnoreCase);
        return new Receipt
        {
            Title = looksLikeFile
                ? string.IsNullOrWhiteSpace(entity.ReceiptNumber) ? originalName : "Receipt " + entity.ReceiptNumber
                : originalName,
            ImagePath = ReceiptImageFor(entity),
            ImageAlt = "Receipt for " + entity.Vendor,
            Reference = entity.ReceiptNumber is null ? string.Empty : "#" + entity.ReceiptNumber,
            Vendor = string.IsNullOrWhiteSpace(entity.Vendor) ? string.Empty : "Vendor: " + entity.Vendor,
            AmountText = "PKR " + (entity.Amount ?? 0m).ToString("N0", CultureInfo.InvariantCulture),
            IsVerified = entity.IsVerified
        };
    }

    public static ContributionRow ToContributionRow(ContributionEntity entity, string campaignTitle)
    {
        var verified = entity.Status.Equals("Verified", StringComparison.OrdinalIgnoreCase);
        return new ContributionRow
        {
            Campaign = campaignTitle,
            DateText = (entity.DateSent ?? entity.SubmittedAtUtc).ToString("MMM dd, yyyy", CultureInfo.InvariantCulture),
            Reference = entity.TransactionRef,
            AmountText = "PKR " + entity.Amount.ToString("N0", CultureInfo.InvariantCulture),
            Status = verified ? "Verified by Treasurer" : "Under Review",
            IsPending = !verified && !entity.Status.Equals("Rejected", StringComparison.OrdinalIgnoreCase),
            RowStyle = verified ? string.Empty : "background: rgba(255,247,237,0.35);"
        };
    }

    public static VerificationRow ToVerificationRow(ContributionEntity entity)
    {
        return new VerificationRow
        {
            ContributionId = entity.ContributionId,
            RowVersion = Convert.ToBase64String(entity.RowVersion),
            Contributor = entity.FullName,
            AmountText = "PKR " + entity.Amount.ToString("N0", CultureInfo.InvariantCulture),
            Reference = entity.TransactionRef
        };
    }

    public static ApprovalRow ToApprovalRow(
        ExpenseEntity entity,
        string campaignTitle,
        string focusKey,
        string? receiptFile)
    {
        return new ApprovalRow
        {
            ExpenseId = entity.ExpenseId,
            RowVersion = Convert.ToBase64String(entity.RowVersion),
            Id = "#" + entity.Reference,
            Attachment = string.IsNullOrWhiteSpace(receiptFile) ? "Attached: Receipt.pdf" : "Attached: " + receiptFile,
            Vendor = entity.PayeeName,
            Detail = entity.Description,
            Campaign = campaignTitle,
            CampaignBadgeClass = focusKey == "education" ? "badge-pending" : "badge-verified",
            AmountText = "PKR " + entity.Amount.ToString("N0", CultureInfo.InvariantCulture),
            Status = "Pending Signoff"
        };
    }

    public static PendingApproval ToPendingApproval(
        PendingApprovalEntity entity,
        ReceiptMetadataEntity? receipt)
    {
        var description = entity.Description;
        return new PendingApproval
        {
            Title = description,
            Reference = entity.Reference,
            Icon = ApprovalIconFor(description),
            Submitter = "Treasury Desk",
            SubmittedText = TimeText(entity.SubmittedAtUtc),
            Attachment = receipt is null
                ? "Attached: Receipt.pdf"
                : string.IsNullOrWhiteSpace(receipt.OriginalFileName) ? receipt.FileName : receipt.OriginalFileName,
            DemoMessage = DemoMessageFor(description),
            AmountText = "Rs. " + entity.Amount.ToString("N2", CultureInfo.InvariantCulture)
        };
    }

    public static CashbookEntry ToCashbook(LedgerEntryEntity entity)
    {
        var isInflow = entity.Direction.Equals("In", StringComparison.OrdinalIgnoreCase)
            || entity.Direction.Equals("Inflow", StringComparison.OrdinalIgnoreCase);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(entity.LedgerEntryId));
        var reference = "TX#0x" + Convert.ToHexString(hash.AsSpan(0, 2)).ToLowerInvariant()
            + "..." + Convert.ToHexString(hash.AsSpan(6, 2)).ToLowerInvariant();

        return new CashbookEntry
        {
            DateText = entity.OccurredAtUtc.ToString("MMM dd, yyyy", CultureInfo.InvariantCulture),
            Reference = reference,
            Party = entity.PartyName,
            Detail = entity.Description,
            Category = CategoryFor(entity.EntryType),
            CategoryKey = CategoryKeyFor(entity.EntryType),
            InflowText = isInflow ? "+Rs. " + entity.Amount.ToString("N2", CultureInfo.InvariantCulture) : "—",
            OutflowText = isInflow ? "—" : "-Rs. " + entity.Amount.ToString("N2", CultureInfo.InvariantCulture),
            ProofIcon = isInflow ? "bi-graph-up" : "bi-receipt",
            ProofFile = string.IsNullOrWhiteSpace(entity.Reference) ? "LedgerEntry.pdf" : entity.Reference + ".pdf",
            AuditStamp = entity.Status == "Verified" ? "Verified ledger entry" : "2/2 Multisig Conf",
            Status = entity.Status == "Verified" ? "Verified" : "Disbursed"
        };
    }

    public static LedgerFund ToFund(FundSummaryEntity entity, string digest)
    {
        var seed = FundSeed(entity.FundKey);
        return new LedgerFund
        {
            Name = seed.Name,
            FundId = seed.FundId,
            FocusArea = seed.FocusArea,
            FocusKey = entity.FundKey,
            Inflow = entity.VerifiedContributions,
            Outflow = entity.ApprovedExpenses,
            Reserve = entity.Balance,
            Seal = digest,
            Status = entity.FundKey == "welfare" ? "In Review" : "Sealed",
            IsSealed = entity.FundKey != "welfare"
        };
    }

    public static string PublicDigest(PublicSummaryEntity summary)
    {
        var source = string.Join('|',
            summary.TotalVerifiedContributions.ToString("F2", CultureInfo.InvariantCulture),
            summary.TotalApprovedExpenses.ToString("F2", CultureInfo.InvariantCulture),
            summary.Balance.ToString("F2", CultureInfo.InvariantCulture),
            summary.VerifiedContributionCount,
            summary.ApprovedExpenseCount,
            summary.CampaignCount);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source))).ToLowerInvariant();
        return hash[..4] + "..." + hash[^4..];
    }

    private static string ApprovedLabelFor(string category) => category switch
    {
        "Education" => "Invoices Cleared",
        "Healthcare" => "Verified Receipts",
        "Campus Welfare" => "Equipment Invoices",
        _ => "Bills Approved"
    };

    private static string ReceiptImageFor(ReceiptMetadataEntity entity)
    {
        var name = entity.FileName + " " + entity.OriginalFileName;
        if (name.Contains("desk", StringComparison.OrdinalIgnoreCase)) return "assets/images/receipt-desks.jpg";
        if (name.Contains("station", StringComparison.OrdinalIgnoreCase) || name.Contains("book", StringComparison.OrdinalIgnoreCase)) return "assets/images/receipt-stationery.jpg";
        if (name.Contains("electric", StringComparison.OrdinalIgnoreCase)) return "assets/images/receipt-electrical.jpg";
        return "assets/images/receipt-desks.jpg";
    }

    private static string TimeText(DateTime value) =>
        value.ToString("MMM dd, yyyy", CultureInfo.InvariantCulture);

    private static string ApprovalIconFor(string description)
    {
        var value = description.ToLowerInvariant();
        if (value.Contains("printer")) return "bi-printer";
        if (value.Contains("circuit") || value.Contains("pcb")) return "bi-cpu";
        if (value.Contains("goggle") || value.Contains("safety")) return "bi-shield-check";
        return "bi-receipt";
    }

    private static string DemoMessageFor(string description)
    {
        var value = description.ToLowerInvariant();
        if (value.Contains("printer")) return "Invoice preview is part of the interactive demo.";
        return "Receipt preview is part of the interactive demo.";
    }

    private static string CategoryFor(string entryType) => entryType switch
    {
        "Contribution" => "Civic Pledges",
        "Expense" => "Approved Disbursement",
        _ => entryType
    };

    private static string CategoryKeyFor(string entryType) => entryType switch
    {
        "Contribution" => "civic",
        "Expense" => "hardware",
        _ => "civic"
    };

    private static (string Name, string FundId, string FocusArea) FundSeed(string key) => key switch
    {
        "education" => ("Oakridge STEM Initiative", "#OK-2025-419", "Education"),
        "water" => ("Green Crescent Water Trust", "#GC-2025-118", "Clean Water"),
        "health" => ("Al-Noor Medical Relief Fund", "#AN-2025-204", "Healthcare"),
        "welfare" => ("Bright Futures Scholarship Fund", "#BF-2025-076", "Campus Welfare"),
        _ => ("Neighbourhood Community Aid Pool", "#NA-2025-331", "Community Aid")
    };
}
