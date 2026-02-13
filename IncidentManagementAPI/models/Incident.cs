using IncidentManagementAPI.models.Enums;
using System;
using System.Collections.Generic;

namespace IncidentManagementAPI.models;

public class Incident
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime DateCreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DateEstimatedUtc { get; set; }
    public DateTime? DateResolvedUtc { get; set; }

    public IncidentStatus Status { get; set; } = IncidentStatus.Open;
    public IncidentPriority Priority { get; set; } = IncidentPriority.Medium;
    public IncidentType IncidentType { get; set; } = IncidentType.Technical;

    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    public ICollection<IncidentAction> Actions { get; set; } = new List<IncidentAction>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public int? AssignedSupportUserId { get; set; }
}
