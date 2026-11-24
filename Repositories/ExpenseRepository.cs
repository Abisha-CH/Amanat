using Amanat.Data;
using Amanat.Models.Entities;
using Amanat.Repositories.Interfaces;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data.Common;

namespace Amanat.Repositories;

public sealed class ExpenseRepository : IExpenseRepository
{
    private static readonly string[] Statuses = { "Pending", "Approved", "Rejected" };
    private static readonly string[] DecisionStatuses = { "Approved", "Rejected" };
    private readonly IDbConnectionFactory _connectionFactory;

    public ExpenseRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<ExpenseEntity>> GetByCampaignAsync(int campaignId, string? status = null)
    {
        RepositoryGuard.Positive(campaignId, nameof(campaignId));
        if (status is not null)
        {
            RepositoryGuard.AllowedStatus(status, nameof(status), Statuses);
        }

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        var sql = status is null
            ? "SELECT * FROM dbo.Expenses WHERE CampaignId = @CampaignId ORDER BY SubmittedAtUtc DESC;"
            : "SELECT * FROM dbo.Expenses WHERE CampaignId = @CampaignId AND Status = @Status ORDER BY SubmittedAtUtc DESC;";
        var rows = await connection.QueryAsync<ExpenseEntity>(sql, new { CampaignId = campaignId, Status = status });
        return rows.AsList();
    }

    public async Task<ExpenseEntity?> GetByIdAsync(long expenseId)
    {
        RepositoryGuard.Positive(expenseId, nameof(expenseId));
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = "SELECT * FROM dbo.Expenses WHERE ExpenseId = @ExpenseId;";
        return await connection.QuerySingleOrDefaultAsync<ExpenseEntity>(sql, new { ExpenseId = expenseId });
    }

    public async Task<long> SubmitAsync(ExpenseEntity expense, SqlTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(expense);
        var connection = RepositoryGuard.Connection(transaction);
        Normalize(expense);
        Validate(expense);

        const string duplicateSql = "SELECT COUNT(1) FROM dbo.Expenses WHERE Reference = @Reference;";
        var duplicateCount = await connection.ExecuteScalarAsync<int>(
            duplicateSql,
            new { expense.Reference },
            transaction);
        if (duplicateCount > 0)
        {
            throw new InvalidOperationException("An expense with this reference already exists.");
        }

        const string insertSql = @"
INSERT dbo.Expenses
    (CampaignId, SubmittedByUserId, Reference, PayeeName, Amount, Description, Status,
     SubmittedAtUtc, DecisionAtUtc, DecisionByUserId, DecisionNote)
VALUES
    (@CampaignId, @SubmittedByUserId, @Reference, @PayeeName, @Amount, @Description, @Status,
     @SubmittedAtUtc, @DecisionAtUtc, @DecisionByUserId, @DecisionNote);
SELECT CAST(SCOPE_IDENTITY() AS bigint);";

        try
        {
            var expenseId = await connection.QuerySingleAsync<long>(insertSql, expense, transaction);
            await RepositoryAudit.WriteAsync(
                connection,
                transaction,
                "Expense",
                expenseId.ToString(),
                "Submitted",
                expense.SubmittedByUserId,
                expense.Reference,
                Guid.NewGuid());
            return expenseId;
        }
        catch (SqlException exception) when (RepositoryGuard.IsDuplicateKey(exception))
        {
            throw new InvalidOperationException("An expense with this reference already exists.", exception);
        }
    }

    public async Task<IReadOnlyList<ExpenseEntity>> GetPendingApprovalsAsync(int? campaignId = null)
    {
        if (campaignId.HasValue)
        {
            RepositoryGuard.Positive(campaignId.Value, nameof(campaignId));
        }

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
SELECT *
FROM dbo.Expenses
WHERE Status = N'Pending' AND (@CampaignId IS NULL OR CampaignId = @CampaignId)
ORDER BY SubmittedAtUtc;";
        var rows = await connection.QueryAsync<ExpenseEntity>(sql, new { CampaignId = campaignId });
        return rows.AsList();
    }

    public async Task<bool> ApproveAsync(
        long expenseId,
        string actorUserId,
        string? note,
        byte[] expectedRowVersion,
        SqlTransaction transaction)
    {
        return await DecideAsync(expenseId, actorUserId, note, expectedRowVersion, transaction, "Approved", "Approved");
    }

    public async Task<bool> RejectAsync(
        long expenseId,
        string actorUserId,
        string? note,
        byte[] expectedRowVersion,
        SqlTransaction transaction)
    {
        return await DecideAsync(expenseId, actorUserId, note, expectedRowVersion, transaction, "Rejected", "Rejected");
    }

    public async Task<IReadOnlyList<ExpenseApprovalEntity>> GetApprovalHistoryAsync(long expenseId)
    {
        RepositoryGuard.Positive(expenseId, nameof(expenseId));
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
SELECT *
FROM dbo.ExpenseApprovals
WHERE ExpenseId = @ExpenseId
ORDER BY DecisionAtUtc, ExpenseApprovalId;";
        var rows = await connection.QueryAsync<ExpenseApprovalEntity>(sql, new { ExpenseId = expenseId });
        return rows.AsList();
    }

    public async Task<IReadOnlyList<ReceiptMetadataEntity>> GetReceiptsAsync(long expenseId)
    {
        RepositoryGuard.Positive(expenseId, nameof(expenseId));
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        const string sql = @"
SELECT *
FROM dbo.ReceiptMetadata
WHERE ExpenseId = @ExpenseId
ORDER BY CreatedAtUtc, ReceiptId;";
        var rows = await connection.QueryAsync<ReceiptMetadataEntity>(sql, new { ExpenseId = expenseId });
        return rows.AsList();
    }

    private async Task<bool> DecideAsync(
        long expenseId,
        string actorUserId,
        string? note,
        byte[] expectedRowVersion,
        SqlTransaction transaction,
        string decision,
        string action)
    {
        RepositoryGuard.Positive(expenseId, nameof(expenseId));
        RepositoryGuard.RequiredText(actorUserId, nameof(actorUserId), 450);
        RepositoryGuard.RowVersion(expectedRowVersion, nameof(expectedRowVersion));
        RepositoryGuard.AllowedStatus(decision, nameof(decision), DecisionStatuses);
        if (note is { Length: > 500 })
        {
            throw new InvalidOperationException("Note must be at most 500 characters.");
        }

        var connection = RepositoryGuard.Connection(transaction);
        const string stateSql = "SELECT Status, RowVersion FROM dbo.Expenses WHERE ExpenseId = @ExpenseId;";
        var current = await connection.QuerySingleOrDefaultAsync<DecisionState>(
            stateSql,
            new { ExpenseId = expenseId },
            transaction);

        if (current is null)
        {
            throw new InvalidOperationException($"Expense {expenseId} was not found.");
        }

        if (current.Status != "Pending")
        {
            throw new InvalidOperationException($"Expense {expenseId} is already {current.Status}.");
        }

        if (!current.RowVersion.SequenceEqual(expectedRowVersion))
        {
            throw RepositoryGuard.Concurrency($"Expense {expenseId} was modified by another operation.");
        }

        const string updateSql = @"
UPDATE dbo.Expenses
SET Status = @Decision,
    DecisionAtUtc = SYSUTCDATETIME(),
    DecisionByUserId = @ActorUserId,
    DecisionNote = @Note
WHERE ExpenseId = @ExpenseId AND Status = N'Pending' AND RowVersion = @Expected;";

        var affected = await connection.ExecuteAsync(updateSql, new
        {
            ExpenseId = expenseId,
            Decision = decision,
            ActorUserId = actorUserId,
            Note = note,
            Expected = expectedRowVersion
        }, transaction);

        if (affected != 1)
        {
            throw RepositoryGuard.Concurrency($"Expense {expenseId} was modified by another operation.");
        }

        const string historySql = @"
INSERT dbo.ExpenseApprovals (ExpenseId, Decision, ActorUserId, Note, DecisionAtUtc)
VALUES (@ExpenseId, @Decision, @ActorUserId, @Note, SYSUTCDATETIME());";
        await connection.ExecuteAsync(historySql, new
        {
            ExpenseId = expenseId,
            Decision = decision,
            ActorUserId = actorUserId,
            Note = note
        }, transaction);

        await RepositoryAudit.WriteAsync(
            connection,
            transaction,
            "Expense",
            expenseId.ToString(),
            action,
            actorUserId,
            note,
            Guid.NewGuid());
        return true;
    }

    private static void Normalize(ExpenseEntity expense)
    {
        expense.Status = "Pending";
        expense.SubmittedAtUtc = expense.SubmittedAtUtc == default ? DateTime.UtcNow : expense.SubmittedAtUtc;
        expense.DecisionAtUtc = null;
        expense.DecisionByUserId = null;
        expense.DecisionNote = null;
    }

    private static void Validate(ExpenseEntity expense)
    {
        RepositoryGuard.Positive(expense.CampaignId, nameof(expense.CampaignId));
        RepositoryGuard.RequiredText(expense.SubmittedByUserId, nameof(expense.SubmittedByUserId), 450);
        RepositoryGuard.RequiredText(expense.Reference, nameof(expense.Reference), 50);
        RepositoryGuard.RequiredText(expense.PayeeName, nameof(expense.PayeeName), 150);
        RepositoryGuard.RequiredText(expense.Description, nameof(expense.Description), 500);
        RepositoryGuard.Positive(expense.Amount, nameof(expense.Amount));
    }

    private sealed class DecisionState
    {
        public string Status { get; set; } = string.Empty;
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
