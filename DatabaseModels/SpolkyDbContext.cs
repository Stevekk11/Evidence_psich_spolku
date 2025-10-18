using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace API_psi_spolky.DatabaseModels;

public class SpolkyDbContext : IdentityDbContext<User>
{
    public SpolkyDbContext(DbContextOptions<SpolkyDbContext> options) : base(options) { }

    public DbSet<Spolek> Spolky { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Exhibition> Exhibitions { get; set; }
    public DbSet<ExhibitionResult> ExhibitionResults { get; set; }
    public DbSet<Dog> Dogs { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<Spolek>()
            .HasOne(s => s.Chairman)
            .WithMany()
            .HasForeignKey(s => s.ChairmanId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Dog>().HasIndex(d => d.Name).IsUnique();
        modelBuilder.Entity<Spolek>().HasIndex(s => s.Name).IsUnique();
        modelBuilder.Entity<Exhibition>().HasIndex(e => e.Name).IsUnique();
        modelBuilder.Entity<AuditLog>().HasIndex(a => a.UserId).IsUnique();
        modelBuilder.Entity<ExhibitionResult>().HasIndex(e => e.ExhibitionId).IsUnique();
    }
}