namespace Amanat.Models.Entities;

public sealed class ContributionVerificationEntity
{
    public long ContributionVerificationId { get; set; }
    public long ContributionId { get; set; }
    public string Decision { get; set; } = string.Empty;
    public string ActorUserId { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime DecisionAtUtc { get; set; }
}
