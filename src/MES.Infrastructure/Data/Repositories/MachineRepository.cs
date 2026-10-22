using System.Data;
using MES.Application.Common.Exceptions;
using MES.Application.Common.Interfaces;
using MES.Domain.Machines;
using Microsoft.Data.SqlClient;
namespace MES.Infrastructure.Data.Repositories;
public sealed class MachineRepository(SqlConnectionFactory factory, SqlDatabaseSettings settings) : IMachineRepository
{
    public async Task<IReadOnlyList<Machine>> GetAsync(string? search, CancellationToken cancellationToken)
    {
        if (!settings.IsConfigured) throw new DataAccessException("Brak konfiguracji bazy lub zapytania maszyn.");
        try
        {
            await using var connection = factory.Create();
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            // SQL pochodzi wyłącznie z konfiguracji administratora.
            command.CommandText = settings.MachinesQuery;
            command.CommandTimeout = settings.CommandTimeoutSeconds;
            command.Parameters.Add("@Search", SqlDbType.NVarChar, 100).Value = (object?)search ?? DBNull.Value;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var id = reader.GetOrdinal("Id");
            var code = reader.GetOrdinal("Code");
            var name = reader.GetOrdinal("Name");
            var result = new List<Machine>();
            while (await reader.ReadAsync(cancellationToken))
                result.Add(new Machine(reader.GetInt32(id), reader.GetString(code), reader.GetString(name)));
            return result;
        }
        catch (Exception ex) when (ex is SqlException or InvalidOperationException or ArgumentException or IndexOutOfRangeException or InvalidCastException)
        {
            throw new DataAccessException("Nie udało się odczytać maszyn.", ex);
        }
    }
}
