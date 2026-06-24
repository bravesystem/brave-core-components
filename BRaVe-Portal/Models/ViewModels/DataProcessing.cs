using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BRaVe_Portal.Models.ViewModels
{
    public class DataProcessing
    {
        public int AssessmentId { get; set; }

        public string? ProgamManagerName { get; set; }
        public string? ProgamManagerContacts { get; set; }

        public string? DataManagerName { get; set; }
        public string? DataManagerContacts { get; set; }
        public int? PrimaryPurposeId { get; set; }
        public string? PrimaryPurposeOther { get; set; }
        public int? SecondaryPurposeId { get; set; }
        public string? SecondaryPurposeOther { get; set; }
        public bool IsCaptured => DataManagerName != null || DataManagerContacts != null || PrimaryPurposeId != null;

    }
}