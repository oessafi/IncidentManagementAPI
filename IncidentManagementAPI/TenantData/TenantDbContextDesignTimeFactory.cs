using IncidentManagementAPI.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace IncidentManagementAPI.TenantData
{
    public class TenantDbContextDesignTimeFactory : IDesignTimeDbContextFactory<TenantDbContext>
    {
        public TenantDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var baseCs = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
            var tenantCs = TenantConnectionStringBuilder.BuildForTenant(baseCs, "design");

            var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
            optionsBuilder.UseSqlServer(tenantCs);

            return new TenantDbContext(optionsBuilder.Options);
        }
    }
}
