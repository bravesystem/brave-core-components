using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Extensions;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using DocumentFormat.OpenXml.Office2019.Word.Cid;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BRaVe_Management_Backend.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class ConsentsController : ControllerBase
    {
        private readonly IConsentService _consentService;
        private readonly ILogger<ConsentsController> _logger;

        public ConsentsController(IConsentService consentService, ILogger<ConsentsController> logger)
        {
            _consentService = consentService;
            _logger = logger;
        }

        [HttpGet("Program/{ProgramId}")]
        public async Task<ActionResult<IEnumerable<Consent>>> GetAll(int ProgramId)
        {
            try
            {
                _logger.LogInformation("Fetching all consents for ProgramId {ProgramId}", ProgramId);
                var consents = await _consentService.GetAllConsents(ProgramId);
                _logger.LogInformation("Fetched {Count} consents for ProgramId {ProgramId}", consents.Count(), ProgramId);
                return Ok(consents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all consents.");

                return BadRequest(new
                {
                    status = "db_error",
                    error = "Unable to retrieve consents from the database. Please try again later."
                });
            }

        }


        [HttpGet("{ConsentId}")]
        public async Task<ActionResult<Consent>> GetConsentById(int ConsentId)
        {
            _logger.LogInformation("Fetching consent with Id {ConsentId}", ConsentId);
            var consent = await _consentService.GetConsentById(ConsentId);
            if (consent == null)
            {
                _logger.LogWarning("Consent with Id {ConsentId} not found", ConsentId);
                return NotFound();
            }
            return Ok(consent);
        }



        [HttpPost]
        public async Task<ActionResult> CreateConsent(ConsentDto data)
        {
            _logger.LogInformation("Creating Consent for Program Id: {Id}", data.ProgramId);
            try
            {
                await _consentService.CreateConsent(data, User.Tenant(), User.Identifier());
                _logger.LogInformation("Consent created successfully for Program Id: {Id}", data.ProgramId);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Consent for Program Id: {Id}", data.ProgramId);
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateConsent(int id, ConsentDto data, string languageCode)
        {
            try
            {
                await _consentService.UpdateConsent(id, data, User.Identifier(), languageCode);

                _logger.LogInformation(
                    "Consent updated successfully. ConsentId={ConsentId}, LanguageCode={LanguageCode}",
                    id,
                    languageCode);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error updating consent. ConsentId={ConsentId}, LanguageCode={LanguageCode}",
                    id,
                    languageCode);

                return StatusCode(500, "An error occurred while updating the consent.");
            }
        }


        [HttpPost("translations/create")]
        public async Task<ActionResult> CreateOrUpdateTranslation([FromBody] ConsentTranslationDto translation)
        {
            if (translation == null)
                return BadRequest("Invalid translation payload.");

            try
            {
                translation.TenantId = User.Tenant();

                await _consentService.AddOrUpdateTranslationAsync(translation, User.Identifier());

                var action = translation.Id == 0 ? "created" : "updated";

                return Ok(new
                {
                    message = $"Translation successfully {action}.",
                    translation
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating/updating translation");
                return StatusCode(500, "Failed to create/update translation.");
            }
        }
        [HttpGet("Program/{ProgramId:int}/Consent/{ConsentId:int}")]
        public async Task<ActionResult<ConsentTranslationDto>> GetTranslation(
            int ProgramId,
            int ConsentId)
        {
            try
            {
                var translation = await _consentService.GetAllTranslationsAsync(ProgramId, ConsentId);

                if (translation == null)
                    return NotFound($"No translation found for Consent {ConsentId} in Program {ProgramId}");

                return Ok(translation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error fetching translation for Consent ID {ConsentId} and Program ID {ProgramId}",
                    ConsentId,
                    ProgramId);

                return StatusCode(500, "Internal server error.");
            }
        }

        // DELETE v1/Consents/translations/delete/{ProgramId}/{ConsentId}/{languageCode}
        [HttpDelete("translations/delete/{ProgramId:int}/{ConsentId:int}/{languageCode}")]
        public async Task<IActionResult> DeleteTranslation(int ProgramId, int ConsentId, string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
                return BadRequest("Language code is required.");

            try
            {
                await _consentService.DeleteTranslationAsync(ProgramId, ConsentId, languageCode);

                return Ok(new { message = "Translation deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to delete translation for ProgramId={ProgramId}, ConsentId={ConsentId}, LanguageCode={LanguageCode}",
                    ProgramId,
                    ConsentId,
                    languageCode);

                return StatusCode(500, "Failed to delete translation.");
            }
        }

    }
}