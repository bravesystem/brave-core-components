using BRaVe_Portal.Models.ViewModels;
using System.ComponentModel.DataAnnotations;

namespace BRaVe_Portal.Models.DTOs

{

    public class DistributionItemCreateDto

    {

        public int TenantId { get; set; }

        public int DistributionTypeId { get; set; }

        public long ItemTypeId { get; set; }

        public int Quantity { get; set; }

        public decimal Measure { get; set; }

        public int UoM { get; set; }


    }

}

