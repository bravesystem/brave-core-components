using System.ComponentModel.DataAnnotations;

namespace BRaVe_Portal.Models
{
    public class EnumeratorIdGeneratorTracking
    {
        public int TenantId { get; set; }
        [MaxLength(5)]
        public string? Prefix { get; set; }
        public int SuffixLength { get; set; }

        public int TotalCodeGenerated { get; set; }
        [MaxLength(50)]
        public string? LastCodeGenerated { get; set; }
         

         public DateTime CreatedOn { get; set; }
        public int LatestId { get; set; }
        public string? CreatedByUserId { get; set; }
        public string? UpdatedByUserId { get; set; }



    }
}
