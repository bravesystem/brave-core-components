namespace BRaVe_Management_Backend.Models
{
    public class DataProcessingPersonalData
    {
        public int AssessmentId { get; set; }
        public int PersonalDataCategoryId { get; set; }
        public bool IsSpecialCategory { get; set; }
        public string? PurposeOfDataCollection { get; set; }

    }
}
