using BRaVe_Management_Backend.Models.es;
using System.Text.Json.Serialization;

namespace BRaVe_Management_Backend.Models
{
    /// <summary>
    /// Holds a Household_Document along with its rule-based score.
    /// </summary>
    public class ScoredHouseholdDocument
    {
        [JsonPropertyName("document")]
        public Household_Document Document { get; set; }

        [JsonPropertyName("score")]
        public int Score { get; set; }

        [JsonPropertyName("rank")]
        public int Rank { get; set; }

        [JsonPropertyName("dense_rank")]
        public int DenseRank { get; set; }

        [JsonPropertyName("row_number")]
        public int RowNumber { get; set; }

        public ScoredHouseholdDocument(Household_Document document, int score = 0)
        {
            Document = document;
            Score = score;
        }
    }
}
