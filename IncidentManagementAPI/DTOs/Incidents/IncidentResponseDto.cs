using IncidentManagementAPI.models.Enums;
using System;

namespace IncidentManagementAPI.DTOs.Incidents;

public class IncidentResponseDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime DateCreatedUtc { get; set; }
    public DateTime? DateEstimatedUtc { get; set; }
    public DateTime? DateResolvedUtc { get; set; }
    public IncidentStatus Status { get; set; }
    public IncidentPriority Priority { get; set; }
    public IncidentType IncidentType { get; set; }
    public int ActionsCount { get; set; }
    public int CommentsCount { get; set; }
    public int? AssignedSupportUserId { get; set; }
    public string? AssignedSupportName { get; set; }
}
