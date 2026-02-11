using IncidentManagementAPI.models;
using Microsoft.EntityFrameworkCore;

namespace IncidentManagementAPI.PlatformData
{
    public class PlatformDbContext : DbContext
    {
        public PlatformDbContext(DbContextOptions<PlatformDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Tenant> Tenants => Set<Tenant>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<MfaChallenge> MfaChallenges => Set<MfaChallenge>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasIndex(x => x.Email).IsUnique();
            modelBuilder.Entity<Tenant>().HasIndex(x => x.TenantKey).IsUnique();
            modelBuilder.Entity<RefreshToken>().HasIndex(x => x.TokenHash);
            modelBuilder.Entity<MfaChallenge>().HasIndex(x => x.TempTokenHash);
        }
    }
}
