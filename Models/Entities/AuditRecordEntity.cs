namespace Amanat.Models.Entities;

public sealed class AuditRecordEntity
{
    public long AuditRecordId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? ActorUserId { get; set; }
    public string? Details { get; set; }
    public Guid CorrelationId { get; set; }
    public DateTime OccurredAtUtc { get; set; }
}
