using System.Security.Cryptography;
using System.Text;
using Cerberus.Domain;
using Cerberus.Infrastructure;

namespace Cerberus.Application;

public class ApiKeyService
{
    private readonly ApiKeyRepository _apiKeyRepository;
    private readonly TenantService _tenantService;

    public ApiKeyService(ApiKeyRepository apiKeyRepository, TenantService tenantService)
    {
        _apiKeyRepository = apiKeyRepository;
        _tenantService = tenantService;
    }

    /// <summary>
    /// Generates a new API key with cryptographically secure random bytes
    /// </summary>
    /// <returns>Tuple of (plaintext key to show user once, ApiKey record with hashed key)</returns>
    public async Task<(string plaintextKey, ApiKey apiKey)> CreateApiKeyAsync(
        string name,
        Guid tenantId,
        Guid? projectId = null,
        DateTime? expiresAt = null)
    {
        // Validate tenant exists
        var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
        if (tenant is null)
        {
            throw new ArgumentException($"Tenant with ID {tenantId} not found", nameof(tenantId));
        }

        // Validate project exists if provided
        if (projectId.HasValue)
        {
            var project = tenant.Projects.FirstOrDefault(p => p.Id == projectId.Value);
            if (project is null)
            {
                throw new ArgumentException($"Project with ID {projectId} not found in tenant {tenantId}", nameof(projectId));
            }
        }

        // Generate cryptographically secure random key
        var keyBytes = new byte[32]; // 256 bits
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(keyBytes);
        }

        // Create a readable key format: cerb_<base64url>
        var plaintextKey = "cerb_" + Convert.ToBase64String(keyBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");

        // Hash the key for storage (SHA-256)
        var keyHash = HashKey(plaintextKey);

        var apiKey = new ApiKey(
            Id: Guid.NewGuid(),
            Name: name,
            KeyHash: keyHash,
            TenantId: tenantId,
            ProjectId: projectId,
            CreatedAt: DateTime.UtcNow,
            ExpiresAt: expiresAt,
            LastUsedAt: null,
            IsActive: true
        );

        await _apiKeyRepository.CreateAsync(apiKey);

        return (plaintextKey, apiKey);
    }

    /// <summary>
    /// Validates an API key and returns the associated ApiKey record if valid
    /// </summary>
    public async Task<ApiKey?> ValidateApiKeyAsync(string plaintextKey)
    {
        if (string.IsNullOrWhiteSpace(plaintextKey) || !plaintextKey.StartsWith("cerb_"))
        {
            return null;
        }

        var keyHash = HashKey(plaintextKey);

        // Find the API key by hash
        var apiKey = await _apiKeyRepository.GetByKeyHashAsync(keyHash);

        if (apiKey is null)
        {
            return null;
        }

        // Check if key is active
        if (!apiKey.IsActive)
        {
            return null;
        }

        // Check if key is expired
        if (apiKey.ExpiresAt.HasValue && apiKey.ExpiresAt.Value < DateTime.UtcNow)
        {
            return null;
        }

        // Update last used timestamp asynchronously (fire and forget)
        _ = Task.Run(async () => await _apiKeyRepository.UpdateLastUsedAsync(apiKey.Id, DateTime.UtcNow));

        return apiKey;
    }

    /// <summary>
    /// Revokes an API key by ID
    /// </summary>
    public async Task<bool> RevokeApiKeyAsync(Guid apiKeyId)
    {
        return await _apiKeyRepository.RevokeAsync(apiKeyId);
    }

    /// <summary>
    /// Gets all API keys for a tenant
    /// </summary>
    public async Task<IEnumerable<ApiKey>> GetApiKeysForTenantAsync(Guid tenantId)
    {
        return await _apiKeyRepository.GetByTenantIdAsync(tenantId);
    }

    /// <summary>
    /// Gets a specific API key by ID
    /// </summary>
    public async Task<ApiKey?> GetApiKeyByIdAsync(Guid apiKeyId)
    {
        return await _apiKeyRepository.GetByIdAsync(apiKeyId);
    }

    /// <summary>
    /// Hashes an API key using SHA-256
    /// </summary>
    private static string HashKey(string plaintextKey)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(plaintextKey));
        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// Checks if an API key has access to a specific tenant
    /// </summary>
    public bool HasTenantAccess(ApiKey apiKey, Guid tenantId)
    {
        return apiKey.TenantId == tenantId;
    }

    /// <summary>
    /// Checks if an API key has access to a specific project
    /// </summary>
    public bool HasProjectAccess(ApiKey apiKey, Guid tenantId, Guid projectId)
    {
        // Key must belong to the tenant
        if (apiKey.TenantId != tenantId)
        {
            return false;
        }

        // If key is scoped to a specific project, it must match
        if (apiKey.ProjectId.HasValue)
        {
            return apiKey.ProjectId.Value == projectId;
        }

        // If key is not scoped to a project, it has access to all projects in the tenant
        return true;
    }
}
