using System.ComponentModel.DataAnnotations;

namespace BRaVe_Portal.Models
{
    public class ClaimSession
    {
        public int SessionId { get; set; }
        public int TenantId { get; set; }

        [MaxLength(100)] 
        public string? Label { get; set; }
        public int PolicyVersion { get; set; }
        public int MaxClaims { get; set; }
        public int ClaimsIssued { get; set; }
        public int Status { get; set; }
        [MaxLength(32)]
        public string? SessionCodeHash { get; set; }
        public DateTime ExpiresAtUtc { get; set; }

        public bool Deactivate { get; set; } = true;


        public bool IsActive => MaxClaims > ClaimsIssued && DateTime.UtcNow < ExpiresAtUtc;

    }
}
