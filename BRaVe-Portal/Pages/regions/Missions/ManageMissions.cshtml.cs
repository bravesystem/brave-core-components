using BRaVe_Portal.Helpers;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using BRaVe_Portal.Models.DTOs;
using BRaVe_Portal.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;

namespace BRaVe_Portal.Pages.regions.Missions
{
    public class ManageMissionsModel : PageModel
    {
        private readonly ILogger<ManageMissionsModel> _logger;
        //private readonly IAppCache _cache;
        private readonly IRestApiService _api;
        private readonly ILookupService _lookupService;

        public ManageMissionsModel(ILogger<ManageMissionsModel> logger, IAppCache cache, IRestApiService api, ILookupService lookupService)
        {
            _logger = logger;
            //_cache = cache;
            _api = api;
            _lookupService = lookupService;
        }

        [BindProperty(SupportsGet = true)]
        public List<LookupItemDto> FocalPoints { get; set; }

        public Country Country { get; set; }

        public List<MissionDto> Missions { get; set; } = new();

        [BindProperty]
        public MissionDto NewMission { get; set; } = new();

        [BindProperty]
        public MissionDto EditMission { get; set; } = new();

        [BindProperty]
        public int EditMissionId { get; set; }

        public async Task<IActionResult> OnGetAsync(string countryISO2)
        {
            _logger.LogInformation("OnGetAsync called for countryISO2: {CountryISO2}", countryISO2);

            NewMission = new MissionDto { CountryIso2 = countryISO2 };
            EditMission = new MissionDto { CountryIso2 = countryISO2 };

            await GetCountryAsync(countryISO2);
            await LoadMissionsAsync();

            string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            CoreLookups coreLookups = await _lookupService.GetCoreLookups(languageCode);
            FocalPoints = coreLookups.FocalPoints;

            return Page();
        }

        private async Task GetCountryAsync(string countryISO2)
        {
            _logger.LogInformation("Loading country info for {CountryISO2}", countryISO2);

            /*if (await _cache.ExistsAsync($"{StaticKeyNames.COUNTRY}{countryISO2}"))
            {
                Country = await _cache.GetAsync<Country>($"{StaticKeyNames.COUNTRY}{countryISO2}");
                _logger.LogInformation("Country loaded from cache: {CountryName}", Country.Name);
            }
            else
            {*/
                try
                {
                    Country = await _api.GetAsync<Country>($"v1/regions/Countries/{countryISO2}");
                    //await _cache.SetAsync($"{StaticKeyNames.COUNTRY}{countryISO2}", Country, TimeSpan.FromHours(1));
                    _logger.LogInformation("Country fetched from API and cached: {CountryName}", Country.Name);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error fetching country for {CountryISO2}", countryISO2);
                    throw e;
                }
            //}
        }

        private async Task LoadMissionsAsync()
        {
            _logger.LogInformation("Loading missions for country {CountryISO2}", Country.CountryIso2);

            /*if (await _cache.ExistsAsync($"{StaticKeyNames.ALL_MISSIONS_IN_COUNTRY}{Country.CountryIso2}"))
            {
                Missions = await _cache.GetAsync<List<MissionDto>>($"{StaticKeyNames.ALL_MISSIONS_IN_COUNTRY}{Country.CountryIso2}");
                _logger.LogInformation("Loaded {Count} missions from cache", Missions.Count);
            }
            else
            {*/
                try
                {
                    Missions = await _api.GetAsync<List<MissionDto>>($"/api/v1/Missions/bycountry/{Country.CountryIso2}") ?? new List<MissionDto>();
                    _logger.LogInformation("Fetched {Count} missions from API", Missions.Count);
                }
                catch (Exception ex)
                {
                    Missions = new List<MissionDto>();
                    _logger.LogError(ex, "Error fetching missions from API for {CountryISO2}", Country.CountryIso2);
                }

                //await _cache.SetAsync($"{StaticKeyNames.ALL_MISSIONS_IN_COUNTRY}{Country.CountryIso2}", Missions, TimeSpan.FromHours(1));
                _logger.LogInformation("Missions cached for country {CountryISO2}", Country.CountryIso2);
            //}
        }

        public async Task<IActionResult> OnPostAddMissionAsync()
        {
            _logger.LogInformation("Adding new mission for country {CountryISO2}", NewMission.CountryIso2);

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Could not save Mission. Please try again.");
                _logger.LogWarning("ModelState invalid when adding mission for country {CountryISO2}", NewMission.CountryIso2);
                return RedirectToPage(null, new { NewMission.CountryIso2 });
            }

            try
            {
                await _api.PostJsonAsync<MissionDto, object>("v1/Missions", NewMission);
                //await _cache.RemoveAsync($"{StaticKeyNames.ALL_MISSIONS_IN_COUNTRY}{NewMission.CountryIso2}");
                _logger.LogInformation("Mission added and cache cleared for country {CountryISO2}", NewMission.CountryIso2);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Could not save Mission. Please try again.");
                _logger.LogError(ex, "Error adding mission for country {CountryISO2}", NewMission.CountryIso2);
            }

            return RedirectToPage(null, new { NewMission.CountryIso2 });
        }

        public async Task<IActionResult> OnPostEditMissionAsync()
        {
            _logger.LogInformation("Editing mission {MissionId} for country {CountryISO2}", EditMission.MissionId, EditMission.CountryIso2);

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Could not update Mission. Please try again.");
                _logger.LogWarning("ModelState invalid when editing mission {MissionId}", EditMission.MissionId);
                return RedirectToPage(null, new { EditMission.CountryIso2 });
            }

            try
            {
                await _api.PutJsonAsync<MissionDto, object>($"/api/v1/Missions/{EditMission.MissionId}", EditMission);
                //await _cache.RemoveAsync($"{StaticKeyNames.ALL_MISSIONS_IN_COUNTRY}{EditMission.CountryIso2}");
                //await _cache.RemoveAsync($"{StaticKeyNames.MISSION}{EditMission.MissionId}");
                _logger.LogInformation("Mission {MissionId} updated and caches cleared", EditMission.MissionId);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Could not save Mission. Please try again.");
                _logger.LogError(ex, "Error editing mission {MissionId} for country {CountryISO2}", EditMission.MissionId, EditMission.CountryIso2);
            }

            return RedirectToPage(null, new { EditMission.CountryIso2 });
        }
    }
}
