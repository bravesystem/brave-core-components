using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BRaVe_Portal.Pages.setup
{
    public class IndexModel : PageModel
    {
        public IActionResult OnGet()
        {
            if (User.IsInRole("Admin")) // Replace "X" with your role name
            {
                return RedirectToPage("AdmSetup"); // e.g. Pages/PageA.cshtml
            }
            else
            {
                return RedirectToPage("PaSetup"); // e.g. Pages/PageB.cshtml
            }
        }
    }
}
