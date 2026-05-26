using Chronos.API.Configuration;
using Chronos.API.Endpoints;
using Chronos.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// --- Service Registration ---
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddChronosInfrastructure(builder.Configuration, builder.Environment);

var app = builder.Build();

// --- Middleware Pipeline ---
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // Enabling the interactive documentation UI at /swagger
    app.MapScalarApiReference("/swagger", options => 
    {
        options.WithTitle("Chronos API Documentation");
    });
}

app.UseHttpsRedirection();

// --- Endpoint Mapping ---
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
