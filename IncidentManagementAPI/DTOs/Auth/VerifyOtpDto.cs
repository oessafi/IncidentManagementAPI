namespace IncidentManagementAPI.DTOs.Auth
{
    public record VerifyOtpDto(string TempMfaToken, string Otp);
}
