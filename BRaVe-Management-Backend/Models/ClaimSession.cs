using System.ComponentModel.DataAnnotations;

namespace BRaVe_Management_Backend.Models
{
    public class ClaimSession
    {
        public long SessionId { get; set; }
        public int TenantId { get; set; }
        
        public string Label { get; set; }
        public int PolicyVersion { get; set; }
        public byte[] SessionCodeHash { get; set; }
        
        public int MaxClaims { get; set; }

        public int ClaimsIssued { get; set; }
        public DateTime? ExpiresAtUtc { get; set; }
        public int Status { get; set; }


        public DateTime? CreatedOnUtc { get; set; }

        public DateTime? UpdatedOnUtc { get; set; }

    }
}
