using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Extensions;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Interfaces.uat_login;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BRaVe_Management_Backend.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class FormAccessController : ControllerBase
    {
        private readonly IUatFormAccessService _uatFormAccessService;
        private readonly ILogger<FormAccessController> _logger;
        private readonly IUatFormAccessService _roleService;

        public FormAccessController(IUatFormAccessService uatFormAccessService,
             ILogger<FormAccessController> logger, IUatFormAccessService roleService)
        {
            
            _uatFormAccessService = uatFormAccessService;
            _logger = logger;
            _roleService = roleService;
        }


        [HttpGet("roles/{userId}/{language}")]
        public async Task<ActionResult<TenantAndRolesDto>> GetUserRoles(string userId, string language, CancellationToken ct)
        {

            string userDetails = User.GetUserEmailLike();
            string profileName = User.DisplayName();


            try
            {

                var tenantAndRoles = await _roleService.GetRolesAsync(userId, language, ct);

                if (tenantAndRoles.Roles == null || tenantAndRoles.Roles.Count == 0)
                    return NotFound($"No roles found for user {userId}");

                if (tenantAndRoles.TenantId == 0 && tenantAndRoles.Roles != null && tenantAndRoles.Roles.Count > 0 && !tenantAndRoles.Roles.Contains(1))
                    return BadRequest("Invalid request: roles cannot be assigned when no tenant is associated (TenantId = 0).");


                return Ok(tenantAndRoles);

            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Failed to fetch roles");

                return NotFound(ex.Message);
            }

        }

        /// <summary>
        /// Registers a new UAT form user using an admin token.
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromBody] UatFormRegisterDto dto,
            CancellationToken ct)
        {
            var result = await _uatFormAccessService.RegisterAsync(
                dto.Email,
                dto.DisplayName,
                dto.Password,
                dto.AdminToken,
                ct);

            if (result == null)
            {
                return BadRequest("Registration failed.");
            }

            return Ok(result);
        }

        /// <summary>
        /// Logs in an existing UAT form user.
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] UatFormLoginDto dto,
            CancellationToken ct)
        {
            try
            {
                var result = await _uatFormAccessService.LoginAsync(
                                dto.Email,
                                dto.Password,
                                ct);

                if (result == null)
                {
                    return Unauthorized();
                }

                return Ok(result);
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "User was unable to login");
                return BadRequest(ex.Message);
            }
            
        }

        /// <summary>
        /// Resets password using a token-based flow.
        /// </summary>
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(
            [FromBody] UatFormResetPasswordDto dto,
            CancellationToken ct)
        {
            var ok = await _uatFormAccessService.ResetPasswordAsync(
                dto.Email,
                dto.NewPassword,
                dto.TenantId,
                dto.Token,
                ct);

            if (!ok)
            {
                return BadRequest("Password reset failed.");
            }

            return Ok();
        }

        /// <summary>
        /// Resets password directly by an administrator (no token).
        /// </summary>
        [HttpPost("reset-password-admin")]
        public async Task<IActionResult> ResetPasswordByAdmin(
            [FromBody] UatFormResetPasswordAdminDto dto,
            CancellationToken ct)
        {
            var ok = await _uatFormAccessService.ResetPasswordByAdminAsync(
                dto.Email,
                dto.NewPassword,
                dto.TenantId,
                ct);

            if (!ok)
            {
                return BadRequest("Admin password reset failed.");
            }

            return Ok();
        }

        /// <summary>
        /// Activates a UAT form user.
        /// </summary>
        [HttpPost("activate")]
        public async Task<IActionResult> Activate(
            [FromBody] UatFormEmailOnlyDto dto,
            CancellationToken ct)
        {
            var ok = await _uatFormAccessService.ActivateUserAsync(dto.Email, dto.TenantId, ct);

            if (!ok)
            {
                return BadRequest("Activation failed.");
            }

            return Ok();
        }

        /// <summary>
        /// Deactivates a UAT form user.
        /// </summary>
        [HttpPost("deactivate")]
        public async Task<IActionResult> Deactivate(
            [FromBody] UatFormEmailOnlyDto dto,
            CancellationToken ct)
        {
            var ok = await _uatFormAccessService.DeactivateUserAsync(dto.Email, dto.TenantId, ct);

            if (!ok)
            {
                return BadRequest("Deactivation failed.");
            }

            return Ok();
        }
    }
}
