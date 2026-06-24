using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace BRaVe_Management_Backend.DTOs
{
    public class DistributionEnrollmentDto
    {
        public string? HouseholdId { get; set; }

        public int IndividualId { get; set; }

     

        public string? Gender { get; set; }
        public string? GenderText { get; set; }

        public string? FullName { get; set; }

        //Distribution details
        public int DistributionId { get; set; }

        //public string? DistributionKit { get; set; }


        public int? Age { get; set; }

        public string? Location { get; set; }

        public bool Distributed { get; set; }

        public DateTime? ReceivedOn { get; set; }
        public bool ReceivedAll { get; set; }

        public string? Distribution { get; set; }

        public DateTime EnrolledOn { get; set; }

        public string? TargetingUsed { get; set; }
        public bool biometricConfirmation { get; set; } = false;
        public bool photoConfirmation { get; set; } = false;
        public string? photoBase64 { get; set; }
        //public string? data { get; set; }
        public Dictionary<string, bool> data { get; set; } = new Dictionary<string, bool>();
        public string? comment { get; set; }

        public decimal Score { get; set; }

        public int Rank { get; set; }

        public int DenseRank { get; set; }

        public int RowNumber { get; set; }
    }
}