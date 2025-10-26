using Dapper;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Cerberus.Tests.Infrastructure;

public class PostgreSqlTestContainer : IAsyncDisposable
{
    private readonly PostgreSqlContainer _container;
    private bool _isInitialized;

    public string ConnectionString => _container.GetConnectionString();

    public PostgreSqlTestContainer()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithName("postgres_test")
            .WithPortBinding(5435)
            .WithDatabase("cerberus_test")
            .WithUsername("test_user")
            .WithPassword("test_password")
            .WithCleanUp(true)
            .Build();
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
            return;

        await _container.StartAsync(cancellationToken);
        await InitializeDatabaseSchema(cancellationToken);
        _isInitialized = true;
    }

    private async Task InitializeDatabaseSchema(CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);

        // Read and execute the init.sql file from the database folder
        var projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var initSqlPath = Path.Combine(projectRoot, "database", "init.sql");

        if (!File.Exists(initSqlPath))
        {
            throw new FileNotFoundException($"Database initialization script not found at: {initSqlPath}");
        }

        var createTablesScript = await File.ReadAllTextAsync(initSqlPath, cancellationToken);

        // PostgreSQL 16 has gen_random_uuid() built-in, so replace uuid_generate_v4() with it
        // This avoids needing the uuid-ossp extension
        createTablesScript = createTablesScript.Replace("uuid_generate_v4()", "gen_random_uuid()");

        // Remove the CREATE EXTENSION command as we don't need it
        createTablesScript = System.Text.RegularExpressions.Regex.Replace(
            createTablesScript,
            @"CREATE\s+EXTENSION\s+IF\s+NOT\s+EXISTS\s+""uuid-ossp"";?\s*",
            "",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        await connection.ExecuteAsync(createTablesScript);
    }

    public async Task ResetDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        var truncateScript = @"
            TRUNCATE TABLE audit_logs CASCADE;
            TRUNCATE TABLE api_keys CASCADE;
            TRUNCATE TABLE animas CASCADE;
            TRUNCATE TABLE projects CASCADE;
            TRUNCATE TABLE tenants CASCADE;
            TRUNCATE TABLE users CASCADE;
        ";

        await connection.ExecuteAsync(truncateScript);
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
