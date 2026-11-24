using Dapper;
using Microsoft.Data.SqlClient;

namespace Amanat.Repositories;

internal static class RepositoryAudit
{
    public static async Task WriteAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string entityType,
        string entityId,
        string action,
        string? actorUserId,
        string? details,
        Guid correlationId)
    {
        RepositoryGuard.RequiredText(entityType, nameof(entityType), 50);
        RepositoryGuard.RequiredText(entityId, nameof(entityId), 50);
        RepositoryGuard.RequiredText(action, nameof(action), 50);

        const string sql = @"
INSERT dbo.AuditRecords (EntityType, EntityId, Action, ActorUserId, Details, CorrelationId, OccurredAtUtc)
VALUES (@EntityType, @EntityId, @Action, @ActorUserId, @Details, @CorrelationId, SYSUTCDATETIME());";

        await connection.ExecuteAsync(sql, new
        {
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            ActorUserId = actorUserId,
            Details = details,
            CorrelationId = correlationId
        }, transaction);
    }
}
