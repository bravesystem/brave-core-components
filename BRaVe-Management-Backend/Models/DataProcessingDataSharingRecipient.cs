namespace BRaVe_Management_Backend.Models
{
    public class DataProcessingDataSharingRecipient
    {

        public int AssessmentId { get; set; }
        public int RecipientId { get; set; }
        public string? RecipientOther { get; set; }
        public int TypeOfAgreementId { get; set; }
        public string? TypeOfAgreementOther { get; set; }
        public int StatusOfAgreementId { get; set; }
        public string? PurposeOfDataSharing { get; set; }
    }
}
