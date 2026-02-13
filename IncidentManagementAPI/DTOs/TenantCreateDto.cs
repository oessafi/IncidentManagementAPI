namespace IncidentManagementAPI.DTOs.Tenants
{
    public class TenantCreateDto
    {
        public string TenantKey { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? ConnectionString { get; set; }
    }

    public class TenantUpdateDto
    {
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string? ConnectionString { get; set; }
    }

    public class TenantResponseDto
    {
        public int Id { get; set; }
        public string TenantKey { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool HasConnectionString { get; set; }
    }
}
