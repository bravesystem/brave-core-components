using BRaVe_Portal.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace BRaVe_Portal.Models.DTOs
{
    public class DistributionItemDto
    {
        public int Id { get; set; }
        public int? TenantId { get; set; }
        [MaxLength(50)]
        public string? SKU { get; set; }
        [MaxLength(100)]
        public string? Name { get; set; }
        public string? Source { get; set; }

        public bool IsActive { get; set; }
        public DateTime? UpdatedOn { get; set; }

    }
}
