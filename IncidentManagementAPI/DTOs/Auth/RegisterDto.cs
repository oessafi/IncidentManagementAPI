namespace IncidentManagementAPI.DTOs.Auth
{
    public record RegisterDto(
        string FirstName,
        string LastName,
        string Email,
        string Password,
        string Role,
        string? TenantKey
    );
}
