namespace Amanat.Models;

public sealed class JsonOperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, string[]> Errors { get; set; } = [];
    public object? Data { get; set; }
}

public sealed class CampaignSummaryJson
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Raised { get; set; }
    public decimal Goal { get; set; }
    public decimal Balance { get; set; }
    public decimal ApprovedExpenses { get; set; }
    public int ProgressPercent { get; set; }
    public int Contributors { get; set; }
    public int ApprovedCount { get; set; }
    public string RaisedText { get; set; } = string.Empty;
    public string GoalText { get; set; } = string.Empty;
    public string BalanceText { get; set; } = string.Empty;
    public string ApprovedExpensesText { get; set; } = string.Empty;
    public string RemainingText { get; set; } = string.Empty;
    public string ContributorsText { get; set; } = string.Empty;
    public string ApprovedCountText { get; set; } = string.Empty;
}

public sealed class DecisionJsonRequest
{
    public long ContributionId { get; set; }
    public long ExpenseId { get; set; }
    public string RowVersion { get; set; } = string.Empty;
    public string Decision { get; set; } = string.Empty;
    public string? Note { get; set; }
}

public sealed class ReferenceSubmissionJson
{
    public long ContributionId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string AmountText { get; set; } = string.Empty;
    public string DateText { get; set; } = string.Empty;
    public string Campaign { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public sealed class DecisionJson
{
    public long RecordId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusClass { get; set; } = string.Empty;
}
