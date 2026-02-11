namespace IncidentManagementAPI.models
{
    public class Tenant
    {
        public int Id { get; set; }
        public string TenantKey { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        // Phase 2 (DB tenant)
        public string? ConnectionString { get; set; }
    }
}
