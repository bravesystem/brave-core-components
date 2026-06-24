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

namespace BRaVe_Portal.Pages.DataPoints
{
    public class ManageDatapointTranslationsModel : PageModel
    {
        private readonly ILogger<ManageDatapointTranslationsModel> _logger;
        private readonly IRestApiService _api;

        public ManageDatapointTranslationsModel(ILogger<ManageDatapointTranslationsModel> logger, IRestApiService api)
        {
            _logger = logger;
            _api = api;
        }

        [BindProperty(SupportsGet = true)]
        public int CategoryId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int DataPointId { get; set; }

        public List<DatapointTranslationDto> Translations { get; set; } = new();

        [BindProperty]
        public DatapointTranslationDto NewTranslation { get; set; } = new();

        [BindProperty]
        public DatapointTranslationDto EditTranslation { get; set; } = new();

        public string DefaultLang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

        // Load translations
        public async Task OnGetAsync()
        {
            try
            {
                Translations = await _api.GetAsync<List<DatapointTranslationDto>>(
                    $"v1/DataPoints/Id/{DataPointId}") ?? new List<DatapointTranslationDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching translations for DataPointId={DataPointId}", DataPointId);
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
                // DataPointId should be sent in a separate property
                NewTranslation.Id = 0;
                NewTranslation.DataPointId = DataPointId; // make sure your DTO has this property

                await _api.PostJsonAsync<DatapointTranslationDto, object>(
                    "v1/DataPoints/translations/create", NewTranslation);

                TempData["SuccessMessage"] = "Translation added successfully.";
                return RedirectToPage(new { CategoryId, DataPointId });
            }
            catch
            {
                TempData["ErrorMessage"] = "Could not add translation. Check for duplicate entries.";
                return RedirectToPage(new { CategoryId, DataPointId });
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
                EditTranslation.Id = DataPointId; // ensure DataPointId is set
                await _api.PostJsonAsync<DatapointTranslationDto, object>(
                    "v1/DataPoints/translations/create", EditTranslation);

                TempData["SuccessMessage"] = "Translation updated successfully.";
                return RedirectToPage(new { CategoryId, DataPointId });
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
                await _api.DeleteAsync($"v1/DataPoints/translations/delete/{DataPointId}/{languageCode}");
                TempData["SuccessMessage"] = "Translation deleted successfully.";
                return RedirectToPage(new { CategoryId, DataPointId });
            }
            catch
            {
                TempData["ErrorMessage"] = "Failed to delete translation.";
                await OnGetAsync();
                return Page();
            }
        }
    }
}
