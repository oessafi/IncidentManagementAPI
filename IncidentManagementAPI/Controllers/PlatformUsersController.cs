using IncidentManagementAPI.DTOs.Platform;
using IncidentManagementAPI.models.Enums;
using IncidentManagementAPI.PlatformData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IncidentManagementAPI.Controllers;

[ApiController]
[Route("api/platform/users")]
public class PlatformUsersController : ControllerBase
{
    private readonly PlatformDbContext _db;

    public PlatformUsersController(PlatformDbContext db)
    {
        _db = db;
    }

    [HttpGet("supports")]
    public async Task<ActionResult<IEnumerable<SupportDto>>> GetSupports()
    {
        var supports = await _db.Users
            .AsNoTracking()
            .Where(u => u.IsActive && u.Role == UserRole.Support)
            .Select(u => new SupportDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email
            })
            .ToListAsync();

        return Ok(supports);
    }
}
