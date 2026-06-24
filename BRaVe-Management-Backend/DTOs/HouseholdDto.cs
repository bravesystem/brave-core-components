namespace BRaVe_Management_Backend.DTOs
{
    public class HouseholdDto
    {
        public string HouseholdId { get; set; } = string.Empty;
        public string? ActivityCode { get; set; }
        public int HouseholdSize { get; set; }
        public string? HouseholdType { get; set; }
        public string? MissionName { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
    }
}
