using Microsoft.Data.SqlClient;

namespace Amanat.Data;

public sealed class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DomainConnection")
            ?? throw new InvalidOperationException("DomainConnection is not configured.");
    }

    public SqlConnection CreateConnection() => new(_connectionString);
}
