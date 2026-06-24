namespace BRaVe_Management_Backend.DTOs
{
    public class CreateFlaggedBeneficiaryRequest
    {
        public List<FlaggedBeneficiaryItem> Beneficiaries { get; set; } = new();

        public string? Reason { get; set; }
    }

    public class FlaggedBeneficiaryItem
    {
        public Guid BeneficiaryId { get; set; }

        public string ActivityCode { get; set; }
    }
}
