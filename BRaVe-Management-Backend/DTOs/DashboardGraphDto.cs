namespace BRaVe_Management_Backend.DTOs
{
    public class DashboardGraphDto
    {


        public GraphMaleFemaleChildren GraphMaleFemaleChildren { get; set; }
        public GraphFemaleChildOtherAsHead GraphFemaleChildOtherAsHead { get; set; }
        public GraphRiskAssessment GraphRiskAssessment { get; set; }
        public GraphDistributionAssistance GraphDistributionAssistance { get; set; }
    }

    public class GraphMaleFemaleChildren
    {
        public int Males { get; set; }
        public int Females { get; set; }
        public int Children { get; set; }
    }

    public class GraphFemaleChildOtherAsHead
    {
        public int FemaleHead { get; set; }
        public int ChildHead { get; set; }
        public int Other { get; set; }
    }

    public class GraphRiskAssessment
    {
        public int Created { get; set; }
        public int InProgress { get; set; }
        public int Approved { get; set; }
        public int Rejected { get; set; }
        public int PendingReview { get; set; }
    }

    public class GraphDistributionAssistance
    {
        public int TotalHouseholdsEnrolled { get; set; }
        public int TotalHouseholdsReceived { get; set; }
    }

}
