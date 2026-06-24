using BRaVe_Portal.Extensions;
using BRaVe_Portal.Helpers;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace BRaVe_Portal.Pages.joinamission
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

        // Bind properties from modal form
        [BindProperty]
        public string Username { get; set; } = string.Empty;
        [BindProperty]
        public string UserId { get; set; } = string.Empty;

        [BindProperty]
        public string ProfileName { get; set; } = string.Empty;

        [BindProperty]
        public string RoleDescription { get; set; } = string.Empty;

        [BindProperty]
        public string? SuccessMessage { get; set; }

        [BindProperty]
        public string? ErrorMessage { get; set; }



        public async Task<IActionResult> OnGet()
        {
            if (Request.ContainsUnexpectedQueryParameters())
            {
                _logger.LogError("Blocked suspicious query pattern on DuplicateRulesets page.");
                return BadRequest("Invalid request.");
            }

            UserId = User.Identifier();
            Username = User.GetUserEmailLike();


            ProfileName = User.DisplayName();

            try
            {
                //if pending request
                bool exists = await _api.GetAsync<bool>($"v1/MissionAccesses/{UserId}/exists");

                if (exists)
                {
                    return RedirectToPage("PendingRequest");
                }

            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Error occured while verifying access to joinmission main page.");
                TempData["ErrorMessage"] = "We're unable to verify your access to any mission. Please try again.";
            }

            return Page();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostSubmitAccessRequestAsync()
        {
            if (Request.ContainsPathProbeInQuery() ||
               await Request.ContainsPathProbeInFormAsync() ||
               StringHelper.IsPotentialPathProbe(RoleDescription))
            {
                _logger.LogError("Blocked suspicious query payload on SubmitAccessRequest POST.");
                return BadRequest("Invalid request.");
            }

            if (string.IsNullOrWhiteSpace(RoleDescription))
            {
                ErrorMessage = "Please provide a role description.";
                return Page();
            }

            var dto = new UserRequestDto
            {
                UserId = UserId,
                ProfileName = ProfileName,
                Justification = RoleDescription,
                UserDetails = Username // from Azure AD
            };

            try
            {
                await _api.PostJsonAsync<UserRequestDto, object>("v1/MissionAccesses", dto);

                SuccessMessage = "Your access request has been submitted successfully! You will receive an email confirmation once your request has been approved.";
                RoleDescription = string.Empty; // reset textarea

                return RedirectToPage("PendingRequest");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting mission access request");
                ErrorMessage = "You have already submitted an access request. Please wait for approval.";
            }

            return Page();
        }
    }
}
