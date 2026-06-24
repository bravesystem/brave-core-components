using BRaVe_Portal.Extensions;
using BRaVe_Portal.Helpers;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using BRaVe_Portal.Models.DTOs;
using BRaVe_Portal.Models.ViewModels;
using DocumentFormat.OpenXml.InkML;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;
using System.Net;
using static System.Net.WebRequestMethods;

namespace BRaVe_Portal.Pages.programs.Activities
{
    public class ManageActivitiesModel : PageModel
    {
        private readonly ILogger<ManageActivitiesModel> _logger;
        private readonly IRestApiService _api;
        private readonly ILookupService _lookupService;
        private readonly IRBAHelperService _rBAHelperService;
        private readonly string languageCode;

        public ManageActivitiesModel(
            IRBAHelperService rBAHelperService,
            ILogger<ManageActivitiesModel> logger,
            IRestApiService api,
            ILookupService lookupService)
        {
            _logger = logger;
            _api = api;
            _lookupService = lookupService;
            _rBAHelperService = rBAHelperService;
            languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        }

        public ProgramDetails? SelectedProgram { get; set; }
        public AssessmentViewModel? SelectedAssessment { get; set; }

        public List<ActivityViewModel> Activities { get; set; } = new();
        public List<ProgramDetails> Programs { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? SelectedProgramId { get; set; }

        [BindProperty(SupportsGet = true)]
        public LocationCascadeViewModel LocationCascade { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SelectedActivityId { get; set; }

        [BindProperty]
        public ActivityViewModel NewActivity { get; set; } = new();

        public ActivityViewModel? SelectedActivity { get; set; }
        public RiskBenefitAssessmentViewModel Assessment { get; set; } = new();


        // GET: Load programs and activities
        public async Task<IActionResult> OnGetAsync()
        {

            if (Request.ContainsPathProbeInQuery())
            {
                _logger.LogWarning("Blocked suspicious query payload on MangeActivities GET.");
                return BadRequest("Invalid request.");
            }

            if (!Request.TryGetOptionalPositiveIntFromQuery(_logger, "SelectedProgramId", out var validatedProgramId) ||
                !Request.TryGetOptionalPositiveIntFromQuery(_logger, "SelectedActivityId", out var validatedActivityId))
            {
                return BadRequest("Invalid request.");
            }

            SelectedProgramId = validatedProgramId;
            SelectedActivityId = validatedActivityId;

            try
            {
                LocationCascade.AdminLevels = await _lookupService.GetAdminLevels();

                LocationCascade.LocationsByLevel = await _lookupService.GetAllLocationsByLevel();

                // Load programs from API
                Programs = await _api.GetAsync<List<ProgramDetails>>("v1/Programs") ?? new List<ProgramDetails>();

                if (!SelectedProgramId.HasValue)
                {
                    _logger.LogWarning("ProgramId not available, skipping activity load");
                    return Page();
                }

                if (SelectedProgramId.Value <= 0)
                {
                    _logger.LogWarning("Invalid ProgramId provided: {ProgramId}", SelectedProgramId.Value);
                    TempData["ErrorMessage"] = "Invalid program selection.";
                    return Page();
                }

                // Load selected program details
                SelectedProgram = Programs.FirstOrDefault(p => p.ProgramId == SelectedProgramId.Value);

                if (SelectedProgram != null)
                {
                    var assessmentId = SelectedProgram.AssessmentId;
                    Assessment = await _rBAHelperService.GetAssessment(User.Identifier(), assessmentId.Value, languageCode);

                }

                // Fetch activities for selected program via API

                Activities = await _api.GetAsync<List<ActivityViewModel>>(
                        $"v1/Activities/Program/{SelectedProgramId.Value}"
                    ) ?? new List<ActivityViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API failure while loading activities for the selected program {ProgramId}", SelectedProgramId.Value);
                TempData["ErrorMessage"] = "Unable to load the activities for the selected program. Please try again.";

            }

            return Page();

        }

        public async Task<IActionResult> OnGetLocationsByLevelAsync(int levelId, int? parentLocationId)
        {

            if (Request.ContainsPathProbeInQuery())
            {
                _logger.LogWarning("Blocked suspicious query payload on GetLocationsByLevel GET.");
                return BadRequest("Invalid request.");
            }

            try
            {
                var allLocations = await _api.GetAsync<List<Location>>($"v1/Locations")
                ?? new List<Location>();

                var locationsAtLevel = allLocations
                    .Where(x => x.IsActive && x.LevelId == levelId);

                if (levelId == 1)
                {
                    locationsAtLevel = locationsAtLevel.Where(x => !x.ParentLocationId.HasValue);
                }
                else if (parentLocationId.HasValue)
                {
                    locationsAtLevel = locationsAtLevel.Where(x => x.ParentLocationId == parentLocationId.Value);
                }
                else
                {
                    locationsAtLevel = Enumerable.Empty<Location>();
                }

                var result = locationsAtLevel
                    .OrderBy(x => x.LocationName)
                    .Select(x => new
                    {
                        id = x.Id,
                        name = x.LocationName
                    })
                    .ToList();

                return new JsonResult(result);

            }
            catch (Exception e)
            {
                _logger.LogError(e, "API failure while loading locations");
                return new JsonResult(new List<SelectItemDto>());
            }

        }


        // POST: Create new activity via API
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostCreateActivityAsync()
        {

            if (Request.ContainsPathProbeInQuery() || await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogWarning("Blocked suspicious query payload on CreateActivity POST.");
                return BadRequest("Invalid request.");
            }

            /*if (JsonPayloadHelper.ContainsPathProbe(NewActivity))
            {
                _logger.LogWarning("Blocked suspicious activity payload on CreateActivity POST.");
                return BadRequest("Invalid activity payload.");
            }*/


            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model while creating activity");
                return await OnGetAsync();
            }

            NewActivity.AdminArea = LocationCascade.getAdminArea();

            try
            {
                // Ensure TenantId and CreatedBy are set
                NewActivity.TenantId = User.Tenant();
                NewActivity.CreatedBy = User.Identity?.Name ?? "System";

                // for test we need to add enum  for created
                NewActivity.StatusId = 1;

                // Call API to create the activity
                //await _api.PostJsonAsync<ActivityViewModel, object>("v1/Activities", NewActivity);
                await _api.PostJsonAsync<ActivityViewModel, object>(
                        "v1/Activities",
                        NewActivity
                    );


                _logger.LogInformation("Activity '{Title}' created for Tenant {TenantId}",
                    NewActivity.Title, NewActivity.TenantId);

                TempData["SuccessMessage"] = "Activity created successfully!";

                return RedirectToPage("./ManageActivities", new { SelectedProgramId = NewActivity.ProgramId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating activity '{Title}'", NewActivity.Title);
                TempData["ErrorMessage"] = "Failed to create activity.";
            }

            return await OnGetAsync();
        }

        // POST: Edit existing activity via API
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostEditActivityAsync()
        {

            if (Request.ContainsPathProbeInQuery() || await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogWarning("Blocked suspicious query payload on EditActivity POST.");
                return BadRequest("Invalid request.");
            }

            /*if (JsonPayloadHelper.ContainsPathProbe(NewActivity))
            {
                _logger.LogWarning("Blocked suspicious activity payload on EditActivity POST.");
                return BadRequest("Invalid activity payload.");
            }*/


            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model while editing activity");
                return await OnGetAsync();
            }

            try
            {
                // Call API to update activity
                await _api.PostJsonAsync<ActivityViewModel, object>("v1/Activities/{NewActivity.ActivityId}", NewActivity);

                _logger.LogInformation("Activity '{Title}' updated", NewActivity.Title);

                return RedirectToPage(new { SelectedProgramId = NewActivity.ProgramId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing activity '{Title}'", NewActivity.Title);
                TempData["ErrorMessage"] = "Failed to edit activity.";
            }

            return await OnGetAsync();
        }

    }
}
