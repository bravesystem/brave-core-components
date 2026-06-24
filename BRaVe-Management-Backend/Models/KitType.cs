using System.ComponentModel.DataAnnotations;

namespace BRaVe_Management_Backend.Models
{
    public class KitType
    {
        public long Id { get; set; }                 // BIGINT

        public int TenantId { get; set; }

        [StringLength(100)]
        public string? SKU { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Notes { get; set; }

        [Required]
        [StringLength(1)]
        public string Source { get; set; } = "I";    // I = Internal, E = External

        public bool IsActive { get; set; }

        [StringLength(100)]
        public string? ExternalId { get; set; }

        [StringLength(500)]
        public string? QRCode { get; set; }

        public string? CreatedByUserId { get; set; }

        public DateTime CreatedOn { get; set; }

        public int ItemCount { get; set; }
        public string? UpdatedByUserId { get; set; }

        public DateTime? UpdatedOn { get; set; }
    }
}