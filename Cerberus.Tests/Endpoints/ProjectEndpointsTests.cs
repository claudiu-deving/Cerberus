using System.Net;
using System.Net.Http.Json;
using Cerberus.Tests.Infrastructure;
using FluentAssertions;

namespace Cerberus.Tests.Endpoints;

public class ProjectEndpointsTests : IntegrationTestBase
{
    public ProjectEndpointsTests(CerberusWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateProject_WithValidRequest_ShouldCreateProject()
    {
        // Arrange
        var apiKey = await CreateBootstrapTenantAsync("Test Organization");
        SetAuthorizationHeader(apiKey);

        var tenantsResponse = await Client.GetAsyncForTest("/tenants");
        var tenants = await tenantsResponse.Content.ReadFromJsonAsync<TenantDto[]>();
        var tenantId = tenants![0].Id;

        var request = new
        {
            name = "Production App",
            description = "Production environment project",
            environment = "PRODUCTION"
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/tenants/{tenantId}/projects", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<CreateProjectResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().NotBeEmpty();
        result.Name.Should().Be("Production App");
        result.Environment.Should().Be("PRODUCTION");
    }

    [Fact]
    public async Task CreateProject_WithInvalidEnvironment_ShouldReturnBadRequest()
    {
        // Arrange
        var apiKey = await CreateBootstrapTenantAsync("Test Organization");
        SetAuthorizationHeader(apiKey);

        var tenantsResponse = await Client.GetAsyncForTest("/tenants");
        var tenants = await tenantsResponse.Content.ReadFromJsonAsync<TenantDto[]>();
        var tenantId = tenants![0].Id;

        var request = new
        {
            name = "Invalid Project",
            description = "Project with invalid environment",
            environment = "INVALID_ENV"
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/tenants/{tenantId}/projects", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetProjectById_WithValidId_ShouldReturnProject()
    {
        // Arrange
        var apiKey = await CreateBootstrapTenantAsync("Test Organization");
        SetAuthorizationHeader(apiKey);

        var tenantsResponse = await Client.GetAsyncForTest("/tenants");
        var tenants = await tenantsResponse.Content.ReadFromJsonAsync<TenantDto[]>();
        var tenantId = tenants![0].Id;

        var createRequest = new
        {
            name = "Test Project",
            description = "A test project",
            environment = "DEVELOPMENT"
        };
        var createResponse = await Client.PostAsJsonAsync($"/tenants/{tenantId}/projects", createRequest);
        var createResult = await createResponse.Content.ReadFromJsonAsync<CreateProjectResponse>();
        var projectId = createResult!.Id;

        // Act
        var response = await Client.GetAsyncForTest($"/tenants/{tenantId}/projects/{projectId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var project = await response.Content.ReadFromJsonAsync<ProjectDto>();
        project.Should().NotBeNull();
        project!.Id.Should().Be(projectId);
        project.Name.Should().Be("Test Project");
    }

    [Fact]
    public async Task GetProjectById_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var apiKey = await CreateBootstrapTenantAsync("Test Organization");
        SetAuthorizationHeader(apiKey);

        var tenantsResponse = await Client.GetAsyncForTest("/tenants");
        var tenants = await tenantsResponse.Content.ReadFromJsonAsync<TenantDto[]>();
        var tenantId = tenants![0].Id;

        var invalidProjectId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsyncForTest($"/tenants/{tenantId}/projects/{invalidProjectId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetProjectAnimas_WithValidProjectId_ShouldReturnAnimas()
    {
        // Arrange
        var apiKey = await CreateBootstrapTenantAsync("Test Organization");
        SetAuthorizationHeader(apiKey);

        var tenantsResponse = await Client.GetAsyncForTest("/tenants");
        var tenants = await tenantsResponse.Content.ReadFromJsonAsync<TenantDto[]>();
        var tenantId = tenants![0].Id;

        // Create project
        var projectRequest = new
        {
            name = "Test Project",
            description = "A test project",
            environment = "DEVELOPMENT"
        };
        var projectResponse = await Client.PostAsJsonAsync($"/tenants/{tenantId}/projects", projectRequest);
        var project = await projectResponse.Content.ReadFromJsonAsync<CreateProjectResponse>();
        var projectId = project!.Id;

        // Create an anima
        var animaRequest = new
        {
            definition = "DATABASE_URL",
            value = "postgresql://localhost:5432/test",
            description = "Test database connection"
        };
        await Client.PostAsJsonAsync($"/tenants/{tenantId}/projects/{projectId}/animas", animaRequest);

        // Act
        var response = await Client.GetAsyncForTest($"/tenants/{tenantId}/projects/{projectId}/animas");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var animas = await response.Content.ReadFromJsonAsync<AnimaDto[]>();
        animas.Should().NotBeNull();
        animas.Should().HaveCount(1);
        animas![0].Definition.Should().Be("DATABASE_URL");
    }

    [Fact]
    public async Task CreateProject_WithoutAuthorization_ShouldReturnUnauthorized()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var request = new
        {
            name = "Unauthorized Project",
            description = "Should fail",
            environment = "DEVELOPMENT"
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/tenants/{tenantId}/projects", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private class TenantDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class ProjectDto
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Environment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<AnimaDto> Animas { get; set; } = new();
    }

    private class CreateProjectResponse
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Environment { get; set; } = string.Empty;
    }

    private class AnimaDto
    {
        public Guid Id { get; set; }
        public string Definition { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
