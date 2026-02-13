using IncidentManagementAPI.models.Enums;
using System;

namespace IncidentManagementAPI.DTOs.Incidents;

public class IncidentUpdateDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? DateEstimatedUtc { get; set; }
    public DateTime? DateResolvedUtc { get; set; }
    public IncidentStatus Status { get; set; } = IncidentStatus.Open;
    public IncidentPriority Priority { get; set; } = IncidentPriority.Medium;
    public IncidentType IncidentType { get; set; } = IncidentType.Technical;
}
