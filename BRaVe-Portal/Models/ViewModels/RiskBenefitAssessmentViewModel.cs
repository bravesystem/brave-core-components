using BRaVe_Portal.Helpers;
using BRaVe_Portal.Models.Enums;

namespace BRaVe_Portal.Models.ViewModels
{
    public class RiskBenefitAssessmentViewModel
    {

        public int AssessmentId { get; set; }
        public int TenantId { get; set; }
        public bool RequireApprovals { get; set; } = true; //ignore
        public string CreatedByUserId { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public int StatusId { get; set; }
        public AssessmentStatus Status => (AssessmentStatus)StatusId;
        
        public DataProcessing DataProcessing { get; set; } = new();
        public List<DataProcessingCollaborator> Collaborators { get; set; } = new();
        public List<DataProcessingDataSubject> Subjects { get; set; } = new();
        public List<DataProcessingPersonalData> PersonalData { get; set; } = new();
        public DataProcessingLawfulBasis LawfulBasis { get; set; }= new();
        public List<DataProcessingDataDisclosureRecipient> DisclosureRecipients { get; set; } = new();
        public List<DataProcessingDataSharingRecipient> SharingRecipients { get; set; } = new();
        public DataProcessingRetention Retention { get; set; } = new();
        public List<DataProcessingSecurityMeasure> SecurityMeasures { get; set; } = new();
        public DataProcessingSourceOfData SourceOfData { get; set; } = new();
        public List<DataProcessingDataOutput> DataOutputs { get; set; } = new();
        public List<UserNote> PastDecisions { get; set; } = new();
        public bool IsReadOnly =>
        Status != AssessmentStatus.Draft &&
        Status != AssessmentStatus.PendingReview;

        public bool IsValid { get; set; } = false;

    }
}
