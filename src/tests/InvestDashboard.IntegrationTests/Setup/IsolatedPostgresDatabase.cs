using Npgsql;

namespace InvestDashboard.IntegrationTests.Setup;

public sealed class IsolatedPostgresDatabase : IAsyncDisposable
{
    private readonly string _adminConnectionString;
    private readonly string _databaseName;

    private IsolatedPostgresDatabase(string adminConnectionString, string databaseName, string connectionString)
    {
        _adminConnectionString = adminConnectionString;
        _databaseName = databaseName;
        ConnectionString = connectionString;
    }

    public string ConnectionString { get; }

    public static async Task<IsolatedPostgresDatabase> CreateAsync(string adminConnectionString)
    {
        var adminBuilder = new NpgsqlConnectionStringBuilder(adminConnectionString);
        var databaseName = $"invest_test_{Guid.NewGuid():N}";

        await using var connection = new NpgsqlConnection(adminBuilder.ConnectionString);
        await connection.OpenAsync();
        await using (var command = new NpgsqlCommand($"CREATE DATABASE \"{databaseName}\"", connection))
            await command.ExecuteNonQueryAsync();

        var testBuilder = new NpgsqlConnectionStringBuilder(adminConnectionString)
        {
            Database = databaseName
        };

        return new IsolatedPostgresDatabase(adminBuilder.ConnectionString, databaseName, testBuilder.ConnectionString);
    }

    public async ValueTask DisposeAsync()
    {
        NpgsqlConnection.ClearPool(new NpgsqlConnection(ConnectionString));

        await using var connection = new NpgsqlConnection(_adminConnectionString);
        await connection.OpenAsync();
        await using (var stopConnections = new NpgsqlCommand($"ALTER DATABASE \"{_databaseName}\" WITH ALLOW_CONNECTIONS false", connection))
            await stopConnections.ExecuteNonQueryAsync();

        await using (var terminateConnections = new NpgsqlCommand(
            "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @databaseName AND pid <> pg_backend_pid()",
            connection))
        {
            terminateConnections.Parameters.AddWithValue("databaseName", _databaseName);
            await terminateConnections.ExecuteNonQueryAsync();
        }

        await using var dropDatabase = new NpgsqlCommand($"DROP DATABASE \"{_databaseName}\"", connection);
        await dropDatabase.ExecuteNonQueryAsync();
    }
}
