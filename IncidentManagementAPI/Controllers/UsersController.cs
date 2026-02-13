using IncidentManagementAPI.DTOs.Platform;
using IncidentManagementAPI.PlatformData;
using IncidentManagementAPI.models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IncidentManagementAPI.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "SuperAdmin")]
public class UsersController : ControllerBase
{
    private readonly PlatformDbContext _db;

    public UsersController(PlatformDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
    {
        var tenants = await _db.Tenants.AsNoTracking().ToDictionaryAsync(t => t.Id, t => t.TenantKey);
        var users = await _db.Users.AsNoTracking().ToListAsync();

        return Ok(users.Select(u => new UserDto
        {
            Id = u.Id,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Email = u.Email,
            Role = u.Role.ToString(),
            IsActive = u.IsActive,
            TenantKey = u.TenantId.HasValue ? tenants.GetValueOrDefault(u.TenantId.Value) : null
        }));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, UpdateUserDto dto)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        if (dto.IsActive.HasValue)
            user.IsActive = dto.IsActive.Value;

        if (!string.IsNullOrWhiteSpace(dto.Role) && Enum.TryParse<UserRole>(dto.Role, true, out var parsedRole))
            user.Role = parsedRole;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
