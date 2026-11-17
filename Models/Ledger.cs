using System.Globalization;

namespace Amanat.Models;

public sealed class LedgerFund
{
    public string Name { get; set; } = string.Empty;
    public string FundId { get; set; } = string.Empty;
    public string FocusArea { get; set; } = string.Empty;
    public string FocusKey { get; set; } = string.Empty;
    public decimal Inflow { get; set; }
    public decimal Outflow { get; set; }
    public decimal Reserve { get; set; }
    public string Seal { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsSealed { get; set; }

    public string InflowText => "PKR " + Inflow.ToString("N2", CultureInfo.InvariantCulture);
    public string OutflowText => "PKR " + Outflow.ToString("N2", CultureInfo.InvariantCulture);
    public string ReserveText => "PKR " + Reserve.ToString("N2", CultureInfo.InvariantCulture);
}

public sealed class CashbookEntry
{
    public string DateText { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Party { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string CategoryKey { get; set; } = string.Empty;
    public string InflowText { get; set; } = string.Empty;
    public string OutflowText { get; set; } = string.Empty;
    public string ProofIcon { get; set; } = string.Empty;
    public string ProofFile { get; set; } = string.Empty;
    public string AuditStamp { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsPending { get; set; }
}

public sealed class PendingApproval
{
    public string Title { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Submitter { get; set; } = string.Empty;
    public string SubmittedText { get; set; } = string.Empty;
    public string Attachment { get; set; } = string.Empty;
    public string AmountText { get; set; } = string.Empty;
}
