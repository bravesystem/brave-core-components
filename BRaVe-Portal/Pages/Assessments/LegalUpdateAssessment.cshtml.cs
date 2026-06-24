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
    public class LegalUpdateAssessmentModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        private readonly IAppCache _cache;

        private readonly IRestApiService _api;
        private readonly ILookupService _lookupService;

        private readonly IRBAHelperService _rBAHelperService;

        private readonly string languageCode;


        public LegalUpdateAssessmentModel(ILogger<IndexModel> logger, IAppCache cache, IRestApiService api, ILookupService lookupService, IRBAHelperService rBAHelperService)
        {
            _logger = logger;
            _cache = cache;
            _api = api;
            _lookupService = lookupService;
            _rBAHelperService = rBAHelperService;

            languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        }

        public RiskBenefitAssessmentViewModel Assessment { get; set; } = new();

        [BindProperty]
        public int AssessmentId { get; set; }

        [BindProperty]
        public string Note { get; set; }

        public string ProgramManagerName { get; set; }

        public string ProgramManagerContacts { get; set; }

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

                Recommendation? recommendation = await _rBAHelperService.GetCurrentRecommendation(User.Identifier(), true, assessmentId);


                if (recommendation != null)
                {
                    Note = recommendation.Note;
                }

                return Page();

            }
            catch (Exception e)
            {

                return RedirectToPage("/Programs/Index");
            }
        }




        public async Task<IActionResult> OnPostSubmitRecommendation(int assessmentId)
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


                var recommendationDto = new RecommendationDto
                {
                    AssessmentId = assessmentId,
                    AssessmentStatusId = Assessment.StatusId,
                    Note = Note,
                    IsSubmitted = true,
                    IsLeg = true

                };

                var customValidationResult = Validate(recommendationDto);

                IsValid = customValidationResult.IsValid;

                TempData["SaveRecommendation"] = true;

                if (customValidationResult.IsValid)
                {
                    try
                    {

                        (bool success, string message) = await _rBAHelperService.SaveRecommendation(User.Identifier(), recommendationDto);

                        if (success)
                        {
                            TempData["SuccessMessage"] = "Recommendation submitted successfully.";

                            return RedirectToPage("/Programs/Index");
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "Unable to submit the Draft Recommendation用lease try again";

                            IsValid = false;

                        }



                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError(string.Empty, "Unable to Submit the Draft Recommendation用lease try again");
                        IsValid = false;
                    }
                }
                else
                {

                    ModelState.AddModelError(string.Empty, customValidationResult.Message);

                }

            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Unable to submit the Draft Recommendation用lease try again");
            }

            return Page();

        }

        public async Task<IActionResult> OnPostSaveRecommendation(int assessmentId)
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

                var recommendationDto = new RecommendationDto
                {
                    AssessmentId = assessmentId,
                    AssessmentStatusId = Assessment.StatusId,
                    Note = Note,
                    IsSubmitted = false,
                    IsLeg = true

                };

                var customValidationResult = Validate(recommendationDto);

                IsValid = customValidationResult.IsValid;

                TempData["SaveRecommendation"] = true;

                if (customValidationResult.IsValid)
                {

                    try
                    {

                        (bool success, string message) = await _rBAHelperService.SaveRecommendation(User.Identifier(), recommendationDto);

                        if (success)
                        {

                            TempData["SuccessMessage"] = "Draft Recommendation saved successfully.";

                            //return RedirectToPage("Index");

                        }
                        else
                        {

                            TempData["ErrorMessage"] = message;

                            IsValid = false;

                        }

                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError(string.Empty, "Unable to save the Draft Recommendation用lease try again");
                        IsValid = false;
                    }


                }
                else
                {
                    //ModelState.AddModelError(string.Empty, customValidationResult.Message);
                    IsValid = false;
                }


            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Unable to save the Draft Recommendation用lease try again");

            }


            return Page();


        }


        private CustomValidationResult Validate(RecommendationDto recommendationDto)
        {
            if (string.IsNullOrEmpty(recommendationDto.Note))
                return new CustomValidationResult
                {
                    IsValid = false,
                    Message = "You must provide a note."
                };

            return new CustomValidationResult
            {
                IsValid = true,
                Message = ""
            };
        }
    }
}
