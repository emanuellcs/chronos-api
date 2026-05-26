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
/// Data transfer object for updating an existing appointment.
/// </summary>
/// <param name="ClientName">The name of the client.</param>
/// <param name="Service">The requested service.</param>
/// <param name="TargetedAt">The scheduled date and time.</param>
public record UpdateAppointmentRequest(string ClientName, string Service, DateTime TargetedAt);

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

        group.MapGet("/{id:guid}", async (Guid id, ChronosDbContext db) =>
        {
            var appointment = await db.Appointments.FindAsync(id);
            return appointment is not null ? Results.Ok(appointment) : Results.NotFound();
        })
        .WithName("GetAppointmentById")
        .WithSummary("Retrieves a specific appointment record by its unique identifier.")
        .Produces<Appointment>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

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

        group.MapPut("/{id:guid}", async (Guid id, UpdateAppointmentRequest request, ChronosDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(request.ClientName))
            {
                return Results.BadRequest("Client name is required.");
            }

            if (request.TargetedAt <= DateTime.UtcNow)
            {
                return Results.BadRequest("Appointment date must be in the future.");
            }

            var appointment = await db.Appointments.FindAsync(id);
            if (appointment is null)
            {
                return Results.NotFound();
            }

            appointment.ClientName = request.ClientName;
            appointment.Service = request.Service;
            appointment.TargetedAt = request.TargetedAt;
            appointment.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            return Results.NoContent();
        })
        .WithName("UpdateAppointment")
        .WithSummary("Updates an existing appointment record with new data.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", async (Guid id, ChronosDbContext db) =>
        {
            var appointment = await db.Appointments.FindAsync(id);
            if (appointment is null)
            {
                return Results.NotFound();
            }

            db.Appointments.Remove(appointment);
            await db.SaveChangesAsync();

            return Results.NoContent();
        })
        .WithName("DeleteAppointment")
        .WithSummary("Securely removes an appointment record from the database.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);
    }
}
