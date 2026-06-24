using BRaVe_Portal.Models.ViewModels;
using System.ComponentModel.DataAnnotations;

namespace BRaVe_Portal.Models.DTOs

{

    public class DistributionAssistanceDto

    {

        public int Id { get; set; }
        public int? TenantId { get; set; }
        public int? KitId { get; set; }

        public int? Quantity { get; set; }
        [MaxLength(150)]
        public string? Notes { get; set; }

        public int? DistributionID { get; set; }

 
        public bool IsActive { get; set; }
        public DateTime? UpdatedOn { get; set; }

        public string? KitName { get; set; }

        [MaxLength(200)]
        public string? ItemTag { get; set; }

        //public int? ItemTagID { get; set; }
    }

}

