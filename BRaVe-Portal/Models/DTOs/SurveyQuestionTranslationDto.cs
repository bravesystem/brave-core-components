namespace BRaVe_Management_Backend.DTOs
{
        public class SurveyQuestionTranslationDto
        {
            public string SurveyCode { get; set; }   
            public int Id { get; set; }           
            public int TenantId { get; set; }
            public string? LanguageCode { get; set; } 
            public string? Text { get; set; }         
        }
    }

