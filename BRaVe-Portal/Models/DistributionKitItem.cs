using DocumentFormat.OpenXml.Spreadsheet;
using System.ComponentModel.DataAnnotations;

namespace BRaVe_Portal.Models
{
    public class DistributionKitItem
    {
        public int Id { get; set; }
        public int? TenantId { get; set; }
        public int? ItemId { get; set; }
        public string? Name { get; set; }

        public string? KitName { get; set; }
        public int? Quantity { get; set; }
        public int? UOM { get; set; }
        public int? KitID { get; set; }
        
        public string? Source { get; set; }
        [MaxLength(150)] 
        public string? Notes { get; set; }

        public string? UOMName { get; set; }
        public bool IsActive { get; set; }


   
        public DateTime? UpdatedOn { get; set; }
        public string? ItemTag { get; set; }

        //public int? ItemTagID { get; set; }


    }
}
