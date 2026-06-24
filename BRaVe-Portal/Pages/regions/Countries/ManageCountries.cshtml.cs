using BRaVe_Portal.Helpers;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using BRaVe_Portal.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BRaVe_Portal.Pages.regions
{
    public class ManageCountriesModel : PageModel
    {
        private readonly ILogger<ManageCountriesModel> _logger;
        //private readonly IAppCache _cache;
        private readonly IRestApiService _api;

        public ManageCountriesModel(ILogger<ManageCountriesModel> logger/*, IAppCache cache*/, IRestApiService api)
        {
            _logger = logger;
            //_cache = cache;
            _api = api;
        }

        public Region Region { get; set; }

        // List of countries in the selected region
        public List<Country> Countries { get; set; } = new();

        [BindProperty]
        public CountryDto NewCountry { get; set; }

        [BindProperty]
        public CountryDto EditCountry { get; set; }

        // Load countries for the selected region
        public async Task<IActionResult> OnGetAsync(int RegionId)
        {
            _logger.LogInformation("OnGetAsync called for RegionId: {RegionId}", RegionId);

            NewCountry = new CountryDto { RegionId = RegionId };
            EditCountry = new CountryDto { RegionId = RegionId };

            await GetRegionAsync(RegionId);
            await LoadCountriesAsync();

            return Page();
        }

        private async Task LoadCountriesAsync()
        {
            _logger.LogInformation("Loading countries for RegionId: {RegionId}", Region.RegionId);

            /*if (await _cache.ExistsAsync($"{StaticKeyNames.ALL_COUNTRIES_IN_REGION}{Region.RegionId}"))
            {
                Countries = await _cache.GetAsync<List<Country>>($"{StaticKeyNames.ALL_COUNTRIES_IN_REGION}{Region.RegionId}");
                _logger.LogInformation("Loaded {Count} countries from cache", Countries.Count);
            }
            else
            {*/
                try
                {
                    Countries = await _api.GetAsync<List<Country>>(
                        $"v1/regions/countries/byregion/{Region.RegionId}"
                    ) ?? new List<Country>();

                    _logger.LogInformation("Fetched {Count} countries from API", Countries.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching countries from API for RegionId {RegionId}", Region.RegionId);
                    Countries = new List<Country>();
                }

                //await _cache.SetAsync($"{StaticKeyNames.ALL_COUNTRIES_IN_REGION}{Region.RegionId}", Countries, TimeSpan.FromHours(1));
                _logger.LogInformation("Countries cached for RegionId {RegionId}", Region.RegionId);
            //}
        }

        private async Task GetRegionAsync(int RegionId)
        {
            _logger.LogInformation("Loading region info for RegionId: {RegionId}", RegionId);

            /*if (await _cache.ExistsAsync($"{StaticKeyNames.REGION}{RegionId}"))
            {
                Region = await _cache.GetAsync<Region>($"{StaticKeyNames.REGION}{RegionId}");
                _logger.LogInformation("Region loaded from cache: {RegionName}", Region.Name);
            }
            else
            {*/
                try
                {
                    Region = await _api.GetAsync<Region>($"v1/regions/{RegionId}");
                    //await _cache.SetAsync($"{StaticKeyNames.REGION}{RegionId}", Region, TimeSpan.FromHours(1));
                    _logger.LogInformation("Region fetched from API and cached: {RegionName}", Region.Name);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error fetching region from API for RegionId {RegionId}", RegionId);
                    throw e;
                }
            //}
        }

        // Add a new country
        public async Task<IActionResult> OnPostAddCountryAsync()
        {
            _logger.LogInformation("Adding new country in RegionId: {RegionId}", NewCountry.RegionId);

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Could not save country. Please try again.");
                _logger.LogWarning("ModelState invalid when adding country for RegionId: {RegionId}", NewCountry.RegionId);
                return RedirectToPage(null, new { NewCountry.RegionId });
            }

            try
            {
                await _api.PostJsonAsync<CountryDto, object>("v1/regions/countries", NewCountry);
                //await _cache.RemoveAsync($"{StaticKeyNames.ALL_COUNTRIES_IN_REGION}{NewCountry.RegionId}");
                _logger.LogInformation("Country added and cache cleared for RegionId: {RegionId}", NewCountry.RegionId);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Could not save country: {ex.Message}");
                _logger.LogError(ex, "Error adding country for RegionId: {RegionId}", NewCountry.RegionId);
            }

            return RedirectToPage(null, new { NewCountry.RegionId });
        }

        // Edit a country
        public async Task<IActionResult> OnPostEditCountryAsync()
        {
            _logger.LogInformation("Editing country {CountryIso2} in RegionId: {RegionId}", EditCountry.CountryIso2, EditCountry.RegionId);

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Could not save country. Please try again.");
                _logger.LogWarning("ModelState invalid when editing country {CountryIso2}", EditCountry.CountryIso2);
                return RedirectToPage(null, new { EditCountry.RegionId });
            }

            try
            {
                await _api.PutJsonAsync<CountryDto, object>($"v1/regions/countries/{EditCountry.CountryIso2}", EditCountry);
                //await _cache.RemoveAsync($"{StaticKeyNames.ALL_COUNTRIES_IN_REGION}{EditCountry.RegionId}");
                //await _cache.RemoveAsync($"{StaticKeyNames.COUNTRY}{EditCountry.CountryIso2}");
                _logger.LogInformation("Country {CountryIso2} updated and caches cleared", EditCountry.CountryIso2);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Could not save country. Please try again.");
                _logger.LogError(ex, "Error editing country {CountryIso2}", EditCountry.CountryIso2);
            }

            return RedirectToPage(null, new { EditCountry.RegionId });
        }
    }
}
