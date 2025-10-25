# Cerberus API Test Suite

Comprehensive integration tests for the Cerberus secrets management API.

## Overview

This test suite uses:
- **xUnit** - Test framework
- **FluentAssertions** - Fluent assertion library for better test readability
- **Microsoft.AspNetCore.Mvc.Testing** - Integration testing infrastructure
- **Testcontainers.PostgreSql** - Docker-based PostgreSQL containers for isolated test databases

## Test Structure

### Infrastructure
- **`CerberusWebApplicationFactory`** - Custom WebApplicationFactory that configures the app for testing
- **`PostgreSqlTestContainer`** - Manages PostgreSQL Docker containers for each test run
- **`IntegrationTestBase`** - Base class with common test utilities and helpers

### Test Files
- **`BootstrapEndpointsTests`** - Tests for initial tenant/API key creation
- **`ApiKeyEndpointsTests`** - Tests for API key management
- **`TenantEndpointsTests`** - Tests for tenant operations
- **`ProjectEndpointsTests`** - Tests for project management
- **`AnimaEndpointsTests`** - Tests for secrets (animas) CRUD operations

## Running the Tests

### Prerequisites
- Docker must be running (for Testcontainers)
- .NET 8 SDK

### Run All Tests
```bash
dotnet test
```

### Run Specific Test Class
```bash
dotnet test --filter "FullyQualifiedName~BootstrapEndpointsTests"
```

### Run Specific Test
```bash
dotnet test --filter "FullyQualifiedName~Bootstrap_WithValidToken_ShouldCreateTenantAndApiKey"
```

### Run with Detailed Output
```bash
dotnet test --logger "console;verbosity=detailed"
```

## Test Coverage

The test suite covers:

### Bootstrap Endpoint ✓
- Creating tenant with valid bootstrap token
- Rejecting invalid bootstrap tokens
- Handling missing tenant names
- Creating API keys with expiration dates

### API Keys ✓
- Creating API keys with new tenants (atomic operation)
- Creating API keys for existing tenants
- Listing API keys for a tenant
- Getting API key details by ID
- Revoking API keys
- Validation of required fields

### Tenants ✓
- Listing tenants for authenticated user
- Getting tenant by ID
- Creating new tenants
- Listing projects for a tenant
- Authorization checks

### Projects ✓
- Creating projects with valid environments
- Validating environment types (DEVELOPMENT, STAGING, PRODUCTION)
- Getting project by ID
- Listing animas for a project
- Authorization checks

### Secrets (Animas) ✓
- Creating secrets
- Getting secrets by definition name
- Case-insensitive secret lookups
- Updating secret values
- Deleting secrets
- Authorization checks

## Test Database

Each test run:
1. Starts a fresh PostgreSQL container
2. Initializes the schema
3. Runs tests in isolation
4. Cleans up the container automatically

The test database is completely isolated from your development or production databases.

## Helper Methods

The `IntegrationTestBase` class provides:
- `CreateBootstrapTenantAsync()` - Quickly bootstrap a test tenant and get an API key
- `SetAuthorizationHeader(apiKey)` - Set Bearer token for authenticated requests
- `ClearAuthorizationHeader()` - Remove authentication for testing unauthorized access

## Example Test

```csharp
[Fact]
public async Task CreateAnima_WithValidRequest_ShouldCreateAnima()
{
    // Arrange
    var apiKey = await CreateBootstrapTenantAsync("Test Org");
    SetAuthorizationHeader(apiKey);

    var request = new
    {
        definition = "DATABASE_URL",
        value = "postgresql://localhost:5432/db",
        description = "Database connection"
    };

    // Act
    var response = await Client.PostAsJsonAsync(
        $"/tenants/{tenantId}/projects/{projectId}/animas",
        request
    );

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Created);
    var result = await response.Content.ReadFromJsonAsync<AnimaDto>();
    result.Definition.Should().Be("DATABASE_URL");
}
```

## Troubleshooting

### Docker Not Running
If you see errors about Docker, ensure Docker Desktop is running:
```
Error: Cannot connect to Docker daemon
```

### Port Conflicts
Testcontainers automatically assigns random ports, but if you have issues:
- Stop any locally running PostgreSQL instances
- Check Docker container logs: `docker ps -a`

### Slow Tests
First test run downloads the PostgreSQL Docker image (~100MB). Subsequent runs are faster as the image is cached.
