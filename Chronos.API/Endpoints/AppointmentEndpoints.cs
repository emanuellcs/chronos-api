using Chronos.API.Core.Entities;
using Chronos.API.Infrastructure.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Chronos.API.Endpoints;

/// <summary>
/// Represents the data transfer object utilized for the creation of a new appointment entity.
/// </summary>
/// <param name="ClientName">The formal name of the client associated with the appointment.</param>
/// <param name="Service">The specific professional service requested for this time slot.</param>
/// <param name="TargetedAt">The precisely scheduled date and time for the appointment commencement.</param>
public record CreateAppointmentRequest(string ClientName, string Service, DateTime TargetedAt);

/// <summary>
/// Represents the data transfer object utilized for updating the properties of an existing appointment entity.
/// </summary>
/// <param name="ClientName">The updated name of the client associated with the appointment.</param>
/// <param name="Service">The updated service designation.</param>
/// <param name="TargetedAt">The adjusted date and time for the appointment.</param>
public record UpdateAppointmentRequest(string ClientName, string Service, DateTime TargetedAt);

/// <summary>
/// Defines the structural routing maps and HTTP channels for the Appointment domain operations.
/// </summary>
public static class AppointmentEndpoints
{
    /// <summary>
    /// Binds the complete matrix of vertical-slice HTTP endpoints for the Appointment entity to the application routing pipeline.
    /// </summary>
    /// <param name="app">The route builder instance used to configure the application's request pipeline.</param>
    public static void MapAppointmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/appointments")
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
        .WithSummary("List Appointments")
        .WithDescription("Retrieves a complete chronological list of future appointment records by querying the backend relational mesh. Filters out historical appointments at the database level to optimize payload and maintain strict domain relevancy.")
        .Produces<List<Appointment>>(StatusCodes.Status200OK);

        group.MapGet("/{id:guid}", async (Guid id, ChronosDbContext db) =>
        {
            var appointment = await db.Appointments.FindAsync(id);
            return appointment is not null ? Results.Ok(appointment) : Results.NotFound();
        })
        .WithName("GetAppointmentById")
        .WithSummary("Get Appointment")
        .WithDescription("Queries the persistence infrastructure for a single appointment record identified by its unique GUID. Returns the complete entity state upon discovery or a terminal not-found response if the identifier does not exist in the relational mesh.")
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
        .WithSummary("Create Appointment")
        .WithDescription("Executes a secured persistence operation to generate a new appointment record within the backend infrastructure. Enforces strict domain guardrails by validating client name presence and ensuring the target date exists in the future. Validation violations immediately reject the payload.")
        .Produces<Appointment>(StatusCodes.Status201Created)
        .Produces<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest);

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
        .WithSummary("Update Appointment")
        .WithDescription("Synchronizes the state of an existing appointment entity with the provided technical payload. Validates temporal constraints against the current system clock and ensures the persistence layer maintains record integrity within the relational database mesh.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest)
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
        .WithSummary("Delete Appointment")
        .WithDescription("Permanently de-registers an appointment record from the relational mesh. This operation is idempotent and terminal, ensuring the target identifier is no longer available for retrieval or modification.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);
    }
}
