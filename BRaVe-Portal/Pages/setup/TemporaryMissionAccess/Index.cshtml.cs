using BRaVe_Management_Backend.DTOs;
using BRaVe_Portal.Extensions;
using BRaVe_Portal.Helpers;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using BRaVe_Portal.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Globalization;
using System.Reflection;

namespace BRaVe_Portal.Pages.Setup.TempAccess
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IRestApiService _api;
        private readonly ILookupService _lookupService;

        public IndexModel(ILogger<IndexModel> logger, IRestApiService api, ILookupService lookupService)
        {
            _logger = logger;
            _api = api;
            _lookupService = lookupService;
        }

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        public int TotalPages { get; set; }

        [BindProperty(SupportsGet = true)]
        public List<TempAccessTableDto> Items { get; private set; } = new();

        [BindProperty]
        public TempAccessTableDto NewRequest { get; set; } = new();

        public List<SelectListItem> Missions { get; set; } = new();

        string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
       

        public async Task OnGetAsync()
        {
            _logger.LogInformation("Loading temporary access items...");

            var all = await _api.GetAsync<List<TempAccessTableDto>>("v1/AccessControl/temp-access-requests") ?? new();

            //Get roles for dropdown values
            CoreLookups coreLookups = await _lookupService.GetCoreLookups(languageCode);
            ViewData["CoreLookups"] = coreLookups;

            // Fetch missions
            var missions = await _api.GetAsync<List<Mission>>("v1/missions") ?? new List<Mission>();
            Missions = missions.Select(m => new SelectListItem
            {
                Value = m.MissionId.ToString(),
                Text = m.Name
            }).ToList();

            // Search by UserId 
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var s = SearchTerm.Trim().ToLower();
                all = all.Where(x => (x.UserId ?? "").ToLower().Contains(s)).ToList();
            }

            // Order: latest request first
            all = all
                .OrderByDescending(x => x.RequestedAt ?? DateTimeOffset.MinValue)
                .ThenByDescending(x => x.AccessedAt ?? DateTimeOffset.MinValue)
                .ToList();

            // Pagination
            var totalItems = all.Count;
            TotalPages = (int)Math.Ceiling(totalItems / (double)PageSize);
            if (CurrentPage < 1) CurrentPage = 1;
            if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;

            Items = all
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();
        }


        // ADD Temporary Access Request
        public async Task<IActionResult> OnPostAddTempAccessAsync()
        {
            CoreLookups coreLookups = await _lookupService.GetCoreLookups(languageCode);
            try
            {
                // Ensure the UserId is set server-side (don’t trust the form for this)
                NewRequest.UserId = User.Identifier();

                // Only required fields for create
                var payload = new TempAccessTableDto
                {
                    UserId = NewRequest.UserId,
                    MissionId = NewRequest.MissionId,
                    RoleId = NewRequest.RoleId,
                    Reason = NewRequest.Reason
                };

                await _api.PostJsonAsync<TempAccessTableDto, object>("v1/AccessControl/create-temp-access", payload);

                TempData["SuccessMessage"] = "Temporary access requested succeessfully. Sign out then sign in again to assume temporary role.";
                return RedirectToPage();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to add temporary access.");

                // Check for 409 Conflict from backend
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    // This contains the message returned by the API
                    TempData["ErrorMessage"] = ex.Message;
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to submit request. Please try again.";
                }

                TempData["KeepRequestModalOpen"] = true;
                return RedirectToPage();
            }

        }
    }
}

