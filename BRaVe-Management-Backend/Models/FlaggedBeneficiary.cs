namespace BRaVe_Management_Backend.Models
{
    public class FlaggedBeneficiary
    {
        public long Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string BeneficiaryId { get; set; } = string.Empty;
        public string HouseholdId { get; set; } = string.Empty;
        public string ActivityCode { get; set; } = string.Empty;
        public DateTime DateFlagged { get; set; }
        public string ParticipationCode { get; set; } = string.Empty;
        public DateTime? DownloadedOn { get; set; }
        public string? DownloadedBy { get; set; }
        public string? DeviceId { get; set; }
        public bool IsResolved { get; set; }
        public DateTime? ResolvedOn { get; set; }
    }
}
