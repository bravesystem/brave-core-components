using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BRaVe_Portal.Models.ViewModels
{
    public class DataProcessingSourceOfData
    {
        public int AssessmentId { get; set; }
        public int PrimarySourceId { get; set; }
        public string? PrimarySourceOther { get; set; }
        public int? SecondarySourceId { get; set; }
        public string? SecondarySourceOther { get; set; }
        public string? AttachmentUrl { get; set; }

        public bool IsCaptured => AssessmentId > 0;
    }
}