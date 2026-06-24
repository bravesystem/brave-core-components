using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Extensions;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Runtime.ConstrainedExecution;

namespace BRaVe_Management_Backend.Controllers.mission_setup.v1
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class MissionAccessesController : ControllerBase
    {
        private readonly IUserMissionMappingService _userMissionMappingService;
        private readonly ILogger<MissionAccessesController> _logger;

        public MissionAccessesController(IUserMissionMappingService userMissionMappingService,
                                         ILogger<MissionAccessesController> logger)
        {
            _userMissionMappingService = userMissionMappingService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<UserMissionRequest>> GetAllRequests()
        {
            _logger.LogInformation("Fetching all user mission requests.");

            try
            {
                var result = await _userMissionMappingService.GetAllUserRequests();
                _logger.LogInformation("Fetched {Count} user mission requests.", User.Identity?.Name);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all user mission requests.");
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpGet("{userid}")]
        public async Task<ActionResult<UserMissionRequest>> GetRequestById(string userid)
        {
            _logger.LogInformation("Fetching user mission request for UserId: {UserId}", userid);

            try
            {
                var result = await _userMissionMappingService.GetUserRequestById(userid);
                if (result == null)
                {
                    _logger.LogWarning("No mission request found for UserId: {UserId}", userid);
                    return NotFound();
                }

                _logger.LogInformation("Successfully fetched mission request for UserId: {UserId}", userid);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching mission request for UserId: {UserId}", userid);
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpGet("{userId}/exists")]
        public async Task<ActionResult<bool>> GetIfUserExists(string userId)
        {
            _logger.LogInformation("Checking if user exists: {UserId}", userId);

            try
            {
                var exists = await _userMissionMappingService.GetIfUserExists(userId);
                _logger.LogInformation("User {UserId} exists: {Exists}", userId, exists);
                return Ok(exists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user exists: {UserId}", userId);
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpPost]
        public async Task<ActionResult> Create(UserRequestDto data)
        {
            _logger.LogInformation("Creating user mission request for UserId: {UserId}", data.UserId);

            try
            {
                await _userMissionMappingService.CreateUserRequest(data);
                _logger.LogInformation("Successfully created mission request for UserId: {UserId}", data.UserId);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user mission request for UserId: {UserId}", data.UserId);
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpPost("assign")]
        public async Task<ActionResult> Assign(UserMissionResultDto data)
        {
            _logger.LogInformation("Assigning tenant to user request: {RequestId}, TenantId: {TenantId}", data.RequestId, data.MissionId);

            try
            {
                await _userMissionMappingService.AssignTenantToUser(data, User.Identifier());
                _logger.LogInformation("Successfully assigned tenant for user request: {RequestId}", data.RequestId);
                return Ok();
            }
            catch (SqlException e)
            {
                _logger.LogError(e, "SQL error assigning tenant for user request: {RequestId}", data.RequestId);

                if (e.Number == 50000 || e.Number == 50002)
                    return Conflict(e.Message);

                return BadRequest("Database error occurred.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning tenant for user request: {RequestId}", data.RequestId);
                return BadRequest("Internal server error.");
            }
        }

        [HttpPost("pre-assign")]
        public async Task<ActionResult> PreAssignUserToMission(MissionPreAssignmentDto data)
        {
            _logger.LogInformation(
                "Pre-assigning user {UserDetails} to TenantId={TenantId} with RoleId={RoleId}",
                data.UserDetails, data.TenantId, data.RoleId);

            if (string.IsNullOrWhiteSpace(data.UserDetails))
            {
                _logger.LogWarning("Pre-assignment failed: UserDetails is empty.");
                return BadRequest("User email is required.");
            }

            try
            {
                await _userMissionMappingService.CreateMissionPreAssignment(data);

                _logger.LogInformation(
                    "Successfully pre-assigned user {UserDetails}",
                    data.UserDetails);

                return Ok();
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("already pre-assigned", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(
                        "Conflict during pre-assignment for user {UserDetails}",
                        data.UserDetails);

                    return Conflict(ex.Message);
                }

                _logger.LogError(
                    ex,
                    "Error pre-assigning user {UserDetails}",
                    data.UserDetails);

                return StatusCode(500, "Internal server error.");
            }
        }



        [HttpPost("bulk/pre-assign")]
        public async Task<ActionResult<List<string>>> BulkPreAssignUsersToMission([FromBody] List<MissionPreAssignmentDto> data)
        {
            try
            {
                if (data == null || data.Count == 0)
                    return BadRequest("At least one assignment is required.");

                var createdBy = User.GetUserEmailLike();

                foreach (var item in data)
                {
                    item.CreatedBy = createdBy;
                }

                var alreadyExists =
                    await _userMissionMappingService
                        .CreateMissionBulkPreAssignment(data);

                return Ok(alreadyExists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bulk pre-assignment failed");
                return StatusCode(500, "Internal server error during bulk assignment.");
            }
        }

        [HttpGet("pre-assign/pending")]
        public async Task<ActionResult<IEnumerable<PreAssignedUserDto>>> GetPendingPreAssignedUsers()
        {
            try
            {
                var tenantId = User.Tenant(); 

                _logger.LogInformation(
                    "Fetching pending pre-assigned users for TenantId={TenantId}", tenantId);

                var result =
                    await _userMissionMappingService
                        .GetPendingPreAssignedUsers(tenantId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pending pre-assigned users");
                return StatusCode(500, "Internal server error.");
            }
        }




    }
}
