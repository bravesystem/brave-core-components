using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BRaVe_Management_Backend.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public sealed class EnrollClaimController : ControllerBase
    {
        private readonly IClaimService _claimSessionServive;
        private readonly ILogger<EnrollClaimController> _logger;

        public EnrollClaimController(IClaimService claimSessionServive, ILogger<EnrollClaimController> logger)
        {
            _claimSessionServive = claimSessionServive;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<ClaimResponse>> Claim([FromBody] ClaimRequest req)
        {
            if (!ClaimRequest.IsValid(req))
            {
                _logger.LogWarning("Invalid claim request received from IP {IpAddress}",
                    HttpContext.Connection.RemoteIpAddress?.ToString());
                return Problem(statusCode: 400, title: "invalid_request");
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            _logger.LogInformation("Processing claim request for SessionCode {SessionCode} from IP {IpAddress}",
                req.SessionCode, ipAddress);

            try
            {
                var result = await _claimSessionServive.ClaimAsync(req, ipAddress);

                _logger.LogInformation("Claim successful for SessionCode {SessionCode} from IP {IpAddress}",
                    req.SessionCode, ipAddress);

                return Ok(new ClaimResponse { EnrollmentJws = result.EnrollmentJws });
            }
            catch (ClaimErrors.NotFound)
            {
                _logger.LogWarning("Claim failed - Session not found or expired. SessionCode: {SessionCode}, IP: {IpAddress}",
                    req.SessionCode, ipAddress);
                return Problem(statusCode: 404, title: "unknown_or_expired_session");
            }
            catch (ClaimErrors.CapacityExceeded)
            {
                _logger.LogWarning("Claim failed - Session capacity exceeded. SessionCode: {SessionCode}, IP: {IpAddress}",
                    req.SessionCode, ipAddress);
                return Problem(statusCode: 410, title: "session_capacity_exceeded");
            }
            catch (ClaimErrors.DuplicateDevice)
            {
                _logger.LogWarning("Claim failed - Device already claimed. SessionCode: {SessionCode}, IP: {IpAddress}",
                    req.SessionCode, ipAddress);
                return Problem(statusCode: 409, title: "device_already_claimed");
            }
            catch (ClaimErrors.AttestationFailed)
            {
                _logger.LogError("Claim failed - Attestation failed. SessionCode: {SessionCode}, IP: {IpAddress}",
                    req.SessionCode, ipAddress);
                return Problem(statusCode: 403, title: "attestation_failed");
            }
            catch (ClaimErrors.RateLimited)
            {
                _logger.LogWarning("Claim failed - Rate limited. SessionCode: {SessionCode}, IP: {IpAddress}",
                    req.SessionCode, ipAddress);
                return Problem(statusCode: 429, title: "rate_limited");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred during claim. SessionCode: {SessionCode}, IP: {IpAddress}",
                    req.SessionCode, ipAddress);
                return Problem(statusCode: 500, title: "internal_server_error");
            }
        }
    }
}
