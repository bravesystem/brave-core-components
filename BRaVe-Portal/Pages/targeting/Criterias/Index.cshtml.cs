using BRaVe_Portal.Extensions;
using BRaVe_Portal.Helpers;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using BRaVe_Portal.Models.DTOs;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BRaVe_Portal.Pages.Targeting.Criterias
{
    public class IndexModel : PageModel
    {
        private readonly TargetingValidationService _validationService;
        private readonly ILogger<IndexModel> _logger;

        private readonly IAppCache _cache;

        private readonly IRestApiService _api;

        public IndexModel(ILogger<IndexModel> logger, IAppCache cache, IRestApiService api, TargetingValidationService validationService    )
        {
            _logger = logger;
            _cache = cache;
            _api = api;
            _validationService = validationService;
        }
        [BindProperty(SupportsGet = true)]
        public int ID { get; set; }

        [BindProperty(SupportsGet = true)]
        public int ProgramId { get; set; }

        [BindProperty(SupportsGet = true)]
        public decimal TotalWeight { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool WeightBound { get; set; }

        [BindProperty(SupportsGet = true)] public decimal Weight { get; set; }
        public string? Description { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? RuleName { get; set; }

        public int CurrentPage { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        public int TotalRecords { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        public decimal TotalCriteriaWeight { get; set; }

        // make sure to remove this
        [BindProperty(SupportsGet = true)]
        public List<TargetingCriteriaDto> TargetingCriteria { get; private set; } = new();

        [TempData]
        public string? ValidationMessage { get; set; }

        public async Task OnGetAsync()
        {
            _logger.LogInformation("Loading Targeting Criterias for Rule ID {Id}", ID);

            if (ID <= 0)
            {
                _logger.LogWarning("No ID supplied to Targeting/Criterias");
                TargetingCriteria = new List<TargetingCriteriaDto>();
                TotalRecords = 0;
                return;
            }

            try
            {
                // Call API endpoint "by-rule/{ruleId}"
                TargetingCriteria = await _api.GetAsync<List<TargetingCriteriaDto>>(
                    $"v1/TargetingCriterias/by-rule/{ID}") ?? new List<TargetingCriteriaDto>();

                TotalRecords = TargetingCriteria.Count;

                TotalCriteriaWeight = TargetingCriteria.Sum(x => x.Weight ?? 0);

                // Apply paging
                TargetingCriteria = TargetingCriteria
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();

                _logger.LogInformation("Loaded {Count} criterias for Rule ID {Id}", TargetingCriteria.Count, ID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load Targeting Criterias for Rule ID {Id}", ID);
                TargetingCriteria = new List<TargetingCriteriaDto>();
                TotalRecords = 0;
            }
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostValidateAsync()
        {
            try
            {
                var result = await _validationService.ValidateRuleAsync(ID, TotalWeight, WeightBound);

                await _validationService.UpdateRuleValidityAsync(ID, result.isValid);

                if (!result.hasCriteria)
                {
                    TempData["CriteriaValidationMessage"] =
                        "You have to define at least one criteria in order to validate.";
                    TempData["CriteriaValidationStatus"] = "warning";
                }
                else if (WeightBound && Math.Abs(result.totalWeight - TotalWeight) > 0.0001m)
                {
                    TempData["CriteriaValidationMessage"] =
                        $"Cannot validate because total criteria weight ({result.totalWeight}) " +
                        $"does not match total target weight ({TotalWeight}). " +
                        $"Difference: {TotalWeight - result.totalWeight}";
                    TempData["CriteriaValidationStatus"] = "error";
                }
                else
                {
                    TempData["CriteriaValidationMessage"] =
                        "All weights match. Targeting validated.";
                    TempData["CriteriaValidationStatus"] = "success";
                }

                return RedirectToPage(new
                {
                    ID,
                    ProgramId,
                    TotalWeight,
                    TotalCriteriaWeight = result.totalWeight,
                    WeightBound,
                    RuleName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate targeting rule {RuleId}", ID);

                TempData["CriteriaValidationMessage"] = "Validation failed due to a system error.";
                TempData["CriteriaValidationStatus"] = "error";

                return RedirectToPage(new
                {
                    ID,
                    ProgramId,
                    TotalWeight,
                    WeightBound,
                    RuleName
                });
            }
        }
        public List<TargetingCriteriaDto> FilteredUsers { get; set; } = new();

        public TargetingCriteriaDto UpdatedTargetingCriteria { get; set; } = new();

        
        public async Task<IActionResult> OnPostAddTargetingCriteriaAsync()
        {

            return RedirectToPage(new
            {
                ID,
                TotalWeight,
                TotalCriteriaWeight,
                WeightBound,
                RuleName
            });

        }

        public async Task<IActionResult> OnPostDeleteCriteriaAsync(int CriteriaId)
        {
            try
            {
                await _api.DeleteAsync($"v1/TargetingCriterias/{CriteriaId}");

                //var result = await _validationService.ValidateRuleAsync(ID, TotalWeight, WeightBound);

                //await _validationService.UpdateRuleValidityAsync(ID, result.isValid);

                TempData["SuccessMessage"] = "Indicator deactivated.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete TargetingCriterias {Id}", CriteriaId);
                TempData["ErrorMessage"] = "Failed to deactivate TargetingCriterias.";
            }

            return RedirectToPage(new
            {
                ID,
                ProgramId,
                TotalWeight,
                TotalCriteriaWeight = TotalCriteriaWeight,
                WeightBound,
                RuleName
            });
        }
        public async Task<IActionResult> OnPostEditTargetingCriteriaAsync()
        {

            return RedirectToPage();

        }
    }
}
