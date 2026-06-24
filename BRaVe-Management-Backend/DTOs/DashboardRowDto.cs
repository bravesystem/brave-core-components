namespace BRaVe_Management_Backend.DTOs
{
    public class DashboardRowDto
    {
        // Static / lookup fields
        public string MissionName { get; set; }
        public int ProgramID { get; set; }
        public string ProgramName { get; set; }
        public string ActivityCode { get; set; }
        public string ActivityTitle { get; set; }

        // Household fields

        public Guid BeneficiaryId { get; set; }
        public string HouseholdId { get; set; }
        public int HouseholdSize { get; set; }
        public string HouseHoldType { get; set; }
        public DateTime? CreatedOn { get; set; }

        // Individual fields


        // Head of household only
        public string? HeadFullName { get; set; }

        public string? HeadGender { get; set; }

        public int? HeadAgeYears { get; set; }

        public bool HeadFingerprintCollected { get; set; }

        public bool IsFlagged { get; set; }

        // Survey counts
        public int? HouseholdSurveys { get; set; }


    }


}
