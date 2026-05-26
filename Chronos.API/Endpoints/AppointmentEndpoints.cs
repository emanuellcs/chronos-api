using Chronos.API.Core.Entities;
using Chronos.API.Infrastructure.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Chronos.API.Endpoints;

/// <summary>
/// Data transfer object for creating a new appointment.
/// </summary>
/// <param name="ClientName">The name of the client.</param>
/// <param name="Service">The requested service.</param>
/// <param name="TargetedAt">The scheduled date and time.</param>
public record CreateAppointmentRequest(string ClientName, string Service, DateTime TargetedAt);

/// <summary>
/// Defines the decoupled routing sheets for Appointment domain operations.
/// </summary>
public static class AppointmentEndpoints
{
    /// <summary>
    /// Maps all vertical-slice HTTP channels for the Appointment entity.
    /// </summary>
    /// <param name="app">The route builder instance.</param>
    public static void MapAppointmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/appointments")
                       .WithOpenApi()
                       .WithTags("Appointments");

        group.MapGet("/", async (ChronosDbContext db) =>
        {
            var now = DateTime.UtcNow;
            var appointments = await db.Appointments
                .Where(a => a.TargetedAt >= now)
                .OrderBy(a => a.TargetedAt)
                .AsNoTracking()
                .ToListAsync();

            return Results.Ok(appointments);
        })
        .WithName("GetFutureAppointments")
        .WithSummary("Retrieves all future appointment records ordered chronologically.")
        .Produces<List<Appointment>>(StatusCodes.Status200OK);

        group.MapPost("/", async (CreateAppointmentRequest request, ChronosDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.ClientName))
            {
                return Results.BadRequest("Client name is required.");
            }

            if (request.TargetedAt <= DateTime.UtcNow)
            {
                return Results.BadRequest("Appointment date must be in the future.");
            }

            var appointment = new Appointment
            {
                ClientName = request.ClientName,
                Service = request.Service,
                TargetedAt = request.TargetedAt
            };

            db.Appointments.Add(appointment);
            await db.SaveChangesAsync();

            return Results.Created($"/api/appointments/{appointment.Id}", appointment);
        })
        .WithName("CreateAppointment")
        .WithSummary("Validates an incoming payload and securely persists the appointment.")
        .Produces<Appointment>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest);
    }
}
