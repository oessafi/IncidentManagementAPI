namespace IncidentManagementAPI.models
{
    public class Project
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;
    }
}
