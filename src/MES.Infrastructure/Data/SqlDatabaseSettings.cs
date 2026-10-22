namespace MES.Infrastructure.Data;
public sealed class SqlDatabaseSettings
{
    public string ConnectionString { get; init; } = "";
    public string MachinesQuery { get; init; } = "";
    public int CommandTimeoutSeconds { get; init; } = 30;
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ConnectionString) && !string.IsNullOrWhiteSpace(MachinesQuery);
}
