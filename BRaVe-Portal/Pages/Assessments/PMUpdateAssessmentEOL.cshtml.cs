using BRaVe_Portal.Extensions;
using BRaVe_Portal.Helpers;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using BRaVe_Portal.Models.DTOs;
using BRaVe_Portal.Models.Enums;
using BRaVe_Portal.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;

namespace BRaVe_Portal.Pages.Assessments
{
    public class PMUpdateAssessmentEOLModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        private readonly IAppCache _cache;

        private readonly IRestApiService _api;
        private readonly ILookupService _lookupService;

        private readonly IRBAHelperService _rBAHelperService;

        private readonly string languageCode;


        public PMUpdateAssessmentEOLModel(ILogger<IndexModel> logger, IAppCache cache, IRestApiService api, ILookupService lookupService, IRBAHelperService rBAHelperService)
        {
            _logger = logger;
            _cache = cache;
            _api = api;
            _lookupService = lookupService;
            _rBAHelperService = rBAHelperService;

            languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        }

        public RiskBenefitAssessmentViewModel Assessment { get; set; } = new();

        public string ProgramManagerName { get; set; }

        public string ProgramManagerContacts { get; set; }


        [BindProperty]
        public int? Decision { get; set; }

        [BindProperty]
        public string Note { get; set; }

        [BindProperty]
        public int AssessmentId { get; set; }


        [BindProperty]
        public bool IsValid { get; set; } = false;

        public async Task<IActionResult> OnGet(int assessmentId, AssessmentStatus statusId)
        {
            try
            {
                Assessment = await _rBAHelperService.GetAssessment(User.Identifier(), assessmentId, languageCode, false);

                ViewData["IsReadOnly"] = true;

                ProgramManagerName = Assessment.DataProcessing.ProgamManagerName;
                ProgramManagerContacts = Assessment.DataProcessing.ProgamManagerContacts;

                ViewData["ProgramManagerInfo"] = (ProgramManagerName, ProgramManagerContacts);


                CoreLookups coreLookups = await _lookupService.GetCoreLookups(languageCode);

                ViewData["CoreLookups"] = coreLookups;

                Decision? pmDecision = await _rBAHelperService.GetCurrentPmDecision(User.Identifier(), assessmentId);


                if (pmDecision != null)
                {

                    Decision = pmDecision.DecisionStatusId;
                    Note = pmDecision.Note;
                }



                return Page();

            }
            catch (Exception e)
            {

                return RedirectToPage("/Programs/Index");
            }

        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostSubmitDecision(int assessmentId)
        {
            try
            {
                Assessment = await _rBAHelperService.GetAssessment(User.Identifier(), assessmentId, languageCode, false);

                AssessmentId = Assessment.AssessmentId;

                ViewData["IsReadOnly"] = Assessment.IsReadOnly;

                ProgramManagerName = Assessment.DataProcessing.ProgamManagerName;
                ProgramManagerContacts = Assessment.DataProcessing.ProgamManagerContacts;

                ViewData["ProgramManagerInfo"] = (ProgramManagerName, ProgramManagerContacts);

                CoreLookups coreLookups = await _lookupService.GetCoreLookups(languageCode);

                ViewData["CoreLookups"] = coreLookups;

                var comDecision = new DecisionDto
                {
                    AssessmentId = assessmentId,
                    AssessmentStatusId = Assessment.StatusId,
                    DecisionStatusId = Decision,
                    Note = Note,
                    IsSubmitted = true

                };

                var customValidationResult = Validate(comDecision);

                IsValid = customValidationResult.IsValid;

                TempData["SaveDecision"] = true;

                if (customValidationResult.IsValid)
                {
                    try
                    {
                        (bool success, string message) = await _rBAHelperService.SavePmDecision(User.Identifier(), comDecision);

                        if (success)
                        {
                            TempData["SuccessMessage"] = "Decision submitted successfully.";

                            return RedirectToPage("/Programs/Index");
                        }
                        else
                        {
                            TempData["ErrorMessage"] = message;

                            IsValid = false;

                        }



                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError(string.Empty, "Unable to Submit the Draft decision用lease try again");
                    }
                }
                else
                {

                    ModelState.AddModelError(string.Empty, customValidationResult.Message);

                }

            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Unable to submit the Draft decision用lease try again");
            }

            return Page();

        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostSaveDecision(int assessmentId)
        {


            try
            {
                Assessment = await _rBAHelperService.GetAssessment(User.Identifier(), assessmentId, languageCode, false);

                AssessmentId = Assessment.AssessmentId;

                ViewData["IsReadOnly"] = Assessment.IsReadOnly;

                ProgramManagerName = Assessment.DataProcessing.ProgamManagerName;
                ProgramManagerContacts = Assessment.DataProcessing.ProgamManagerContacts;

                ViewData["ProgramManagerInfo"] = (ProgramManagerName, ProgramManagerContacts);

                CoreLookups coreLookups = await _lookupService.GetCoreLookups(languageCode);

                ViewData["CoreLookups"] = coreLookups;

                var comDecision = new DecisionDto
                {
                    AssessmentId = assessmentId,
                    AssessmentStatusId = Assessment.StatusId,
                    DecisionStatusId = Decision,
                    Note = Note,
                    IsSubmitted = false

                };

                var customValidationResult = Validate(comDecision);

                IsValid = customValidationResult.IsValid;

                TempData["SaveDecision"] = true;

                if (customValidationResult.IsValid)
                {

                    try
                    {
                        (bool success, string message) = await _rBAHelperService.SavePmDecision(User.Identifier(), comDecision);

                        if (success)
                        {

                            TempData["SuccessMessage"] = "Draft decision saved successfully.";

                        }
                        else
                        {

                            TempData["SuccessMessage"] = message;

                            IsValid = false;

                        }

                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError(string.Empty, "Unable to save the Draft decision用lease try again");
                    }


                }
                else
                {

                    ModelState.AddModelError(string.Empty, customValidationResult.Message);

                }


            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Unable to save the Draft decision用lease try again");
                //IsValid = false;

            }


            return Page();


        }

        private CustomValidationResult Validate(DecisionDto coMDecisionDto)
        {

            if (string.IsNullOrEmpty(coMDecisionDto.Note))
                return new CustomValidationResult
                {
                    IsValid = false,
                    Message = "You must provide a note."
                };


           if (!coMDecisionDto.DecisionStatusId.HasValue)
                return new CustomValidationResult
                {
                    IsValid = false,
                    Message = "A decision is required. Please select one before saving"
                };


            return new CustomValidationResult
            {
                IsValid = true,
                Message = ""
            };
        }

    }
}
