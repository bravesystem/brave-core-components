using BRaVe_Management_Backend.DTOs;
using BRaVe_Portal.Extensions;
using BRaVe_Portal.Helpers;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using BRaVe_Portal.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BRaVe_Portal.Pages.Targeting
{
    public class TargetingModel : PageModel
    {
        private readonly ILogger<TargetingModel> _logger;
        private readonly IRestApiService _api;
        private readonly IAppCache _cache;

        public TargetingModel(
            ILogger<TargetingModel> logger, IAppCache cache,
            IRestApiService api)
        {
            _logger = logger;
            _cache = cache;
            _api = api;
        }

        [BindProperty(SupportsGet = true)]
        public List<TargetingRuleDto> Rules { get; set; } = new();

        [BindProperty]
        public TargetingRuleDto TargetingRule { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        public List<ProgramDetails> Programs { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? SelectedProgramId { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);


        public Dictionary<int, int> CriteriaCounts { get; set; } = new(); // TargetingId => number of criteria

        public async Task<IActionResult> OnGetAsync()
        {

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid query parameters for Targeting GET request.");
                TempData["ErrorMessage"] = "Invalid request.";
                ModelState.Clear();
                CurrentPage = 1;
            }

            if (StringHelper.IsPotentialPathProbe(SearchTerm))
            {
                _logger.LogWarning("Blocked suspicious search term pattern on Targeting page.");
                TempData["ErrorMessage"] = "Invalid search term.";
                SearchTerm = null;
                CurrentPage = 1;
            }

            try
            {
                _logger.LogInformation("Fetching targeting rules...");

                var rawRules = await _api.GetAsync<List<TargetingRuleDto>>("v1/TargetingRules") ?? new();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new RuleItemConverter() }
                };

                var allRules = rawRules.Select(rule =>
                {
                    if (!string.IsNullOrWhiteSpace(rule.RuleJson))
                    {
                        try
                        {
                            var wrappedJson = $"{{ \"combinator\": \"AND\", \"rules\": {rule.RuleJson} }}";
                            rule.Criteria = JsonSerializer.Deserialize<RuleGroupDto>(wrappedJson, options);
                        }
                        catch
                        {
                            rule.Criteria = new RuleGroupDto { Rules = new List<object>() };
                        }
                    }
                    else
                    {
                        rule.Criteria = new RuleGroupDto { Rules = new List<object>() };
                    }
                    return rule;
                }).ToList();

                // Apply search filter (by name/description/code)
                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    var term = SearchTerm.Trim();
                    allRules = allRules
                        .Where(r =>
                            (!string.IsNullOrWhiteSpace(r.RuleName) && r.RuleName.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrWhiteSpace(r.Description) && r.Description.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                            (!string.IsNullOrWhiteSpace(r.Code) && r.Code.Contains(term, StringComparison.OrdinalIgnoreCase)))
                        .ToList();
                }

                // Fetch all criteria counts
                try
                {
                    var allCriterias = await _api.GetAsync<List<TargetingCriteriaDto>>("v1/TargetingCriterias") ?? new List<TargetingCriteriaDto>();
                    CriteriaCounts = allCriterias.GroupBy(c => c.TargetingId).ToDictionary(g => g.Key, g => g.Count());
                }
                catch
                {
                    CriteriaCounts = new Dictionary<int, int>();
                }

                TotalRecords = allRules.Count;

                // Clamp CurrentPage
                if (CurrentPage < 1) CurrentPage = 1;
                var totalPages = TotalPages;
                if (totalPages == 0) totalPages = 1;
                if (CurrentPage > totalPages) CurrentPage = totalPages;

                // Apply paging
                Rules = allRules
                    .Skip((CurrentPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();

                await LoadPrograms();
                

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading targeting rules");
                TempData["ErrorMessage"] = "Unable to load targeting rules.";
            }

            return Page();

        }
        public async Task LoadPrograms()
        {
            Programs = await _api.GetAsync<List<ProgramDetails>>("v1/Programs") ?? new List<ProgramDetails>();
            _logger.LogInformation("Programs loaded from API and cached. Count: {Count}", Programs.Count);
            ViewData["Programs"] = Programs;
        }

        private string ExtractDuplicateValue(string message)
        {
            var start = message.IndexOf("(");
            var end = message.IndexOf(")");

            if (start >= 0 && end > start)
                return message.Substring(start + 1, end - start - 1);

            return "duplicate value";
        }


        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostSaveTargetingRuleAsync()
        {
            TargetingRule.TenantId = User.Tenant();
            TargetingRule.CreatedBy = User.Identifier();

            // Server-side validation: when weight bound, total weight must be provided and > 0
            if (TargetingRule.WeightBound)
            {
                if (!TargetingRule.TotalWeight.HasValue || TargetingRule.TotalWeight.Value <= 0)
                {
                    ModelState.AddModelError(nameof(TargetingRule.TotalWeight),
                        "Total Weight is required and must be greater than 0 when Weight Bound is checked.");

                    // Reload list data so page can re-render with validation message
                    return await OnGetAsync();
                    //await LoadPrograms();
                    //return Page();
                }
            }

            try
            {
                if (TargetingRule.Id > 0)
                {
                    // Update existing targeting rule
                    await _api.PutJsonAsync<TargetingRuleDto, TargetingRuleDto>(
                        $"v1/TargetingRules/{TargetingRule.Id}",
                        TargetingRule);
                }
                else
                {
                    // Create new targeting rule
                    TargetingRule.ProgramId = SelectedProgramId;
                    var newId = await _api.PostJsonAsync<TargetingRuleDto, int>(
                         "v1/TargetingRules",
                         TargetingRule);

                    TargetingRule.Id = newId;

                }
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error saving targeting rule");
                var message = ex.Message;

                if (message != null && message.Contains("UQ_tbl_TargetingRules_CODE"))
                {
                    var cleanMessage = ExtractDuplicateValue(message);

                    //throw new Exception($"A targeting rule with code '{cleanMessage}' already exists.");
                    TempData["ErrorMessage"] = "Failed: Targeting Rule with this code already exist.";
                    //TempData["InsertStatus"] = "error";
                    //ModelState.AddModelError(string.Empty, "Targeting Rule with this code already exist.");
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to save the targeting rule. An unknown error occurred.";
                    //ModelState.AddModelError(string.Empty, "Failed to save targeting rule.");
                }
                    
                //await LoadPrograms();
                //return await OnGetAsync();

            }

            // Redirect back to page so modal closes
            return RedirectToPage();
        }
    }
}

