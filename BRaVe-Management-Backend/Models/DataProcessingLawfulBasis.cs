namespace BRaVe_Management_Backend.Models
{
    public class DataProcessingLawfulBasis
    {
        public int AssessmentId { get; set; }
        public int ProcessingActivityLawfulBasisId { get; set; }
        public string? LawfulBasisExplanation { get; set; }
        public string? LawfulBasisDocPath { get; set; }
        public string InitialDataCollectionExplanation { get; set; }
        public string? InitialDataCollectionDocPath { get; set; }
        public string OngoingDataManagementExplanation { get; set; }
        public string? OngoingDataManagementDocPath { get; set; }
        public string DataSharingExplanation { get; set; }
        public string? DataSharingDocPath { get; set; }

    }
}
