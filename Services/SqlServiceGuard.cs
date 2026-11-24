using System.Data;
using Microsoft.Data.SqlClient;

namespace Amanat.Services;

internal static class SqlServiceGuard
{
    public static async Task<T> RunAsync<T>(Func<Task<T>> operation, string fallbackMessage)
    {
        try
        {
            return await operation();
        }
        catch (DomainValidationException)
        {
            throw;
        }
        catch (DomainUnauthorizedException)
        {
            throw;
        }
        catch (SqlException)
        {
            throw new DomainUnavailableException(fallbackMessage);
        }
        catch (DBConcurrencyException)
        {
            throw new DomainValidationException("This record was changed by another user. Reload the page and try again.");
        }
    }

    public static async Task RunAsync(Func<Task> operation, string fallbackMessage)
    {
        try
        {
            await operation();
        }
        catch (DomainValidationException)
        {
            throw;
        }
        catch (DomainUnauthorizedException)
        {
            throw;
        }
        catch (SqlException)
        {
            throw new DomainUnavailableException(fallbackMessage);
        }
        catch (DBConcurrencyException)
        {
            throw new DomainValidationException("This record was changed by another user. Reload the page and try again.");
        }
    }
}
