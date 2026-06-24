namespace BRaVe_Management_Backend.Models
{
    public class DataProcessingCollaborator
    {
        public int AssessmentId { get; set; }
        public int PartnerId { get; set; }
        public string PartnerOrganizationName { get; set; }
        public string PartnerOrganizationContacts { get; set; }
        public string Description { get; set; }

        public string? FilePath { get; set; }

    }
}
