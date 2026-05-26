using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Chronos.API.Endpoints;

/// <summary>
/// Defines the platform health probe endpoints.
/// </summary>
public static class HealthEndpoints
{
    /// <summary>
    /// Maps the health check probes for the platform.
    /// </summary>
    /// <param name="app">The route builder instance.</param>
    public static void MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/healthz", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }))
           .WithName("HealthCheck")
           .WithSummary("A lightweight platform probe reporting structural runtime health.")
           .WithOpenApi()
           .WithTags("Infrastructure");
    }
}
