namespace Amanat.Models.Entities;

public sealed class ContributionEntity
{
    public long ContributionId { get; set; }
    public int CampaignId { get; set; }
    public string? ContributorUserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string TransactionRef { get; set; } = string.Empty;
    public string ExternalPaymentMethod { get; set; } = "External";
    public DateTime? DateSent { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime SubmittedAtUtc { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }
    public string? VerifiedByUserId { get; set; }
    public string? VerificationNote { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
