namespace IncidentManagementAPI.models
{
    public class MfaChallenge
    {
        public long Id { get; set; }
        public int UserId { get; set; }

        public string TempTokenHash { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }

        public string OtpHash { get; set; } = string.Empty;
        public DateTime OtpExpiresAtUtc { get; set; }

        public int Attempts { get; set; } = 0;
        public bool IsLocked { get; set; } = false;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? VerifiedAtUtc { get; set; }
    }
}
