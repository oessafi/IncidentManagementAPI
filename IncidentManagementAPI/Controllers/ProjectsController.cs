using System;
using IncidentManagementAPI.Common;
using IncidentManagementAPI.DTOs.Projects;
using IncidentManagementAPI.PlatformData;
using IncidentManagementAPI.TenantData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IncidentManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectsController : ControllerBase
    {
        private readonly PlatformDbContext _platformDb;
        private readonly TenantDbContextFactory _tenantDbFactory;
        private readonly IConfiguration _configuration;
        private readonly TenantProvisioningService _tenantProvisioning;

        public ProjectsController(
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
        public async Task<ActionResult<IEnumerable<ProjectResponseDto>>> GetAll([FromQuery] string? tenantKey, [FromQuery] bool includeAllTenants = false)
        {
            if (includeAllTenants)
            {
                var tenants = await _platformDb.Tenants
                    .AsNoTracking()
                    .Where(t => t.IsActive)
                    .OrderBy(t => t.Name)
                    .ToListAsync();

                var projects = new List<ProjectResponseDto>();
                foreach (var tenant in tenants)
                {
                    var tenantCs = GetConnectionStringForTenant(tenant);
                    await using var tenantDbContext = _tenantDbFactory.Create(tenantCs);

                    var tenantProjects = await tenantDbContext.Projects
                        .AsNoTracking()
                        .OrderByDescending(p => p.CreatedAtUtc)
                        .Select(p => new ProjectResponseDto
                        {
                            Id = p.Id,
                            TenantId = tenant.Id,
                            TenantKey = tenant.TenantKey,
                            TenantName = tenant.Name,
                            Name = p.Name,
                            Description = p.Description,
                            IsActive = p.IsActive,
                            CreatedAtUtc = p.CreatedAtUtc
                        })
                        .ToListAsync();

                    projects.AddRange(tenantProjects);
                }

                var ordered = projects
                    .OrderBy(p => p.TenantName)
                    .ThenBy(p => p.Name)
                    .ToList();

                return Ok(ordered);
            }

            var resolved = await ResolveTenantAsync(tenantKey);
            if (resolved == null) return BadRequest("TenantKey is required.");

            await using var tenantDb = _tenantDbFactory.Create(resolved.ConnectionString);

            var list = await tenantDb.Projects
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAtUtc)
                .Select(p => new ProjectResponseDto
                {
                    Id = p.Id,
                    TenantId = resolved.Tenant.Id,
                    TenantKey = resolved.Tenant.TenantKey,
                    TenantName = resolved.Tenant.Name,
                    Name = p.Name,
                    Description = p.Description,
                    IsActive = p.IsActive,
                    CreatedAtUtc = p.CreatedAtUtc
                })
                .ToListAsync();

            return Ok(list);
        }

        // GET: api/projects/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProjectResponseDto>> GetById(int id, [FromQuery] string? tenantKey)
        {
            var resolved = await ResolveTenantAsync(tenantKey);
            if (resolved == null) return BadRequest("TenantKey is required.");

            await using var tenantDb = _tenantDbFactory.Create(resolved.ConnectionString);

            var p = await tenantDb.Projects
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (p == null) return NotFound();

            return Ok(new ProjectResponseDto
            {
                Id = p.Id,
                TenantId = resolved.Tenant.Id,
                TenantKey = resolved.Tenant.TenantKey,
                Name = p.Name,
                Description = p.Description,
                IsActive = p.IsActive,
                CreatedAtUtc = p.CreatedAtUtc
            });
        }

        // POST: api/projects
        [HttpPost]
        public async Task<ActionResult<ProjectResponseDto>> Create(ProjectCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("Name is required.");

            var resolved = await ResolveTenantAsync(dto.TenantKey);
            if (resolved == null) return BadRequest("TenantKey is required.");

            var name = dto.Name.Trim();
            await using var tenantDb = _tenantDbFactory.Create(resolved.ConnectionString);

            var exists = await tenantDb.Projects.AnyAsync(p => p.Name == name);
            if (exists) return Conflict("Project name already exists for this tenant.");

            var project = new models.Project
            {
                Name = name,
                Description = dto.Description?.Trim(),
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            tenantDb.Projects.Add(project);
            await tenantDb.SaveChangesAsync();

            var response = new ProjectResponseDto
            {
                Id = project.Id,
                TenantId = resolved.Tenant.Id,
                TenantKey = resolved.Tenant.TenantKey,
                Name = project.Name,
                Description = project.Description,
                IsActive = project.IsActive,
                CreatedAtUtc = project.CreatedAtUtc
            };

            return CreatedAtAction(nameof(GetById), new { id = project.Id, tenantKey = resolved.Tenant.TenantKey }, response);
        }

        // PUT: api/projects/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, ProjectUpdateDto dto, [FromQuery] string? tenantKey)
        {
            var resolved = await ResolveTenantAsync(tenantKey);
            if (resolved == null) return BadRequest("TenantKey is required.");

            await using var tenantDb = _tenantDbFactory.Create(resolved.ConnectionString);

            var project = await tenantDb.Projects.FirstOrDefaultAsync(p => p.Id == id);
            if (project == null) return NotFound();

            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("Name is required.");

            var name = dto.Name.Trim();

            var exists = await tenantDb.Projects.AnyAsync(p =>
                p.Id != id &&
                p.Name == name);

            if (exists) return Conflict("Project name already exists for this tenant.");

            project.Name = name;
            project.Description = dto.Description?.Trim();
            project.IsActive = dto.IsActive;

            await tenantDb.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/projects/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id, [FromQuery] string? tenantKey)
        {
            var resolved = await ResolveTenantAsync(tenantKey);
            if (resolved == null) return BadRequest("TenantKey is required.");

            await using var tenantDb = _tenantDbFactory.Create(resolved.ConnectionString);

            var project = await tenantDb.Projects.FirstOrDefaultAsync(p => p.Id == id);
            if (project == null) return NotFound();

            tenantDb.Projects.Remove(project);
            await tenantDb.SaveChangesAsync();

            return NoContent();
        }

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

        private string GetConnectionStringForTenant(models.Tenant tenant)
        {
            var connectionString = tenant.ConnectionString;
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                var baseCs = _configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
                connectionString = TenantConnectionStringBuilder.BuildForTenant(baseCs, tenant.TenantKey);
            }

            return connectionString;
        }

        private string? GetTenantKeyFromRequest(string? tenantKeyOverride)
        {
            if (!string.IsNullOrWhiteSpace(tenantKeyOverride))
                return tenantKeyOverride;

            if (Request.Headers.TryGetValue("X-Tenant-Key", out var headerValue))
                return headerValue.FirstOrDefault();

            if (Request.Query.TryGetValue("tenantKey", out var queryValue))
                return queryValue.FirstOrDefault();

            return null;
        }

        private async Task<(models.Tenant? Tenant, bool FromClaim)> ResolveTenantFromClaimsOrKeyAsync(string? tenantKey)
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

        private async Task<models.Tenant?> GetTenantFromClaimsAsync()
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

        private sealed record ResolvedTenant(models.Tenant Tenant, string ConnectionString);
    }
}
