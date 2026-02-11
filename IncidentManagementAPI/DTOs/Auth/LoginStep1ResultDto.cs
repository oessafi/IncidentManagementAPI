namespace IncidentManagementAPI.DTOs.Auth
{
    public record LoginStep1ResultDto(
        bool MfaRequired,
        string? TempMfaToken,
        string Role
    );
}
