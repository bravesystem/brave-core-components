namespace BRaVe_Portal.Models
{
    public class CreateSessionResponse
    {
        public long SessionId { get; set; }
        public string SessionCode { get; set; } 
        public int TenantId { get; set; }
        public int PolicyVersion { get; set; }
        public int MaxClaims { get; set; }
        public int ClaimsIssued { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
        public string Status { get; set; }
    }
}
