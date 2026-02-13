namespace IncidentManagementAPI.DTOs.Projects
{
    public class ProjectCreateDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        // optional: allow tenantKey in body when header is not used
        public string? TenantKey { get; set; }
    }

    public class ProjectUpdateDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class ProjectResponseDto
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public string TenantKey { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        public bool IsActive { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public string TenantName { get; set; } = string.Empty;
    }
}
