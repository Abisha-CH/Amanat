namespace Amanat.Models.Entities;

public sealed class CampaignEntity
{
    public int CampaignId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Organization { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string FocusKey { get; set; } = string.Empty;
    public string? Thumbnail { get; set; }
    public string? ThumbnailAlt { get; set; }
    public string? ThumbnailLabel { get; set; }
    public string? HomeThumbnail { get; set; }
    public string? HomeThumbnailAlt { get; set; }
    public string? DetailsImage { get; set; }
    public string? DetailsImageAlt { get; set; }
    public string? Summary { get; set; }
    public string? HomeSummary { get; set; }
    public string? LedgerNote { get; set; }
    public decimal Goal { get; set; }
    public DateTime StartedOn { get; set; }
    public DateTime Deadline { get; set; }
    public string Status { get; set; } = string.Empty;
    public string CreatedByUserId { get; set; } = string.Empty;
    public string? UpdatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
    public string? FinalSummary { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
