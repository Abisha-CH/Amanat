namespace Amanat.Models.Entities;

public sealed class ReceiptMetadataEntity
{
    public long ReceiptId { get; set; }
    public long? ExpenseId { get; set; }
    public long? ContributionId { get; set; }
    public string? ReceiptNumber { get; set; }
    public string? Vendor { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public string? StoragePath { get; set; }
    public decimal? Amount { get; set; }
    public DateTime? IssuedOn { get; set; }
    public bool IsVerified { get; set; }
    public string UploadedByUserId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
