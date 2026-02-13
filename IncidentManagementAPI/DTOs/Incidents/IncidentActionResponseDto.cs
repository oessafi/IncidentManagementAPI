using IncidentManagementAPI.models.Enums;
using System;

namespace IncidentManagementAPI.DTOs.Incidents;

public class IncidentActionResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public MaintenanceType MaintenanceType { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public int? AssignerId { get; set; }
}
