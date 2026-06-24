using BRaVe_Management_Backend.DTOs;
using BRaVe_Portal.Extensions;
using BRaVe_Portal.Helpers;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using BRaVe_Portal.Models.DTOs;
using BRaVe_Portal.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.RegularExpressions;

namespace BRaVe_Portal.Pages.DataPoints
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IAppCache _cache;
        private readonly IRestApiService _api;
        private readonly ILookupService _lookupService;

        public IndexModel(
            ILogger<IndexModel> logger,
            IAppCache cache,
            IRestApiService api,
            ILookupService lookupService)
        {
            _logger = logger;
            _cache = cache;
            _api = api;
            _lookupService = lookupService;
        }

        // -------------------------
        // PROPERTIES
        // -------------------------
        [BindProperty(SupportsGet = true)]
        public int ProgramId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? DataPointId { get; set; }

        public Datapoint? DataPoint { get; set; }

        [BindProperty]
        public DataPointDto ExistingDatapoint { get; set; }

        [BindProperty]
        public DataPointDto NewDatapoint { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        public string? DisplaySearchTerm { get; private set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        public int TotalPages { get; set; }

        [BindProperty]
        public string? RestrictionJson { get; set; }


        [BindProperty(SupportsGet = true)]
        public List<LookupTableNameDto> LookupNames { get; private set; } = new();
        [BindProperty]
        public LookupTableValueDto NewLookupValue { get; set; } = new();
        public List<DataPointDto> DataPoints { get; set; } = new();
        public List<DataPointDto> MissionDataPoints { get; set; } = new();


        // ---------------------------------------------------------
        // GET REQUEST
        // ---------------------------------------------------------
        public async Task<IActionResult> OnGetAsync(int programId, int? dataPointId)
        {
            //if (Request.ContainsUnexpectedQueryParameters())
            //{
            //    _logger.LogWarning("Blocked unexpected query parameters on Datapoints GET.");
            //    return BadRequest("Invalid request.");
            //}


            if ( Request.ContainsPathProbeInQuery() || 
                await Request.ContainsPathProbeInFormAsync() ||
                StringHelper.IsPotentialPathProbe(SearchTerm) )
            {
                _logger.LogWarning("Blocked suspicious query payload on DataPoints GET.");
                return BadRequest("Invalid request.");
            }

            DisplaySearchTerm = SearchTerm;

            _logger.LogInformation( "==== GET DataPoints ====\nProgramId={ProgramId}, DataPointId={DataPointId}, Page={PageNumber}, Search='{SearchTerm}'", programId, dataPointId, PageNumber, SearchTerm);

            ProgramId = programId;
            DataPointId = dataPointId;

            string DefaultLang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            try
            {
                _logger.LogDebug("Loading core lookups for language {Lang}", DefaultLang);
                var coreLookups = await _lookupService.GetCoreLookups(DefaultLang)
                    ?? new CoreLookups();

                ViewData["CoreLookups"] = coreLookups;

                MissionDataPoints = await _api.GetAsync<List<DataPointDto>>(
                    $"v1/DataPoints/?languageCode={DefaultLang}")
                    ?? new List<DataPointDto>();

                _logger.LogInformation("Fetched {Count} datapoints before filtering", MissionDataPoints.Count);

                // ---------- SEARCH ----------
                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    _logger.LogDebug("Applying search filter: '{SearchTerm}'", SearchTerm);

                    MissionDataPoints = MissionDataPoints
                        .Where(x => !string.IsNullOrWhiteSpace(x.Text)
                            && x.Text.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    _logger.LogInformation("Remaining datapoints after search: {Count}", MissionDataPoints.Count);
                }

                // ---------- PAGINATION ----------
                int totalItems = MissionDataPoints.Count;
                TotalPages = (int)Math.Ceiling(totalItems / (double)PageSize);

                DataPoints = MissionDataPoints
                    .OrderBy(x => x.DataPointOrder)
                    .Skip((PageNumber - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();

                _logger.LogInformation(
                    "Pagination applied -> Page {PageNumber}/{TotalPages}, Items on page: {Count}",
                    PageNumber, TotalPages, DataPoints.Count);

                // ---------- LOAD SINGLE ITEM ----------
                if (DataPointId.HasValue)
                {
                    _logger.LogDebug("Loading single datapoint Id={Id}", DataPointId.Value);

                    DataPoint = await _api.GetAsync<Datapoint>($"v1/DataPoints/Id/{DataPointId.Value}");

                    if (DataPoint == null)
                        _logger.LogWarning("Datapoint Id={Id} returned NULL", DataPointId.Value);
                }

                LookupNames = await _api.GetAsync<List<LookupTableNameDto>>("v1/Lookups/lookups") ?? new();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading datapoints for Program={ProgramId}", ProgramId);
                TempData["ErrorMessage"] = $"Unable to load datapoint list  for Program={ProgramId}";
            }

            return Page();
        }

        // ---------------------------------------------------------
        // CREATE
        // ---------------------------------------------------------
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAddDatapointAsync()
        {
            _logger.LogInformation("==== POST AddDatapoint ====");

            if (Request.ContainsPathProbeInQuery() || await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogError("Blocked suspicious query payload on AddDatapoint POST.");
                return BadRequest("Invalid request.");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("AddDatapoint ModelState INVALID");
                return Page();
            }

            try
            {
                NewDatapoint.TenantId = User.Tenant();

                string DefaultLang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

                NewDatapoint.DefaultLang = DefaultLang;

                _logger.LogDebug("Creating new datapoint: {@NewDatapoint}", NewDatapoint);

                await _api.PostJsonAsync<DataPointDto, object>("v1/DataPoints", NewDatapoint);

                TempData["SuccessMessage"] = "Datapoint created successfully!";
                _logger.LogInformation("Datapoint created");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating datapoint");
                TempData["ErrorMessage"] = "Failed to create datapoint.";
                return Page();
            }

            return RedirectToPage("./Index", new { ProgramId });
        }

        // ---------------------------------------------------------
        // UPDATE
        // ---------------------------------------------------------

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostUpdateDataPointAsync()
        {
            _logger.LogInformation("==== POST UpdateDataPoint ====");

            if (Request.ContainsPathProbeInQuery() || await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogError("Blocked suspicious query payload on UpdateDatapoint POST.");
                return BadRequest("Invalid request.");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogError("UpdateDatapoint ModelState INVALID");
                return RedirectToPage(); //Page();
            }

            try
            {
                _logger.LogDebug("Updating datapoint Id={Id}: {@ExistingDatapoint}",
                    ExistingDatapoint.Id, ExistingDatapoint);

                string DefaultLang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

                ExistingDatapoint.DefaultLang = DefaultLang;

                await _api.PutJsonAsync<DataPointDto, object>(
                    $"v1/DataPoints/{ExistingDatapoint.Id}",
                    ExistingDatapoint);

                TempData["SuccessMessage"] = "Datapoint updated successfully!";
                _logger.LogInformation("Datapoint Id={Id} updated", ExistingDatapoint.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating datapoint Id={Id}", ExistingDatapoint.Id);
                TempData["ErrorMessage"] = "Failed to update datapoint.";
            }

            return RedirectToPage("./Index", new { ProgramId });
        }

        // ---------------------------------------------------------
        // DELETE
        // ---------------------------------------------------------


        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDeleteAsync(int dataPointId)
        {
            _logger.LogInformation("==== POST DeleteDatapoint Id={Id} ====", dataPointId);

            if (Request.ContainsPathProbeInQuery() || await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogError("Blocked suspicious query payload on Delete POST.");
                return BadRequest("Invalid request.");
            }

            try
            {
                await _api.DeleteAsync($"v1/DataPoints/{dataPointId}");

                TempData["SuccessMessage"] = "Datapoint deleted successfully!";
                _logger.LogInformation("Datapoint Id={Id} deleted", dataPointId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting datapoint Id={Id}", dataPointId);
                TempData["ErrorMessage"] = "Failed to delete datapoint.";
            }

            return RedirectToPage("./Index", new { ProgramId });
        }



        // ---------------------------------------------------------
        // SAVE VALIDATION
        // ---------------------------------------------------------
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostSaveValidationAsync()
        {
            if (Request.ContainsPathProbeInQuery() || await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogWarning("Blocked suspicious query payload on SaveValidation POST.");
                return BadRequest("Invalid request.");
            }

            try
            {
                _logger.LogInformation("Saving validation JSON for datapoint {Id}", DataPointId);

                var datapoint = await _api.GetAsync<DataPointDto>($"v1/DataPoints/{DataPointId}");

                if (datapoint == null)
                {
                    TempData["ErrorMessage"] = "Datapoint not found.";
                    return RedirectToPage("./Index", new { ProgramId });
                }

                datapoint.Restriction = RestrictionJson;

                await _api.PutJsonAsync<DataPointDto, object>(
                    $"v1/DataPoints/{datapoint.Id}",
                    datapoint);


                TempData["SuccessMessage"] = "Validation rules saved successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving validation JSON");
                TempData["ErrorMessage"] = "Failed to save validation rules.";
            }

            return RedirectToPage("./Index", new { ProgramId });
        }


        public async Task<JsonResult> OnGetLookupValuesAsync(int lookupId)
        {
            if (Request.ContainsPathProbeInQuery() )
            {
                _logger.LogWarning("Blocked suspicious query payload on LookupValues GET.");
                return new JsonResult(new List<LookupTableValueDto>());
            }

            try
            {
                var values = await _api.GetAsync<List<LookupTableValueDto>>(
                    $"v1/Lookups/values/by-lookup/{lookupId}");

                return new JsonResult(values ?? new List<LookupTableValueDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load lookup values {LookupId}", lookupId);
                return new JsonResult(new List<LookupTableValueDto>());
            }
        }
    }
}
