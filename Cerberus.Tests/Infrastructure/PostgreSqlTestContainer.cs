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
            .WithDatabase("cerberus_test")
            .WithUsername("cerberus_test_user")
            .WithPassword("cerberus_test_password")
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

        // Create tables schema
        var createTablesScript = @"
            -- Tenants table
            CREATE TABLE IF NOT EXISTS tenants (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                name VARCHAR(255) NOT NULL,
                created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
            );

            -- Projects table
            CREATE TABLE IF NOT EXISTS projects (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
                name VARCHAR(255) NOT NULL,
                description TEXT,
                environment VARCHAR(50) NOT NULL,
                created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
            );

            -- Animas (secrets) table
            CREATE TABLE IF NOT EXISTS animas (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                project_id UUID NOT NULL REFERENCES projects(id) ON DELETE CASCADE,
                definition VARCHAR(255) NOT NULL,
                value TEXT NOT NULL,
                description TEXT,
                created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
                UNIQUE(project_id, definition)
            );

            -- API keys table
            CREATE TABLE IF NOT EXISTS api_keys (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
                project_id UUID REFERENCES projects(id) ON DELETE CASCADE,
                name VARCHAR(255) NOT NULL,
                key_hash VARCHAR(64) NOT NULL UNIQUE,
                created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
                expires_at TIMESTAMP WITH TIME ZONE,
                last_used_at TIMESTAMP WITH TIME ZONE,
                is_active BOOLEAN DEFAULT TRUE
            );

            -- Audit logs table
            CREATE TABLE IF NOT EXISTS audit_logs (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
                api_key_id UUID REFERENCES api_keys(id) ON DELETE SET NULL,
                action VARCHAR(50) NOT NULL,
                resource_type VARCHAR(50) NOT NULL,
                resource_id UUID,
                timestamp TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
                metadata JSONB
            );

            -- Users table
            CREATE TABLE IF NOT EXISTS users (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                tenant_id UUID NOT NULL REFERENCES tenants(id) ON DELETE CASCADE,
                email VARCHAR(255) NOT NULL UNIQUE,
                password_hash VARCHAR(255) NOT NULL,
                created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
                updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
            );

            -- Create indexes
            CREATE INDEX IF NOT EXISTS idx_projects_tenant_id ON projects(tenant_id);
            CREATE INDEX IF NOT EXISTS idx_animas_project_id ON animas(project_id);
            CREATE INDEX IF NOT EXISTS idx_api_keys_tenant_id ON api_keys(tenant_id);
            CREATE INDEX IF NOT EXISTS idx_api_keys_key_hash ON api_keys(key_hash);
            CREATE INDEX IF NOT EXISTS idx_audit_logs_tenant_id ON audit_logs(tenant_id);
        ";

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
