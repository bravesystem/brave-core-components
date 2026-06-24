using BRaVe_Portal.Helpers;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using BRaVe_Portal.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BRaVe_Portal.Pages.regions
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        //private readonly IAppCache _cache;
        private readonly IRestApiService _api;

        public IndexModel(ILogger<IndexModel> logger/*, IAppCache cache*/, IRestApiService api)
        {
            _logger = logger;
            //_cache = cache;
            _api = api;
        }

        [BindProperty(SupportsGet = true)]
        public List<Region> Regions { get; private set; } = new();

        public async Task OnGetAsync()
        {
            _logger.LogInformation("OnGetAsync called to load all regions");

            /*if (await _cache.ExistsAsync(StaticKeyNames.ALL_REGIONS))
            {
                Regions = await _cache.GetAsync<List<Region>>(StaticKeyNames.ALL_REGIONS);
                _logger.LogInformation("Loaded {Count} regions from cache", Regions.Count);
            }
            else
            {*/
                try
                {
                    Regions = await _api.GetAsync<List<Region>>("v1/regions") ?? new List<Region>();
                    _logger.LogInformation("Fetched {Count} regions from API", Regions.Count);
                    //await _cache.SetAsync(StaticKeyNames.ALL_REGIONS, Regions, TimeSpan.FromHours(1));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching regions from API");
                    Regions = new List<Region>();
                }
            //}
        }

        [BindProperty]
        public RegionDto Region { get; set; } = new();

        [BindProperty]
        public Region UpdatedRegion { get; set; } = new();

        public async Task<IActionResult> OnPostAddRegionAsync()
        {
            _logger.LogInformation("OnPostAddRegionAsync called to add a new region: {RegionName}", Region.Name);
            ModelState.Remove("Region.Note");
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Could not save region. Please try again.");
                _logger.LogWarning("ModelState invalid when adding region: {RegionName}", Region.Name);
                Regions = await _api.GetAsync<List<Region>>("v1/regions") ?? new();
                return Page();
            }

            try
            {
                await _api.PostJsonAsync<RegionDto, object>("v1/regions", Region);
                //await _cache.RemoveAsync(StaticKeyNames.ALL_REGIONS);
                _logger.LogInformation("Region added and cache cleared: {RegionName}", Region.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding region: {RegionName}", Region.Name);
                ModelState.AddModelError(string.Empty, "Could not save region. Please try again.");
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditRegionAsync()
        {
            _logger.LogInformation("OnPostEditRegionAsync called to edit region: {RegionId}", UpdatedRegion.RegionId);

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Could not update region. Please try again.");
                _logger.LogWarning("ModelState invalid when editing region: {RegionId}", UpdatedRegion.RegionId);
                Regions = await _api.GetAsync<List<Region>>("v1/regions") ?? new();
                return Page();
            }

            try
            {
                RegionDto regionDto = new RegionDto()
                {
                    Name = UpdatedRegion.Name,
                    Note = UpdatedRegion.Note
                };

                await _api.PutJsonAsync<RegionDto, object>($"v1/regions/{UpdatedRegion.RegionId}", regionDto);

                //await _cache.RemoveAsync(StaticKeyNames.ALL_REGIONS);
                //await _cache.RemoveAsync($"{StaticKeyNames.REGION}{UpdatedRegion.RegionId}");
                _logger.LogInformation("Region updated and cache cleared: {RegionId}", UpdatedRegion.RegionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing region: {RegionId}", UpdatedRegion.RegionId);
                ModelState.AddModelError(string.Empty, "Could not update region. Please try again.");
            }

            return RedirectToPage();
        }
    }
}
