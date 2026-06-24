namespace BRaVe_Management_Backend.DTOs
{
    public class DataProcessingPersonalDataDto
    {
        public int PersonalDataCategoryId { get; set; }
        public bool IsSpecialCategory { get; set; } = false;
        public string? PurposeOfDataCollection { get; set; }
    }
}
