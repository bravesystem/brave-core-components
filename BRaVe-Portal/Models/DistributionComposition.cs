using DocumentFormat.OpenXml.Spreadsheet;
using System.ComponentModel.DataAnnotations;

namespace BRaVe_Portal.Models
{
    public class DistributionComposition
    {
        public int DistributionId { get; set; }
        public int TenantId { get; set; }

        public long? KitTypeId { get; set; }
        public long? ItemTypeId { get; set; }

        public string? UnitTitle { get; set; }

        public bool IsActive { get; set; }

        public string? TargetType { get; set; }

        public int Quantity { get; set; }

        public string? QRCode { get; set; }

        public string? SKU { get; set; }

        public decimal? Measure { get; set; }

        public string? UoM { get; set; }
        public int ItemCount { get; set; }

        public List<KitTypeItem> KitItems { get; set; } = new();


    }
}
