using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Exceptions;
using BRaVe_Management_Backend.Extensions;
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;


namespace BRaVe_Management_Backend.Controller
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class SurveysController : ControllerBase
    {
        private readonly ISurveyService _surveyService;
        private readonly ILogger<SurveysController> _logger;

        public SurveysController(ISurveyService surveyService, ILogger<SurveysController> logger)
        {
            _surveyService = surveyService;
            _logger = logger;
        }



        [HttpGet]
        public async Task<ActionResult<IEnumerable<Surveys>>> GetAllSurveys()
        {
            _logger.LogInformation("Fetching all surveys for ProgramId");


            string userId = User.Identifier();
            int tenantId = User.Tenant();

            var surveys = await _surveyService.GetAllSurveys(userId,tenantId);

            _logger.LogInformation("Fetched {Count} surveys for userId ", surveys.Count());

            return Ok(surveys);
        }
        [HttpGet("{ProgramId}")]
        public async Task<ActionResult<IEnumerable<Surveys>>> GetAll(int ProgramId)
        {
            _logger.LogInformation("Fetching all surveys for ProgramId {ProgramId}", ProgramId);

            var surveys = await _surveyService.GetAllSurveyByProgramId(User.Tenant(),ProgramId);

            _logger.LogInformation("Fetched {Count} surveys for ProgramId {ProgramId}", surveys.Count(), ProgramId);

            return Ok(surveys);
        }

        [HttpGet("Survey/{SurveyId}")]
        public async Task<ActionResult<IEnumerable<Surveys>>> GetSurveyById(int SurveyId)
        {

            return Ok(await _surveyService.GetSurveyById(SurveyId));
        }


        [HttpPost("create")]
        public async Task<ActionResult> CreateSurvey(SurveyDto data)
        {
            _logger.LogInformation("Creating survey for user {UserId}", User.Identifier());

            try
            {
                data.TenantId = User.Tenant();
                await _surveyService.CreateSurvey(User.Identifier(), data);

                _logger.LogInformation("Survey created successfully for user {UserId}", User.Identifier());
                return Ok();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error creating survey for user {UserId}", User.Identifier());
                throw; // Or return StatusCode(500, ...)
            }
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdateSurvey(SurveyDto data)
        {
            _logger.LogInformation("Updating survey {SurveyId} for user {UserId}", data.ProgramId, User.Identifier());

            try
            {
                data.TenantId = User.Tenant();
                await _surveyService.UpdateSurvey(User.Identifier(), data);

                _logger.LogInformation("Survey {SurveyId} updated successfully", data.ProgramId);
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating survey {SurveyId} for user {UserId}", data.ProgramId, User.Identifier());
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut("activate/{id}")]
        public async Task<IActionResult> ActivateSurvey(int id)
        {

            await _surveyService.ActivateSurvey(User.Identifier(), id);

            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSurvey(int id)
        {
            _logger.LogInformation("Deleting survey {SurveyId} for user {UserId}", id, User.Identifier());

            try
            {
                await _surveyService.DeleteSurvey(User.Identifier(), new DeleteSurveyDto
                {
                    SurveyId = id,
                    TenantId = User.Tenant()
                });

                _logger.LogInformation("Survey {SurveyId} deleted successfully", id);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting survey {SurveyId} for user {UserId}", id, User.Identifier());
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
