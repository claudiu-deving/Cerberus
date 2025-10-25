using Cerberus.Application;
using Cerberus.Domain;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Cerberus.Surface;

public static class TenantEndpoints
{
    public static WebApplication MapTenantEndpoints(this WebApplication application)
    {
        var group = application.MapGroup("/tenants")
            .WithTags("Tenants")
            .WithDescription("Manage tenants (organizations)");

        MapTenantManagement(group);

        return application;
    }

    private static void MapTenantManagement(RouteGroupBuilder group)
    {
        // GET all tenants - returns only the tenant the API key has access to
        group.MapGet("", async (
            HttpContext httpContext,
            [FromServices] TenantService tenantService,
            [FromServices] ApiKeyService apiKeyService) =>
        {
            var apiKey = httpContext.GetApiKey();
            if (apiKey is null)
            {
                return Results.Unauthorized();
            }

            var tenant = await tenantService.GetTenantByIdAsync(apiKey.TenantId);
            if (tenant is null)
            {
                return Results.NotFound(new { message = "Tenant not found" });
            }

            return Results.Ok(new[] { tenant });
        })
        .WithName("GetAllTenants")
        .WithSummary("List tenants")
        .WithDescription("Returns the tenant associated with the authenticated API key.");

        // GET tenant by ID
        group.MapGet("/{id:guid}", async (
            Guid id,
            HttpContext httpContext,
            [FromServices] TenantService tenantService,
            [FromServices] ApiKeyService apiKeyService) =>
        {
            var apiKey = httpContext.GetApiKey();
            if (apiKey is null)
            {
                return Results.Unauthorized();
            }

            var tenant = await tenantService.GetTenantByIdAsync(id);

            // Return 404 if tenant doesn't exist OR if API key doesn't have access
            // This prevents information disclosure about resource existence
            if (tenant is null || !apiKeyService.HasTenantAccess(apiKey, id))
            {
                return Results.NotFound(new { message = $"Tenant with ID {id} not found" });
            }

            return Results.Ok(tenant);
        })
        .WithName("GetTenantById")
        .WithSummary("Get tenant by ID")
        .WithDescription("Retrieves details for a specific tenant.");

        // GET all projects for a tenant
        group.MapGet("/{id:guid}/projects", async (
            Guid id,
            HttpContext httpContext,
            [FromServices] TenantService tenantService,
            [FromServices] ApiKeyService apiKeyService) =>
        {
            var apiKey = httpContext.GetApiKey();
            if (apiKey is null)
            {
                return Results.Unauthorized();
            }

            var tenant = await tenantService.GetTenantByIdAsync(id);

            // Return 404 if tenant doesn't exist OR if API key doesn't have access
            // This prevents information disclosure about resource existence
            if (tenant is null || !apiKeyService.HasTenantAccess(apiKey, id))
            {
                return Results.NotFound(new { message = $"Tenant with ID {id} not found" });
            }

            // If API key is scoped to a specific project, only return that project
            if (apiKey.ProjectId.HasValue)
            {
                var scopedProject = tenant.Projects.Where(p => p.Id == apiKey.ProjectId.Value);
                return Results.Ok(scopedProject);
            }

            return Results.Ok(tenant.Projects);
        })
        .WithName("GetTenantProjects")
        .WithSummary("List projects for a tenant")
        .WithDescription("Retrieves all projects associated with a tenant. If API key is project-scoped, only returns that specific project.");

        // POST create a new tenant (requires existing API key - use /bootstrap for first tenant)
        group.MapPost("", async (
            [FromBody] CreateTenantRequest request,
            HttpContext httpContext,
            [FromServices] TenantService tenantService,
            [FromServices] ApiKeyService apiKeyService) =>
        {
            var apiKey = httpContext.GetApiKey();
            if (apiKey is null)
            {
                return Results.Unauthorized();
            }

            // Only tenant-wide keys (no project scope) can create new tenants
            if (apiKey.ProjectId.HasValue)
            {
                return Results.Json(new { message = "Only tenant-wide API keys can create new tenants" }, statusCode: 403);
            }

            var tenantId = await tenantService.CreateTenantAsync(request.Name);
            return Results.Created($"/tenants/{tenantId}", new { id = tenantId, name = request.Name });
        })
        .WithName("CreateTenant")
        .WithSummary("Create a new tenant")
        .WithDescription("Creates a new tenant. Requires a tenant-wide API key (not project-scoped).");
    }
}

public record CreateTenantRequest(string Name);
