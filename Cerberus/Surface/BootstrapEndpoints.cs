using Cerberus.Application;
using Microsoft.AspNetCore.Mvc;

namespace Cerberus.Surface;

public static class BootstrapEndpoints
{
    public static WebApplication MapBootstrapEndpoints(this WebApplication application)
    {
        var group = application.MapGroup("")
            .WithTags("Bootstrap")
            .WithDescription("Initial setup endpoints for creating the first tenant and API key");

        MapBootstrap(group);

        return application;
    }

    private static void MapBootstrap(RouteGroupBuilder group)
    {
        // Bootstrap endpoint - creates tenant + master API key
        // Requires bootstrap token from configuration
        group.MapPost("/bootstrap", async (
            [FromBody] BootstrapRequest request,
            [FromServices] TenantService tenantService,
            [FromServices] ApiKeyService apiKeyService,
            [FromServices] IConfiguration configuration) =>
        {
            // Verify bootstrap token
            var bootstrapToken = configuration["Cerberus:BootstrapToken"];
            if (string.IsNullOrWhiteSpace(bootstrapToken) || bootstrapToken == "CHANGE_THIS_IN_PRODUCTION_VIA_ENV_VAR")
            {
                return Results.Problem(
                    detail: "Bootstrap token not configured. Set Cerberus:BootstrapToken in appsettings.json or environment variable.",
                    statusCode: 500);
            }

            if (request.BootstrapToken != bootstrapToken)
            {
                return Results.Unauthorized();
            }

            // Validate tenant name
            if (string.IsNullOrWhiteSpace(request.TenantName))
            {
                return Results.BadRequest(new { message = "Tenant name is required" });
            }

            try
            {
                // Step 1: Create the tenant
                var tenantId = await tenantService.CreateTenantAsync(request.TenantName);

                // Step 2: Create master API key for the tenant
                var (plaintextKey, apiKey) = await apiKeyService.CreateApiKeyAsync(
                    name: request.ApiKeyName ?? "Master API Key",
                    tenantId: tenantId,
                    projectId: null, // null = tenant-wide access
                    expiresAt: request.ExpiresAt
                );

                return Results.Ok(new BootstrapResponse
                {
                    TenantId = tenantId,
                    TenantName = request.TenantName,
                    ApiKeyId = apiKey.Id,
                    ApiKey = plaintextKey,
                    Warning = "Store this API key securely. It will not be shown again.",
                    Message = $"Tenant '{request.TenantName}' created successfully with master API key."
                });
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 400);
            }
        })
        .WithName("Bootstrap")
        .WithSummary("Create first tenant and master API key")
        .WithDescription("Creates the initial tenant and master API key. Requires bootstrap token from configuration. Use this for first-time setup only.");
    }
}

public record BootstrapRequest(
    string BootstrapToken,
    string TenantName,
    string? ApiKeyName,
    DateTime? ExpiresAt
);

public record BootstrapResponse
{
    public Guid TenantId { get; init; }
    public string TenantName { get; init; } = string.Empty;
    public Guid ApiKeyId { get; init; }
    public string ApiKey { get; init; } = string.Empty;
    public string Warning { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}
