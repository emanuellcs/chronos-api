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
    /// <returns>The modified service collection.</returns>
    public static IServiceCollection AddChronosInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<ChronosDbContext>(options =>
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(ChronosDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
            }));

        return services;
    }
}
