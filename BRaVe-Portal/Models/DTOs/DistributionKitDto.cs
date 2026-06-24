using BRaVe_Portal.Models.ViewModels;
using System.ComponentModel.DataAnnotations;

namespace BRaVe_Portal.Models.DTOs

{

    public class DistributionKitDto

    {

        public int Id { get; set; }
        public int? TenantId { get; set; }
        [MaxLength(100)]

        public string? SKU { get; set; }
        [MaxLength(50)]
        public string Name { get; set; }
         public string? Source { get; set; }
        [MaxLength(150)]
        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? UpdatedOn { get; set; }


    }

}

