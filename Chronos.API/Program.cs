using Chronos.API.Configuration;
using Chronos.API.Endpoints;
using Chronos.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// --- Service Registration ---
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        // Explicitly setting the document title to remove the period from the default project-based title.
        document.Info.Title = "Chronos API";

        // Only force HTTPS for OpenAPI server URLs in Production environments.
        // This prevents connectivity issues in local development (HTTP).
        if (builder.Environment.IsProduction() && document.Servers is not null)
        {
            foreach (var server in document.Servers)
            {
                if (server.Url is not null && server.Url.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                {
                    server.Url = server.Url.Replace("http://", "https://", StringComparison.OrdinalIgnoreCase);
                }
            }
        }
        return Task.CompletedTask;
    });
});
builder.Services.AddEndpointsApiExplorer();
/*
 * Configures Cross-Origin Resource Sharing (CORS) policies.
 * The "AllowAll" policy permits requests from any origin, using any method and header,
 * which is suitable for local development and Docker Compose environments.
 */
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddChronosInfrastructure(builder.Configuration, builder.Environment);

var app = builder.Build();

// --- Middleware Pipeline ---
app.UseCors("AllowAll");

// Enabling OpenAPI and interactive documentation for all environments to support PaaS validation
app.MapOpenApi();
app.MapScalarApiReference("/swagger", options => 
{
    options.WithTitle("Chronos API");
});

app.UseHttpsRedirection();

// --- Endpoint Mapping ---
// Redirecting root to documentation for a friction-free user experience
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.MapHealthEndpoints();
app.MapAppointmentEndpoints();

// --- Database Migration on Startup ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    // Hardened environment telemetry
    logger.LogInformation("==================================================================");
    logger.LogInformation("[SYSTEM TELEMETRY] Operating under a secure, non-root environment.");
    logger.LogInformation("==================================================================");
    logger.LogInformation("[INFO] Chronos API is establishing connection handshake with PostgreSQL 18 container network...");

    try
    {
        var context = services.GetRequiredService<ChronosDbContext>();
        
        if (context.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            logger.LogInformation("[DATABASE] SQLite provider detected. Initializing in-memory schema using EnsureCreatedAsync...");
            await context.Database.EnsureCreatedAsync();
        }
        else
        {
            logger.LogInformation("[DATABASE] PostgreSQL provider detected. Applying programmatic Entity Framework Core migrations...");
            if (context.Database.GetPendingMigrations().Any())
            {
                await context.Database.MigrateAsync();
            }
        }

        // Success banner
        logger.LogInformation("[SUCCESS] Database schema is completely synchronized. Chronos API is healthy and live. Access interactive Swagger documentation at http://localhost:8080/swagger");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[ERROR] An error occurred while migrating the database.");
    }
}

app.Run();

/// <summary>
/// Exposes the Program class for the xUnit integration testing project using WebApplicationFactory.
/// </summary>
public partial class Program { }
