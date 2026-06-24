using System.Collections.Generic;

namespace BRaVe_Portal.Models.ViewModels
{
    public class DataProcessingPersonalData
    {
        public int AssessmentId { get; set; }
        public int PersonalDataCategoryId { get; set; } 
        public bool IsSpecialCategory { get; set; } = false;
        public string? PurposeOfDataCollection { get; set; }
    }
}