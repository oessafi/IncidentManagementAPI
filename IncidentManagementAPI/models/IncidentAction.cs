using IncidentManagementAPI.models.Enums;
using System;

namespace IncidentManagementAPI.models;

public class IncidentAction
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public MaintenanceType MaintenanceType { get; set; } = MaintenanceType.Corrective;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public int IncidentId { get; set; }
    public Incident Incident { get; set; } = null!;
    public int? AssignerId { get; set; }
}
