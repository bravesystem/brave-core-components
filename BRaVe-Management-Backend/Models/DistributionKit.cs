using System.ComponentModel.DataAnnotations;

namespace BRaVe_Management_Backend.Models
{
    public class DistributionKit
    {


        public int? Id { get; set; }
        public int TenantId { get; set; }
         public string SKU { get; set; }
        public string Name { get; set; }
        
             public int? Quantity { get; set; }
        public string Notes { get; set; }
         public string? Source { get; set; }
        public bool IsActive { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string? Code { get; set; }



    }
}
