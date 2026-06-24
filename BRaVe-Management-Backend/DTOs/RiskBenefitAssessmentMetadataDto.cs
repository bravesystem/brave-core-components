using BRaVe_Management_Backend.Models;

namespace BRaVe_Management_Backend.DTOs
{
    public class RiskBenefitAssessmentMetadataDto
    {
        public int AssessmentId { get; set; }
        public int TenantId { get; set; }
        public bool RequireApprovals { get; set; }
        public string CreatedByUserId { get; set; }
        public DateTime CreatedOn { get; set; }
        public int StatusId { get; set; }
        public DataProcessing DataProcessing { get; set; } = new();
        public List<DataProcessingCollaborator> Collaborators { get; set; } = new();
        public List<DataProcessingDataSubject> Subjects { get; set; } = new();
        public List<DataProcessingPersonalData> PersonalData { get; set; } = new();
        public DataProcessingLawfulBasis LawfulBasis { get; set; }
        public List<DataProcessingDataDisclosureRecipient> DisclosureRecipients { get; set; } = new();
        public List<DataProcessingDataSharingRecipient> SharingRecipients { get; set; } = new();
        public DataProcessingRetention Retention { get; set; } = new();
        public List<DataProcessingSecurityMeasure> SecurityMeasures { get; set; } = new();
        public DataProcessingSourceOfData SourceOfData { get; set; } = new();
        public List<DataProcessingDataOutput> DataOutputs { get; set; } = new();
        public List<UserNote> PastDecisions { get; set; } = new();

    }
}
