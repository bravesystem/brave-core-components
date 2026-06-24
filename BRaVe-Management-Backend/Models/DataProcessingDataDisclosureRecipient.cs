namespace BRaVe_Management_Backend.Models
{
    public class DataProcessingDataDisclosureRecipient
    {
        public int AssessmentId { get; set; }
        public int RecipientId { get; set; }
        public string? RecipientOther { get; set; }
        public string NameOfRecipientDepartment { get; set; }
        public string? PurposeOfDataDisclosure { get; set; }
        public bool IsDataAggregatedBeforeSharing { get; set; } = true;
        public bool IsDataAnonymizedBeforeSharing { get; set; } = true;
        public string? PurposeOfDataAccess { get; set; }

    }
}
