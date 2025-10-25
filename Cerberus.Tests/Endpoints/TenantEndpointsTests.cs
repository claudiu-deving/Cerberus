using System.Net;
using System.Net.Http.Json;
using Cerberus.Tests.Infrastructure;
using FluentAssertions;

namespace Cerberus.Tests.Endpoints;

public class TenantEndpointsTests : IntegrationTestBase
{
    public TenantEndpointsTests(CerberusWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetAllTenants_WithValidApiKey_ShouldReturnTenant()
    {
        // Arrange
        var apiKey = await CreateBootstrapTenantAsync("Test Organization");
        SetAuthorizationHeader(apiKey);

        // Act
        var response = await Client.GetAsyncForTest("/tenants");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var tenants = await response.Content.ReadFromJsonAsync<TenantDto[]>();
        tenants.Should().NotBeNull();
        tenants.Should().HaveCount(1);
        tenants![0].Name.Should().Be("Test Organization");
    }

    [Fact]
    public async Task GetAllTenants_WithoutAuthorization_ShouldReturnUnauthorized()
    {
        // Act
        var response = await Client.GetAsyncForTest("/tenants");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTenantById_WithValidId_ShouldReturnTenant()
    {
        // Arrange
        var apiKey = await CreateBootstrapTenantAsync("Test Organization");
        SetAuthorizationHeader(apiKey);

        var tenantsResponse = await Client.GetAsyncForTest("/tenants");
        var tenants = await tenantsResponse.Content.ReadFromJsonAsync<TenantDto[]>();
        var tenantId = tenants![0].Id;

        // Act
        var response = await Client.GetAsyncForTest($"/tenants/{tenantId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var tenant = await response.Content.ReadFromJsonAsync<TenantDto>();
        tenant.Should().NotBeNull();
        tenant!.Id.Should().Be(tenantId);
        tenant.Name.Should().Be("Test Organization");
    }

    [Fact]
    public async Task GetTenantById_WithInvalidId_ShouldReturnForbidden()
    {
        // Arrange
        var apiKey = await CreateBootstrapTenantAsync("Test Organization for Access Check");
        SetAuthorizationHeader(apiKey);
        var invalidTenantId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsyncForTest($"/tenants/{invalidTenantId}");

        // Assert
        // Should return Forbidden when trying to access a tenant the API key doesn't have access to
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetTenantProjects_WithValidTenantId_ShouldReturnProjects()
    {
        // Arrange
        var apiKey = await CreateBootstrapTenantAsync("Test Organization");
        SetAuthorizationHeader(apiKey);

        var tenantsResponse = await Client.GetAsyncForTest("/tenants");
        var tenants = await tenantsResponse.Content.ReadFromJsonAsync<TenantDto[]>();
        var tenantId = tenants![0].Id;

        // Create a project first
        var projectRequest = new
        {
            name = "Test Project",
            description = "A test project",
            environment = "DEVELOPMENT"
        };
        await Client.PostAsJsonAsync($"/tenants/{tenantId}/projects", projectRequest);

        // Act
        var response = await Client.GetAsyncForTest($"/tenants/{tenantId}/projects");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var projects = await response.Content.ReadFromJsonAsync<ProjectDto[]>();
        projects.Should().NotBeNull();
        projects.Should().HaveCount(1);
        projects![0].Name.Should().Be("Test Project");
    }

    [Fact]
    public async Task CreateTenant_WithTenantWideApiKey_ShouldCreateTenant()
    {
        // Arrange
        var apiKey = await CreateBootstrapTenantAsync("First Organization");
        SetAuthorizationHeader(apiKey);

        var request = new
        {
            name = "Second Organization"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/tenants", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<CreateTenantResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().NotBeEmpty();
        result.Name.Should().Be("Second Organization");
    }

    [Fact]
    public async Task CreateTenant_WithoutAuthorization_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new
        {
            name = "Unauthorized Tenant"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/tenants", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private class TenantDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<ProjectDto> Projects { get; set; } = new();
    }

    private class ProjectDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Environment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    private class CreateTenantResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
