using BRaVe_Portal.Models.Enums;
using BRaVe_Portal.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BRaVe_Portal.Pages.Assessments
{
    public class ViewDecidedAssessmentModel : PageModel
    {
        [BindProperty]
        public RiskBenefitAssessmentViewModel Assessment { get; set; } = new();
        public int AssessmentId { get; set; }
        public AssessmentStatus StatusId { get; set; }

        public void OnGet(int assessmentId, AssessmentStatus statusId)
        {
            Assessment.AssessmentId = assessmentId;
            Assessment.StatusId = (int)statusId;
        }
    }
}
