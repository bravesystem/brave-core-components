using BRaVe_Portal.Models.ViewModels;

namespace BRaVe_Portal.Models
{
    public class SectionA
    {

        public SectionA() { }
        public SectionA(DataProcessing dataProcessing)
        {
            AssessmentId = dataProcessing.AssessmentId;
            DataManagerName = dataProcessing.DataManagerName;
            DataManagerContacts = dataProcessing.DataManagerContacts;
            PrimaryPurposeId = dataProcessing.PrimaryPurposeId;
            PrimaryPurposeOther = dataProcessing.PrimaryPurposeOther;
            SecondaryPurposeId = dataProcessing.SecondaryPurposeId;
            SecondaryPurposeOther = dataProcessing.SecondaryPurposeOther;
        }
        public int AssessmentId { get; set; }
        public string? DataManagerName { get; set; }
        public string? DataManagerContacts { get; set; }
        public int? PrimaryPurposeId { get; set; }
        public string? PrimaryPurposeOther { get; set; }
        public int? SecondaryPurposeId { get; set; }
        public string? SecondaryPurposeOther { get; set; }
    }
}
