using DocumentFormat.OpenXml.Spreadsheet;
using System.ComponentModel.DataAnnotations;

namespace BRaVe_Portal.Models
{
    public class DistributionItemTypes
    {
        public long Id { get; set; }                 // bigint identity

        public int TenantId { get; set; }            // int not null
        public string? SKU { get; set; }             // nvarchar(50)
        public string Name { get; set; } = string.Empty;   // nvarchar(100) not null

        public string? Notes { get; set; }           // nvarchar(500)

        public string Source { get; set; } = string.Empty; // 'I' or 'E'

        public bool IsActive { get; set; }           // bit not null

        public string? ExternalId { get; set; }      // nvarchar(100)
        public string? QRCode { get; set; }          // nvarchar(500)

        public string? CreatedByUserId { get; set; } = string.Empty; // varchar(50)

        public DateTime CreatedOn { get; set; }      // datetime not null
        public string? UpdatedByUserId { get; set; } // varchar(50)
        public DateTime? UpdatedOn { get; set; }     // datetime nullable

    }
}
