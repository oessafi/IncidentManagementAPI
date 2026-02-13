using IncidentManagementAPI.models;
using Microsoft.EntityFrameworkCore;

namespace IncidentManagementAPI.TenantData
{
    public class TenantDbContext : DbContext
    {
        public TenantDbContext(DbContextOptions<TenantDbContext> options)
            : base(options)
        {
        }

        public DbSet<Project> Projects => Set<Project>();
        public DbSet<Incident> Incidents => Set<Incident>();
        public DbSet<IncidentAction> IncidentActions => Set<IncidentAction>();
        public DbSet<Comment> Comments => Set<Comment>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Project>()
                .HasIndex(p => p.Name)
                .IsUnique();

            modelBuilder.Entity<Incident>()
                .HasOne(i => i.Project)
                .WithMany()
                .HasForeignKey(i => i.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Incident>()
                .HasMany(i => i.Actions)
                .WithOne(a => a.Incident)
                .HasForeignKey(a => a.IncidentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Incident>()
                .HasMany(i => i.Comments)
                .WithOne(c => c.Incident)
                .HasForeignKey(c => c.IncidentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Incident)
                .WithMany(i => i.Comments)
                .HasForeignKey(c => c.IncidentId);
        }
    }
}
