using IncidentManagementAPI.Common;
using IncidentManagementAPI.DTOs.Tenants;
using IncidentManagementAPI.models;
using IncidentManagementAPI.PlatformData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IncidentManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TenantsController : ControllerBase
    {
        private readonly PlatformDbContext _db;
        private readonly TenantProvisioningService _provisioning;
        private readonly IConfiguration _configuration;

        public TenantsController(
            PlatformDbContext db,
            TenantProvisioningService provisioning,
            IConfiguration configuration)
        {
            _db = db;
            _provisioning = provisioning;
            _configuration = configuration;
        }

        // GET: api/tenants
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TenantResponseDto>>> GetAll()
        {
            var tenants = await _db.Tenants
                .OrderBy(t => t.Name)
                .Select(t => new TenantResponseDto
                {
                    Id = t.Id,
                    TenantKey = t.TenantKey,
                    Name = t.Name,
                    IsActive = t.IsActive,
                    HasConnectionString = !string.IsNullOrWhiteSpace(t.ConnectionString)
                })
                .ToListAsync();

            return Ok(tenants);
        }

        // GET: api/tenants/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<TenantResponseDto>> GetById(int id)
        {
            var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == id);
            if (tenant == null) return NotFound();

            return Ok(new TenantResponseDto
            {
                Id = tenant.Id,
                TenantKey = tenant.TenantKey,
                Name = tenant.Name,
                IsActive = tenant.IsActive,
                HasConnectionString = !string.IsNullOrWhiteSpace(tenant.ConnectionString)
            });
        }

        // GET: api/tenants/by-key/acme
        [HttpGet("by-key/{tenantKey}")]
        public async Task<ActionResult<TenantResponseDto>> GetByKey(string tenantKey)
        {
            var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.TenantKey == tenantKey);
            if (tenant == null) return NotFound();

            return Ok(new TenantResponseDto
            {
                Id = tenant.Id,
                TenantKey = tenant.TenantKey,
                Name = tenant.Name,
                IsActive = tenant.IsActive,
                HasConnectionString = !string.IsNullOrWhiteSpace(tenant.ConnectionString)
            });
        }

        // POST: api/tenants
        [HttpPost]
        public async Task<ActionResult<TenantResponseDto>> Create([FromBody] TenantCreateDto dto)
        {
            dto.TenantKey = dto.TenantKey.Trim();

            if (string.IsNullOrWhiteSpace(dto.TenantKey))
                return BadRequest("TenantKey is required.");

            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("Name is required.");

            var exists = await _db.Tenants.AnyAsync(t => t.TenantKey == dto.TenantKey);
            if (exists) return Conflict("TenantKey already exists.");

            var tenant = new Tenant
            {
                TenantKey = dto.TenantKey,
                Name = dto.Name.Trim(),
                IsActive = true,
                ConnectionString = dto.ConnectionString
            };

            _db.Tenants.Add(tenant);
            await _db.SaveChangesAsync();

            if (string.IsNullOrWhiteSpace(tenant.ConnectionString))
            {
                var baseCs = _configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
                tenant.ConnectionString = TenantConnectionStringBuilder.BuildForTenant(baseCs, tenant.TenantKey);
                await _db.SaveChangesAsync();
            }

            await _provisioning.EnsureTenantDatabaseAsync(tenant.ConnectionString);

            var response = new TenantResponseDto
            {
                Id = tenant.Id,
                TenantKey = tenant.TenantKey,
                Name = tenant.Name,
                IsActive = tenant.IsActive,
                HasConnectionString = !string.IsNullOrWhiteSpace(tenant.ConnectionString)
            };

            return CreatedAtAction(nameof(GetById), new { id = tenant.Id }, response);
        }

        // PUT: api/tenants/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] TenantUpdateDto dto)
        {
            var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == id);
            if (tenant == null) return NotFound();

            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("Name is required.");

            tenant.Name = dto.Name.Trim();
            tenant.IsActive = dto.IsActive;
            tenant.ConnectionString = dto.ConnectionString;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // PATCH: api/tenants/5/activate
        [HttpPatch("{id:int}/activate")]
        public async Task<IActionResult> Activate(int id)
        {
            var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == id);
            if (tenant == null) return NotFound();

            tenant.IsActive = true;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // PATCH: api/tenants/5/deactivate
        [HttpPatch("{id:int}/deactivate")]
        public async Task<IActionResult> Deactivate(int id)
        {
            var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == id);
            if (tenant == null) return NotFound();

            tenant.IsActive = false;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/tenants/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == id);
            if (tenant == null) return NotFound();

            _db.Tenants.Remove(tenant);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}
