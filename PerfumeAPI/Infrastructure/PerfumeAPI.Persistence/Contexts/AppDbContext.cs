using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PerfumeAPI.Domain.Entities;
using PerfumeAPI.Domain.Entities.Identity;
using System.Reflection.Emit;

namespace PerfumeAPI.Persistence.Contexts;

public class AppDbContext : IdentityDbContext<AppUser, AppRole, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Perfume> Perfumes { get; set; }
    public DbSet<UserPerfumeInteraction> UserPerfumeInteractions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        
        modelBuilder.Entity<UserPerfumeInteraction>()
        .HasIndex(u => new { u.UserId, u.PerfumeId })
        .IsUnique();

        
        modelBuilder.Entity<UserPerfumeInteraction>()
            .HasOne(u => u.User)
            .WithMany()
            .HasForeignKey(u => u.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserPerfumeInteraction>()
            .HasOne(u => u.Perfume)
            .WithMany()
            .HasForeignKey(u => u.PerfumeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
