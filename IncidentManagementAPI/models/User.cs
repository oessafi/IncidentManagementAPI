using IncidentManagementAPI.models.Enums;

namespace IncidentManagementAPI.models
{
    public class User
    {
        public int Id { get; set; }

        // null = utilisateur plateforme (SuperAdmin, Support)
        public int? TenantId { get; set; }

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // mot de passe hashé (BCrypt)
        public string PasswordHash { get; set; } = string.Empty;

        public UserRole Role { get; set; }

        public bool MfaEnabled { get; set; } = true;
        public bool IsActive { get; set; } = true;
    }
}
