using Cerberus.Application;
using Cerberus.Domain;

using Microsoft.AspNetCore.Mvc;

namespace Cerberus.Surface;

public static class ApiKeyEndpoints
{
    public static WebApplication MapApiKeyEndpoints(this WebApplication application)
    {
        var group = application.MapGroup("/api-keys")
            .WithTags("API Keys")
            .WithDescription("Manage API keys for authentication");

        MapApiKeyManagement(group);

        return application;
    }

    private static void MapApiKeyManagement(RouteGroupBuilder group)
    {
        // Create a new API key
        group.MapPost("", async (
            [FromBody] CreateApiKeyRequest request,
            [FromServices] ApiKeyService apiKeyService,
            [FromServices] TenantService tenantService) =>
        {
            try
            {
                Guid tenantId;

                // If tenant name provided, create tenant first
                if (!string.IsNullOrWhiteSpace(request.TenantName))
                {
                    tenantId = await tenantService.CreateTenantAsync(request.TenantName);
                }
                // Otherwise, use provided tenant ID
                else if (request.TenantId.HasValue)
                {
                    tenantId = request.TenantId.Value;
                }
                else
                {
                    return Results.BadRequest(new { message = "Either TenantId or TenantName must be provided" });
                }

                var (plaintextKey, apiKey) = await apiKeyService.CreateApiKeyAsync(
                    name: request.Name,
                    tenantId: tenantId,
                    projectId: request.ProjectId,
                    expiresAt: request.ExpiresAt
                );

                return Results.Created($"/api-keys/{apiKey.Id}", new
                {
                    apiKey = new
                    {
                        apiKey.Id,
                        apiKey.Name,
                        apiKey.TenantId,
                        apiKey.ProjectId,
                        apiKey.CreatedAt,
                        apiKey.ExpiresAt,
                        apiKey.IsActive
                    },
                    key = plaintextKey,
                    warning = "Store this key securely. It will not be shown again."
                });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
        .WithName("CreateApiKey")
        .WithSummary("Create a new API key")
        .WithDescription("Creates a new API key for authentication. Can create a new tenant atomically or use an existing tenant.");

        // Get all API keys for a tenant
        group.MapGet("/tenant/{tenantId:guid}", async (
            Guid tenantId,
            [FromServices] ApiKeyService apiKeyService) =>
        {
            var apiKeys = await apiKeyService.GetApiKeysForTenantAsync(tenantId);

            // Don't return the key hash
            var sanitizedKeys = apiKeys.Select(k => new
            {
                k.Id,
                k.Name,
                k.TenantId,
                k.ProjectId,
                k.CreatedAt,
                k.ExpiresAt,
                k.LastUsedAt,
                k.IsActive
            });

            return Results.Ok(sanitizedKeys);
        })
        .WithName("GetApiKeysForTenant")
        .WithSummary("List API keys for a tenant")
        .WithDescription("Retrieves all API keys associated with a specific tenant.");

        // Get a specific API key by ID
        group.MapGet("/{id:guid}", async (
            Guid id,
            [FromServices] ApiKeyService apiKeyService) =>
        {
            var apiKey = await apiKeyService.GetApiKeyByIdAsync(id);

            if (apiKey is null)
            {
                return Results.NotFound(new { message = $"API key with ID {id} not found" });
            }

            // Don't return the key hash
            return Results.Ok(new
            {
                apiKey.Id,
                apiKey.Name,
                apiKey.TenantId,
                apiKey.ProjectId,
                apiKey.CreatedAt,
                apiKey.ExpiresAt,
                apiKey.LastUsedAt,
                apiKey.IsActive
            });
        })
        .WithName("GetApiKeyById")
        .WithSummary("Get API key details")
        .WithDescription("Retrieves details for a specific API key by ID.");

        // Revoke an API key
        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] ApiKeyService apiKeyService) =>
        {
            var success = await apiKeyService.RevokeApiKeyAsync(id);

            if (!success)
            {
                return Results.NotFound(new { message = $"API key with ID {id} not found" });
            }

            return Results.Ok(new { message = "API key revoked successfully" });
        })
        .WithName("RevokeApiKey")
        .WithSummary("Revoke an API key")
        .WithDescription("Revokes an API key, preventing it from being used for authentication.");
    }
}

public record CreateApiKeyRequest(
    string Name,
    Guid? TenantId,
    string? TenantName,
    Guid? ProjectId,
    DateTime? ExpiresAt
);
