using Microsoft.Data.SqlClient;
using System.Data;

namespace Amanat.Repositories;

internal static class RepositoryGuard
{
    public static void RequiredText(string? value, string name, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{name} is required.");
        }

        if (value.Length > maxLength)
        {
            throw new InvalidOperationException($"{name} must be at most {maxLength} characters.");
        }
    }

    public static void Positive(int value, string name)
    {
        if (value <= 0)
        {
            throw new InvalidOperationException($"{name} must be greater than zero.");
        }
    }

    public static void Positive(long value, string name)
    {
        if (value <= 0)
        {
            throw new InvalidOperationException($"{name} must be greater than zero.");
        }
    }

    public static void Positive(decimal value, string name)
    {
        if (value <= 0)
        {
            throw new InvalidOperationException($"{name} must be greater than zero.");
        }
    }

    public static void RowVersion(byte[]? rowVersion, string name)
    {
        if (rowVersion is null || rowVersion.Length != 8)
        {
            throw new InvalidOperationException($"{name} is invalid.");
        }
    }

    public static void AllowedStatus(string? status, string name, params string[] allowed)
    {
        if (!allowed.Contains(status, StringComparer.Ordinal))
        {
            throw new InvalidOperationException($"{name} must be one of: {string.Join(", ", allowed)}.");
        }
    }

    public static SqlConnection Connection(SqlTransaction transaction)
    {
        return transaction.Connection
            ?? throw new InvalidOperationException("The supplied transaction has no connection.");
    }

    public static bool IsDuplicateKey(SqlException exception)
    {
        return exception.Number is 2601 or 2627;
    }

    public static DBConcurrencyException Concurrency(string message)
    {
        return new DBConcurrencyException(message);
    }
}
