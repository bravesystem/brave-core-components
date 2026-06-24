namespace BRaVe_Portal.Models.DTOs
{
   public class DatapointTranslationDto
    {
        public int Id { get; set; }

        // The parent DataPoint this translation belongs to
        public int DataPointId { get; set; }

        public int TenantId { get; set; }

        // Language code like "en", "fr", "es"
        public string? LanguageCode { get; set; }

        // The translation text
        public string? Text { get; set; }
    }
}

