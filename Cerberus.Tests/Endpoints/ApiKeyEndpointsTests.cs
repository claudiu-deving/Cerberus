using System.Net;
using System.Net.Http.Json;
using Cerberus.Tests.Infrastructure;
using FluentAssertions;

namespace Cerberus.Tests.Endpoints;

public class ApiKeyEndpointsTests : IntegrationTestBase
{
    public ApiKeyEndpointsTests(CerberusWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateApiKey_WithNewTenant_ShouldCreateBothTenantAndApiKey()
    {
        // Arrange
        var request = new
        {
            name = "Test API Key",
            tenantId = (Guid?)null,
            tenantName = "New Organization",
            projectId = (Guid?)null,
            expiresAt = (DateTime?)null
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api-keys", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<CreateApiKeyResponse>();
        result.Should().NotBeNull();
        result!.Key.Should().StartWith("cerb_");
        result.ApiKey.Id.Should().NotBeEmpty();
        result.ApiKey.TenantId.Should().NotBeEmpty();
        result.Warning.Should().Contain("Store this key securely");
    }

    [Fact]
    public async Task CreateApiKey_WithExistingTenant_ShouldCreateApiKey()
    {
        // Arrange
        var bootstrapApiKey = await CreateBootstrapTenantAsync("Existing Tenant");
        SetAuthorizationHeader(bootstrapApiKey);

        // Get the tenant ID by calling the tenants endpoint
        var tenantsResponse = await Client.GetAsyncForTest("/tenants");
        var tenants = await tenantsResponse.Content.ReadFromJsonAsync<TenantDto[]>();
        var tenantId = tenants![0].Id;

        ClearAuthorizationHeader();

        var request = new
        {
            name = "Secondary API Key",
            tenantId,
            tenantName = (string?)null,
            projectId = (Guid?)null,
            expiresAt = (DateTime?)null
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api-keys", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<CreateApiKeyResponse>();
        result.Should().NotBeNull();
        result!.Key.Should().StartWith("cerb_");
        result.ApiKey.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public async Task GetApiKeysForTenant_WithValidTenantId_ShouldReturnApiKeys()
    {
        // Arrange
        var bootstrapApiKey = await CreateBootstrapTenantAsync("Test Organization");
        SetAuthorizationHeader(bootstrapApiKey);

        var tenantsResponse = await Client.GetAsyncForTest("/tenants");
        var tenants = await tenantsResponse.Content.ReadFromJsonAsync<TenantDto[]>();
        var tenantId = tenants![0].Id;

        // Act
        var response = await Client.GetAsyncForTest($"/api-keys/tenant/{tenantId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var apiKeys = await response.Content.ReadFromJsonAsync<ApiKeyDto[]>();
        apiKeys.Should().NotBeNull();
        apiKeys.Should().HaveCountGreaterThanOrEqualTo(1);
        apiKeys![0].Name.Should().Be("Master API Key");
    }

    [Fact]
    public async Task GetApiKeyById_WithValidId_ShouldReturnApiKey()
    {
        // Arrange
        var bootstrapApiKey = await CreateBootstrapTenantAsync("Test Organization");
        SetAuthorizationHeader(bootstrapApiKey);

        var tenantsResponse = await Client.GetAsyncForTest("/tenants");
        var tenants = await tenantsResponse.Content.ReadFromJsonAsync<TenantDto[]>();
        var tenantId = tenants![0].Id;

        var apiKeysResponse = await Client.GetAsyncForTest($"/api-keys/tenant/{tenantId}");
        var apiKeys = await apiKeysResponse.Content.ReadFromJsonAsync<ApiKeyDto[]>();
        var apiKeyId = apiKeys![0].Id;

        // Act
        var response = await Client.GetAsyncForTest($"/api-keys/{apiKeyId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var apiKey = await response.Content.ReadFromJsonAsync<ApiKeyDto>();
        apiKey.Should().NotBeNull();
        apiKey!.Id.Should().Be(apiKeyId);
    }

    [Fact]
    public async Task RevokeApiKey_WithValidId_ShouldRevokeApiKey()
    {
        // Arrange
        var bootstrapApiKey = await CreateBootstrapTenantAsync("Test Organization");
        SetAuthorizationHeader(bootstrapApiKey);

        var tenantsResponse = await Client.GetAsyncForTest("/tenants");
        var tenants = await tenantsResponse.Content.ReadFromJsonAsync<TenantDto[]>();
        var tenantId = tenants![0].Id;

        // Create a second API key to revoke
        ClearAuthorizationHeader();
        var createRequest = new
        {
            name = "Key to Revoke",
            tenantId,
            tenantName = (string?)null,
            projectId = (Guid?)null,
            expiresAt = (DateTime?)null
        };
        var createResponse = await Client.PostAsJsonAsync("/api-keys", createRequest);
        var createResult = await createResponse.Content.ReadFromJsonAsync<CreateApiKeyResponse>();
        var apiKeyId = createResult!.ApiKey.Id;

        // Act
        var response = await Client.DeleteAsyncForTest($"/api-keys/{apiKeyId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateApiKey_WithoutTenantIdOrName_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new
        {
            name = "Invalid API Key",
            tenantId = (Guid?)null,
            tenantName = (string?)null,
            projectId = (Guid?)null,
            expiresAt = (DateTime?)null
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api-keys", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private class CreateApiKeyResponse
    {
        public ApiKeyDto ApiKey { get; set; } = null!;
        public string Key { get; set; } = string.Empty;
        public string Warning { get; set; } = string.Empty;
    }

    private class ApiKeyDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid TenantId { get; set; }
        public Guid? ProjectId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public bool IsActive { get; set; }
    }

    private class TenantDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
