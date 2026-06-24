using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models.DTOs;
using DocumentFormat.OpenXml.Math;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.RegularExpressions;

namespace BRaVe_Portal.Pages
{
    [AllowAnonymous]
    public class FormRegisterUserModel : PageModel
    {
        private readonly ILogger<FormRegisterUserModel> _logger;
        private readonly IAccessControlClient _access;

        private const int PasswordMinLength = 8;
        private static readonly Regex PasswordSpecialCharRegex = new(@"^(?=.*[^a-zA-Z0-9]).+$", RegexOptions.Compiled);


        public FormRegisterUserModel(
            ILogger<FormRegisterUserModel> logger,
             IAccessControlClient access)
        {
            _logger = logger;
            _access = access;
        }

        [BindProperty]
        public UatFormRegisterDto Dto { get; set; } = new UatFormRegisterDto();

        [BindProperty]
        public string? ConfirmPassword { get; set; }

        public async Task<IActionResult> OnGet(UatFormRegisterDto? dto)
        {
            Dto.DisplayName = dto.DisplayName;
            Dto.Email = dto.Email;

            return Page();
        }


        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostRegisterAsync(CancellationToken ct)
        {

            if (string.IsNullOrEmpty(ConfirmPassword) || Dto.Password != ConfirmPassword)
            {
                ModelState.AddModelError(nameof(ConfirmPassword), "Password and confirmation do not match.");
            }

            if (!string.IsNullOrEmpty(Dto.Password))
            {
                if (Dto.Password.Length < PasswordMinLength)
                    ModelState.AddModelError(nameof(Dto) + "." + nameof(UatFormRegisterDto.Password), "Password must be at least 8 characters.");
                else if (!PasswordSpecialCharRegex.IsMatch(Dto.Password))
                    ModelState.AddModelError(nameof(Dto) + "." + nameof(UatFormRegisterDto.Password), "Password must contain at least one special character.");
            }


            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Could not register user. Please try again.");
                return await OnGet(new UatFormRegisterDto
                {
                    Email = Dto.Email,
                    DisplayName = Dto.DisplayName
                });
            }

            try
            {
                await _access.FormRegisterUser(Dto, ct);
                return RedirectToPage("/FormLoginUser");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Form registration failed for {Email}", Dto.Email);

                ModelState.AddModelError(string.Empty, "Could not register user. Please verify your token and try again.");
                return await OnGet(new UatFormRegisterDto
                {
                    Email = Dto.Email,
                    DisplayName = Dto.DisplayName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during form registration for {Email}", Dto.Email);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again later.");
                return await OnGet(new UatFormRegisterDto
                {
                    Email = Dto.Email,
                    DisplayName = Dto.DisplayName
                });
            }
        }
    }
}
