using BRaVe_Portal.Extensions;
using BRaVe_Portal.Helpers;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using BRaVe_Portal.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BRaVe_Portal.Pages.Distributions
{
    public class DistributionsTypeModel : PageModel
    {
        private readonly ILogger<DistributionsTypeModel> _logger;
        private readonly IAppCache _cache;
        private readonly IRestApiService _api;

        public DistributionsTypeModel(
            ILogger<DistributionsTypeModel> logger,
            IAppCache cache,
            IRestApiService api)
        {
            _logger = logger;
            _cache = cache;
            _api = api;
        }

        [BindProperty(SupportsGet = true)]
        public List<DistributionTypes> Distributions { get; private set; } = new();

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        public int TotalRecords { get; set; }

        public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        public List<DistributionTypes> FilteredDistributions { get; set; } = new();

        public Dictionary<int, int> ItemCounts { get; set; } = new(); // TargetingId => number of criteria


        public async Task<IActionResult> OnGetAsync()
        {

            if (Request.ContainsPathProbeInQuery() )
            {
                _logger.LogError("Blocked suspicious query pattern on DistributionType page.");
                return BadRequest("Invalid request.");
            }

            try
            {

                // Fetch all distributions
                Distributions = await _api.GetAsync<List<DistributionTypes>>("v1/DistributionsType")
                    ?? new();

                // Apply search filter if needed
                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    Distributions = Distributions
                        .Where(u =>
                            (!string.IsNullOrEmpty(u.Title) &&
                             u.Title.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase))
                            ||
                            (!string.IsNullOrEmpty(u.ExternalId) &&
                             u.ExternalId.Contains(SearchTerm, StringComparison.OrdinalIgnoreCase))
                        )
                        .ToList();
                }

                // Sort by last activity (UpdatedOn or CreatedOn), then by Id as tie-breaker
                Distributions = Distributions
                    .OrderByDescending(x => x.UpdatedOn ?? x.CreatedOn)
                    .ThenByDescending(x => x.Id)
                    .ToList();

                // Fetch item counts per distribution

                var allItems = await _api.GetAsync<List<DistributionComposition>>("v1/DistributionsComposition") ?? new();
                ItemCounts = allItems
                    .GroupBy(x => x.DistributionId)
                    .ToDictionary(x => x.Key, x => x.Count());

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occured while fetching distribution type list.");
                Distributions = new List<DistributionTypes>();
                ItemCounts = new();
            }

            // Pagination setup
            TotalRecords = Distributions.Count;

            if (CurrentPage < 1)
                CurrentPage = 1;

            if (CurrentPage > TotalPages)
                CurrentPage = TotalPages == 0 ? 1 : TotalPages;

            // Apply pagination
            FilteredDistributions = Distributions
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            return Page();
        }

        [BindProperty]
        public DistributionTypes Distribution { get; set; } = new();

        [BindProperty]
        public DistributionTypes UpdatedDistribution { get; set; } = new();

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAddDistributionAsync()
        {

            if (Request.ContainsPathProbeInQuery() ||
                await Request.ContainsPathProbeInFormAsync() )
            {
                _logger.LogError("Blocked suspicious query payload on AddDistribution POST.");
                return BadRequest("Invalid request.");
            }


            if (!ModelState.IsValid)
                return Page();

            try
            {
                Distribution.TenantId = User.Tenant();
                Distribution.CreatedByUserId = User.Identifier();
                Distribution.IsActive = true;

                await _api.PostJsonAsync<DistributionTypes, object>(
                    "v1/DistributionsType",
                    Distribution);

                TempData["SuccessMessage"] = "Distribution created successfully";

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding distribution type");

                TempData["ErrorMessage"] = "Error occured while adding distribution type. Please try again.";
                
            }

            return RedirectToPage();
        }


        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostEditDistributionAsync()
        {

            if (Request.ContainsPathProbeInQuery() ||
                await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogError("Blocked suspicious query payload on EditDistribution POST.");
                return BadRequest("Invalid request.");
            }


            if (!ModelState.IsValid)
                return Page();

            try
            {
                UpdatedDistribution.TenantId = User.Tenant();
                UpdatedDistribution.UpdatedByUserId = User.Identifier();

                await _api.PutJsonAsync<DistributionTypes, object>(
                    $"v1/DistributionsType/{UpdatedDistribution.Id}",
                    UpdatedDistribution);

                TempData["SuccessMessage"] = "Distribution updated successfully";
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating distribution type Id {Id}", UpdatedDistribution?.Id);

                TempData["ErrorMessage"] = "Error occured while updating distribution type. Please try again.";

            }

            return RedirectToPage();
        }
    }
}