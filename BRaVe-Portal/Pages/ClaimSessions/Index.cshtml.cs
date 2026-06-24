using BRaVe_Portal.Extensions;
using BRaVe_Portal.Helpers;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using BRaVe_Portal.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BRaVe_Portal.Pages.ClaimSessions
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        private readonly IRestApiService _api;

        public IndexModel(ILogger<IndexModel> logger, IRestApiService api)
        {
            _logger = logger;
            _api = api;
        }

        [BindProperty(SupportsGet = true)]
        public List<ClaimSession> ClaimSessionss { get; private set; } = new();


        [BindProperty]
        public ClaimSessionDto ClaimSession { get; set; } = new();



        public async Task<IActionResult> OnGetAsync()
        {

            if (Request.ContainsPathProbeInQuery())
            {
                _logger.LogError("Blocked suspicious query payload on ClaimSession GET.");
                return BadRequest("Invalid request.");
            }

            try
            {
                ClaimSessionss = await _api.GetAsync<List<ClaimSession>>("v1/ClaimSession")
                    ?? new List<ClaimSession>();

            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error loading ClaimSession page");

                TempData["ErrorMessage"] = "Unable to load claim sessions.";

            }

            return Page();
        }

        [BindProperty]
        public ClaimSessionDto ClaimSessionsss  { get; set; } = new();


        [BindProperty]
        public ClaimSession UpdatedClaimSession { get; set; } = new();

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAddClaimSessionAsync()
        {
            if (Request.ContainsPathProbeInQuery() || await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogError("Blocked suspicious query payload on AddClaimSession POST.");
                return BadRequest("Invalid request.");
            }

            try
            {
                if (!ModelState.IsValid || ClaimSessionsss != null && !ClaimSessionsss.IsValid())
                {
                    ModelState.AddModelError(string.Empty, "Could not save Claim Session. Please try again.");
                    ClaimSessionss = await _api.GetAsync<List<ClaimSession>>("v1/ClaimSession") ?? new();
                    return await OnGetAsync();
                }

                CreateSessionResponse response = await _api.PostJsonAsync<ClaimSessionDto, CreateSessionResponse>("v1/ProvisioningSessions", ClaimSessionsss);
                // ViewData["SessionCode"] = response.SessionCode.ToString();

                TempData["SessionCode"] = response.SessionCode;
                
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Error while adding Claim session");

                TempData["ErrorMessage"] = "Unable to create claim session. Please try again later.";

            }

            return RedirectToPage("./MobileClaims");

        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDeleteAsync()
        {

            if (Request.ContainsPathProbeInQuery() || await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogError("Blocked suspicious query payload on RemoveClaimSession POST.");
                return BadRequest("Invalid request.");
            }

            try
            {
                _logger.LogInformation("Revoking Session {SessionId} for SessionId {SessionId}", UpdatedClaimSession.SessionId);

                await _api.DeleteAsync($"v1/ClaimSession/{UpdatedClaimSession.SessionId}");
                _logger.LogInformation("Claim Session {SessionId} Revoked.", UpdatedClaimSession.SessionId);

            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Error while revoking Claim session");

                TempData["ErrorMessage"] = "Unable to revoke claim session. Please try again later.";

            }


            return RedirectToPage("./MobileClaims");
        }
    }
}
