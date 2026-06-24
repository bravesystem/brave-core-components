using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.DTOs.RoleManagement;
using BRaVe_Management_Backend.Extensions;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using BRaVe_Management_Backend.Services;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Globalization;

namespace BRaVe_Management_Backend.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class AccessControlController : ControllerBase
    {
        private readonly IRoleService _roleService;
        public AccessControlController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        [HttpGet("assignments")]
        public async Task<ActionResult<IReadOnlyList<UserRoleInfo>>> GetUserRolesAndMissions([FromQuery] string languageCode = "en", [FromQuery] int? tenantId = null, CancellationToken ct = default)
        {
            //string languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            var roles = await _roleService.GetUserRolesAndMissionsAsync(languageCode, tenantId, ct);

            if (roles == null || roles.Count == 0)
                return NotFound("No user roles found");

            return Ok(roles);
        }


        [HttpGet("{userId}/{language}")]
        public async Task<ActionResult<TenantAndRolesDto>> GetUserRoles(string userId,  string language, CancellationToken ct)
        {

            string userDetails = User.GetUserEmailLike();
            string profileName= User.DisplayName();


            try
            {

                var tenantAndRoles = await _roleService.GetRolesAsync(userId, userDetails, profileName, language, ct);

                if (tenantAndRoles.Roles == null || tenantAndRoles.Roles.Count == 0)
                    return NotFound($"No roles found for user {userId}");

                if (tenantAndRoles.TenantId == 0 && tenantAndRoles.Roles != null && tenantAndRoles.Roles.Count > 0 && !tenantAndRoles.Roles.Contains(1))
                    return BadRequest("Invalid request: roles cannot be assigned when no tenant is associated (TenantId = 0).");


                return Ok(tenantAndRoles);

            }
            catch (Exception ex)
            {

                return NotFound(ex.Message);
            }

        }

        [HttpPost("assign-tenant")]
        public async Task<IActionResult> AssignUserToTenant([FromQuery] string userId, [FromQuery] int tenantId, [FromQuery] string createdByUserId, CancellationToken ct)
        {
            await _roleService.AssignUserToTenant(userId, tenantId, createdByUserId, ct);
            return Ok();
        }

        [HttpPost("assign-roles")]
        public async Task<IActionResult> AssignUserRoles([FromQuery] string userId, [FromQuery] int tenantId, [FromQuery] string roleIds, [FromQuery] string createdByUserId, CancellationToken ct)
        {
            await _roleService.AssignUserRoles(userId, tenantId, roleIds, createdByUserId, ct);
            return Ok();
        }

        [HttpDelete("remove-roles")]
        public async Task<IActionResult> RemoveUserRoles([FromQuery] string userId, [FromQuery] int tenantId, [FromQuery] string roleIds, [FromQuery] string deletedByUserId, CancellationToken ct)
        {
            await _roleService.RemoveUserRoles(userId, tenantId, roleIds, deletedByUserId, ct);
            return Ok();
        }


        [HttpGet("temp-access-requests")]
        public async Task<IActionResult> GetTemporaryAccess(CancellationToken ct)
        {
            var result = await _roleService.GetTemporaryAccess(ct);
            return Ok(result);
        }

        [HttpPost("create-temp-access")]
        public async Task<ActionResult<object>> AddTempAccess(
            [FromBody] TempAccessTableDto dto,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.UserId))
                return BadRequest("UserId is required.");

            if (dto.MissionId <= 0 || dto.RoleId <= 0)
                return BadRequest("MissionId and RoleId must be positive.");

            try
            {
                var id = await _roleService.AddTemporaryAccess(dto, ct);
                return CreatedAtAction(nameof(AddTempAccess), new { userId = dto.UserId }, new { TempAccessId = id });
            }
            catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
            {
                // Unique index violation => active temp already exists
                return Conflict("An active temporary access already exists for this user. This should be used before another is created.");
            }
        }


        //Return logged in User Mission and role including temp mission access

        [HttpGet("effective-roles")]
        public async Task<IActionResult> GetEffectiveUserRoles(CancellationToken ct = default)
        {
            var userId = User.Identifier();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            // auto-detect language
            var languageCode = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            var results = await _roleService.GetEffectiveUserRolesInfoAsync(userId, languageCode, ct);

            if (results == null || results.Count == 0)
                return NotFound("No roles found.");

            return Ok(results);
        }


        //Update logout time in TempMission Access
        [HttpPost("update-logout-time")]
        public async Task<IActionResult> UpdateLogoutTime(CancellationToken ct)
        {
            var userId = User.Identifier();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            await _roleService.UpdateLogoutTimeForUserAsync(userId, ct);

            return Ok(new { Message = "Logout time updated." });
        }


        //Get all system users
        [HttpGet("system-users")]
        public async Task<ActionResult<IReadOnlyList<SystemUserDto>>> GetSystemUsers([FromQuery] CancellationToken ct = default)
        {

            var tenantId = User.Tenant();
            var language = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            var users = await _roleService.GetSystemUsersAsync(tenantId, language, ct);

            if (users == null || users.Count == 0)
                return NotFound("No system users found.");

            return Ok(users);
        }

    }
}
