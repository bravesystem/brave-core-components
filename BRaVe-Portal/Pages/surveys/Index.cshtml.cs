using BRaVe_Portal.Extensions;
using BRaVe_Portal.Helpers;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using BRaVe_Portal.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
//using Microsoft.Identity.Web;
using System.Globalization;

namespace BRaVe_Portal.Pages.surveys
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IAppCache _cache;
        private readonly IRestApiService _api;
        private readonly ILookupService _lookupService;

        public IndexModel(ILogger<IndexModel> logger, IAppCache cache, IRestApiService api, ILookupService lookupService)
        {
            _logger = logger;
            _cache = cache;
            _api = api;
            _lookupService = lookupService;
        }

        public List<Survey> Surveys { get; set; } = new();
        public List<ProgramDetails> Programs { get; set; } = new();
        public List<LookupItemDto> SurveyTypes { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? SelectedProgramId { get; set; } = 0;

        //[BindProperty]
        //public SurveyDto Survey { get; set; } = new();

        [BindProperty]
        public SurveyDto UpdatedSurvey { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            _logger.LogInformation("Loading Programs, SurveyTypes, and Surveys for tenant.");

            int tenantId = User.Tenant();

            // Load Programs
            Programs = await _api.GetAsync<List<ProgramDetails>>("v1/Programs") ?? new List<ProgramDetails>();
            await _cache.SetAsync($"{StaticKeyNames.ALL_PROGRAMS_IN_MISSION}{tenantId}", Programs);
            _logger.LogInformation("Programs loaded from API and cached. Count: {Count}", Programs.Count);

            ViewData["Programs"] = Programs;

            // Load Survey Types from Lookups
            string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            CoreLookups coreLookups = await _lookupService.GetCoreLookups(languageCode);
            SurveyTypes = coreLookups.SurveyTypes;
            ViewData["CoreLookups"] = coreLookups;


            _logger.LogInformation("SurveyTypes loaded. Count: {Count}", SurveyTypes.Count);

            // Load Surveys
            if (!SelectedProgramId.HasValue || SelectedProgramId == 0)
            {
                // ALL programs user has access to
                Surveys = await _api.GetAsync<List<Survey>>("v1/Surveys")
                          ?? new List<Survey>();

                _logger.LogInformation("Loaded ALL accessible surveys. Count: {Count}", Surveys.Count);
            }
            else
            {
                // Specific program
                Surveys = await _api.GetAsync<List<Survey>>(
                    $"v1/Surveys/{SelectedProgramId.Value}")
                    ?? new List<Survey>();

                _logger.LogInformation(
                    "Surveys loaded for ProgramId {ProgramId}. Count: {Count}",
                    SelectedProgramId.Value,
                    Surveys.Count);
            }

            return Page();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAddSurveyAsync()
        {
            _logger.LogInformation("Adding new survey for ProgramId {ProgramId}", UpdatedSurvey.ProgramId);

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Could not save survey. Please try again.");
                _logger.LogWarning("ModelState invalid when adding survey.");
                await OnGetAsync(); // Reload Programs and Lookups
                return Page();
            }

            await _api.PostJsonAsync<SurveyDto, object>("v1/Surveys/create", UpdatedSurvey);
            await _cache.RemoveAsync(StaticKeyNames.ALL_PROGRAMS_IN_MISSION);
            _logger.LogInformation("Survey created and cache cleared for programs.");

            return RedirectToPage(new { SelectedProgramId = UpdatedSurvey.ProgramId });
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostEditSurvey()
        {
            SelectedProgramId = UpdatedSurvey.ProgramId;
            _logger.LogInformation("Editing survey {SurveyId} for ProgramId {ProgramId}", UpdatedSurvey.SurveyId, UpdatedSurvey.ProgramId);

            try {

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("ModelState invalid when editing survey {SurveyId}.", UpdatedSurvey.SurveyId);
                    await OnGetAsync(); // Reload Programs and Lookups
                    return Page();
                }

                // TODO: Call API to update on server
                await _api.PostJsonAsync<SurveyDto, object>("v1/Surveys/update", UpdatedSurvey);
                await _cache.RemoveAsync(StaticKeyNames.ALL_PROGRAMS_IN_MISSION);
                _logger.LogInformation("Survey {SurveyId} updated and cache cleared.", UpdatedSurvey.SurveyId);

                return RedirectToPage(new { SelectedProgramId = UpdatedSurvey.ProgramId });

            }
            catch (Exception e) {

                _logger.LogError(e, "Failed to edit survey {Id}", UpdatedSurvey.SurveyId);
                TempData["ErrorMessage"] = "Failed to update the survey. Please ensure it is not currently used in any active activity.";

            }

            return await OnGetAsync();
            
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDeleteAsync()
        {
            try
            {
                SelectedProgramId = UpdatedSurvey.ProgramId;
                _logger.LogInformation("Deleting survey {SurveyId} for ProgramId {ProgramId}", UpdatedSurvey.SurveyId, UpdatedSurvey.ProgramId);

                await _api.DeleteAsync($"v1/Surveys/{UpdatedSurvey.SurveyId}");
                _logger.LogInformation("Survey {SurveyId} deleted.", UpdatedSurvey.SurveyId);

                return RedirectToPage(new { SelectedProgramId = UpdatedSurvey.ProgramId });

            }
            catch (Exception e)
            {

                _logger.LogError(e, "Failed to delete survey {Id}", UpdatedSurvey.SurveyId);
                TempData["ErrorMessage"] = "Failed to delete the survey. Please ensure it is not currently used in any active activity.";

            }

            return await OnGetAsync();

        }
    }
}
