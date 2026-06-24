using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace BRaVe_Portal.Pages
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

        public async Task<IActionResult> OnGet()
        {
            _logger.LogInformation("Index page accessed by user {User}", User.Identity?.Name ?? "Anonymous");

            try
            {
                if (!User.Identity.IsAuthenticated &&
                    !HttpContext.Request.Path.Equals("/Login", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("User not authenticated, redirecting to Login page.");
                    return RedirectToPage("/Login");
                }

                if (User.Identity.IsAuthenticated && !User.IsInRole("NoAuth"))
                {
                    return RedirectToPage("/Dashboard/Index");
                }

                if (User.IsInRole("NoAuth"))
                {
                    _logger.LogInformation("User in NoAuth role, redirecting to joinamission/Index page.");
                    return RedirectToPage("/joinamission/Index");
                }


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in Index OnGet.");
            }

            return Page();
        }
    }
}
