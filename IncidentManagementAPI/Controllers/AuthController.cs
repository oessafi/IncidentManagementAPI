using IncidentManagementAPI.Common;
using IncidentManagementAPI.DTOs.Auth;
using Microsoft.AspNetCore.Mvc;

namespace IncidentManagementAPI.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _auth;
        public AuthController(AuthService auth) => _auth = auth;

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            try { await _auth.RegisterAsync(dto); return Ok(new { ok = true }); }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            try { return Ok(await _auth.LoginStep1Async(dto)); }
            catch (UnauthorizedAccessException ex) { return Unauthorized(new { error = ex.Message }); }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp(VerifyOtpDto dto)
        {
            try { return Ok(await _auth.VerifyOtpAsync(dto)); }
            catch (UnauthorizedAccessException ex) { return Unauthorized(new { error = ex.Message }); }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(RefreshDto dto)
        {
            try { return Ok(await _auth.RefreshAsync(dto)); }
            catch (UnauthorizedAccessException ex) { return Unauthorized(new { error = ex.Message }); }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout(LogoutDto dto)
        {
            await _auth.LogoutAsync(dto);
            return Ok(new { ok = true });
        }
    }
}
