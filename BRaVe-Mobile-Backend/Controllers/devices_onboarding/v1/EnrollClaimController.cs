using BRaVe_Mobile_Backend.DTOs;
using BRaVe_Mobile_Backend.Exceptions;
using BRaVe_Mobile_Backend.Interfaces;
using BRaVe_Mobile_Backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BRaVe_Mobile_Backend.Controllers.devices_onboarding
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public sealed class EnrollClaimController : ControllerBase
    {

        private readonly IClaimService _claimServive;
        private readonly ILogger<EnrollClaimController> _logger;
        private readonly AzureKeyVaultHelper _kvhelper;

        public EnrollClaimController(AzureKeyVaultHelper kvhelper,IClaimService claimSessionServive, ILogger<EnrollClaimController> logger)
        {
            _kvhelper = kvhelper;
            _claimServive = claimSessionServive;
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
                string pubRsaKey =  await _kvhelper.GetRsaPubKeyB64();

                var result = await _claimServive.ClaimAsync(req, ipAddress);

                _logger.LogInformation("Claim successful for SessionCode {SessionCode} from IP {IpAddress}",
                    req.SessionCode, ipAddress);

                return Ok(new ClaimResponse { HouseholdPrefix= result.HouseholdPrefix, InitialId= result.InitialId, JwsToken = result.EnrollmentJws, RefreshToken= result.RefreshToken, PubRsaKeyB64 = pubRsaKey });
            }
            catch (ClaimErrors.NotFound)
            {
                _logger.LogWarning("Claim failed - Session not found or expired. SessionCode: {SessionCode}, IP: {IpAddress}",
                    req.SessionCode, ipAddress);
                return Problem(statusCode: 404, title: "unknown_or_expired_session");
            }
            catch (ClaimErrors.TenantMismatch)
            {
                _logger.LogWarning("Claim failed - Tenant mismatch detected. SessionCode: {SessionCode}, IP: {IpAddress}",
                    req.SessionCode, ipAddress);
                return Problem(statusCode: 404, title: "session_tenant_mismatch");
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
            catch (ClaimErrors.BindingPoolExhausted)
            {
                _logger.LogWarning("Claim failed - Household device signature binding pool exhausted. SessionCode: {SessionCode}, IP: {IpAddress}",
                    req.SessionCode, ipAddress);
                return Problem(statusCode: 430, title: "rate_limited");
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
