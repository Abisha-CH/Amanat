using Amanat.Data;
using Amanat.Models.Entities;
using Amanat.Repositories.Interfaces;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data.Common;

namespace Amanat.Repositories;

public sealed class ContributionRepository : IContributionRepository
{
    private static readonly string[] Statuses = { "Pending", "Verified", "Rejected" };
    private static readonly string[] DecisionStatuses = { "Verified", "Rejected" };
    private readonly IDbConnectionFactory _connectionFactory;

    public ContributionRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<ContributionEntity>> GetByCampaignAsync(int campaignId, string? status = null)
    {
        RepositoryGuard.Positive(campaignId, nameof(campaignId));
        if (status is not null)
        {
            RepositoryGuard.AllowedStatus(status, nameof(status), Statuses);
        }

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var sql = status is null
            ? "SELECT * FROM dbo.Contributions WHERE CampaignId = @CampaignId ORDER BY SubmittedAtUtc DESC;"
            : "SELECT * FROM dbo.Contributions WHERE CampaignId = @CampaignId AND Status = @Status ORDER BY SubmittedAtUtc DESC;";
        var rows = await connection.QueryAsync<ContributionEntity>(sql, new { CampaignId = campaignId, Status = status });
        return rows.AsList();
    }

    public async Task<IReadOnlyList<ContributionEntity>> GetByContributorAsync(string userId)
    {
        RepositoryGuard.RequiredText(userId, nameof(userId), 450);
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = "SELECT * FROM dbo.Contributions WHERE ContributorUserId = @UserId ORDER BY SubmittedAtUtc DESC;";
        var rows = await connection.QueryAsync<ContributionEntity>(sql, new { UserId = userId });
        return rows.AsList();
    }

    public async Task<ContributionEntity?> GetByIdAsync(long contributionId)
    {
        RepositoryGuard.Positive(contributionId, nameof(contributionId));
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = "SELECT * FROM dbo.Contributions WHERE ContributionId = @ContributionId;";
        return await connection.QuerySingleOrDefaultAsync<ContributionEntity>(sql, new { ContributionId = contributionId });
    }

    public async Task<IReadOnlyList<ContributionEntity>> GetPendingAsync(int? campaignId = null)
    {
        if (campaignId.HasValue)
        {
            RepositoryGuard.Positive(campaignId.Value, nameof(campaignId));
        }

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
SELECT *
FROM dbo.Contributions
WHERE Status = N'Pending' AND (@CampaignId IS NULL OR CampaignId = @CampaignId)
ORDER BY SubmittedAtUtc;";
        var rows = await connection.QueryAsync<ContributionEntity>(sql, new { CampaignId = campaignId });
        return rows.AsList();
    }

    public async Task<long> SubmitAsync(ContributionEntity contribution, SqlTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(contribution);
        var connection = RepositoryGuard.Connection(transaction);
        Normalize(contribution);
        Validate(contribution);

        const string duplicateSql = @"
SELECT COUNT(1)
FROM dbo.Contributions
WHERE CampaignId = @CampaignId AND TransactionRef = @TransactionRef;";
        var duplicateCount = await connection.ExecuteScalarAsync<int>(
            duplicateSql,
            new { contribution.CampaignId, contribution.TransactionRef },
            transaction);
        if (duplicateCount > 0)
        {
            throw new InvalidOperationException("A contribution with this transaction reference already exists for the campaign.");
        }

        const string insertSql = @"
INSERT dbo.Contributions
    (CampaignId, ContributorUserId, FullName, Amount, TransactionRef, ExternalPaymentMethod,
     DateSent, Notes, Status, SubmittedAtUtc, VerifiedAtUtc, VerifiedByUserId, VerificationNote)
VALUES
    (@CampaignId, @ContributorUserId, @FullName, @Amount, @TransactionRef, @ExternalPaymentMethod,
     @DateSent, @Notes, @Status, @SubmittedAtUtc, @VerifiedAtUtc, @VerifiedByUserId, @VerificationNote);
SELECT CAST(SCOPE_IDENTITY() AS bigint);";

        try
        {
            var contributionId = await connection.QuerySingleAsync<long>(insertSql, contribution, transaction);
            await RepositoryAudit.WriteAsync(
                connection,
                transaction,
                "Contribution",
                contributionId.ToString(),
                "Submitted",
                contribution.ContributorUserId,
                contribution.TransactionRef,
                Guid.NewGuid());
            return contributionId;
        }
        catch (SqlException exception) when (RepositoryGuard.IsDuplicateKey(exception))
        {
            throw new InvalidOperationException("A contribution with this transaction reference already exists for the campaign.", exception);
        }
    }

    public async Task<bool> VerifyAsync(
        long contributionId,
        string actorUserId,
        string? note,
        byte[] expectedRowVersion,
        SqlTransaction transaction)
    {
        return await DecideAsync(contributionId, actorUserId, note, expectedRowVersion, transaction, "Verified", "Verified");
    }

    public async Task<bool> RejectAsync(
        long contributionId,
        string actorUserId,
        string? note,
        byte[] expectedRowVersion,
        SqlTransaction transaction)
    {
        return await DecideAsync(contributionId, actorUserId, note, expectedRowVersion, transaction, "Rejected", "Rejected");
    }

    public async Task<IReadOnlyList<ContributionVerificationEntity>> GetVerificationHistoryAsync(long contributionId)
    {
        RepositoryGuard.Positive(contributionId, nameof(contributionId));
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
SELECT *
FROM dbo.ContributionVerifications
WHERE ContributionId = @ContributionId
ORDER BY DecisionAtUtc, ContributionVerificationId;";
        var rows = await connection.QueryAsync<ContributionVerificationEntity>(sql, new { ContributionId = contributionId });
        return rows.AsList();
    }

    private async Task<bool> DecideAsync(
        long contributionId,
        string actorUserId,
        string? note,
        byte[] expectedRowVersion,
        SqlTransaction transaction,
        string decision,
        string action)
    {
        RepositoryGuard.Positive(contributionId, nameof(contributionId));
        RepositoryGuard.RequiredText(actorUserId, nameof(actorUserId), 450);
        RepositoryGuard.RowVersion(expectedRowVersion, nameof(expectedRowVersion));
        RepositoryGuard.AllowedStatus(decision, nameof(decision), DecisionStatuses);
        if (note is { Length: > 500 })
        {
            throw new InvalidOperationException("Note must be at most 500 characters.");
        }

        var connection = RepositoryGuard.Connection(transaction);
        const string stateSql = "SELECT Status, RowVersion FROM dbo.Contributions WHERE ContributionId = @ContributionId;";
        var current = await connection.QuerySingleOrDefaultAsync<DecisionState>(
            stateSql,
            new { ContributionId = contributionId },
            transaction);

        if (current is null)
        {
            throw new InvalidOperationException($"Contribution {contributionId} was not found.");
        }

        if (current.Status != "Pending")
        {
            throw new InvalidOperationException($"Contribution {contributionId} is already {current.Status}.");
        }

        if (!current.RowVersion.SequenceEqual(expectedRowVersion))
        {
            throw RepositoryGuard.Concurrency($"Contribution {contributionId} was modified by another operation.");
        }

        const string updateSql = @"
UPDATE dbo.Contributions
SET Status = @Decision,
    VerifiedAtUtc = SYSUTCDATETIME(),
    VerifiedByUserId = @ActorUserId,
    VerificationNote = @Note
WHERE ContributionId = @ContributionId AND Status = N'Pending' AND RowVersion = @Expected;";

        var affected = await connection.ExecuteAsync(updateSql, new
        {
            ContributionId = contributionId,
            Decision = decision,
            ActorUserId = actorUserId,
            Note = note,
            Expected = expectedRowVersion
        }, transaction);

        if (affected != 1)
        {
            throw RepositoryGuard.Concurrency($"Contribution {contributionId} was modified by another operation.");
        }

        const string historySql = @"
INSERT dbo.ContributionVerifications (ContributionId, Decision, ActorUserId, Note, DecisionAtUtc)
VALUES (@ContributionId, @Decision, @ActorUserId, @Note, SYSUTCDATETIME());";
        await connection.ExecuteAsync(historySql, new
        {
            ContributionId = contributionId,
            Decision = decision,
            ActorUserId = actorUserId,
            Note = note
        }, transaction);

        await RepositoryAudit.WriteAsync(
            connection,
            transaction,
            "Contribution",
            contributionId.ToString(),
            action,
            actorUserId,
            note,
            Guid.NewGuid());
        return true;
    }

    private static void Normalize(ContributionEntity contribution)
    {
        contribution.Status = "Pending";
        contribution.ExternalPaymentMethod = string.IsNullOrWhiteSpace(contribution.ExternalPaymentMethod)
            ? "External"
            : contribution.ExternalPaymentMethod;
        contribution.SubmittedAtUtc = contribution.SubmittedAtUtc == default ? DateTime.UtcNow : contribution.SubmittedAtUtc;
        contribution.VerifiedAtUtc = null;
        contribution.VerifiedByUserId = null;
        contribution.VerificationNote = null;
    }

    private static void Validate(ContributionEntity contribution)
    {
        RepositoryGuard.Positive(contribution.CampaignId, nameof(contribution.CampaignId));
        RepositoryGuard.RequiredText(contribution.FullName, nameof(contribution.FullName), 100);
        RepositoryGuard.RequiredText(contribution.TransactionRef, nameof(contribution.TransactionRef), 100);
        RepositoryGuard.RequiredText(contribution.ExternalPaymentMethod, nameof(contribution.ExternalPaymentMethod), 50);
        RepositoryGuard.Positive(contribution.Amount, nameof(contribution.Amount));

        if (contribution.ContributorUserId is { Length: > 450 })
        {
            throw new InvalidOperationException("ContributorUserId must be at most 450 characters.");
        }

        if (contribution.Notes is { Length: > 500 })
        {
            throw new InvalidOperationException("Notes must be at most 500 characters.");
        }
    }

    private sealed class DecisionState
    {
        public string Status { get; set; } = string.Empty;
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
