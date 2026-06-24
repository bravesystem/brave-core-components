using System.ComponentModel.DataAnnotations;

namespace BRaVe_Portal.Models
{
    public class ClaimSessionDto
    {
        public int SessionId { get; set; }
        //public string? SessionCodeHash { get; set; }

        //public int TenantId { get; set; }
        [MaxLength(100)]

        public string? Label { get; set; }
        public int PolicyVersion { get; set; } = 1;
        public int MaxClaims { get; set; }
        public int TtlMinutes { get; set; }


        public bool IsValid()
        {
            if (!string.IsNullOrEmpty(Label) && MaxClaims > 0 && TtlMinutes > 5)
                return true;

            return false;
        }

        //public int ClaimsIssued { get; set; }
        //public int Status { get; set; }
        //public DateTime ExpiresAtUtc { get; set; }

}
}
