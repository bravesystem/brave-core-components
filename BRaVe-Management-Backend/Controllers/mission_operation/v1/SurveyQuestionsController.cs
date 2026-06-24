using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Exceptions;
using BRaVe_Management_Backend.Extensions;
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Globalization;

namespace BRaVe_Management_Backend.Controller
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class SurveyQuestionsController : ControllerBase
    {
        private readonly ISurveyQuestionService _surveyQuestionService;
        private readonly ILogger<SurveyQuestionsController> _logger;

        public SurveyQuestionsController(ISurveyQuestionService surveyQuestionService, ILogger<SurveyQuestionsController> logger)
        {
            _surveyQuestionService = surveyQuestionService;
            _logger = logger;
        }

        // ---- GET ALL QUESTIONS BY SURVEY ----
        [HttpGet("{SurveyCode}")]
        public async Task<ActionResult<IEnumerable<SurveyQuestionDto>>> GetAll(string SurveyCode)
        {
            _logger.LogInformation("GET all survey questions called for SurveyCode={SurveyCode}", SurveyCode);

            try
            {
                var result = await _surveyQuestionService.GetAllQuestionsBySurveyCode(SurveyCode);
                _logger.LogInformation("Fetched {Count} questions for SurveyCode={SurveyCode}", result.Count(), SurveyCode);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all questions for SurveyCode={SurveyCode}", SurveyCode);
                return StatusCode(500, $"Error fetching questions: {ex.Message}");
            }
        }

        // ---- GET ALL QUESTIONS BY SURVEY ----
        [HttpGet("byId/{SurveyId}")]
        public async Task<ActionResult<IEnumerable<SurveyQuestionDto>>> GetAllBySurveyId(int SurveyId)
        {
            _logger.LogInformation("GET all survey questions called for SurveyId={SurveyCode}", SurveyId);

            try
            {
                var result = await _surveyQuestionService.GetAllQuestionsBySurveyId(SurveyId);
                _logger.LogInformation("Fetched {Count} questions for SurveyId={SurveyId}", result.Count(), SurveyId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all questions for SurveyId={SurveyCode}", SurveyId);
                return StatusCode(500, $"Error fetching questions: {ex.Message}");
            }
        }

        // ---- GET SPECIFIC QUESTION ----
        [HttpGet("{SurveyCode}/{questionId}")]
        public async Task<ActionResult<IEnumerable<SurveyQuestionDto>>> GetQuestionById(string SurveyCode, int questionId)
        {
            _logger.LogInformation("GET question by ID called for SurveyCode={SurveyCode}, QuestionId={QuestionId}", SurveyCode, questionId);

            try
            {
                var result = await _surveyQuestionService.GetQuestionById(SurveyCode, questionId);
                if (result == null)
                {
                    _logger.LogWarning("Question not found: SurveyCode={SurveyCode}, QuestionId={QuestionId}", SurveyCode, questionId);
                    return NotFound();
                }

                _logger.LogInformation("Fetched question successfully: SurveyCode={SurveyCode}, QuestionId={QuestionId}", SurveyCode, questionId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching question: SurveyCode={SurveyCode}, QuestionId={QuestionId}", SurveyCode, questionId);
                return StatusCode(500, $"Error fetching question: {ex.Message}");
            }
        }

        // ---- CREATE NEW QUESTION ----
        [HttpPost("create")]
        public async Task<ActionResult> CreateSurveyQuestion(SurveyQuestionDto data)
        {
            _logger.LogInformation("POST create question called for SurveyCode={SurveyCode}", data?.SurveyCode);

            try
            {
                data.TenantId = User.Tenant();
                //data.DefaultLang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                data.DefaultLang = "en";

                await _surveyQuestionService.CreateSurveyQuestion(User.Identifier(), data);

                _logger.LogInformation("Question created successfully: SurveyCode={SurveyCode}", data.SurveyCode);
                return Ok();
            }
            catch (Exception ex)
       
            {
                _logger.LogError(ex, "Error creating survey question for SurveyCode={SurveyCode}", data?.SurveyCode);
                throw; // preserve original behavior
            }
        }

        // ---- UPDATE QUESTION ----
        [HttpPut("update/{SurveyCode}/{id}")]
        public async Task<IActionResult> UpdateSurveyQuestion(string SurveyCode, int id, [FromBody] SurveyQuestionDto data)
        {
            _logger.LogInformation("PUT update question called: SurveyCode={SurveyCode}, QuestionId={QuestionId}", SurveyCode, id);

            try
            {
                data.TenantId = User.Tenant();
                await _surveyQuestionService.UpdateSurveyQuestion(User.Identifier(), data, id, SurveyCode);

                _logger.LogInformation("Question updated successfully: SurveyCode={SurveyCode}, QuestionId={QuestionId}", SurveyCode, id);
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating question: SurveyCode={SurveyCode}, QuestionId={QuestionId}", SurveyCode, id);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // ---- DELETE QUESTION ----
        [HttpDelete("delete/{SurveyCode}/{id}")]
        public async Task<IActionResult> DeleteSurveyQuestion(int id, string SurveyCode)
        {
            _logger.LogInformation("DELETE question called: SurveyCode={SurveyCode}, QuestionId={QuestionId}", SurveyCode, id);

            try
            {
                await _surveyQuestionService.DeleteSurveyQuestion(User.Identifier(), User.Tenant(), id, SurveyCode);
                _logger.LogInformation("Question deleted successfully: SurveyCode={SurveyCode}, QuestionId={QuestionId}", SurveyCode, id);
                return Ok();
            }
            catch(SqlException ex) when (ex.Number == 50020)
            {
                _logger.LogError(ex, "Hard delete is not allowed. The survey question has been soft deleted instead. SurveyCode={SurveyCode}, QuestionId={QuestionId}", SurveyCode, id);
                //return BadRequest(ex.Message);

                return Conflict(ex.Message);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "Error deleting question: SurveyCode={SurveyCode}, QuestionId={QuestionId}", SurveyCode, id);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting question: SurveyCode={SurveyCode}, QuestionId={QuestionId}", SurveyCode, id);
                return StatusCode(500, $"Error deleting question: SurveyCode={SurveyCode}, QuestionId={id}");
            }
        }

        // ---- GET TRANSLATIONS ----
        [HttpGet("translations/{SurveyCode}/{QuestionId}")]
        public async Task<ActionResult<IEnumerable<SurveyQuestionTranslationDto>>> GetAll(string SurveyCode, int QuestionId)
        {
            _logger.LogInformation("GET translations called: SurveyCode={SurveyCode}, QuestionId={QuestionId}", SurveyCode, QuestionId);

            try
            {
                var tenantId = User.Tenant();
                var translations = await _surveyQuestionService.GetAllSurveyQuestionTranslations(SurveyCode, QuestionId, tenantId);

                _logger.LogInformation("Fetched {Count} translations for SurveyCode={SurveyCode}, QuestionId={QuestionId}", translations.Count(), SurveyCode, QuestionId);
                return Ok(translations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching translations for SurveyCode={SurveyCode}, QuestionId={QuestionId}", SurveyCode, QuestionId);
                return StatusCode(500, $"Error fetching translations: {ex.Message}");
            }
        }

        // ---- CREATE TRANSLATION ----
        [HttpPost("translations/create")]
        public async Task<ActionResult> CreateTranslation(SurveyQuestionTranslationDto dto)
        {
            _logger.LogInformation("POST create translation called: SurveyCode={SurveyCode}, QuestionId={QuestionId}, LanguageCode={LanguageCode}",
                dto?.SurveyCode, dto?.Id, dto?.LanguageCode);

            try
            {
                dto.TenantId = User.Tenant();
                await _surveyQuestionService.CreateSurveyQuestionTranslation(dto);

                _logger.LogInformation("Translation created successfully: SurveyCode={SurveyCode}, QuestionId={QuestionId}, LanguageCode={LanguageCode}",
                    dto.SurveyCode, dto.Id, dto.LanguageCode);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating translation: SurveyCode={SurveyCode}, QuestionId={QuestionId}, LanguageCode={LanguageCode}",
                    dto?.SurveyCode, dto?.Id, dto?.LanguageCode);
                return StatusCode(500, $"Error creating translation: {ex.Message}");
            }
        }

        // ---- UPDATE TRANSLATION ----
        [HttpPut("translations/update/{SurveyCode}/{QuestionId}/{LanguageCode}")]
        public async Task<ActionResult> UpdateSurveyQuestionTranslation(SurveyQuestionTranslationDto dto, string SurveyCode, int QuestionId, string LanguageCode)
        {
            _logger.LogInformation("PUT update translation called: SurveyCode={SurveyCode}, QuestionId={QuestionId}, LanguageCode={LanguageCode}",
                SurveyCode, QuestionId, LanguageCode);

            try
            {
                dto.TenantId = User.Tenant();
                dto.SurveyCode = SurveyCode;
                dto.Id = QuestionId;
                dto.LanguageCode = LanguageCode;

                await _surveyQuestionService.UpdateSurveyQuestionTranslation(dto);

                _logger.LogInformation("Translation updated successfully: SurveyCode={SurveyCode}, QuestionId={QuestionId}, LanguageCode={LanguageCode}",
                    SurveyCode, QuestionId, LanguageCode);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating translation: SurveyCode={SurveyCode}, QuestionId={QuestionId}, LanguageCode={LanguageCode}",
                    SurveyCode, QuestionId, LanguageCode);
                return StatusCode(500, $"Error updating translation: {ex.Message}");
            }
        }

        // ---- DELETE TRANSLATION ----
        [HttpDelete("translations/delete/{SurveyCode}/{QuestionId}/{LanguageCode}")]
        public async Task<ActionResult> DeleteTranslation(string SurveyCode, int QuestionId, string LanguageCode)
        {
            _logger.LogInformation("DELETE translation called: SurveyCode={SurveyCode}, QuestionId={QuestionId}, LanguageCode={LanguageCode}",
                SurveyCode, QuestionId, LanguageCode);

            try
            {
                var dto = new SurveyQuestionTranslationDto
                {
                    SurveyCode = SurveyCode,
                    Id = QuestionId,
                    LanguageCode = LanguageCode,
                    TenantId = User.Tenant()
                };

                await _surveyQuestionService.DeleteSurveyQuestionTranslation(dto);

                _logger.LogInformation("Translation deleted successfully: SurveyCode={SurveyCode}, QuestionId={QuestionId}, LanguageCode={LanguageCode}",
                    SurveyCode, QuestionId, LanguageCode);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting translation: SurveyCode={SurveyCode}, QuestionId={QuestionId}, LanguageCode={LanguageCode}",
                    SurveyCode, QuestionId, LanguageCode);
                return StatusCode(500, $"Error deleting translation: {ex.Message}");
            }
        }
    }
}
