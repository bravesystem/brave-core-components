using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BRaVe_Portal.Pages
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(ILogger<LoginModel> logger)
        {
            _logger = logger;
        }

        public string PartnerAccessUrl { get; set; } = "/FormLoginUser";

        public void OnGet()
        {
            _logger.LogInformation(
                "Login page requested. Environment={Environment}, Hostname={Hostname}",
                Environment.GetEnvironmentVariable("Environment"),
                Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME"));
        }
    }
}
