using Chronos.API.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Chronos.API.Infrastructure.Data;

/// <summary>
/// Represents the Entity Framework Core database context for the Chronos API.
/// Manages the PostgreSQL schema and entity mappings.
/// </summary>
public class ChronosDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChronosDbContext"/> class.
    /// </summary>
    /// <param name="options">The options to be used by the DbContext.</param>
    public ChronosDbContext(DbContextOptions<ChronosDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the database set for managing appointment records.
    /// </summary>
    public DbSet<Appointment> Appointments => Set<Appointment>();

    /// <summary>
    /// Configures the model mapping and constraints for the PostgreSQL schema.
    /// </summary>
    /// <param name="modelBuilder">The builder used to define the database schema.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.ToTable("Appointments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ClientName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Service).IsRequired().HasMaxLength(200);
            entity.Property(e => e.TargetedAt).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
        });
    }
}
