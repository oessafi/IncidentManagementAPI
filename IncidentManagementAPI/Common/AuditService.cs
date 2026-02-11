using IncidentManagementAPI.PlatformData;
using IncidentManagementAPI.models;

namespace IncidentManagementAPI.Common
{
    public class AuditService
    {
        private readonly PlatformDbContext _db;
        public AuditService(PlatformDbContext db) => _db = db;

        public async Task LogAsync(int? userId, int? tenantId, string action, string? details = null)
        {
            _db.AuditLogs.Add(new AuditLog
            {
                UserId = userId,
                TenantId = tenantId,
                Action = action,
                Details = details
            });
            await _db.SaveChangesAsync();
        }
    }
}
