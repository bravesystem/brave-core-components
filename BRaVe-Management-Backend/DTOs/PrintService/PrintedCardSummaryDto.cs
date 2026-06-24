namespace BRaVe_Management_Backend.DTOs.PrintService
{
    public class PrintedCardSummaryDto
    {
        public int CardId { get; set; }
        public string? HouseholdId { get; set; }
        public string? FullName { get; set; }
        public string? PrintedBy { get; set; }
        public DateTime? PrintedOn { get; set; }
        public int NoOfReprints { get; set; }
    }
}
