using System.ComponentModel.DataAnnotations;

namespace Chronos.API.Core.Entities;

/// <summary>
/// Represents a high-integrity domain model for an appointment within the Chronos system.
/// </summary>
public class Appointment
{
    /// <summary>
    /// Gets or initializes the unique identification key for the appointment.
    /// </summary>
    [Key]
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the name of the client associated with the appointment.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string ClientName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the specific service requested for this appointment.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Service { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the targeted date and time for the appointment.
    /// </summary>
    /// <remarks>
    /// Domain validation should ensure that appointments are not scheduled in the past.
    /// </remarks>
    [Required]
    public DateTime TargetedAt { get; set; }

    /// <summary>
    /// Gets or sets the audit timestamp indicating when the record was initially created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the audit timestamp indicating the last time the record was modified.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
