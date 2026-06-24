using BRaVe_Portal.Extensions;
using BRaVe_Portal.Helpers;
using BRaVe_Portal.Interfaces;
using BRaVe_Portal.Models;
using BRaVe_Portal.Models.DTOs;
using DocumentFormat.OpenXml.Office2013.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;

namespace BRaVe_Portal.Pages.Consents
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        private readonly IRestApiService _api;
        private readonly string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        public IndexModel(ILogger<IndexModel> logger, IRestApiService api)
        {
            _logger = logger;
            _api = api;
        }

        public List<ProgramDetails> Programs { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int? SelectedProgramId { get; set; }

        [BindProperty(SupportsGet = true)]
        public List<Consent> Consents { get; private set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {

            _logger.LogInformation("Loading Programs for tenant.");

            if (Request.ContainsPathProbeInQuery())
            {
                _logger.LogError("Blocked suspicious query payload on Consents GET.");
                return BadRequest("Invalid request.");
            }

            try
            {

                int tenantId = User.Tenant();

                Programs = await _api.GetAsync<List<ProgramDetails>>("v1/Programs") ?? new List<ProgramDetails>();

                _logger.LogInformation("Programs loaded from API and cached. Count: {Count}", Programs.Count);

                ViewData["Programs"] = Programs;

                if (SelectedProgramId.HasValue)
                {
                    Consents = await _api.GetAsync<List<Consent>>($"v1/Consents/Program/{SelectedProgramId.Value}")
                        ?? new List<Consent>();
                    _logger.LogInformation("Consents loaded for ProgramId {ProgramId}. Count: {Count}", SelectedProgramId.Value, Consents.Count);
                }

            }
            catch (Exception ex) 
            {

                _logger.LogError(ex, "Error loading Consents page");

                TempData["ErrorMessage"] = "Unable to load consent list.";

            }

            return Page();

        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAddConsentAsync(ConsentDto consentDto)
        {

            if (Request.ContainsPathProbeInQuery() || await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogError("Blocked suspicious query payload on AddConsent POST.");
                return BadRequest("Invalid request.");
            }

            try
            {

                if (!ModelState.IsValid)
                {
                    ModelState.AddModelError(string.Empty, "Could not save Consent. Please try again.");

                    SelectedProgramId = consentDto.ProgramId;

                    return await OnGetAsync();
                }
                string DefaultLang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

                consentDto.DefaultLang = DefaultLang;

                await _api.PostJsonAsync<ConsentDto, object>("v1/Consents", consentDto);

                SelectedProgramId = consentDto.ProgramId;

            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Error occured while adding Consent.");

                TempData["ErrorMessage"] = "Unable to add consent. Please try again.";
            }

            
            return await OnGetAsync();

        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostEditConsentAsync(Consent consent)
        {
            if (Request.ContainsPathProbeInQuery() || await Request.ContainsPathProbeInFormAsync())
            {
                _logger.LogError("Blocked suspicious query payload on EditConsent POST.");
                return BadRequest("Invalid request.");
            }

            try
            {
                if (!ModelState.IsValid)
                {
                    ModelState.AddModelError(string.Empty, "Could not update Consent. Please try again.");
                    SelectedProgramId = consent.ProgramId;

                    return await OnGetAsync();
                }

                await _api.PutJsonAsync<ConsentDto, object>(
                    $"v1/Consents/{consent.Id}?languageCode={languageCode}",
                    new ConsentDto
                    {
                        Title = consent.Title,
                        Description = consent.Description,
                        IsActive = consent.IsActive,
                        ProgramId = consent.ProgramId
                    });

                _logger.LogInformation(
                    "Consent updated successfully. ConsentId={ConsentId}, ProgramId={ProgramId}, LanguageCode={LanguageCode}",
                    consent.Id,
                    consent.ProgramId,
                    languageCode);

                SelectedProgramId = consent.ProgramId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while editing Consent.");

                TempData["ErrorMessage"] = "Unable to update consent. Please try again.";
            }

            return await OnGetAsync();
        }
    }
}
