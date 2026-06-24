using BRaVe_Portal.Helpers;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using BRaVe_Portal.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BRaVe_Portal.Pages.ClaimSessions
{
    public class CardPrintingClaimsModel : PageModel
    {
        private readonly ILogger<CardPrintingClaimsModel> _logger;
        private readonly IRestApiService _api;

        public CardPrintingClaimsModel(
            ILogger<CardPrintingClaimsModel> logger,
            IRestApiService api)
        {
            _logger = logger;
            _api = api;
        }

        // ================================
        // LIST DATA
        // ================================
        [BindProperty(SupportsGet = true)]
        public List<PrintSessionDto> PrintSessions { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            PrintSessions = await _api.GetAsync<List<PrintSessionDto>>("v1/provisioningSessions/print")
                            ?? new List<PrintSessionDto>();

            return Page();
        }

        // ================================
        // CREATE SESSION
        // ================================
        [BindProperty]
        public CreatePrintSessionRequest NewPrintSession { get; set; } = new();

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid || NewPrintSession.MaxDevices <= 0)
            {
                ModelState.AddModelError(string.Empty, "Invalid input.");
                return await OnGetAsync();
            }

            var response = await _api.PostJsonAsync<CreatePrintSessionRequest, CreatePrintSessionResponse>(
                "v1/provisioningSessions/print",
                NewPrintSession
            );

            TempData["SessionCode"] = response.SessionCode;

            _logger.LogInformation("Print session created: {SessionId}", response.SessionId);

            return RedirectToPage("./CardPrintingClaims");
        }

        // ================================
        // REVOKE SESSION
        // ================================
        [BindProperty]
        public long SessionId { get; set; }

        public async Task<IActionResult> OnPostRevokeAsync()
        {
            _logger.LogInformation("Revoking print session {SessionId}", SessionId);

            await _api.PostJsonAsync<object, object>($"v1/provisioningSessions/print/{SessionId}/revoke",new { });

            return RedirectToPage("./CardPrintingClaims");
        }
    }
}