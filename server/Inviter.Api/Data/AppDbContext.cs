using Inviter.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Inviter.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Event> Events => Set<Event>();
    public DbSet<Rsvp> Rsvps => Set<Rsvp>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Event>(e =>
        {
            e.Property(x => x.Title).IsRequired().HasMaxLength(200);
            e.Property(x => x.Description).HasMaxLength(4000);
            e.Property(x => x.Location).HasMaxLength(500);
            e.Property(x => x.InviteToken).IsRequired().HasMaxLength(64);
            e.Property(x => x.AdminToken).IsRequired().HasMaxLength(128);
            e.Property(x => x.AllowMaybe).HasDefaultValue(true);
            e.Property(x => x.ContactRequirement).HasConversion<int>().HasDefaultValue(ContactRequirement.None);
            e.Property(x => x.OrganizerEmail).HasMaxLength(320);
            e.Property(x => x.OrganizerName).HasMaxLength(200);
            e.HasIndex(x => x.InviteToken).IsUnique();
            e.HasIndex(x => x.AdminToken).IsUnique();
            e.HasMany(x => x.Rsvps)
                .WithOne(r => r.Event!)
                .HasForeignKey(r => r.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Rsvp>(r =>
        {
            r.Property(x => x.GuestName).IsRequired().HasMaxLength(200);
            r.Property(x => x.Comment).HasMaxLength(2000);
            r.Property(x => x.Status).HasConversion<int>();
            r.Property(x => x.Email).HasMaxLength(200);
            r.Property(x => x.Phone).HasMaxLength(50);
            r.HasIndex(x => x.EventId);
        });
    }
}
