using BRaVe_Management_Backend.DTOs;
using BRaVe_Portal.Extensions;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using BRaVe_Portal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace BRaVe_Portal.Pages.surveys.Questionnaire
{
    public class ManageTranslationModel : PageModel
    {
        private readonly ILogger<ManageTranslationModel> _logger;
        private readonly IRestApiService _api;

        public ManageTranslationModel(ILogger<ManageTranslationModel> logger, IRestApiService api)
        {
            _logger = logger;
            _api = api;
        }

        [BindProperty(SupportsGet = true)]
        public int QuestionId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SurveyCode { get; set; }

        [BindProperty(SupportsGet = true)]
        public int SurveyId { get; set; }

        public SurveyQuestionDto? CurrentQuestion { get; set; }
        public List<SurveyQuestionTranslationDto> Translations { get; set; } = new();

        [BindProperty]
        public SurveyQuestionTranslationDto NewTranslation { get; set; } = new();

        [BindProperty]
        public SurveyQuestionTranslationDto EditTranslation { get; set; } = new();

        public string DefaultLang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

        // ---- LOAD TRANSLATIONS ----
        public async Task OnGetAsync()
        {
            _logger.LogInformation("OnGetAsync called: SurveyCode={SurveyCode}, QuestionId={QuestionId}", SurveyCode, QuestionId);

            try
            {
                _logger.LogInformation("Fetching question details for SurveyCode={SurveyCode}, QuestionId={QuestionId}", SurveyCode, QuestionId);
                CurrentQuestion = await _api.GetAsync<SurveyQuestionDto>($"v1/SurveyQuestions/{SurveyCode}/{QuestionId}");

                _logger.LogInformation("Fetching translations for SurveyCode={SurveyCode}, QuestionId={QuestionId}", SurveyCode, QuestionId);
                Translations = await _api.GetAsync<List<SurveyQuestionTranslationDto>>(
                    $"v1/SurveyQuestions/translations/{SurveyCode}/{QuestionId}"
                ) ?? new List<SurveyQuestionTranslationDto>();

                _logger.LogInformation("Loaded {Count} translations for QuestionId={QuestionId}", Translations.Count, QuestionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching question or translations for SurveyCode={SurveyCode}, QuestionId={QuestionId}", SurveyCode, QuestionId);
            }
        }

        // ---- CREATE NEW TRANSLATION ----
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAddTranslationAsync()
        {
            _logger.LogInformation("OnPostAddTranslationAsync called: SurveyCode={SurveyCode}, QuestionId={QuestionId}", SurveyCode, QuestionId);

            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid ModelState while adding translation for SurveyCode={SurveyCode}, QuestionId={QuestionId}. Errors: {Errors}",
                        SurveyCode, QuestionId, string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));

                    ModelState.AddModelError(string.Empty, "Please fill out all required fields.");
                    await OnGetAsync();
                    return Page();
                }

                _logger.LogInformation("Posting new translation for SurveyCode={SurveyCode}, QuestionId={QuestionId}, Language={LanguageCode}",
                    SurveyCode, QuestionId, NewTranslation.LanguageCode);

                await _api.PostJsonAsync<SurveyQuestionTranslationDto, object>(
                    "v1/SurveyQuestions/translations/create", NewTranslation);

                _logger.LogInformation("Translation added successfully for Language={LanguageCode}, QuestionId={QuestionId}", NewTranslation.LanguageCode, QuestionId);
                TempData["SuccessMessage"] = "Translation added successfully.";

                return RedirectToPage(new { QuestionId, SurveyCode });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating translation for SurveyCode={SurveyCode}, QuestionId={QuestionId}", SurveyCode, QuestionId);
                ModelState.AddModelError(string.Empty, "Failed to add translation.");
                TempData["ErrorMessage"] = "Could not add translation. Check for duplicate entries.";
                return RedirectToPage(new { QuestionId, SurveyCode });
            }
        }

        // ---- UPDATE TRANSLATION ----
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostEditTranslationAsync(string languageCode)
        {
            _logger.LogInformation("OnPostEditTranslationAsync called: SurveyCode={SurveyCode}, QuestionId={QuestionId}, LanguageCode={LanguageCode}",
                SurveyCode, QuestionId, languageCode);

            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid ModelState while updating translation for SurveyCode={SurveyCode}, QuestionId={QuestionId}, LanguageCode={LanguageCode}. Errors: {Errors}",
                        SurveyCode, QuestionId, languageCode, string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));

                    ModelState.AddModelError(string.Empty, "Please fill out all required fields.");
                    await OnGetAsync();
                    return Page();
                }

                _logger.LogInformation("Updating translation for SurveyCode={SurveyCode}, QuestionId={QuestionId}, LanguageCode={LanguageCode}",
                    SurveyCode, QuestionId, languageCode);

                await _api.PutJsonAsync<SurveyQuestionTranslationDto, object>(
                    $"v1/SurveyQuestions/translations/update/{SurveyCode}/{QuestionId}/{languageCode}",
                    EditTranslation);

                _logger.LogInformation("Translation updated successfully: QuestionId={QuestionId}, LanguageCode={LanguageCode}", QuestionId, languageCode);
                TempData["SuccessMessage"] = "Translation updated successfully.";
                return RedirectToPage(new { QuestionId, SurveyCode });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating translation for SurveyCode={SurveyCode}, QuestionId={QuestionId}, LanguageCode={LanguageCode}",
                    SurveyCode, QuestionId, languageCode);
                ModelState.AddModelError(string.Empty, "Failed to update translation.");
                return Page();
            }
        }

        // ---- DELETE TRANSLATION ----
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDeleteTranslationAsync(string languageCode)
        {
            _logger.LogInformation("OnPostDeleteTranslationAsync called: SurveyCode={SurveyCode}, QuestionId={QuestionId}, LanguageCode={LanguageCode}",
                SurveyCode, QuestionId, languageCode);

            try
            {
                _logger.LogInformation("Deleting translation for SurveyCode={SurveyCode}, QuestionId={QuestionId}, LanguageCode={LanguageCode}",
                    SurveyCode, QuestionId, languageCode);

                await _api.DeleteAsync(
                    $"v1/SurveyQuestions/translations/delete/{SurveyCode}/{QuestionId}/{languageCode}");

                _logger.LogInformation("Translation deleted successfully for LanguageCode={LanguageCode}, QuestionId={QuestionId}",
                    languageCode, QuestionId);

                TempData["SuccessMessage"] = "Translation deleted successfully.";
                return RedirectToPage(new { QuestionId, SurveyCode });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting translation for SurveyCode={SurveyCode}, QuestionId={QuestionId}, LanguageCode={LanguageCode}",
                    SurveyCode, QuestionId, languageCode);
                TempData["ErrorMessage"] = "Failed to delete translation.";
                await OnGetAsync();
                return Page();
            }
        }
    }
}
