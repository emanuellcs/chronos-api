using Chronos.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Chronos.API.Configuration;

/// <summary>
/// Provides extension methods for the <see cref="IServiceCollection"/> to register domain-specific services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the infrastructure persistence layer with the PostgreSQL Npgsql provider.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="environment">The host environment.</param>
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddChronosInfrastructure(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        // Conditionally registering PostgreSQL only if not in Testing environment
        // This allows integration tests to inject an in-memory SQLite provider without provider collision.
        if (!environment.IsEnvironment("Testing"))
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // Dynamic translation for RFC-compliant database URI protocols (e.g., from Render)
            // to the ADO.NET key-value matrix expected by Npgsql.
            if (!string.IsNullOrEmpty(connectionString) &&
                (connectionString.StartsWith("postgres://") || connectionString.StartsWith("postgresql://")))
            {
                var uri = new Uri(connectionString);
                var userInfo = uri.UserInfo.Split(':');

                var host = uri.Host;
                var port = uri.Port <= 0 ? 5432 : uri.Port;
                var database = uri.LocalPath.TrimStart('/');
                var username = userInfo[0];
                var password = userInfo.Length > 1 ? userInfo[1] : string.Empty;

                connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true;";
            }

            services.AddDbContext<ChronosDbContext>(options =>
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(ChronosDbContext).Assembly.FullName);
                    npgsqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                }));
        }

        return services;
    }
}
