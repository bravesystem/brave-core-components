using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Extensions;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BRaVe_Management_Backend.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class MissionsController : ControllerBase
    {
        private readonly IMissionService _missionService;
        private readonly ILogger<MissionsController> _logger;
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly IServiceScopeFactory _scopeFactory;

        public MissionsController(IBackgroundTaskQueue taskQueue, IServiceScopeFactory scopeFactory, IMissionService missionService, ILogger<MissionsController> logger)
        {
            _taskQueue = taskQueue;
            _scopeFactory = scopeFactory;
            _missionService = missionService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Mission>>> GetAll()
        {
            _logger.LogInformation("Fetching all missions.");
            try
            {
                var missions = await _missionService.GetAllMissions();
                _logger.LogInformation("Fetched {Count} missions.", missions?.Count() ?? 0);
                return Ok(missions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all missions.");
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpGet("/api/v1/Missions/bycountry/{id}")]
        public async Task<ActionResult<IEnumerable<Mission>>> GetMissionsByCountryId(string id)
        {
            _logger.LogInformation("Fetching missions for country code: {CountryCode}", id);
            try
            {
                var missions = await _missionService.GetAllMissionsByCountry(id);

                if (missions == null || !missions.Any())
                {
                    _logger.LogWarning("No missions found for country code: {CountryCode}", id);
                    return NotFound($"No missions found for country code: {id}");
                }

                _logger.LogInformation("Fetched {Count} missions for country code: {CountryCode}", missions.Count(), id);
                return Ok(missions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching missions for country code: {CountryCode}", id);
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Mission>> GetById(int id)
        {
            _logger.LogInformation("Fetching mission with ID: {MissionId}", id);
            try
            {
                var mission = await _missionService.GetMission(id);
                if (mission == null)
                {
                    _logger.LogWarning("Mission not found with ID: {MissionId}", id);
                    return NotFound();
                }

                _logger.LogInformation("Fetched mission with ID: {MissionId}", id);
                return Ok(mission);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching mission with ID: {MissionId}", id);
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Create(MissionDto data)
        {
            _logger.LogInformation("Creating new mission: {MissionName}", data.Name);

            string? tenantCode = null;
            int attempts = 0;
            const int maxAttempts = 3;

            try
            {
                do
                {
                    tenantCode = await _missionService.CreateMission(data, User.Identifier());
                    attempts++;
                } while (tenantCode == null && attempts < maxAttempts);

                if (tenantCode == null)
                {
                    _logger.LogWarning("Failed to create mission {MissionName} after {Attempts} attempts due to duplicate code.", data.Name, attempts);
                    return StatusCode(500, new { error = "DUPLICATE_CODE" });
                }


                _logger.LogInformation("Start the background job to initialize the mission setup.");
                
                _taskQueue.QueueBackgroundWorkItem(async ct =>
                {
                    try
                    {
                        _missionService.BootstrapMission(tenantCode);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error occurred while bootstrapping the mission after it was created. Contact the administrator for manual setup or verification.");
                    }

                    await Task.CompletedTask;
                });


                _logger.LogInformation("Successfully created mission {MissionName} with tenant code {TenantCode}.", data.Name, tenantCode);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating mission {MissionName}", data.Name);
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, MissionDto data)
        {
            _logger.LogInformation("Updating mission with ID: {MissionId}", id);

            var mission = new Mission
            {
                MissionId = id,
                CountryIso2 = data.CountryIso2,
                FocalpointType = data.FocalpointType,
                Name = data.Name,
                Note = data.Note,
            };

            try
            {
                await _missionService.UpdateMission(mission, User.Identifier());
                _logger.LogInformation("Successfully updated mission with ID: {MissionId}", id);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating mission with ID: {MissionId}", id);
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteMission(int id)
        {
            _logger.LogInformation("Deleting mission with ID: {MissionId}", id);
            try
            {
                await _missionService.DeleteMission(id);
                _logger.LogInformation("Successfully deleted mission with ID: {MissionId}", id);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting mission with ID: {MissionId}", id);
                return StatusCode(500, "Internal server error.");
            }
        }

        //Get user mission

        [HttpGet("usermission/{UserId}")]
        public async Task<ActionResult<Mission>> GetUserMission(string UserId)
        {
            _logger.LogInformation("Fetching mission with user name: {UserId}", UserId);
            try
            {
                var mission = await _missionService.GetUserMission(UserId);
                if (mission == null)
                {
                    _logger.LogWarning("Mission not found for user: {UserId}", UserId);
                    return NotFound();
                }

                _logger.LogInformation("Fetched mission for user: {UserId}", UserId);
                return Ok(mission);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching mission for user: {UserId}", UserId);
                return StatusCode(500, "Internal server error.");
            }
        }
    }
}
