namespace IncidentManagementAPI.DTOs.Auth
{
    public record AuthTokensDto(string AccessToken, string RefreshToken, string Role);
}
