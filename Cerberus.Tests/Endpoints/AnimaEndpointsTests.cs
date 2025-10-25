global using Cerberus.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Cerberus.Tests.Endpoints;

public class AnimaEndpointsTests : IntegrationTestBase
{
    public AnimaEndpointsTests(CerberusWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateAnima_WithValidRequest_ShouldCreateAnima()
    {
        // Arrange
        var (tenantId, projectId) = await CreateTenantAndProjectAsync();

        var request = new
        {
            definition = "DATABASE_URL",
            value = "postgresql://localhost:5432/mydb",
            description = "Main database connection string"
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/tenants/{tenantId}/projects/{projectId}/animas", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<CreateAnimaResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().NotBeEmpty();
        result.Definition.Should().Be("DATABASE_URL");
        result.Description.Should().Be("Main database connection string");
    }

    [Fact]
    public async Task GetAnimaByDefinition_WithValidDefinition_ShouldReturnAnima()
    {
        // Arrange
        var (tenantId, projectId) = await CreateTenantAndProjectAsync();

        var createRequest = new
        {
            definition = "API_KEY",
            value = "secret_value_123",
            description = "External API key"
        };
        await Client.PostAsJsonAsync($"/tenants/{tenantId}/projects/{projectId}/animas", createRequest);

        // Act
        var response = await Client.GetAsyncForTest($"/tenants/{tenantId}/projects/{projectId}/animas/API_KEY");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var anima = await response.Content.ReadFromJsonAsync<AnimaDto>();
        anima.Should().NotBeNull();
        anima!.Definition.Should().Be("API_KEY");
        anima.Value.Should().Be("secret_value_123");
        anima.Description.Should().Be("External API key");
    }

    [Fact]
    public async Task GetAnimaByDefinition_WithInvalidDefinition_ShouldReturnNotFound()
    {
        // Arrange
        var (tenantId, projectId) = await CreateTenantAndProjectAsync();

        // Act
        var response = await Client.GetAsyncForTest($"/tenants/{tenantId}/projects/{projectId}/animas/NONEXISTENT");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateAnima_WithValidRequest_ShouldUpdateAnima()
    {
        // Arrange
        var (tenantId, projectId) = await CreateTenantAndProjectAsync();

        var createRequest = new
        {
            definition = "SECRET_KEY",
            value = "old_value",
            description = "Old description"
        };
        var createResponse = await Client.PostAsJsonAsync($"/tenants/{tenantId}/projects/{projectId}/animas", createRequest);
        var createResult = await createResponse.Content.ReadFromJsonAsync<CreateAnimaResponse>();
        var animaId = createResult!.Id;

        var updateRequest = new
        {
            value = "new_value",
            description = "Updated description"
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/tenants/{tenantId}/projects/{projectId}/animas/{animaId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify the update
        var getResponse = await Client.GetAsyncForTest($"/tenants/{tenantId}/projects/{projectId}/animas/SECRET_KEY");
        var updatedAnima = await getResponse.Content.ReadFromJsonAsync<AnimaDto>();
        updatedAnima!.Value.Should().Be("new_value");
        updatedAnima.Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task UpdateAnima_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var (tenantId, projectId) = await CreateTenantAndProjectAsync();
        var invalidAnimaId = Guid.NewGuid();

        var updateRequest = new
        {
            value = "new_value",
            description = "Updated description"
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/tenants/{tenantId}/projects/{projectId}/animas/{invalidAnimaId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteAnima_WithValidId_ShouldDeleteAnima()
    {
        // Arrange
        var (tenantId, projectId) = await CreateTenantAndProjectAsync();

        var createRequest = new
        {
            definition = "TEMP_SECRET",
            value = "temporary_value",
            description = "Temporary secret"
        };
        var createResponse = await Client.PostAsJsonAsync($"/tenants/{tenantId}/projects/{projectId}/animas", createRequest);
        var createResult = await createResponse.Content.ReadFromJsonAsync<CreateAnimaResponse>();
        var animaId = createResult!.Id;

        // Act
        var response = await Client.DeleteAsyncForTest($"/tenants/{tenantId}/projects/{projectId}/animas/{animaId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify deletion
        var getResponse = await Client.GetAsyncForTest($"/tenants/{tenantId}/projects/{projectId}/animas/TEMP_SECRET");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteAnima_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var (tenantId, projectId) = await CreateTenantAndProjectAsync();
        var invalidAnimaId = Guid.NewGuid();

        // Act
        var response = await Client.DeleteAsyncForTest($"/tenants/{tenantId}/projects/{projectId}/animas/{invalidAnimaId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateAnima_WithoutAuthorization_ShouldReturnUnauthorized()
    {
        // Arrange
        ClearAuthorizationHeader();
        var tenantId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        var request = new
        {
            definition = "UNAUTHORIZED_SECRET",
            value = "value",
            description = "Should fail"
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/tenants/{tenantId}/projects/{projectId}/animas", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAnimaByDefinition_CaseInsensitive_ShouldReturnAnima()
    {
        // Arrange
        var (tenantId, projectId) = await CreateTenantAndProjectAsync();

        var createRequest = new
        {
            definition = "MY_SECRET",
            value = "secret_value",
            description = "Test secret"
        };
        await Client.PostAsJsonAsync($"/tenants/{tenantId}/projects/{projectId}/animas", createRequest);

        // Act - Request with different case
        var response = await Client.GetAsyncForTest($"/tenants/{tenantId}/projects/{projectId}/animas/my_secret");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var anima = await response.Content.ReadFromJsonAsync<AnimaDto>();
        anima.Should().NotBeNull();
        anima!.Definition.Should().Be("MY_SECRET");
    }

    /// <summary>
    /// Helper method to create a tenant and project for testing
    /// </summary>
    private async Task<(Guid tenantId, Guid projectId)> CreateTenantAndProjectAsync()
    {
        var apiKey = await CreateBootstrapTenantAsync("Test Organization");
        SetAuthorizationHeader(apiKey);

        var tenantsResponse = await Client.GetAsyncForTest("/tenants");
        var tenants = await tenantsResponse.Content.ReadFromJsonAsync<TenantDto[]>();
        var tenantId = tenants![0].Id;

        var projectRequest = new
        {
            name = "Test Project",
            description = "A test project",
            environment = "DEVELOPMENT"
        };
        var projectResponse = await Client.PostAsJsonAsync($"/tenants/{tenantId}/projects", projectRequest);
        var project = await projectResponse.Content.ReadFromJsonAsync<CreateProjectResponse>();
        var projectId = project!.Id;

        return (tenantId, projectId);
    }

    private class TenantDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class CreateProjectResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class AnimaDto
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public string Definition { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    private class CreateAnimaResponse
    {
        public Guid Id { get; set; }
        public string Definition { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
