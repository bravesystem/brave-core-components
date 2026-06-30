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

        public string AppEnvironment { get; private set; } = "";

        public bool IsPartner =>
            string.Equals(AppEnvironment, "PARTNER", StringComparison.OrdinalIgnoreCase);

        public string PrimaryLoginButtonText =>
            IsPartner ? "Partner Login" : "IOM Staff Login";

        public void OnGet()
        {
            AppEnvironment = Environment.GetEnvironmentVariable("Environment") ?? "";
            var hostname = Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME") ?? "";

            if (string.IsNullOrEmpty(AppEnvironment))
            {
                AppEnvironment = "PARTNER";
            }

            _logger.LogInformation(
                "Login page requested. Environment={Environment}, Hostname={Hostname}",
                AppEnvironment,
                hostname);
        }
    }
}
