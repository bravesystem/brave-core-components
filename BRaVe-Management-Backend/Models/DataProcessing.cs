namespace BRaVe_Management_Backend.Models
{
    public class DataProcessing
    {
        public int AssessmentId { get; set; }
        public string ProgamManagerName { get; set; }
        public string ProgamManagerContacts { get; set; }
        public string? DataManagerName { get; set; }
        public string? DataManagerContacts { get; set; }
        public int? PrimaryPurposeId { get; set; }
        public string? PrimaryPurposeOther { get; set; }
        public int? SecondaryPurposeId { get; set; }
        public string? SecondaryPurposeOther { get; set; }

    }

}
