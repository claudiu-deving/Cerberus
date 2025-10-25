using System.Net;
using System.Net.Http.Json;
using Cerberus.Tests.Infrastructure;
using FluentAssertions;

namespace Cerberus.Tests.Endpoints;

public class BootstrapEndpointsTests : IntegrationTestBase
{
    public BootstrapEndpointsTests(CerberusWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Bootstrap_WithValidToken_ShouldCreateTenantAndApiKey()
    {
        // Arrange
        var request = new
        {
            bootstrapToken = "TEST_BOOTSTRAP_TOKEN_FOR_INTEGRATION_TESTS",
            tenantName = "Test Organization",
            apiKeyName = "Master Key",
            expiresAt = (DateTime?)null
        };

        // Act
        var response = await Client.PostAsJsonAsync("/bootstrap", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<BootstrapResponse>();
        result.Should().NotBeNull();
        result!.TenantId.Should().NotBeEmpty();
        result.TenantName.Should().Be("Test Organization");
        result.ApiKeyId.Should().NotBeEmpty();
        result.ApiKey.Should().StartWith("cerb_");
        result.Warning.Should().Contain("Store this API key securely");
        result.Message.Should().Contain("created successfully");
    }

    [Fact]
    public async Task Bootstrap_WithInvalidToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new
        {
            bootstrapToken = "INVALID_TOKEN",
            tenantName = "Test Organization",
            apiKeyName = "Master Key",
            expiresAt = (DateTime?)null
        };

        // Act
        var response = await Client.PostAsJsonAsync("/bootstrap", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Bootstrap_WithMissingTenantName_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new
        {
            bootstrapToken = "TEST_BOOTSTRAP_TOKEN_FOR_INTEGRATION_TESTS",
            tenantName = "",
            apiKeyName = "Master Key",
            expiresAt = (DateTime?)null
        };

        // Act
        var response = await Client.PostAsJsonAsync("/bootstrap", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Bootstrap_WithExpirationDate_ShouldCreateApiKeyWithExpiration()
    {
        // Arrange
        var expiresAt = DateTime.UtcNow.AddDays(30);
        var request = new
        {
            bootstrapToken = "TEST_BOOTSTRAP_TOKEN_FOR_INTEGRATION_TESTS",
            tenantName = "Expiring Tenant",
            apiKeyName = "Expiring Key",
            expiresAt
        };

        // Act
        var response = await Client.PostAsJsonAsync("/bootstrap", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<BootstrapResponse>();
        result.Should().NotBeNull();
        result!.ApiKey.Should().StartWith("cerb_");
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
