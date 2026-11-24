namespace Amanat.Models.Entities;

public sealed class ExpenseApprovalEntity
{
    public long ExpenseApprovalId { get; set; }
    public long ExpenseId { get; set; }
    public string Decision { get; set; } = string.Empty;
    public string ActorUserId { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime DecisionAtUtc { get; set; }
}
