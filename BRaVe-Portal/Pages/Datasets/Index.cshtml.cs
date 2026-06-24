using BRaVe_Management_Backend.DTOs;
using BRaVe_Portal.Extensions;
using BRaVe_Portal.Helpers;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using static BRaVe_Portal.Helpers.KeyVaultSecretNames;

namespace BRaVe_Portal.Pages.Datasets
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _config;
        private readonly ILogger<IndexModel> _logger;
        private readonly IAppCache _cache;
        private readonly IRestApiService _api;
        private readonly ILookupService _lookupService;

        public IndexModel(
            ILogger<IndexModel> logger,
            IConfiguration config,
            IAppCache cache,
            IRestApiService api,
            ILookupService lookupService)
        {
            _logger = logger;
            _config = config;
            _cache = cache;
            _api = api;
            _lookupService = lookupService;
        }

        [BindProperty]
        public string DatasetJson { get; set; } = string.Empty;

        [BindProperty]
        public DatasetsDto NewDataset { get; set; } = new();

        public List<DatasetsDto> DataSets { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public List<LookupTableNameDto> LookupNames { get; private set; } = new();


        public async Task<IActionResult> OnGetAsync()
        {
            if (Request.ContainsUnexpectedQueryParameters())
            {
                _logger.LogWarning("Blocked unexpected query parameters on Datasets GET.");
                return BadRequest("Invalid request.");
            }

            if (Request.ContainsPathProbeInQuery() || await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogWarning("Blocked suspicious query payload on Datasets GET.");
                return BadRequest("Invalid request.");
            }

            try
            {
                _logger.LogInformation("Fetching existing datasets...");

                // Fetch datasets from API
                DataSets = await _api.GetAsync<List<DatasetsDto>>("v1/Datasets") ?? new List<DatasetsDto>();
                LookupNames = await _api.GetAsync<List<LookupTableNameDto>>("v1/Lookups/lookups") ?? new();

                _logger.LogInformation("Fetched {Count} datasets successfully", DataSets.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching datasets");
                TempData["ErrorMessage"] = "Unable to load datasets at this time / Check if there are datasets defined for your mission.";
            }

            return Page();
        }


        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostCreateDatasetAsync()
        {
            if (Request.ContainsPathProbeInQuery() || await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogWarning("Blocked suspicious query payload on CreateDataset POST.");
                return BadRequest("Invalid request.");
            }

            if (JsonPayloadHelper.ContainsPathProbe(DatasetJson))
            {
                _logger.LogWarning("Blocked suspicious dataset payload pattern on create.");
                return BadRequest("Invalid dataset payload.");
            }

            /*if (ContainsPathProbe(NewDataset.Title, NewDataset.Description, DatasetJson))
            {
                _logger.LogWarning("Blocked suspicious dataset payload pattern on create.");
                return BadRequest("Invalid dataset payload.");
            }*/

            NewDataset.SchemaJson = DatasetJson;

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Dataset creation failed: invalid model state");
                TempData["ErrorMessage"] = "Please correct the errors and try again.";
                return await OnGetAsync();
            }

            if (string.IsNullOrWhiteSpace(DatasetJson))
            {
                _logger.LogWarning("Dataset creation failed: DatasetJson is empty");
                TempData["ErrorMessage"] = "Dataset JSON cannot be empty.";
                return await OnGetAsync();
            }

            try
            {
                _logger.LogInformation("Creating new dataset: {Title}", NewDataset.Title);

                // Ensure SchemaJson is set
                if (string.IsNullOrWhiteSpace(NewDataset.SchemaJson))
                {
                    NewDataset.SchemaJson = DatasetJson;
                }

                // Optional: validate JSON
                using var doc = JsonDocument.Parse(NewDataset.SchemaJson);

                await _api.PostJsonAsync<DatasetsDto, object>("v1/Datasets", NewDataset);

                _logger.LogInformation("Dataset {Title} created successfully", NewDataset.Title);
                TempData["SuccessMessage"] = "Dataset created successfully";

                return RedirectToPage();
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Invalid JSON provided for dataset creation");
                TempData["ErrorMessage"] = "Invalid JSON format.";
                //return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating dataset {Title}", NewDataset.Title);
                TempData["ErrorMessage"] = "Failed to create dataset. Please try again.";
                //return Page();
            }

            return RedirectToPage();

            //return await OnGetAsync();
        }


        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostUpdateDatasetAsync()
        {

            if (Request.ContainsPathProbeInQuery() || await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogWarning("Blocked suspicious query payload on UpdateDataset POST.");
                return BadRequest("Invalid request.");
            }

            if (JsonPayloadHelper.ContainsPathProbe(DatasetJson))
            {
                _logger.LogWarning("Blocked suspicious dataset payload pattern on update.");
                return BadRequest("Invalid dataset payload.");
            }

            if (string.IsNullOrWhiteSpace(DatasetJson))
            {
                //TempData["ErrorMessage"] = "Dataset JSON was not saved, you must have canceled the saving process.";
                return RedirectToPage();
            }

            try
            {
                NewDataset.SchemaJson = DatasetJson;

                await _api.PutJsonAsync<DatasetsDto, object>(
                    $"v1/Datasets/{NewDataset.Id}", NewDataset);

                TempData["SuccessMessage"] = "Dataset updated successfully";

            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Error occured while updating dataset.");

                TempData["ErrorMessage"] = "Unable to update dataset. Please try again.";
            }

            return RedirectToPage();


        }

        [BindProperty]
        public int DatasetId { get; set; }


        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDeleteDatasetAsync()
        {
            if (Request.ContainsPathProbeInQuery() || await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogWarning("Blocked suspicious query payload on DeleteDataset POST.");
                return BadRequest("Invalid request.");
            }

            try
            {
                _logger.LogInformation("Deactivating dataset {Id}", DatasetId);

                await _api.DeleteAsync($"v1/Datasets/{DatasetId}");

                TempData["SuccessMessage"] = "Dataset deactivated successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating dataset {Id}", DatasetId);
                TempData["ErrorMessage"] = "Failed to deactivate dataset";
            }

            return RedirectToPage();
        }


    }
}
