namespace BRaVe_Management_Backend.DTOs.PrintService
{
    public class CardPrintQueueItem
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public int FamilySize { get; set; }
        public string HouseholdId { get; set; }
        public string Mission { get; set; }
        public string Program { get; set; }
        public string LocationInformation { get; set; }

        public string? AdditionalInformation { get; set; }
        public string Activity { get; set; }

        public string? barcodeId { get; set; }
        public DateTime RegDate { get; set; }

        public bool CardPrinted { get; set; }
        public DateTime? PrintedOn { get; set; }
        public string PrintedBy { get; set; }

        public bool IsLocked { get; set; }
        public string LockedBy { get; set; }
        public DateTime? LockedOn { get; set; }
        public string MachineId { get; set; }
    }
}
