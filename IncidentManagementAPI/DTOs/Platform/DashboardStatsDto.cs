namespace IncidentManagementAPI.DTOs.Platform;

public class DashboardStatsDto
{
    public int ActiveTenants { get; set; }
    public int TotalIncidents { get; set; }
    public int PendingIncidents { get; set; }
    public int ResolvedIncidents { get; set; }
    public int TotalUsers { get; set; }
}
