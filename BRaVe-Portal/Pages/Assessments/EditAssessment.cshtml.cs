using BRaVe_Portal.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace BRaVe_Portal.Pages.Assessments
{
    public class EditAssessmentModel : PageModel
    {
        private readonly ILogger<EditAssessmentModel> _logger;

        public EditAssessmentModel(ILogger<EditAssessmentModel> logger)
        {
            _logger = logger;
        }

        public int AssessmentId { get; set; }
        public AssessmentStatus StatusId { get; set; }

        public IActionResult OnGet(int assessmentId, AssessmentStatus statusId)
        {
            _logger.LogInformation("OnGet called for AssessmentId={AssessmentId}, StatusId={StatusId}", assessmentId, statusId);

            AssessmentId = assessmentId;
            StatusId = statusId;

            if (statusId == AssessmentStatus.Draft || statusId == AssessmentStatus.PendingReview)
            {
                _logger.LogInformation("Redirecting to PMUpdateAssessment for AssessmentId={AssessmentId}", assessmentId);
                return RedirectToPage("/Assessments/PMUpdateAssessment", new { assessmentId, statusId });
            }

            if (statusId == AssessmentStatus.Submitted || statusId == AssessmentStatus.CoM_ReviewInProgress)
            {
                _logger.LogInformation("Redirecting to CoMUpdateAssessment for AssessmentId={AssessmentId}", assessmentId);
                return RedirectToPage("/Assessments/CoMUpdateAssessment", new { assessmentId, statusId });
            }

            if (statusId == AssessmentStatus.ROHQ_ReviewInProgress)
            {
                _logger.LogInformation("Redirecting to RO_HQUpdateAssessment for AssessmentId={AssessmentId}", assessmentId);
                return RedirectToPage("/Assessments/RO_HQUpdateAssessment", new { assessmentId, statusId });
            }

            if (statusId == AssessmentStatus.LEG_ReviewInProgress)
            {
                _logger.LogInformation("Redirecting to LegalUpdateAssessment for AssessmentId={AssessmentId}", assessmentId);
                return RedirectToPage("/Assessments/LegalUpdateAssessment", new { assessmentId, statusId });
            }

            if (statusId == AssessmentStatus.PM_ReviewInProgress)
            {
                _logger.LogInformation("Redirecting to PMUpdateAssessmentEOL for AssessmentId={AssessmentId}", assessmentId);
                return RedirectToPage("/Assessments/PMUpdateAssessmentEOL", new { assessmentId, statusId });
            }

            _logger.LogInformation("Redirecting to Assessments index for AssessmentId={AssessmentId}", assessmentId);
            return RedirectToPage("/Assessments");
        }
    }
}
