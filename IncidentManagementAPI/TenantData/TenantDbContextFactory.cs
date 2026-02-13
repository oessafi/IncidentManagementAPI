using Microsoft.EntityFrameworkCore;

namespace IncidentManagementAPI.TenantData
{
    public class TenantDbContextFactory
    {
        public TenantDbContext Create(string connectionString)
        {
            var options = new DbContextOptionsBuilder<TenantDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            return new TenantDbContext(options);
        }
    }
}
