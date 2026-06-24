using System.Collections.Generic;

namespace BRaVe_Portal.Models.ViewModels
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
        public bool HasData =>
    ProcessingActivityLawfulBasisId != 0 ||
    !string.IsNullOrWhiteSpace(LawfulBasisExplanation) ||
    !string.IsNullOrWhiteSpace(LawfulBasisDocPath) ||
    !string.IsNullOrWhiteSpace(InitialDataCollectionExplanation) ||
    !string.IsNullOrWhiteSpace(InitialDataCollectionDocPath) ||
    !string.IsNullOrWhiteSpace(OngoingDataManagementExplanation) ||
    !string.IsNullOrWhiteSpace(OngoingDataManagementDocPath) ||
    !string.IsNullOrWhiteSpace(DataSharingExplanation) ||
    !string.IsNullOrWhiteSpace(DataSharingDocPath);


    }
}