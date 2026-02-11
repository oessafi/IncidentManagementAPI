namespace IncidentManagementAPI.models
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        public string TokenHash { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? RevokedAtUtc { get; set; }
        public string? ReplacedByHash { get; set; }
    }
}
