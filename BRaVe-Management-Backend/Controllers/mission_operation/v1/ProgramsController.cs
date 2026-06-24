using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Extensions;
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
    public class ProgramsController : ControllerBase
    {
        private readonly IProgramService _programService;
        private readonly ILogger<ProgramsController> _logger;

        public ProgramsController(IProgramService programService, ILogger<ProgramsController> logger)
        {
            _programService = programService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProgramDef>>> GetAll()
        {
            int tenantId = User.Tenant();

            _logger.LogInformation($"Request received to get all programs for TenantId={tenantId}");

            try
            {
                var programs = await _programService.GetAllProgramsByTenant(User.Identifier(), tenantId);

                _logger.LogInformation($"Retrieved { programs.Count()} programs for TenantId={tenantId}");

                return Ok(programs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while fetching programs for TenantId={tenantId}");
                return Problem(statusCode: 500, title: "An error occurred while fetching programs.");
            }
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateProgram(ProgramDto data)
        {
            _logger.LogInformation($"Creating Program based on Assessment {data.AssessmentId} for user {User.Identifier()}");

            try
            {
                data.TenantId = User.Tenant();
                await _programService.CreateProgram(User.Identifier(), data);

                _logger.LogInformation("Program created successfully");
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating program for Assessment {data.AssessmentId} for user { User.Identifier()}");
                
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpPost("update")]
        public async Task<IActionResult> UpdateProgram(ProgramDto data)
        {
            _logger.LogInformation($"Updating Program {data.ProgramId} for user {User.Identifier()}");

            try
            {
                data.TenantId = User.Tenant();
                await _programService.UpdateProgram(User.Identifier(), data);

                _logger.LogInformation("Survey {SurveyId} updated successfully", data.ProgramId);
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating survey {SurveyId} for user {UserId}", data.ProgramId, User.Identifier());
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpGet("{missionId}")]
        public async Task<IActionResult> GetPrograms(int missionId)
        {
            try
            {
                var programs = await _programService.GetProgramsByMissionAsync(missionId);
                return Ok(programs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load programs by mission");
                return StatusCode(500, "Failed to load programs");
            }
        }
    }
}

