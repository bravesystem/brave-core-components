using BRaVe_Portal.Extensions;
using BRaVe_Portal.Helpers;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using BRaVe_Portal.Models.DTOs;
using BRaVe_Portal.Models.ViewModels;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Office2013.Excel;
using DocumentFormat.OpenXml.Office2019.Word.Cid;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text.Json;

namespace BRaVe_Portal.Pages.programs.Activities
{
    public class ViewEditActivityModel : PageModel
    {
        private readonly ILogger<ViewEditActivityModel> _logger;
        private readonly IRestApiService _api;
        private readonly IAppCache _cache;
        private readonly ILookupService _lookupService;
        private readonly string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

        public ViewEditActivityModel(
            ILogger<ViewEditActivityModel> logger,
            IAppCache cache,
            IRestApiService api,
            ILookupService lookupService)
        {
            _logger = logger;
            _cache = cache;
            _api = api;
            _lookupService = lookupService;
        }

        [BindProperty(SupportsGet = true)]
        public int? Id { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FlashSection { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FlashType { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FlashMessage { get; set; }

        public ActivityViewModel? SelectedActivity { get; set; }

        [BindProperty]
        public ActivityViewModel NewActivity { get; set; } = new();
        [BindProperty]
        public List<string> EnumeratorCodes { get; set; } = new();

        public string DefaultLang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        public List<ProgramDetails> Programs { get; set; } = new();
        public List<DataPointViewModel> DataPoints { get; set; } = new();
        public List<ActivityDataPoint> SelectedDataPoints { get; set; } = new();

        [BindProperty]
        public List<Preference> Preferences { get; set; } = new();

        [BindProperty]
        public Dictionary<string, string> PreferenceValues { get; set; } = new();

        [BindProperty]
        public Dictionary<string, string> PreferenceTypes { get; set; } = new();

        public List<MissionPreferences> AllAvailablePreferences { get; set; } = new();
        public List<ActivityPreference> AttachedPreferences { get; set; } = new();

        //OLD MAPING FOR DISTRIBUTION TO ACTIVITY
        // public List<DistributionViewModel> AllDistributors { get; set; } = new();

        //NEW MAPPING FOR DISTRIBUTION TO ACTIVITY
        public List<DistributionTypes> AllDistributors { get; set; } = new();
        public List<ActivityDistributors> AttachedDistributers { get; set; } = new();

        public List<Models.Survey> AvailableSurveys { get; set; } = new();
        public List<ActivitySurveys> SelectedSurveys { get; set; } = new();


        public List<ConcentViewModel> AvailableConsents { get; set; } = new();
        public List<ConcentViewModel> SelectedConsents { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public LocationCascadeViewModel LocationCascade { get; set; }

        public List<Enumerator> AllEnumerators { get; set; } = new();
        public List<ActivityEnumerator> SelectedDEnumerator { get; set; } = new();
        [BindProperty] public bool Required { get; set; }

        // ───────────────────────────────
        // GET
        // ───────────────────────────────
        public async Task<IActionResult> OnGetAsync()
        {
            if (Request.ContainsPathProbeInQuery())
            {
                _logger.LogError("Blocked suspicious query payload on ViewEditActivity GET.");
                return BadRequest("Invalid request.");
            }


            if (!Request.TryGetOptionalPositiveIntFromQuery(_logger,"id",out var activityId))
            {
                _logger.LogError("Blocked suspicious activity payload on ViewEditActivity GET.");
                return BadRequest("Invalid request.");
            }

            Id = activityId;

            try
            {

                LocationCascade.AdminLevels = await _lookupService.GetAdminLevels();

                LocationCascade.LocationsByLevel = await _lookupService.GetAllLocationsByLevel();


                if (!Id.HasValue)
                {
                    _logger.LogWarning("No activity ID provided in request");
                    TempData["ErrorMessage"] = "No activity selected.";
                    return RedirectToPage("/programs/Activities/ManageActivities");
                }
                // ───────────────────────────────
                // Load Selected Activity
                // ───────────────────────────────
                SelectedActivity = await _api.GetAsync<ActivityViewModel>($"v1/Activities/{Id.Value}");
                if (SelectedActivity == null)
                {
                    _logger.LogWarning("Activity with ID {Id} not found", Id);
                    TempData["ErrorMessage"] = $"Activity with ID {Id} not found.";
                    return RedirectToPage("/programs/Activities/ManageActivities");
                }

                LocationCascade.setAdminArea(SelectedActivity.AdminArea);

                var programId = SelectedActivity.ProgramId;

                Programs = await _api.GetAsync<List<ProgramDetails>>("v1/Programs") ?? new List<ProgramDetails>();

                CoreLookups coreLookups = await _lookupService.GetCoreLookups(languageCode);
                ViewData["CoreLookups"] = coreLookups;

                DataPoints = await _api.GetAsync<List<DataPointViewModel>>(
                 $"v1/DataPoints/activity?languageCode={DefaultLang}")
                  ?? new List<DataPointViewModel>();

                AvailableConsents = await _api.GetAsync<List<ConcentViewModel>>($"v1/Consents/Program/{programId}")
                           ?? new List<ConcentViewModel>();

                AvailableSurveys = await _api.GetAsync<List<Models.Survey>>($"v1/Surveys/{programId}") ?? new List<Models.Survey>();

                AllAvailablePreferences = await _api.GetAsync<List<MissionPreferences>>($"v1/Preferences")
                                  ?? new List<MissionPreferences>();

                AllEnumerators = await _api.GetAsync<List<Enumerator>>("v1/Enumerators") ?? new List<Enumerator>();
                // OLD DISTRIBUTION MAPPING TO ACTIVITY
                //AllDistributors = await _api.GetAsync<List<DistributionViewModel>>($"v1/Distributions") ?? new List<DistributionViewModel>();

                // NEW DISTRIBUTION MAPPING TO ACTIVITY
                AllDistributors = await _api.GetAsync<List<DistributionTypes>>($"v1/DistributionsType") ?? new List<DistributionTypes>();

                // ───────────────────────────────
                // Load Related Data
                // ───────────────────────────────

                SelectedDataPoints = await _api.GetAsync<List<ActivityDataPoint>>($"v1/Activities/{Id}/DataPoints") ?? new List<ActivityDataPoint>();

                SelectedDEnumerator = await _api.GetAsync<List<ActivityEnumerator>>($"v1/Activities/{Id}/Enumerator") ?? new List<ActivityEnumerator>();

                SelectedSurveys = await _api.GetAsync<List<ActivitySurveys>>($"v1/Activities/{Id}/Surveys")
                   ?? new List<ActivitySurveys>();


                SelectedConsents = await _api.GetAsync<List<ConcentViewModel>>(
                  $"v1/Activities/{Id}/Consents?languageCode={languageCode}")
                ?? new List<ConcentViewModel>();

                AttachedPreferences = await _api.GetAsync<List<ActivityPreference>>($"v1/Activities/{Id}/preference") ?? new List<ActivityPreference>();

                AttachedDistributers = await _api.GetAsync<List<ActivityDistributors>>($"v1/Activities/{Id}/Distributions") ?? new List<ActivityDistributors>();

                ViewData["datapoints"] = DataPoints;


                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error loading activity details for Id {Id}", Id);
                TempData["ErrorMessage"] = "Error loading activity details—please try again later.";
                return RedirectToPage("/programs/Activities/ManageActivities");
            }
        }


        public async Task<IActionResult> OnGetLocationsByLevelAsync(int levelId, int? parentLocationId)
        {
            if (Request.ContainsPathProbeInQuery())
            {
                _logger.LogError("Blocked suspicious query payload on GetLocationsByLevel GET.");
                return BadRequest("Invalid request.");
            }

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


        // ───────────────────────────────
        // VALIDATION
        // ───────────────────────────────
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostValidateActivityAsync()
        {
            if (Request.ContainsPathProbeInQuery() || await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogError("Blocked suspicious payload on ValidateActivity POST.");
                return BadRequest("Invalid request.");
            }

            TempData["Section"] = "ValidateActivity";

            if (!Id.HasValue)
            {
                TempData["ErrorMessage"] = "No activity selected to activate.";
                return RedirectToPage(new { Id = 0 });
            }

            try
            {
                // Mark activity as validated/downloadable.
                // Assumption: API uses IsActive to represent the validation state.
                await _api.PostJsonAsync<object>($"v1/Activities/validate/{Id.Value}");
                //TempData["SuccessMessage"] = "Activity validated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate activity {Id}", Id.Value);
                //TempData["ErrorMessage"] = "Unable to validate activity—please try again.";
            }

            return RedirectToPage(new { Id = Id.Value });
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostInvalidateActivityAsync()
        {
            if (Request.ContainsPathProbeInQuery() || await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogError("Blocked suspicious payload on InvalidateActivity POST.");
                return BadRequest("Invalid request.");
            }

            TempData["Section"] = "InvalidateActivity";

            if (!Id.HasValue)
            {
                TempData["ErrorMessage"] = "No activity selected to de-activate.";
                return RedirectToPage(new { Id = 0 });
            }

            try
            {
                // Mark activity as validated/downloadable.
                // Assumption: API uses IsActive to represent the validation state.
                await _api.PostJsonAsync<object>($"v1/Activities/invalidate/{Id.Value}");
                //TempData["SuccessMessage"] = "Activity validated successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Unable to de-activate activity—please try again.";
                _logger.LogError(ex, "Failed to de-activate activity {Id}", Id.Value);
            }

            return RedirectToPage(new { Id = Id.Value });
        }



        // ───────────────────────────────
        // EDIT ACTIVITY
        // ───────────────────────────────
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostEditActivityAsync()
        {
            if (Request.ContainsPathProbeInQuery() || await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogError("Blocked suspicious payload on EditActivity POST.");
                return BadRequest("Invalid request.");
            }

            TempData["Section"] = "EditActivity";

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model while editing activity");
                await OnGetAsync();
                return Page();
            }

            NewActivity.AdminArea = LocationCascade.getAdminArea();
            
            //if (ActivityContainsPathProbe(NewActivity))
            if (JsonPayloadHelper.ContainsPathProbe(NewActivity))
            {
                _logger.LogWarning("Blocked suspicious activity payload on EditActivity POST.");
                return BadRequest("Invalid activity payload.");
            }

            try
            {
                //set status to on going this ill have to confim with Abdul
                NewActivity.StatusId = 2;
                // Call API to update activity
                await _api.PutJsonAsync<ActivityViewModel, object>("v1/Activities", NewActivity);

                _logger.LogInformation("Activity '{Title}' updated", NewActivity.Title);
                TempData["SuccessMessage"] = "Activity edited successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update activity {Id}", NewActivity.ActivityId);
                TempData["ErrorMessage"] = "Unable to edit activity—please try again";
            }

            return RedirectToPage(new { Id = NewActivity.ActivityId });
        }

        // ───────────────────────────────
        // CONSENTS
        // ───────────────────────────────
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> OnPostAddConcentToActivityAsync(int ActivityId, int Id)
        //{
        //    if (Request.ContainsPathProbeInQuery() || await Request.ContainsPathProbeInFormAsync())
        //    {
        //        _logger.LogError("Blocked suspicious payload on AddConcentToActivity POST.");
        //        return BadRequest("Invalid request.");
        //    }

        //    TempData["Section"] = "Consent";
        //    try
        //    {
        //        await _api.PostJsonAsync<object, object>($"v1/Activities/{Id}/Consents/{Id}", null);
        //        TempData["SuccessMessage"] = "Consent added successfully.";
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error adding consent {concentId} to activity {ActivityId}", Id, ActivityId);
        //        TempData["ErrorMessage"] = "Unable to add consent" + ex.InnerException;
        //    }
        //    return RedirectToPage(new { Id = ActivityId });
        //}

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostRemoveConcentFromActivityAsync(int activityId, int Id)
        {
            if (Request.ContainsPathProbeInQuery() || await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogError("Blocked suspicious payload on RemoveConcentFromActivity POST.");
                return BadRequest("Invalid request.");
            }

            TempData["Section"] = "Consent";
            try
            {
                await _api.DeleteAsync($"v1/Activities/{activityId}/Consents/{Id}");
                TempData["SuccessMessage"] = "Consent removed successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing consent {Id} from activity {activityId}", Id, activityId);
                TempData["ErrorMessage"] = "Unable to remove consent—please try again";
            }
            return RedirectToPage(new { Id = activityId });
        }



        // ───────────────────────────────
        // DATA POINTS
        // ───────────────────────────────
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAddActivityDataPointAsync(int ActivityId, int Id, int dataPointType)
        {
            if (Request.ContainsPathProbeInQuery() || await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogError("Blocked suspicious payload on AddActivityDataPoint POST.");
                return BadRequest("Invalid request.");
            }

            TempData["Section"] = "Datapoint";
            try
            {
                await _api.PostJsonAsync<object, object>(
                    $"v1/Activities/{ActivityId}/DataPoints/{Id}/DatapointType/{dataPointType}",
                    new { Required }
                );

                TempData["SuccessMessage"] = "Data point added successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding datapoint {Id} to activity {ActivityId}", Id, ActivityId);
                TempData["ErrorMessage"] = "Unable to add datapoint—please try again";
            }
            return RedirectToPage(new { Id = ActivityId });
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostRemoveActivityDataPointAsync(int ActivityId, int Id)
        {
            if (Request.ContainsPathProbeInQuery() || await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogError("Blocked suspicious payload on RemoveActivityDataPoint POST.");
                return BadRequest("Invalid request.");
            }

            TempData["Section"] = "Datapoint";
            try
            {
                await _api.DeleteAsync($"v1/Activities/{ActivityId}/DataPoints/{Id}");
                TempData["SuccessMessage"] = "Data point removed successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing datapoint {Id}", Id);
                TempData["ErrorMessage"] = "Unable to remove datapoint—please try again";
            }
            return RedirectToPage(new { Id = ActivityId });
        }

        // ───────────────────────────────
        // DEFAULT PREFERENCES
        // ───────────────────────────────
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> OnPostAddPreferenceToActivityAsync(int activityId)
        {
            if (Request.ContainsPathProbeInQuery() ||
                await Request.ContainsPathProbeInFormAsync() ||
                JsonPayloadHelper.ContainsPathProbe(PreferenceValues) ||
                JsonPayloadHelper.ContainsPathProbe(PreferenceTypes))
            {
                _logger.LogError("Blocked suspicious payload on AddPreferenceToActivity POST.");
                return BadRequest("Invalid request.");
            }

            Id = activityId;

            TempData["Section"] = "Preference";

            if (PreferenceValues == null || !PreferenceValues.Any())
            {
                ModelState.AddModelError(string.Empty, "No preferences to add.");

                return await OnGetAsync();

            }

            var dtoList = PreferenceValues
                 .Where(kvp => int.TryParse(kvp.Key, out _))
                 .Select(kvp =>
                 {
                     PreferenceTypes.TryGetValue(kvp.Key, out var prefType);

                     return new MissionPreferencesDto
                     {
                         Id = int.Parse(kvp.Key),
                         DefaultValue = kvp.Value,
                         PreferenceType = prefType,
                         TenantId = User.Tenant(),
                         CreatedByUserId = User.Identity?.Name ?? "system",
                         CreatedOn = DateTime.UtcNow,
                         UpdatedByUserId = User.Identity?.Name ?? "system",
                         UpdatedOn = DateTime.UtcNow
                     };
                 })
                 .ToList();


            if (!dtoList.Any())
            {
                ModelState.AddModelError(string.Empty, "No valid preferences to add.");

                return await OnGetAsync();
            }

            try
            {
                var url = $"v1/activities/{activityId}/preferences";

                await _api.PostJsonAsync<List<MissionPreferencesDto>, object>(
                    url,
                    dtoList);

                return RedirectToPage(new { Id = activityId });

            }
            catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.BadRequest)
            {
                _logger.LogError(e, "Error adding preferences to activity {ActivityId}", activityId);
                //ModelState.AddModelError(string.Empty, e.Message);
                TempData["ErrorMessage"] = e.Message;
            }
            catch (HttpRequestException e)
            {
                _logger.LogError(e, "Error adding preferences to activity {ActivityId}", activityId);
                ModelState.AddModelError(string.Empty, string.Format("Error adding preferences to activity {ActivityId}", activityId));
                //TempData["ErrorMessage"] = string.Format("Error adding preferences to activity {ActivityId}", activityId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding preferences to activity {ActivityId}", activityId);
                //TempData["ErrorMessage"] = "Failed to add preferences.";
                ModelState.AddModelError(string.Empty, "Failed to add preferences.");
            }

            return RedirectToPage(new { Id = activityId });

        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostRemoveAPrefferenceFromActivityAsync(int ActivityId, int Id)
        {
            if (Request.ContainsPathProbeInQuery() || await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogError("Blocked suspicious payload on RemoveAPrefferenceFromActivity POST.");
                return BadRequest("Invalid request.");
            }

            TempData["Section"] = "Preference";
            try
            {
                await _api.DeleteAsync($"v1/Activities/{ActivityId}/DefaultPreferences/{Id}");
                TempData["SuccessMessage"] = "Default preference removed successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing preference {Id}", Id);
                TempData["ErrorMessage"] = "Unable to remove default preference—please try again";
            }
            return RedirectToPage(new { Id = ActivityId });
        }



        // ───────────────────────────────
        // ENUMERATORS
        // ───────────────────────────────
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAttachEnumeratorToActivityAsync(int ActivityId)
        {
            if (Request.ContainsPathProbeInQuery() ||
                await Request.ContainsPathProbeInFormAsync() ||
                StringHelper.IsPotentialPathProbe(string.Join(",", EnumeratorCodes ?? new())))
            {
                _logger.LogError("Blocked suspicious payload on bulk AttachEnumerator.");
                return BadRequest("Invalid request.");
            }

            TempData["Section"] = "Enumerator";

            if (EnumeratorCodes == null || !EnumeratorCodes.Any())
            {
                TempData["ErrorMessage"] = "Please select at least one enumerator.";
                return RedirectToPage(new { Id = ActivityId });
            }

            try
            {
                await _api.PostJsonAsync<List<string>, object>(
                    $"v1/Activities/{ActivityId}/Enumerators/bulk",
                    EnumeratorCodes
                );

                _logger.LogInformation(
                    "Attached {Count} enumerators to activity {ActivityId}",
                    EnumeratorCodes.Count,
                    ActivityId
                );

                TempData["SuccessMessage"] = "Enumerators attached successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to attach enumerators to activity {ActivityId}",
                    ActivityId);

                TempData["ErrorMessage"] = "Unable to attach enumerators—please try again";
            }

            return RedirectToPage(new { Id = ActivityId });
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostRemoveEnumeratorAsync(int ActivityId, string id)
        {
            if (Request.ContainsPathProbeInQuery() ||
                await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogError("Blocked suspicious payload on RemoveEnumerator POST.");
                return BadRequest("Invalid request.");
            }

            TempData["Section"] = "Enumerator";
            try
            {
                await _api.DeleteAsync($"v1/Activities/{ActivityId}/Enumerators/{id}");
                TempData["SuccessMessage"] = "Enumerator removed successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing enumerator {Id}", Id);
                TempData["ErrorMessage"] = "Unable to remove enumerator—please try again";
            }
            return RedirectToPage(new { Id = ActivityId });
        }

        // ───────────────────────────────
        // SURVEYS
        // ───────────────────────────────
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAddSurveyToActivityAsync(int ActivityId, int surveyId)
        {
            if (Request.ContainsPathProbeInQuery() || 
                await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogWarning("Blocked suspicious payload on AddSurveyToActivity POST.");
                return BadRequest("Invalid request.");
            }

            TempData["Section"] = "Survey";

            try
            {
                // Call the API to attach the survey
                await _api.PostJsonAsync<object, object>(
                    $"v1/Activities/{ActivityId}/Surveys/{surveyId}",
                   new { Required }
                );


                TempData["SuccessMessage"] = "Survey attached successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error attaching survey {SurveyId} to activity {ActivityId}", surveyId, ActivityId);
                TempData["ErrorMessage"] = "Unable to attach survey—please try again";
            }

            return RedirectToPage(new { Id = ActivityId });
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostRemoveSurveyFromActivityAsync(int activityId, int Id)
        {
            if (Request.ContainsPathProbeInQuery() || 
                await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogError("Blocked suspicious payload on RemoveSurveyFromActivity POST.");
                return BadRequest("Invalid request.");
            }

            TempData["Section"] = "Survey";

            try
            {
                // Call your API to remove the survey
                await _api.DeleteAsync($"v1/Activities/{activityId}/Surveys/{Id}");

                TempData["SuccessMessage"] = "Survey removed successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing survey {SurveyId} from activity {ActivityId}", Id, activityId);
                TempData["ErrorMessage"] = "Unable to remove survey—please try again";
            }

            // Reload the page with the activity id
            return RedirectToPage(new { Id = activityId });
        }


        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAddConsentToActivityAsync(int activityId, int consentId, int consentType, bool required)
        {
            if (Request.ContainsPathProbeInQuery() || 
                await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogError("Blocked suspicious payload on AddConsentToActivity POST.");
                return BadRequest("Invalid request.");
            }

            TempData["Section"] = "Consent";

            try
            {
                await _api.PostJsonAsync<object, object>($"v1/Activities/{activityId}/Consents/{consentId}/ConsentType/{consentType}", new { Required });

                TempData["SuccessMessage"] = "Consent successfully attached to activity.";
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error attaching consent {ConsentId} to activity {ActivityId}",
                    consentId,
                    activityId);

                var errorMessage = ex.Message
                    .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                    .FirstOrDefault();

                TempData["ErrorMessage"] = $"Unable to attach consent. {errorMessage}";
            }

            return RedirectToPage(new { Id = activityId }); ;
        }

        // ───────────────────────────────
        // DISTRIBUTIONS
        // ───────────────────────────────
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAddDistributersToActivity(int ActivityId, int Id, int distributionType, int EnrollmentMode, bool PhotoConfirmation, bool BiometricVerification)
        {
            if (Request.ContainsPathProbeInQuery() || await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogError("Blocked suspicious payload on AddDistributersToActivity POST.");
                return BadRequest("Invalid request.");
            }

            TempData["Section"] = "Distributors";
            try
            {

                await _api.PostJsonAsync<object, object>(
                            $"v1/Activities/{ActivityId}/Distributions/{Id}/DistributionType/{distributionType}" +
                            $"?EnrollmentMode={EnrollmentMode}" +
                            $"&PhotoConfirmation={PhotoConfirmation}" +
                            $"&BiometricVerification={BiometricVerification}",
                            new { Required }
                        ); ;

                TempData["SuccessMessage"] = "Distribution added successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding distribution {Id} to activity {ActivityId}", Id, ActivityId);
                TempData["ErrorMessage"] = "Unable to attach distribution—please try again";
            }
            return RedirectToPage(new { Id = ActivityId });
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostRemoveDistributionFromActivity(int ActivityId, int Id)
        {
            if (Request.ContainsPathProbeInQuery() || await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogError("Blocked suspicious payload on RemoveDistributionFromActivity POST.");
                return BadRequest("Invalid request.");
            }

            TempData["Section"] = "Distributors";
            try
            {
                await _api.DeleteAsync($"v1/Activities/{ActivityId}/Distributions/{Id}");
                TempData["SuccessMessage"] = "Distribution Type removed successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing distribution {Id}", Id);
                TempData["ErrorMessage"] = "Unable to detach distribution from this activity please try again";
            }
            return RedirectToPage(new { Id = ActivityId });
        }

    }
}
