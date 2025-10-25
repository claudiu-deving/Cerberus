using Cerberus.Domain;
using Dapper;

namespace Cerberus.Infrastructure;

public class ApiKeyRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ApiKeyRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Guid> CreateAsync(ApiKey apiKey)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = @"
            INSERT INTO api_keys (id, name, key_hash, tenant_id, project_id, created_at, expires_at, last_used_at, is_active)
            VALUES (@Id, @Name, @KeyHash, @TenantId, @ProjectId, @CreatedAt, @ExpiresAt, @LastUsedAt, @IsActive)
            RETURNING id";

        return await connection.ExecuteScalarAsync<Guid>(sql, new
        {
            apiKey.Id,
            apiKey.Name,
            apiKey.KeyHash,
            apiKey.TenantId,
            apiKey.ProjectId,
            apiKey.CreatedAt,
            apiKey.ExpiresAt,
            apiKey.LastUsedAt,
            apiKey.IsActive
        });
    }

    public async Task<ApiKey?> GetByKeyHashAsync(string keyHash)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = @"
            SELECT id, name, key_hash AS KeyHash, tenant_id AS TenantId, project_id AS ProjectId,
                   created_at AS CreatedAt, expires_at AS ExpiresAt, last_used_at AS LastUsedAt, is_active AS IsActive
            FROM api_keys
            WHERE key_hash = @KeyHash";

        return await connection.QueryFirstOrDefaultAsync<ApiKey>(sql, new { KeyHash = keyHash });
    }

    public async Task<ApiKey?> GetByIdAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = @"
            SELECT id, name, key_hash AS KeyHash, tenant_id AS TenantId, project_id AS ProjectId,
                   created_at AS CreatedAt, expires_at AS ExpiresAt, last_used_at AS LastUsedAt, is_active AS IsActive
            FROM api_keys
            WHERE id = @Id";

        return await connection.QueryFirstOrDefaultAsync<ApiKey>(sql, new { Id = id });
    }

    public async Task<IEnumerable<ApiKey>> GetByTenantIdAsync(Guid tenantId)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = @"
            SELECT id, name, key_hash AS KeyHash, tenant_id AS TenantId, project_id AS ProjectId,
                   created_at AS CreatedAt, expires_at AS ExpiresAt, last_used_at AS LastUsedAt, is_active AS IsActive
            FROM api_keys
            WHERE tenant_id = @TenantId
            ORDER BY created_at DESC";

        return await connection.QueryAsync<ApiKey>(sql, new { TenantId = tenantId });
    }

    public async Task UpdateLastUsedAsync(Guid id, DateTime lastUsedAt)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = @"
            UPDATE api_keys
            SET last_used_at = @LastUsedAt
            WHERE id = @Id";

        await connection.ExecuteAsync(sql, new { Id = id, LastUsedAt = lastUsedAt });
    }

    public async Task<bool> RevokeAsync(Guid id)
    {
        using var connection = _connectionFactory.CreateConnection();

        const string sql = @"
            UPDATE api_keys
            SET is_active = false
            WHERE id = @Id";

        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
        return rowsAffected > 0;
    }
}
