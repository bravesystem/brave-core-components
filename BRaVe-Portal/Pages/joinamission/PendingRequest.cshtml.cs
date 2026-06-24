using BRaVe_Portal.Extensions;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace BRaVe_Portal.Pages.joinamission
{
    public class PendingRequestModel : PageModel
    {
        private readonly ILogger<PendingRequestModel> _logger;
        private readonly IRestApiService _api;

        public PendingRequestModel(ILogger<PendingRequestModel> logger, IRestApiService api)
        {
            _logger = logger;
            _api = api;
        }

        public DateTime SubmissionDate { get; set; }
        public string Username { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync()
        {
            if (Request.ContainsPathProbeInQuery())
            {
                _logger.LogError("Blocked suspicious query pattern on PendingRequest page.");
                return BadRequest("Invalid request.");
            }

            Username = User.Identifier();

            try
            {
            
                var dto = await _api.GetAsync<UserMissionRequest>($"v1/MissionAccesses/{Username}");

                if (dto == null)
                {
                    return NotFound("No pending request found for this user.");
                }

                SubmissionDate = dto.RequestedOn;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving mission access request for {User}", Username);
                SubmissionDate = DateTime.MinValue;
            }

            return Page();
        }
    }
}
