using BRaVe_Portal.Models.ViewModels;
using System.ComponentModel.DataAnnotations;

namespace BRaVe_Portal.Models.DTOs
{
    public class EnumeratorIdGeneratorTrackingDto
    {
        public int TenantId { get; set; } = 0;
        [MaxLength(5)] 
        public string? Prefix { get; set; }
        public int SuffixLength { get; set; }

        public int TotalCodeGenerated { get; set; }
        [MaxLength(50)] 
        public string? LastCodeGenerated { get; set; }

        public int LatestId { get; set; }
        public string? CreatedByUserId { get; set; }
        public string? UpdatedByUserId { get; set; }
        public DateTime? CreatedOn { get; set; }


        public bool IsValid()
        {
            if (string.IsNullOrEmpty(Prefix) || Prefix.Length>5 || SuffixLength == 0 || TotalCodeGenerated == 0)
                return false;

            return true;
        }



    }
}
