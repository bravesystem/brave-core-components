using BRaVe_Portal.Models.DTOs;

namespace BRaVe_Portal.Models
{
    public class CoreLookups
    {
        public List<LookupItemDto> Roles { get; set; } = new();
        public List<LookupItemDto> FocalPoints { get; set; } = new();
        public List<LookupItemDto> AssessmentStatuses { get; set; } = new();
        public List<LookupItemDto> AssessmentPurposeOfProcessing { get; set; } = new();
        public List<LookupItemDto> PartnerOganizations { get; set; } = new();
        public List<LookupItemDto> HouseholdTypes { get; set; } = new();
        public List<LookupItemDto> PersonalDataCategories { get; set; } = new();
        public List<LookupItemDto> LawfulBases { get; set; } = new();
        public List<LookupItemDto> DataDisclosureRecipients { get; set; } = new();
        public List<LookupItemDto> DataSharingRecipients { get; set; } = new();
        public List<LookupItemDto> TypeOfAgreements { get; set; } = new();
        public List<LookupItemDto> StatusOfAgreements { get; set; } = new();
        public List<LookupItemDto> ArchivingMethodTypes { get; set; } = new();
        public List<LookupItemDto> DisposalMethodTypes { get; set; } = new();
        public List<LookupItemDto> SecurityMeasures { get; set; } = new();
        public List<LookupItemDto> SourceOfData { get; set; } = new();
        public List<LookupItemDto> DataOutputs { get; set; } = new();
        public List<LookupItemDto> DataOutputRecipients { get; set; } = new();
        public List<LookupItemDto> ApprovalStatuses { get; set; } = new();
        public List<LookupItemDto> RecommendationImplementationStatuses { get; set; } = new();
        public List<LookupItemDto> CommonStatuses { get; set; } = new();
        public List<LookupItemDto> QuestionTypes { get; set; } = new();
        public List<LookupItemDto> SurveyTypes { get; set; } = new();
        public List<LookupItemDto> PreferenceTypes { get; set; } = new();
        public List<LookupItemDto> UOM { get; set; } = new();

     }
}
