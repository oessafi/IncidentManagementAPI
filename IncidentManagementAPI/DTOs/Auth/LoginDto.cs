namespace IncidentManagementAPI.DTOs.Auth
{
    public record LoginDto(
        string Email,
        string Password,
        string? TenantKey
    );
}
