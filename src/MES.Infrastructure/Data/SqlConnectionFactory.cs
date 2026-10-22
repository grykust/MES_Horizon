using Microsoft.Data.SqlClient;
namespace MES.Infrastructure.Data;
public sealed class SqlConnectionFactory(SqlDatabaseSettings settings)
{
    public SqlConnection Create() => new(settings.ConnectionString);
}
