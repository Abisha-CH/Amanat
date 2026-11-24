using Microsoft.Data.SqlClient;

namespace Amanat.Data;

public interface IDbConnectionFactory
{
    SqlConnection CreateConnection();
}
