namespace Amanat.Models.Entities;

public sealed class CampaignUpdateEntity
{
    public long CampaignUpdateId { get; set; }
    public int CampaignId { get; set; }
    public string AuthorUserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
