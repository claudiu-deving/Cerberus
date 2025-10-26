using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Cerberus.Tests.Infrastructure;

public abstract class IntegrationTestBase : IClassFixture<CerberusWebApplicationFactory>
{
    protected readonly CerberusWebApplicationFactory Factory;
    protected readonly HttpClient Client;

    protected IntegrationTestBase(CerberusWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    /// <summary>
    /// Creates a bootstrap tenant and returns the API key
    /// </summary>
    protected async Task<string> CreateBootstrapTenantAsync(string tenantName = "Test Organization", string? apiKeyName = null)
    {
        var response = await Client.PostAsJsonAsync("/cerberus/bootstrap", new
        {
            bootstrapToken = "TEST_BOOTSTRAP_TOKEN_FOR_INTEGRATION_TESTS",
            tenantName,
            apiKeyName = apiKeyName ?? "Master API Key",
            expiresAt = (DateTime?)null
        });

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<BootstrapResponse>();
        return result!.ApiKey;
    }

    /// <summary>
    /// Sets the Authorization header for subsequent requests
    /// </summary>
    protected void SetAuthorizationHeader(string apiKey)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    }

    /// <summary>
    /// Clears the Authorization header
    /// </summary>
    protected void ClearAuthorizationHeader()
    {
        Client.DefaultRequestHeaders.Authorization = null;
    }

    private class BootstrapResponse
    {
        public Guid TenantId { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public Guid ApiKeyId { get; set; }
        public string ApiKey { get; set; } = string.Empty;
        public string Warning { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}
