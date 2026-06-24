using System;
using System.ComponentModel.DataAnnotations;

namespace BRaVe_Portal.Models
{
    public class DistributionTypes
    {
        public int Id { get; set; }                 // int, part of PK

        public int TenantId { get; set; }           // int, part of PK

        [StringLength(50)]
        public string? Title { get; set; }          // nvarchar(50), nullable

        [StringLength(500)]
        public string? Description { get; set; }    // nvarchar(500), nullable

        public bool? IsActive { get; set; }         // bit, nullable

        [StringLength(100)]
        public string? ExternalId { get; set; }     // nvarchar(100), nullable

        public string? CreatedByUserId { get; set; } = string.Empty; // varchar(50), not null

        public DateTime? CreatedOn { get; set; }    // datetime, nullable

        [StringLength(50)]
        public string? UpdatedByUserId { get; set; } // varchar(50), nullable

        public DateTime? UpdatedOn { get; set; }    // datetime, nullable

        public int unitCount { get; set; } //total number of item or kit

    }
}