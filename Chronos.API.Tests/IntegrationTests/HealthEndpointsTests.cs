using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Chronos.API.Tests.IntegrationTests;

/// <summary>
/// Contains integration tests for the health monitoring endpoints.
/// </summary>
public class HealthEndpointsTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthEndpointsTests"/> class.
    /// </summary>
    /// <param name="factory">The custom web application factory.</param>
    public HealthEndpointsTests(ApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Verifies that the /healthz endpoint responds with a 200 OK status code.
    /// </summary>
    [Fact]
    public async Task GetHealth_ReturnsOkStatusCode()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/healthz");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
