namespace IncidentManagementAPI.models
{
    public class AuditLog
    {
        public long Id { get; set; }
        public int? TenantId { get; set; }
        public int? UserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? Details { get; set; }
        public DateTime AtUtc { get; set; } = DateTime.UtcNow;
    }
}
