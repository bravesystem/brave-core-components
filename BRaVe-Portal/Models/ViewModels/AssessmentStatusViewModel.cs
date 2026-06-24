using BRaVe_Portal.Models.Enums;

namespace BRaVe_Portal.Models.ViewModels
{
    public class AssessmentStatusViewModel
    {
        public string AssessmentId { get; set; }
        public string ProgramManager { get; set; }
        public AssessmentStatus Status { get; set; }

    }
}