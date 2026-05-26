using System.Net;
using System.Net.Http.Json;
using Chronos.API.Core.Entities;
using Xunit;

namespace Chronos.API.Tests.IntegrationTests;

/// <summary>
/// Provides comprehensive automated integration testing for all appointment management endpoints.
/// </summary>
public class AppointmentEndpointsTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppointmentEndpointsTests"/> class.
    /// </summary>
    /// <param name="factory">The custom web application factory.</param>
    public AppointmentEndpointsTests(ApiFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Verifies that GET /api/appointments returns a valid collection and 200 OK.
    /// </summary>
    [Fact]
    public async Task GetAppointments_ReturnsOkStatusCode()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/appointments");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var collection = await response.Content.ReadFromJsonAsync<List<Appointment>>();
        Assert.NotNull(collection);
    }

    /// <summary>
    /// Verifies that POST /api/appointments creates a new record and returns 201 Created.
    /// </summary>
    [Fact]
    public async Task PostAppointment_ReturnsCreatedStatusCode()
    {
        // Arrange
        var client = _factory.CreateClient();
        var payload = new
        {
            ClientName = "Comprehensive Test",
            Service = "Full Coverage",
            TargetedAt = DateTime.UtcNow.AddDays(7)
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/appointments", payload);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<Appointment>();
        Assert.NotNull(created);
        Assert.Equal(payload.ClientName, created.ClientName);
    }

    /// <summary>
    /// Verifies that POST /api/appointments returns 400 BadRequest for invalid past dates.
    /// </summary>
    [Fact]
    public async Task PostAppointment_PastDate_ReturnsBadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();
        var payload = new
        {
            ClientName = "Invalid Date",
            Service = "Past Service",
            TargetedAt = DateTime.UtcNow.AddDays(-1)
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/appointments", payload);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Verifies that GET /api/appointments/{id} returns 200 OK for an existing record.
    /// </summary>
    [Fact]
    public async Task GetAppointmentById_ExistingId_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();
        var payload = new { ClientName = "Lookup Test", Service = "Find Me", TargetedAt = DateTime.UtcNow.AddDays(2) };
        var postResponse = await client.PostAsJsonAsync("/api/appointments", payload);
        var created = await postResponse.Content.ReadFromJsonAsync<Appointment>();
        Assert.NotNull(created);

        // Act
        var response = await client.GetAsync($"/api/appointments/{created.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var found = await response.Content.ReadFromJsonAsync<Appointment>();
        Assert.NotNull(found);
        Assert.Equal(created.Id, found.Id);
    }

    /// <summary>
    /// Verifies that GET /api/appointments/{id} returns 404 NotFound for non-existent IDs.
    /// </summary>
    [Fact]
    public async Task GetAppointmentById_MissingId_ReturnsNotFound()
    {
        // Arrange
        var client = _factory.CreateClient();
        var missingId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/appointments/{missingId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>
    /// Verifies that PUT /api/appointments/{id} updates a record and returns 204 NoContent.
    /// </summary>
    [Fact]
    public async Task PutAppointment_ExistingId_ReturnsNoContent()
    {
        // Arrange
        var client = _factory.CreateClient();
        var postPayload = new { ClientName = "Update Target", Service = "Initial", TargetedAt = DateTime.UtcNow.AddDays(3) };
        var postResponse = await client.PostAsJsonAsync("/api/appointments", postPayload);
        var created = await postResponse.Content.ReadFromJsonAsync<Appointment>();
        Assert.NotNull(created);

        var putPayload = new { ClientName = "Updated Name", Service = "Updated Service", TargetedAt = DateTime.UtcNow.AddDays(4) };

        // Act
        var response = await client.PutAsJsonAsync($"/api/appointments/{created.Id}", putPayload);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify
        var verifyResponse = await client.GetAsync($"/api/appointments/{created.Id}");
        var updated = await verifyResponse.Content.ReadFromJsonAsync<Appointment>();
        Assert.NotNull(updated);
        Assert.Equal(putPayload.ClientName, updated.ClientName);
        Assert.Equal(putPayload.Service, updated.Service);
    }

    /// <summary>
    /// Verifies that DELETE /api/appointments/{id} removes a record and returns 204 NoContent.
    /// </summary>
    [Fact]
    public async Task DeleteAppointment_ExistingId_ReturnsNoContent()
    {
        // Arrange
        var client = _factory.CreateClient();
        var postPayload = new { ClientName = "Delete Target", Service = "Temporary", TargetedAt = DateTime.UtcNow.AddDays(5) };
        var postResponse = await client.PostAsJsonAsync("/api/appointments", postPayload);
        var created = await postResponse.Content.ReadFromJsonAsync<Appointment>();
        Assert.NotNull(created);

        // Act
        var response = await client.DeleteAsync($"/api/appointments/{created.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify
        var verifyResponse = await client.GetAsync($"/api/appointments/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, verifyResponse.StatusCode);
    }
}
