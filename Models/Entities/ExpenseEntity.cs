namespace Amanat.Models.Entities;

public sealed class ExpenseEntity
{
    public long ExpenseId { get; set; }
    public int CampaignId { get; set; }
    public string SubmittedByUserId { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string PayeeName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime SubmittedAtUtc { get; set; }
    public DateTime? DecisionAtUtc { get; set; }
    public string? DecisionByUserId { get; set; }
    public string? DecisionNote { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
