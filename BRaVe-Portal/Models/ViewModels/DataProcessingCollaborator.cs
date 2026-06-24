namespace BRaVe_Portal.Models.ViewModels
{
    public class DataProcessingCollaborator
    {
        public int AssessmentId { get; set; }
        public int PartnerId { get; set; } //show select list with dummy data where 99 match with value Other
        public string PartnerOrganizationName { get; set; }
        public string PartnerOrganizationContacts { get; set; }
        public string Description { get; set; }

        public string? FilePath { get; set; }
    }
}