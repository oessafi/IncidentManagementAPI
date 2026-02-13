using IncidentManagementAPI.models.Enums;

namespace IncidentManagementAPI.DTOs.Incidents;

public class IncidentActionCreateDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public MaintenanceType MaintenanceType { get; set; } = MaintenanceType.Corrective;
    public int? AssignerId { get; set; }
}
