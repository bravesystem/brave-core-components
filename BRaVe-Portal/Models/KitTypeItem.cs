using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace BRaVe_Portal.Models
{
    public class KitTypeItem
    {
        public long KitId { get; set; }
        public long ItemId { get; set; }
        public int TenantId { get; set; }
        public decimal Measure { get; set; }
        public int UoM { get; set; }
        public string CreatedByUserId { get; set; } = string.Empty;
        public DateTime? CreatedOn { get; set; }
        public string? UpdatedByUserId { get; set; }
        public DateTime? UpdatedOn { get; set; }

        // Optional: navigation property if you want to include item details
        public DistributionItemTypes? Item { get; set; }
    }

}