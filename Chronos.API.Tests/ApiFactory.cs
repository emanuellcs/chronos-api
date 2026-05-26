using Chronos.API.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Chronos.API.Tests;

/// <summary>
/// A specialized WebApplicationFactory that configures an in-memory SQLite database 
/// with a persistent connection to maintain state during integration tests.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiFactory"/> class and opens the persistent connection.
    /// </summary>
    public ApiFactory()
    {
        // Establishing a long-lived connection to keep the in-memory database alive
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    /// <summary>
    /// Configures the web host to use the in-memory SQLite database and sets the Testing environment.
    /// </summary>
    /// <param name="builder">The web host builder.</param>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Setting environment to Testing to bypass default PostgreSQL registration
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Injecting SQLite in-memory provider using the persistent connection
            services.AddDbContext<ChronosDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });
        });
    }

    /// <summary>
    /// Disposes the persistent SQLite connection when the factory is destroyed.
    /// </summary>
    /// <param name="disposing">Whether disposing is occurring.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection.Close();
            _connection.Dispose();
        }
        base.Dispose(disposing);
    }
}
