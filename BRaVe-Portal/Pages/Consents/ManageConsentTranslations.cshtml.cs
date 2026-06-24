using BRaVe_Management_Backend.DTOs;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace BRaVe_Portal.Pages.Consents
{
    public class ManageConsentTranslationsModel : PageModel
    {
        private readonly ILogger<ManageConsentTranslationsModel> _logger;
        private readonly IRestApiService _api;

        public ManageConsentTranslationsModel(ILogger<ManageConsentTranslationsModel> logger, IRestApiService api)
        {
            _logger = logger;
            _api = api;
        }

        [BindProperty(SupportsGet = true)]
        public int ConsentId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int ProgramId { get; set; }

        public List<ConsentTranslationDto> Translations { get; set; } = new();

        [BindProperty]
        public ConsentTranslationDto NewTranslation { get; set; } = new();

        [BindProperty]
        public ConsentTranslationDto EditTranslation { get; set; } = new();

        public string DefaultLang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

        // Load translations
        public async Task OnGetAsync()
        {
            try
            {
                _logger.LogInformation(
                    "Fetching translations for ProgramId={ProgramId}, ConsentId={ConsentId}",
                    ProgramId,
                    ConsentId);

                Translations = await _api.GetAsync<List<ConsentTranslationDto>>(
                    $"v1/Consents/Program/{ProgramId}/Consent/{ConsentId}")
                    ?? new List<ConsentTranslationDto>();

                _logger.LogInformation(
                    "Retrieved {Count} translations for ProgramId={ProgramId}, ConsentId={ConsentId}",
                    Translations.Count,
                    ProgramId,
                    ConsentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error fetching translations for ProgramId={ProgramId}, ConsentId={ConsentId}",
                    ProgramId,
                    ConsentId);

                Translations = new List<ConsentTranslationDto>();
            }
        }

        // Add new translation
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAddTranslationAsync()
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Please fill out all required fields.");
                await OnGetAsync();
                return Page();
            }

            try
            {
                // Id here should be 0 for a new translation, 
                // ConsentId should be sent in a separate property
                NewTranslation.Id = 0;
                NewTranslation.ConsentId = ConsentId; // make sure your DTO has this property

                await _api.PostJsonAsync<ConsentTranslationDto, object>(
                    "v1/Consents/translations/create", NewTranslation);

                TempData["SuccessMessage"] = "Translation added successfully.";
                return RedirectToPage(new { ProgramId, ConsentId });
            }
            catch
            {
                TempData["ErrorMessage"] = "Could not add translation. Check for duplicate entries.";
                return RedirectToPage(new { ProgramId, ConsentId });
            }
        }


        // Edit translation
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostEditTranslationAsync()
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Please fill out all required fields.");
                await OnGetAsync();
                return Page();
            }

            try
            {
                EditTranslation.Id = ConsentId; // ensure ConsentId is set
                await _api.PostJsonAsync<ConsentTranslationDto, object>(
                    "v1/Consents/translations/create", EditTranslation);

                TempData["SuccessMessage"] = "Translation updated successfully.";
                return RedirectToPage(new { ProgramId, ConsentId });
            }
            catch
            {
                TempData["ErrorMessage"] = "Could not update translation.";
                return Page();
            }
        }

        // Delete translation
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDeleteTranslationAsync(string languageCode)
        {
            try
            {
                await _api.DeleteAsync(
                    $"v1/Consents/translations/delete/{ProgramId}/{ConsentId}/{languageCode}");

                TempData["SuccessMessage"] = "Translation deleted successfully.";

                return RedirectToPage(new { ProgramId, ConsentId });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to delete translation for ProgramId={ProgramId}, ConsentId={ConsentId}, LanguageCode={LanguageCode}",
                    ProgramId,
                    ConsentId,
                    languageCode);

                TempData["ErrorMessage"] = "Failed to delete translation.";

                await OnGetAsync();
                return Page();
            }
        }
    }
}
