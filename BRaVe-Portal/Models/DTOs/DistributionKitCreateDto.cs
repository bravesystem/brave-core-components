using BRaVe_Portal.Models.ViewModels;
using System.ComponentModel.DataAnnotations;

namespace BRaVe_Portal.Models.DTOs

{
    public class DistributionKitCreateDto
    {
        public int TenantId { get; set; }

        public int DistributionTypeId { get; set; }

        public long KitTypeId { get; set; }

        public int Quantity { get; set; }
        public int UoM { get; set; }
    }

}

