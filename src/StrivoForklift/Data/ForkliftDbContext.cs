using Microsoft.EntityFrameworkCore;
using StrivoForklift.Models;

namespace StrivoForklift.Data;

/// <summary>
/// EF Core database context for forklift event storage.
/// </summary>
public class ForkliftDbContext : DbContext
{
    public ForkliftDbContext(DbContextOptions<ForkliftDbContext> options) : base(options)
    {
    }

    public DbSet<ForkliftEvent> ForkliftEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ForkliftEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).IsRequired();
            entity.Property(e => e.Timestamp).IsRequired();
            entity.Property(e => e.LastUpdated).IsRequired();
        });
    }
}
