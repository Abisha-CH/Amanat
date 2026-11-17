using System.Globalization;

namespace Amanat.Models;

public sealed class Campaign
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Organization { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string FocusKey { get; set; } = string.Empty;
    public string Thumbnail { get; set; } = string.Empty;
    public string ThumbnailAlt { get; set; } = string.Empty;
    public string ThumbLabel { get; set; } = string.Empty;
    public string HomeThumbnail { get; set; } = string.Empty;
    public string HomeThumbnailAlt { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string HomeSummary { get; set; } = string.Empty;
    public string DetailsImage { get; set; } = string.Empty;
    public string DetailsImageAlt { get; set; } = string.Empty;
    public decimal Raised { get; set; }
    public decimal Goal { get; set; }
    public int Contributors { get; set; }
    public int ApprovedCount { get; set; }
    public string ApprovedLabel { get; set; } = string.Empty;
    public string LedgerNote { get; set; } = string.Empty;
    public DateTime StartedOn { get; set; }
    public DateTime Deadline { get; set; }

    public int ProgressPercent => Goal == 0 ? 0 : (int)Math.Round(Raised * 100m / Goal);
    public string RaisedRaw => Raised.ToString("0", CultureInfo.InvariantCulture);
    public string RaisedText => "PKR " + Raised.ToString("N0", CultureInfo.InvariantCulture);
    public string GoalText => "PKR " + Goal.ToString("N0", CultureInfo.InvariantCulture);
    public string StartedOnText => StartedOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    public string DeadlineText => Deadline.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}

public sealed class Receipt
{
    public string Title { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public string ImageAlt { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Vendor { get; set; } = string.Empty;
    public string AmountText { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
}
