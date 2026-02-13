using System;

namespace IncidentManagementAPI.DTOs.Incidents;

public class IncidentCommentResponseDto
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
