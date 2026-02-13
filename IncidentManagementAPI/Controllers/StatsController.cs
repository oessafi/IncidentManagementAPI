using System.Linq;
using System.Security.Claims;
using System.Threading;
using IncidentManagementAPI.Common;
using IncidentManagementAPI.DTOs.Platform;
using IncidentManagementAPI.PlatformData;
using IncidentManagementAPI.models.Enums;
using IncidentManagementAPI.models;
using IncidentManagementAPI.TenantData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IncidentManagementAPI.Controllers;

[ApiController]
[Route("api/stats")]
[Authorize]
public class StatsController : ControllerBase
{
    private static readonly string[] AllowedRoleNames = { "SUPERADMIN", "ADMIN", "ADMINCLIENT" };

    private readonly PlatformDbContext _platformDb;
    private readonly TenantDbContextFactory _tenantDbFactory;
    private readonly TenantProvisioningService _tenantProvisioning;
    private readonly IConfiguration _configuration;

    public StatsController(
        PlatformDbContext platformDb,
        TenantDbContextFactory tenantDbFactory,
        TenantProvisioningService tenantProvisioning,
        IConfiguration configuration)
    {
        _platformDb = platformDb;
        _tenantDbFactory = tenantDbFactory;
        _tenantProvisioning = tenantProvisioning;
        _configuration = configuration;
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardStatsDto>> GetDashboardStats(CancellationToken cancellationToken)
    {
        if (!UserHasAccess())
        {
            return Forbid();
        }

        var baseConnectionString = _configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        var tenants = await _platformDb.Tenants
            .AsNoTracking()
            .Where(t => t.IsActive)
            .ToListAsync(cancellationToken);

        var dto = new DashboardStatsDto
        {
            ActiveTenants = tenants.Count,
            TotalUsers = await _platformDb.Users.CountAsync(cancellationToken)
        };

        foreach (var tenant in tenants)
        {
            var connectionString = GetConnectionStringForTenant(tenant, baseConnectionString);
            await _tenantProvisioning.EnsureTenantDatabaseAsync(connectionString, cancellationToken);

            await using var tenantDb = _tenantDbFactory.Create(connectionString);
            var incidents = tenantDb.Incidents.AsNoTracking();

            var total = await incidents.CountAsync(cancellationToken);
            var resolved = await incidents.CountAsync(i => i.Status == IncidentStatus.Closed, cancellationToken);

            dto.TotalIncidents += total;
            dto.ResolvedIncidents += resolved;
            dto.PendingIncidents += total - resolved;
        }

        return Ok(dto);
    }

    private string GetConnectionStringForTenant(Tenant tenant, string baseConnectionString)
    {
        var connectionString = tenant.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = TenantConnectionStringBuilder.BuildForTenant(baseConnectionString, tenant.TenantKey);
        }

        return connectionString;
    }

    private bool UserHasAccess()
    {
        var role = User?.FindFirst(ClaimTypes.Role)?.Value;
        if (string.IsNullOrWhiteSpace(role))
        {
            return false;
        }

        return AllowedRoleNames.Contains(role, StringComparer.OrdinalIgnoreCase);
    }
}
