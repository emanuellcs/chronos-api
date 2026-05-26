using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Chronos.API.Endpoints;

/// <summary>
/// Represents the structured heartbeat payload for platform health monitoring.
/// </summary>
/// <param name="Status">The current operational status of the service.</param>
/// <param name="Timestamp">The precise UTC timestamp of the probe execution.</param>
public record HealthResponse(string Status, DateTime Timestamp);

/// <summary>
/// Defines the structural routing maps for platform health and infrastructure probes.
/// </summary>
public static class HealthEndpoints
{
    /// <summary>
    /// Binds the high-availability health check probes to the application routing pipeline.
    /// </summary>
    /// <param name="app">The route builder instance used to configure the application's request pipeline.</param>
    public static void MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/healthz", () => Results.Ok(new HealthResponse("Healthy", DateTime.UtcNow)))
           .WithName("HealthCheck")
           .WithSummary("Check Health")
           .WithDescription("Executes a high-frequency, non-terminating health probe to verify the operational integrity of the platform runtime. Returns a heartbeat payload containing the current system status and a UTC timestamp to assist in load balancing and container orchestration monitoring.")
           .WithTags("Infrastructure")
           .Produces<HealthResponse>(StatusCodes.Status200OK);
    }
}
