namespace BRaVe_Portal.Models.DTOs
{
   public class ConsentTranslationDto
    {
        public int Id { get; set; }

        // The parent Consent this translation belongs to
        public int ConsentId { get; set; }
        public int ProgramId { get; set; }

        public int TenantId { get; set; }

        // Language code like "en", "fr", "es"
        public string? LanguageCode { get; set; }

        // The translation text
        public string? Text { get; set; }
        public string? Description { get; set; }
    }
}

