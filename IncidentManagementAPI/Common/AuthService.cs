using IncidentManagementAPI.DTOs.Auth;
using IncidentManagementAPI.models;
using IncidentManagementAPI.models.Enums;
using IncidentManagementAPI.PlatformData;
using Microsoft.EntityFrameworkCore;

namespace IncidentManagementAPI.Common
{
    public class AuthService
    {
        private readonly PlatformDbContext _db;
        private readonly JwtTokenService _jwt;
        private readonly EmailService _email;
        private readonly AuditService _audit;
        private readonly IConfiguration _config;

        public AuthService(PlatformDbContext db, JwtTokenService jwt, EmailService email, AuditService audit, IConfiguration config)
        {
            _db = db; _jwt = jwt; _email = email; _audit = audit; _config = config;
        }

        public async Task RegisterAsync(RegisterDto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();

            if (await _db.Users.AnyAsync(x => x.Email == email))
                throw new InvalidOperationException("Email already used.");

            if (!Enum.TryParse<UserRole>(dto.Role, true, out var role))
                throw new InvalidOperationException("Invalid role.");

            int? tenantId = null;

            // pour les rôles client, tenantKey obligatoire
            if (role == UserRole.AdminClient || role == UserRole.ClientUser)
            {
                if (string.IsNullOrWhiteSpace(dto.TenantKey))
                    throw new InvalidOperationException("TenantKey required for client roles.");

                var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.TenantKey == dto.TenantKey && t.IsActive);
                if (tenant == null)
                    throw new InvalidOperationException("Invalid tenant.");

                tenantId = tenant.Id;
            }

            var user = new User
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = role,
                TenantId = tenantId,
                MfaEnabled = true,
                IsActive = true
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(user.Id, user.TenantId, "REGISTER", $"role={user.Role}");
        }

        // STEP 1: login password OK => generate challenge + OTP
        public async Task<LoginStep1ResultDto> LoginStep1Async(LoginDto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();
            var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == email && x.IsActive);
            if (user == null) throw new UnauthorizedAccessException("Invalid credentials.");

            // si user tenant => tenantKey obligatoire
            if (user.TenantId != null)
            {
                if (string.IsNullOrWhiteSpace(dto.TenantKey))
                    throw new InvalidOperationException("TenantKey required.");

                var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == user.TenantId && t.TenantKey == dto.TenantKey);
                if (tenant == null) throw new InvalidOperationException("Invalid tenantKey.");
            }

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid credentials.");

            // MFA obligatoire
            if (user.MfaEnabled)
            {
                var otpMinutes = _config.GetValue<int>("Mfa:OtpMinutes", 5);

                var tempToken = Guid.NewGuid().ToString("N");
                var tempHash = CryptoHelper.Sha256Base64(tempToken);

                var otp = CryptoHelper.Otp6Digits();
                var otpHash = CryptoHelper.Sha256Base64(otp);

                var challenge = new MfaChallenge
                {
                    UserId = user.Id,
                    TempTokenHash = tempHash,
                    ExpiresAtUtc = DateTime.UtcNow.AddMinutes(otpMinutes),
                    OtpHash = otpHash,
                    OtpExpiresAtUtc = DateTime.UtcNow.AddMinutes(otpMinutes),
                    Attempts = 0,
                    IsLocked = false
                };

                _db.MfaChallenges.Add(challenge);
                await _db.SaveChangesAsync();

                await _email.SendOtpAsync(user.Email, otp);
                await _audit.LogAsync(user.Id, user.TenantId, "LOGIN_MFA_CHALLENGE");

                return new LoginStep1ResultDto(true, tempToken, user.Role.ToString());
            }

            return new LoginStep1ResultDto(false, null, user.Role.ToString());
        }

        // STEP 2: verify OTP => issue access + refresh token
        public async Task<AuthTokensDto> VerifyOtpAsync(VerifyOtpDto dto)
        {
            var tempHash = CryptoHelper.Sha256Base64(dto.TempMfaToken);

            var challenge = await _db.MfaChallenges
                .OrderByDescending(x => x.Id)
                .FirstOrDefaultAsync(x => x.TempTokenHash == tempHash);

            if (challenge == null) throw new UnauthorizedAccessException("Invalid temp token.");
            if (challenge.IsLocked) throw new UnauthorizedAccessException("Challenge locked.");
            if (challenge.VerifiedAtUtc != null) throw new UnauthorizedAccessException("Already verified.");
            if (challenge.ExpiresAtUtc <= DateTime.UtcNow) throw new UnauthorizedAccessException("Challenge expired.");
            if (challenge.OtpExpiresAtUtc <= DateTime.UtcNow) throw new UnauthorizedAccessException("OTP expired.");

            challenge.Attempts++;
            if (challenge.Attempts > 5)
            {
                challenge.IsLocked = true;
                await _db.SaveChangesAsync();
                throw new UnauthorizedAccessException("Too many attempts.");
            }

            if (CryptoHelper.Sha256Base64(dto.Otp) != challenge.OtpHash)
            {
                await _db.SaveChangesAsync();
                throw new UnauthorizedAccessException("Invalid OTP.");
            }

            challenge.VerifiedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            var user = await _db.Users.FindAsync(challenge.UserId);
            if (user == null || !user.IsActive) throw new UnauthorizedAccessException("User not found.");

            var access = _jwt.CreateAccessToken(user);

            var refreshRaw = CryptoHelper.SecureRandomBase64();
            var refreshHash = CryptoHelper.Sha256Base64(refreshRaw);
            var refreshDays = _config.GetSection("Jwt").GetValue<int>("RefreshTokenDays", 30);

            _db.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                TokenHash = refreshHash,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(refreshDays)
            });

            await _db.SaveChangesAsync();
            await _audit.LogAsync(user.Id, user.TenantId, "LOGIN_SUCCESS_MFA");

            return new AuthTokensDto(access, refreshRaw, user.Role.ToString());
        }

        // REFRESH rotation
        public async Task<AuthTokensDto> RefreshAsync(RefreshDto dto)
        {
            var incomingHash = CryptoHelper.Sha256Base64(dto.RefreshToken);

            var rt = await _db.RefreshTokens.FirstOrDefaultAsync(x =>
                x.TokenHash == incomingHash &&
                x.RevokedAtUtc == null &&
                x.ExpiresAtUtc > DateTime.UtcNow);

            if (rt == null) throw new UnauthorizedAccessException("Invalid refresh token.");

            var user = await _db.Users.FindAsync(rt.UserId);
            if (user == null || !user.IsActive) throw new UnauthorizedAccessException("User not found.");

            rt.RevokedAtUtc = DateTime.UtcNow;

            var newRefreshRaw = CryptoHelper.SecureRandomBase64();
            var newHash = CryptoHelper.Sha256Base64(newRefreshRaw);
            rt.ReplacedByHash = newHash;

            var refreshDays = _config.GetSection("Jwt").GetValue<int>("RefreshTokenDays", 30);

            _db.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                TokenHash = newHash,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(refreshDays)
            });

            await _db.SaveChangesAsync();

            var access = _jwt.CreateAccessToken(user);
            await _audit.LogAsync(user.Id, user.TenantId, "REFRESH_ROTATION");

            return new AuthTokensDto(access, newRefreshRaw, user.Role.ToString());
        }

        public async Task LogoutAsync(LogoutDto dto)
        {
            var hash = CryptoHelper.Sha256Base64(dto.RefreshToken);
            var rt = await _db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash && x.RevokedAtUtc == null);
            if (rt == null) return;

            rt.RevokedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await _audit.LogAsync(rt.UserId, null, "LOGOUT");
        }
    }
}
