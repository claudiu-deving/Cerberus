using Cerberus.Domain;

namespace Cerberus.Application;

public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private const string ApiKeyHeaderName = "Authorization";

    public ApiKeyAuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ApiKeyService apiKeyService)
    {
        // Skip authentication for bootstrap, API key management, and documentation endpoints
        if (context.Request.Path.StartsWithSegments("/cerberus/api-keys") ||
            context.Request.Path.StartsWithSegments("/cerberus/bootstrap") ||
            context.Request.Path.StartsWithSegments("/cerberus/swagger") ||
            context.Request.Path.StartsWithSegments("/cerberus/scalar"))
        {
            await _next(context);
            return;
        }

        // Get the Authorization header
        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var authHeaderValue))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { message = "Missing Authorization header" });
            return;
        }

        var authHeader = authHeaderValue.ToString();

        // Expected format: "Bearer cerb_xxxxxxxxxxxx"
        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { message = "Invalid Authorization header format. Expected: Bearer <api-key>" });
            return;
        }

        var apiKey = authHeader.Substring("Bearer ".Length).Trim();

        // Validate the API key
        var validatedKey = await apiKeyService.ValidateApiKeyAsync(apiKey);

        if (validatedKey is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { message = "Invalid or expired API key" });
            return;
        }

        // Store the validated API key in HttpContext for use in endpoints
        context.Items["ApiKey"] = validatedKey;

        await _next(context);
    }
}

/// <summary>
/// Extension methods for accessing the authenticated API key from HttpContext
/// </summary>
public static class HttpContextExtensions
{
    public static ApiKey? GetApiKey(this HttpContext context)
    {
        return context.Items["ApiKey"] as ApiKey;
    }
}
