namespace BRaVe_Management_Backend.DTOs
{
    public sealed class ClaimSessionRecord
    {
        public int TenantId { get; set; }
        public string Label { get; set; } = "";
        public byte[] SessionCodeHash { get; set; } = Array.Empty<byte>();
        public int PolicyVersion { get; set; } = 1;
        public int MaxClaims { get; set; } = 1;
        public int ClaimsIssued { get; set; } = 0;
        public DateTime ExpiresAtUtc { get; set; }
        public byte Status { get; set; } = 1;
    }
}
