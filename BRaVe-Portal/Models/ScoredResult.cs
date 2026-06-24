namespace BRaVe_Portal.Models
{
    public class ScoredResult
    {
        public string DocumentId { get; set; } = default!;
        public double Score { get; set; }
        public int Rank { get; set; } = 0;
        public int DenseRank { get; set; } = 0;
        public int RowNumber { get; set; } = 0;
    }
}
