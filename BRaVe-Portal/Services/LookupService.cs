using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using System.Net;
using System.Text.Json;

namespace BRaVe_Portal.Services
{
    public class LookupService : ILookupService
    {
        public CoreLookups CoreLookups { get; }

        public List<AdministrativeLevel> AdministrativeLevels { get; set;  }
        public Dictionary<int, List<Location>> LocationsByLevel { get; set; }

        private readonly ILogger<LookupService> _logger;

        private readonly IRestApiService _api;

        public LookupService(ILogger<LookupService> logger, IRestApiService api)
        {
            _logger = logger;
            _api = api;
        }

        public async Task<CoreLookups> GetCoreLookups(string lang)
        {

            if (string.IsNullOrWhiteSpace(lang))
                lang = "en";


            if (CoreLookups != null)
                return CoreLookups;

            // Ensure no leading slash
            string relativePath = $"/api/v1/Lookups/core/{Uri.EscapeDataString(lang.TrimStart('/'))}";

            _logger.LogInformation("Requesting core lookups from URL: {Url}", relativePath);

            try
            {
                // Get raw response for debugging
                var (status, body) = await _api.GetRawAsync(relativePath);
                _logger.LogInformation("HTTP {Status} Response Body: {Body}", status, body);

                if (status == HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Core lookups for language {Lang} not found", lang);
                    throw new HttpRequestException($"404 Not Found: Core lookups for '{lang}' not found.");
                }

                // Check for non-success status codes (anything not 2xx)
                if ((int)status < 200 || (int)status >= 300)
                {
                    throw new HttpRequestException($"Unexpected status code {status}: {body}");
                }

                // Deserialize the response into CoreLookups
                var coreLookups = await _api.GetAsync<CoreLookups>(relativePath);
                if (coreLookups == null)
                    throw new InvalidOperationException("Core lookups response was null or invalid.");

                return coreLookups;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch core lookups for language {Lang}", lang);
                throw;
            }


        }

        public async Task<List<AdministrativeLevel>> GetAdminLevels()
        {

            if (AdministrativeLevels != null && AdministrativeLevels.Count()>0)
                return AdministrativeLevels;

            AdministrativeLevels = await _api
                .GetAsync<List<AdministrativeLevel>>("v1/AdministrativeLevels")
                ?? new List<AdministrativeLevel>();

            return AdministrativeLevels;
        }

        public async Task<Dictionary<int, List<Location>>> GetAllLocationsByLevel()
        {
            if (LocationsByLevel != null && LocationsByLevel.Count()>0)
                return LocationsByLevel;

            // Fetch list of locations, fallback to empty list if null
            var locations = await _api.GetAsync<List<Location>>("v1/Locations")
                            ?? new List<Location>();

            // Group by LevelId and convert to dictionary
            LocationsByLevel = locations
                .GroupBy(l => l.LevelId)
                .ToDictionary(g => g.Key, g => g.ToList());

            return LocationsByLevel;

        }
    }
}
