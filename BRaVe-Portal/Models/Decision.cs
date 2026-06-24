using BRaVe_Portal.Models.Enums;

namespace BRaVe_Portal.Models
{
    public class Decision
    {
        public int AssessmentId { get; set; }

        public int AssessmentStatusId { get; set; }

        public int? DecisionStatusId { get; set; }
        public int TenantId { get; set; }
        public string Note { get; set; }

        public bool IsSubmitted { get; set; }


    }
}
