using BRaVe_Management_Backend.DTOs;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models.DTOs;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Json;

namespace BRaVe_Portal.Pages.Targeting
{
    public class IndicatorsModel : PageModel
    {
        private readonly ILogger<IndicatorsModel> _logger;
        private readonly IRestApiService _api;

        public IndicatorsModel(ILogger<IndicatorsModel> logger, IRestApiService api)
        {
            _logger = logger;
            _api = api;
        }

        // =========================
        // INDICATORS
        // =========================
        public List<IndicatorDto> Indicators { get; set; } = new();

        [BindProperty]
        public IndicatorDto NewIndicator { get; set; } = new();

        [BindProperty]
        public int IndicatorId { get; set; }

        // =========================
        // PREVIEW PAYLOADS
        // =========================
        [BindProperty]
        public string DatasetJson { get; set; } = string.Empty;

        // =========================
        // PREVIEW RESULTS
        // =========================
        public List<IndicatorPreviewDto> IndicatorPreviewResults { get; set; } = new();
        public List<TargetingPreviewResultDto> TargetPreviewResults { get; set; } = new();

        // =========================
        // GET
        // =========================
        public async Task OnGetAsync()
        {
            try
            {
                Indicators = await _api.GetAsync<List<IndicatorDto>>("v1/Indicator") ?? new List<IndicatorDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching indicators");
                TempData["ErrorMessage"] = "Unable to load indicators at this time.";
            }
        }

  

        // =========================
        // INDICATOR BUILDER
        // =========================
        public IActionResult OnPostAddColumn()
        {
            NewIndicator.Columns ??= new List<IndicatorColumnDto>();
            NewIndicator.Columns.Add(new IndicatorColumnDto());
            return Page();
        }


        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostCreateIndicatorAsync()
        {
            if (string.IsNullOrWhiteSpace(NewIndicator.Name)
                || NewIndicator.Columns is null
                || !NewIndicator.Columns.Any())
            {
                TempData["ErrorMessage"] = "Indicator name and columns are required.";
                return Page();
            }

            try
            {
                // Post typed object to API
                await _api.PostJsonAsync<object, IndicatorDto>("v1/Indicator", NewIndicator);

                TempData["SuccessMessage"] = "Indicator created successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create indicator");
                TempData["ErrorMessage"] = "An error occurred while creating the indicator.";
            }

            return RedirectToPage();
        }


        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDeleteIndicatorAsync()
        {
            try
            {
                await _api.DeleteAsync($"v1/Indicator/{IndicatorId}");
                TempData["SuccessMessage"] = "Indicator deactivated.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete indicator {Id}", IndicatorId);
                TempData["ErrorMessage"] = "Failed to deactivate indicator.";
            }

            return RedirectToPage();
        }

        // =========================
        // INDICATOR PREVIEW
        // =========================

      
        public async Task<IActionResult> OnPostPreviewIndicatorAsync()
        {
            if (string.IsNullOrWhiteSpace(DatasetJson))
            {
                TempData["ErrorMessage"] = "Generate indicator JSON first.";
                return Page();
            }

            try
            {
                // Deserialize into request DTO
                var requestPayload = JsonSerializer.Deserialize<IndicatorPreviewRequestDto>(DatasetJson);
                if (requestPayload == null)
                {
                    TempData["ErrorMessage"] = "Invalid JSON format!";
                    return Page();
                }

                // Call API with typed request
                var preview = await _api.PostJsonAsync<
                    IndicatorPreviewRequestDto,
                    IndicatorPreviewResponseDto>(
                      "v1/Indicator/preview",
                      requestPayload
                     );

                IndicatorPreviewResults = preview?.Results ?? new List<IndicatorPreviewDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating indicator preview");
                TempData["ErrorMessage"] = "Failed to generate indicator preview.";
            }

            return Page();
        }

        // =========================
        // TARGETING PREVIEW
        // =========================
        public async Task<IActionResult> OnPostPreviewTargetingAsync()
        {
            try
            {
                var run = new TargetingRunDto
                {
                    //RunId = 1,
                    //RuleId = 1,
                    //TargetingLevel = IndicatorLevel.Household,
                    //RunBy = "Portal"
                };

                var preview = await _api.PostJsonAsync<
                 TargetingRunDto,
                 TargetingPreviewResponseDto>(
                 "v1/Targeting/preview",
                  run
                );


                TargetPreviewResults = preview?.Results ?? new List<TargetingPreviewResultDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Targeting preview failed");
                TempData["ErrorMessage"] = "Target preview failed.";
            }

            return Page();
        }

    }
}
