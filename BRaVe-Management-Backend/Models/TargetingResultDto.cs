namespace BRaVe_Management_Backend.Models
{
    public class TargetingResultDto
    {
        public string Program { get; set; }
        public string ActivityCode { get; set; }
        public string HouseholdId { get; set; }
        public int HouseholdSize { get; set; }
        public string FullName { get; set; }
        public int Age { get; set; }
        public string Gender { get; set; }
        public double Score { get; set; }
        public int Rank { get; set; }
        public int DenseRank { get; set; }
        public int RowNumber { get; set; }
    }
}
