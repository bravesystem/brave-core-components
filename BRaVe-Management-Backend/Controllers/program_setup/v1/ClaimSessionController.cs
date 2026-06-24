using BRaVe_Management_Backend.Controller;
using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Extensions;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BRaVe_Management_Backend.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class ClaimSessionController : ControllerBase
    {
        private readonly IClaimSessionService _claimSessionService;
        private readonly ILogger<ClaimSessionController> _logger;
        public ClaimSessionController(IClaimSessionService claimSessionService, ILogger<ClaimSessionController> logger)
        {
            _claimSessionService = claimSessionService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ClaimSession>>> GetAllClaimSessions()
        {
            int tenantId = User.Tenant();
            return Ok(await _claimSessionService.GetAllClaimSessions(tenantId));
        }


        

        [HttpGet("{id}")]
        public async Task<ActionResult<ClaimSession>> GetClaimSessionById(int id)
        {
            return Ok(await _claimSessionService.GetClaimSessionById(id));
        }

      


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatedClaimSession(int id, ClaimSessionDto data)
        {
            ClaimSession claimSession = new ClaimSession()
            {
                //TenantId = Convert.ToInt32(User.Tenant),
                SessionId = id,
                Label = data.Label,
                SessionCodeHash = data.SessionCodeHash,
                MaxClaims = data.MaxClaims,
                ClaimsIssued = data.ClaimsIssued,
                ExpiresAtUtc = data.ExpiresAtUtc,
                Status = data.Status
            };

            _claimSessionService.UpdateClaimSession(claimSession, User.Identifier());

            return Ok();
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteClaimSession(int id)
        {
            _logger.LogInformation("Deleting ClaimSession {ClaimSessionId} for user {UserId}", id, User.Identifier());

            try
            {
                await _claimSessionService.RevokeClaimSession(User.Identifier(), id);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Claim Session {SessionId} for user {UserId}", id, User.Identifier());
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
