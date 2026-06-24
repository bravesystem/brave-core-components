namespace BRaVe_Portal.Models.DTOs
{
    public class BiometricVerificationResultDto
    {
        public string JobId { get; set; }

        public string? ActivityCode { get; set; }
        public Guid BiometricId { get; set; }
        public DateTime ProcessedOn { get; set; }

        public string Match { get; set; }

        public string FullName { get; set; }
        public int? Age { get; set; }
        public string Gender { get; set; }
        public string Relationship { get; set; }

        public string HouseholdId { get; set; }
        public string? Photo { get; set; }
        public string Mission { get; set; }
        public DateTime? RegistrationDate { get; set; }
    }
}
