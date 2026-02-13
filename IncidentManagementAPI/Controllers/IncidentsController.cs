using System;
using System.Security.Claims;
using IncidentManagementAPI.Common;
using IncidentManagementAPI.DTOs.Incidents;
using IncidentManagementAPI.PlatformData;
using IncidentManagementAPI.TenantData;
using IncidentManagementAPI.models;
using IncidentManagementAPI.models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace IncidentManagementAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IncidentsController : ControllerBase
{
    private readonly PlatformDbContext _platformDb;
    private readonly TenantDbContextFactory _tenantDbFactory;
    private readonly IConfiguration _configuration;
    private readonly TenantProvisioningService _tenantProvisioning;

    public IncidentsController(
        PlatformDbContext platformDb,
        TenantDbContextFactory tenantDbFactory,
        IConfiguration configuration,
        TenantProvisioningService tenantProvisioning)
    {
        _platformDb = platformDb;
        _tenantDbFactory = tenantDbFactory;
        _configuration = configuration;
        _tenantProvisioning = tenantProvisioning;
    }

    [HttpGet]
        public async Task<ActionResult<IEnumerable<IncidentResponseDto>>> GetAll([FromQuery] int? projectId, [FromQuery] string? tenantKey, [FromQuery] bool assignedToSupportOnly = false)
        {
            var resolved = await ResolveTenantAsync(tenantKey);
            if (resolved == null)
            {
                return BadRequest("TenantKey is required.");
            }

            await using var tenantDb = _tenantDbFactory.Create(resolved.ConnectionString);

            IQueryable<Incident> query = tenantDb.Incidents
            .AsNoTracking()
            .Include(i => i.Actions)
            .Include(i => i.Comments);

        if (assignedToSupportOnly)
        {
            if (!IsSupportUser())
            {
                return Forbid("Accès réservé aux supports.");
            }

            var supportUserId = GetCurrentUserId();
            if (!supportUserId.HasValue)
            {
                return Forbid("Impossible d'identifier l'utilisateur support.");
            }

            query = query.Where(i => i.AssignedSupportUserId == supportUserId.Value);
        }

        query = query.OrderByDescending(i => i.DateCreatedUtc);

        if (projectId.HasValue)
        {
            query = query.Where(i => i.ProjectId == projectId.Value);
        }

            var incidents = await query.ToListAsync();
            var supports = await LoadSupportLookupAsync(incidents);
            return Ok(incidents.Select(i => MapToDto(i, supports)));
        }

    [HttpGet("{id:int}")]
        public async Task<ActionResult<IncidentResponseDto>> GetById(int id, [FromQuery] string? tenantKey)
        {
            var resolved = await ResolveTenantAsync(tenantKey);
            if (resolved == null)
            {
                return BadRequest("TenantKey is required.");
            }

            await using var tenantDb = _tenantDbFactory.Create(resolved.ConnectionString);

        var incident = await tenantDb.Incidents
            .AsNoTracking()
            .Include(i => i.Actions)
            .Include(i => i.Comments)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (incident == null)
        {
            return NotFound();
        }

            var supports = await LoadSupportLookupAsync(new[] { incident });
            return Ok(MapToDto(incident, supports));
        }

    [HttpPost]
    public async Task<ActionResult<IncidentResponseDto>> Create([FromBody] IncidentCreateDto dto, [FromQuery] string? tenantKey)
    {
        if (dto == null)
        {
            return BadRequest("Payload required.");
        }

        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            return BadRequest("Title is required.");
        }

        var resolved = await ResolveTenantAsync(tenantKey);
        if (resolved == null)
        {
            return BadRequest("TenantKey is required.");
        }

        await using var tenantDb = _tenantDbFactory.Create(resolved.ConnectionString);

        var incident = new Incident
        {
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim(),
            DateEstimatedUtc = dto.DateEstimatedUtc,
            DateResolvedUtc = dto.DateResolvedUtc,
            Status = dto.Status,
            Priority = dto.Priority,
            IncidentType = dto.IncidentType,
            ProjectId = dto.ProjectId
        };

        tenantDb.Incidents.Add(incident);
        await tenantDb.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = incident.Id, tenantKey = resolved.Tenant.TenantKey }, MapToDto(incident));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] IncidentUpdateDto dto, [FromQuery] string? tenantKey)
    {
        if (dto == null)
        {
            return BadRequest("Payload required.");
        }

        var resolved = await ResolveTenantAsync(tenantKey);
        if (resolved == null)
        {
            return BadRequest("TenantKey is required.");
        }

        await using var tenantDb = _tenantDbFactory.Create(resolved.ConnectionString);

        var incident = await tenantDb.Incidents.FirstOrDefaultAsync(i => i.Id == id);
        if (incident == null)
        {
            return NotFound();
        }

        incident.Title = dto.Title.Trim();
        incident.Description = dto.Description?.Trim();
        incident.DateEstimatedUtc = dto.DateEstimatedUtc;
        incident.DateResolvedUtc = dto.DateResolvedUtc;
        incident.Status = dto.Status;
        incident.Priority = dto.Priority;
        incident.IncidentType = dto.IncidentType;

        await tenantDb.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] string? tenantKey)
    {
        var resolved = await ResolveTenantAsync(tenantKey);
        if (resolved == null)
        {
            return BadRequest("TenantKey is required.");
        }

        await using var tenantDb = _tenantDbFactory.Create(resolved.ConnectionString);

        var incident = await tenantDb.Incidents.FirstOrDefaultAsync(i => i.Id == id);
        if (incident == null)
        {
            return NotFound();
        }

        tenantDb.Incidents.Remove(incident);
        await tenantDb.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("{incidentId:int}/comments")]
    public async Task<ActionResult<IEnumerable<IncidentCommentResponseDto>>> GetComments(int incidentId, [FromQuery] string? tenantKey)
    {
        var resolved = await ResolveTenantAsync(tenantKey);
        if (resolved == null)
        {
            return BadRequest("TenantKey is required.");
        }

        await using var tenantDb = _tenantDbFactory.Create(resolved.ConnectionString);

        var comments = await tenantDb.Comments
            .AsNoTracking()
            .Where(c => c.IncidentId == incidentId)
            .OrderBy(c => c.CreatedAtUtc)
            .ToListAsync();

        return Ok(comments.Select(MapCommentToDto));
    }

    [HttpPost("{incidentId:int}/comments")]
    public async Task<ActionResult<IncidentCommentResponseDto>> AddComment(int incidentId, [FromBody] IncidentCommentCreateDto dto, [FromQuery] string? tenantKey)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Content))
        {
            return BadRequest("Content is required.");
        }

        var resolved = await ResolveTenantAsync(tenantKey);
        if (resolved == null)
        {
            return BadRequest("TenantKey is required.");
        }

        await using var tenantDb = _tenantDbFactory.Create(resolved.ConnectionString);

        var incident = await tenantDb.Incidents.FindAsync(incidentId);
        if (incident == null)
        {
            return NotFound();
        }

        var comment = new Comment
        {
            Content = dto.Content.Trim(),
            IncidentId = incidentId
        };

        tenantDb.Comments.Add(comment);
        await tenantDb.SaveChangesAsync();

        return CreatedAtAction(nameof(GetComments), new { incidentId, tenantKey = resolved.Tenant.TenantKey }, MapCommentToDto(comment));
    }

    [HttpGet("{incidentId:int}/actions")]
    public async Task<ActionResult<IEnumerable<IncidentActionResponseDto>>> GetActions(int incidentId, [FromQuery] string? tenantKey)
    {
        var resolved = await ResolveTenantAsync(tenantKey);
        if (resolved == null)
        {
            return BadRequest("TenantKey is required.");
        }

        await using var tenantDb = _tenantDbFactory.Create(resolved.ConnectionString);

        var actions = await tenantDb.IncidentActions
            .AsNoTracking()
            .Where(a => a.IncidentId == incidentId)
            .OrderByDescending(a => a.CreatedAtUtc)
            .ToListAsync();

        return Ok(actions.Select(MapActionToDto));
    }

        [HttpPost("{incidentId:int}/actions")]
        public async Task<ActionResult<IncidentActionResponseDto>> AddAction(int incidentId, [FromBody] IncidentActionCreateDto dto, [FromQuery] string? tenantKey)
        {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Title))
        {
            return BadRequest("Title is required.");
        }

        var resolved = await ResolveTenantAsync(tenantKey);
        if (resolved == null)
        {
            return BadRequest("TenantKey is required.");
        }

        await using var tenantDb = _tenantDbFactory.Create(resolved.ConnectionString);

        var incident = await tenantDb.Incidents.FindAsync(incidentId);
        if (incident == null)
        {
            return NotFound();
        }

        var action = new IncidentAction
        {
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim(),
            MaintenanceType = dto.MaintenanceType,
            AssignerId = dto.AssignerId,
            IncidentId = incidentId
        };

        tenantDb.IncidentActions.Add(action);
        await tenantDb.SaveChangesAsync();

            return CreatedAtAction(nameof(GetActions), new { incidentId, tenantKey = resolved.Tenant.TenantKey }, MapActionToDto(action));
        }

        [HttpPatch("{id:int}/assign-support")]
        public async Task<IActionResult> AssignSupport(int id, IncidentAssignSupportDto dto, [FromQuery] string? tenantKey)
        {
            if (dto == null)
            {
                return BadRequest("Payload required.");
            }

            var resolved = await ResolveTenantAsync(tenantKey);
            if (resolved == null)
            {
                return BadRequest("TenantKey is required.");
            }

            int? supportUserId = dto.SupportUserId;
            if (supportUserId.HasValue)
            {
                var supportUser = await _platformDb.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == supportUserId.Value && u.Role == UserRole.Support && u.IsActive);
                if (supportUser == null)
                {
                    return BadRequest("Support user invalide.");
                }
            }

            await using var tenantDb = _tenantDbFactory.Create(resolved.ConnectionString);
            var incident = await tenantDb.Incidents.FirstOrDefaultAsync(i => i.Id == id);
            if (incident == null)
            {
                return NotFound();
            }

            incident.AssignedSupportUserId = supportUserId;
            await tenantDb.SaveChangesAsync();

            return NoContent();
        }

        private static IncidentResponseDto MapToDto(Incident incident, IReadOnlyDictionary<int, string>? supportLookup = null) =>
            new()
            {
                Id = incident.Id,
            ProjectId = incident.ProjectId,
            Title = incident.Title,
            Description = incident.Description,
            DateCreatedUtc = incident.DateCreatedUtc,
            DateEstimatedUtc = incident.DateEstimatedUtc,
            DateResolvedUtc = incident.DateResolvedUtc,
            Status = incident.Status,
            Priority = incident.Priority,
            IncidentType = incident.IncidentType,
            ActionsCount = incident.Actions?.Count ?? 0,
            CommentsCount = incident.Comments?.Count ?? 0,
            AssignedSupportUserId = incident.AssignedSupportUserId,
            AssignedSupportName = incident.AssignedSupportUserId.HasValue && supportLookup != null && supportLookup.TryGetValue(incident.AssignedSupportUserId.Value, out var supportName)
                ? supportName
                : null
        };

        private async Task<IReadOnlyDictionary<int, string>> LoadSupportLookupAsync(IEnumerable<Incident> incidents)
        {
            var ids = incidents
                .Where(i => i.AssignedSupportUserId != null)
                .Select(i => i.AssignedSupportUserId!.Value)
                .Distinct()
                .ToList();

            if (ids.Count == 0)
            {
                return new Dictionary<int, string>();
            }

            return await _platformDb.Users
                .AsNoTracking()
                .Where(u => ids.Contains(u.Id) && u.Role == UserRole.Support && u.IsActive)
                .Select(u => new
                {
                    u.Id,
                    FullName = (u.FirstName + " " + u.LastName).Trim()
                })
                .ToDictionaryAsync(u => u.Id, u => u.FullName);
        }

    private static IncidentCommentResponseDto MapCommentToDto(Comment comment) =>
        new()
        {
            Id = comment.Id,
            Content = comment.Content,
            CreatedAtUtc = comment.CreatedAtUtc
        };

        private static IncidentActionResponseDto MapActionToDto(IncidentAction action) =>
        new()
        {
            Id = action.Id,
            Title = action.Title,
            Description = action.Description,
            MaintenanceType = action.MaintenanceType,
            CreatedAtUtc = action.CreatedAtUtc,
            AssignerId = action.AssignerId
        };

    private async Task<ResolvedTenant?> ResolveTenantAsync(string? tenantKeyOverride)
    {
            var tenantKey = GetTenantKeyFromRequest(tenantKeyOverride);
            var (tenant, fromClaim) = await ResolveTenantFromClaimsOrKeyAsync(tenantKey);
            if (tenant == null)
            {
                return null;
            }

            if (fromClaim && !string.IsNullOrWhiteSpace(tenantKey) &&
                !string.Equals(tenantKey.Trim(), tenant.TenantKey, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var connectionString = GetConnectionStringForTenant(tenant);
            await _tenantProvisioning.EnsureTenantDatabaseAsync(connectionString);
            return new ResolvedTenant(tenant, connectionString);
        }

    private string? GetTenantKeyFromRequest(string? tenantKeyOverride)
    {
        if (!string.IsNullOrWhiteSpace(tenantKeyOverride))
        {
            return tenantKeyOverride;
        }

        if (Request.Headers.TryGetValue("X-Tenant-Key", out var headerValue))
        {
            return headerValue.FirstOrDefault();
        }

        if (Request.Query.TryGetValue("tenantKey", out var queryValue))
        {
            return queryValue.FirstOrDefault();
        }

        return null;
    }

    private int? GetCurrentUserId()
    {
        var claimValue = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(claimValue))
        {
            return null;
        }

        return int.TryParse(claimValue, out var id) ? id : null;
    }

    private bool IsSupportUser()
    {
        var roleClaim = User?.FindFirst(ClaimTypes.Role)?.Value;
        return string.Equals(roleClaim, UserRole.Support.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private async Task<(Tenant? Tenant, bool FromClaim)> ResolveTenantFromClaimsOrKeyAsync(string? tenantKey)
    {
        var tenantFromClaim = await GetTenantFromClaimsAsync();
        if (tenantFromClaim != null)
        {
            return (tenantFromClaim, true);
        }

        if (string.IsNullOrWhiteSpace(tenantKey))
        {
            return (null, false);
        }

        var tk = tenantKey.Trim().ToLowerInvariant();
        var tenant = await _platformDb.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.IsActive && t.TenantKey.ToLower() == tk);

        return (tenant, false);
    }

    private async Task<Tenant?> GetTenantFromClaimsAsync()
    {
        if (User?.Identity is not { IsAuthenticated: true })
        {
            return null;
        }

        var claimValue = User.FindFirst("tenantId")?.Value;
        if (string.IsNullOrWhiteSpace(claimValue))
        {
            return null;
        }

        if (!int.TryParse(claimValue, out var tenantId))
        {
            return null;
        }

        return await _platformDb.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.IsActive && t.Id == tenantId);
    }

    private string GetConnectionStringForTenant(Tenant tenant)
    {
        var connectionString = tenant.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            var baseCs = _configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
            connectionString = TenantConnectionStringBuilder.BuildForTenant(baseCs, tenant.TenantKey);
        }

        return connectionString;
    }

    private sealed record ResolvedTenant(Tenant Tenant, string ConnectionString);
}
