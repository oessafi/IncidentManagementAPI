using System;

namespace IncidentManagementAPI.models;

public class Comment
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public int IncidentId { get; set; }
    public Incident Incident { get; set; } = null!;
}
