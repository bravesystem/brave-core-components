using BRaVe_Portal.Helpers;
using BRaVe_Portal.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BRaVe_Portal.Pages.Assessments
{
    public class ViewAssessmentModel : PageModel
    {
        public int AssessmentId { get; set; }
        public AssessmentStatus StatusId { get; set; }

        public IActionResult OnGet(int assessmentId, AssessmentStatus statusId)
        {

            AssessmentId = assessmentId;
            StatusId = statusId;

            if (User.IsInRole(EnumUserRoles.CoM.ToString()) && (statusId == AssessmentStatus.Submitted || statusId == AssessmentStatus.ReviewByPM || statusId == AssessmentStatus.CoM_FinalReviewInProgress || statusId == AssessmentStatus.CoM_ReviewInProgress))
                return RedirectToPage("/Assessments/CoMUpdateAssessment", new { assessmentId, statusId });


            if ((User.IsInRole(EnumUserRoles.RO.ToString()) || User.IsInRole(EnumUserRoles.HQ.ToString())) && (statusId == AssessmentStatus.ReviewByCoM || statusId == AssessmentStatus.ROHQ_ReviewInProgress))
                return RedirectToPage("/Assessments/ROHQUpdateAssessment", new { assessmentId, statusId });


            if (User.IsInRole(EnumUserRoles.LEG.ToString()) && (statusId == AssessmentStatus.ROHQ_Submitted || statusId == AssessmentStatus.LEG_ReviewInProgress))
                return RedirectToPage("/Assessments/LegalUpdateAssessment", new { assessmentId, statusId });

            if (User.IsInRole(EnumUserRoles.PM.ToString()) && (statusId == AssessmentStatus.LEG_Submitted || statusId == AssessmentStatus.PM_ReviewInProgress))
                return RedirectToPage("/Assessments/PMUpdateAssessmentEOL", new { assessmentId, statusId });


            return RedirectToPage("/Assessments/ReadonlyAssessment", new { assessmentId });

        }
    }
}
