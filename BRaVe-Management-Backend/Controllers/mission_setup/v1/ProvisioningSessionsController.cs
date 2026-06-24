using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.DTOs.claim_session;
using BRaVe_Management_Backend.Exceptions;
using BRaVe_Management_Backend.Extensions;
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BRaVe_Management_Backend.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class ProvisioningSessionsController : ControllerBase
    {
        private readonly IClaimService _claimSessionService;
        private readonly ILogger<ProvisioningSessionsController> _logger;

        public ProvisioningSessionsController(IClaimService claimSessionService,
                                              ILogger<ProvisioningSessionsController> logger)
        {
            _claimSessionService = claimSessionService;
            _logger = logger;
        }

        // POST /api/admin/sessions
        [HttpPost]
        public async Task<ActionResult<CreateSessionResponse>> Create( CreateSessionRequest req)
        {

            int TenantId = User.Tenant();

            _logger.LogInformation("Received request to create provisioning session for TenantId={TenantId}, MaxClaims={MaxClaims}, TtlMinutes={Ttl}, PolicyVersion={PolicyVersion}, Label={Label}",
               TenantId, req.MaxClaims, req.TtlMinutes, req.PolicyVersion, req.Label);


            if (req.MaxClaims <= 0)
                return Problem(statusCode: 400, title: "invalid_request");


            int ttl = Math.Clamp(req.TtlMinutes ?? 120, 5, 7 * 24 * 60);
            int policy = req.PolicyVersion is > 0 ? req.PolicyVersion!.Value : 1;

            string sessionCode;
            byte[] sessionCodeHash;

            for (int tries = 0; ; tries++)
            {
                sessionCode = CodeGen.SessionCode(prefix: null, bodyLen: 4, addCheckDigit: true);
                sessionCodeHash = Crypto.Sha256Bytes(sessionCode);

                try
                {

                    var record = new ClaimSessionRecord
                    {
                        TenantId = TenantId,
                        Label = req.Label ?? "",
                        SessionCodeHash = sessionCodeHash,
                        PolicyVersion = policy,
                        MaxClaims = req.MaxClaims,
                        ClaimsIssued = 0,
                        ExpiresAtUtc = DateTime.UtcNow.AddMinutes(ttl),
                        Status = 1 // Active
                    };

                    long sessionId = await _claimSessionService.CreateAsync(record);

                    _logger.LogInformation("Provisioning session created successfully. SessionId={SessionId}, TenantId={TenantId}, SessionCode={SessionCode}",
                        sessionId, TenantId, sessionCode);

                    return Ok(new CreateSessionResponse
                    {
                        SessionId = sessionId,
                        SessionCode = sessionCode,
                        TenantId = TenantId,
                        PolicyVersion = policy,
                        MaxClaims = req.MaxClaims,
                        ClaimsIssued = 0,
                        ExpiresAtUtc = record.ExpiresAtUtc,
                        Status = "Active"
                    });
                }
                catch (SqlUniqueConstraintViolationException ex)
                {
                    _logger.LogWarning(ex, "Unique constraint violation while generating SessionCode={SessionCode}. Attempt {Try}", sessionCode, tries + 1);

                    if (tries > 3)
                    {
                        _logger.LogError(ex, "Failed to create provisioning session after {Tries} attempts", tries + 1);
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error while creating provisioning session for TenantId={TenantId}", TenantId);
                    throw;
                }
            }
        }


        // ================================
        // CREATE PRINT SESSION
        // ================================
        [HttpPost("Print")]
        public async Task<ActionResult<CreatePrintSessionResponse>> Create(CreatePrintSessionRequest req)
        {
            int tenantId = User.Tenant();

            if (req.MaxDevices <= 0)
                return Problem(statusCode: 400, title: "invalid_request");

            string sessionCode;
            byte[] sessionCodeHash;

            for (int tries = 0; ; tries++)
            {
                sessionCode = CodeGen.SessionCode(prefix: "P", bodyLen: 4, addCheckDigit: true);
                sessionCodeHash = Crypto.Sha256Bytes(sessionCode);

                try
                {
                    var record = new PrintSessionRecordCreate
                    {
                        TenantId = tenantId,
                        Label = req.Label ?? "",
                        SessionCodeHash = sessionCodeHash,
                        MaxDevices = req.MaxDevices
                    };

                    long sessionId = await _claimSessionService.CreatePrintSessionAsync(record);

                    _logger.LogInformation("Print session created. SessionId={SessionId}, TenantId={TenantId}",
                        sessionId, tenantId);

                    return Ok(new CreatePrintSessionResponse
                    {
                        SessionId = sessionId,
                        SessionCode = sessionCode,
                        TenantId = tenantId,
                        MaxDevices = req.MaxDevices,
                        DevicesConnected = 0,
                        Status = "Active"
                    });
                }
                catch (SqlUniqueConstraintViolationException ex)
                {
                    _logger.LogWarning(ex, "SessionCode collision: {SessionCode}", sessionCode);

                    if (tries > 3)
                        throw;
                }
            }
        }

        // ================================
        // GET ALL PRINT SESSIONS
        // ================================
        [HttpGet("Print")]
        public async Task<ActionResult<IEnumerable<PrintSessionDto>>> GetAll()
        {
            int tenantId = User.Tenant();

            var sessions = await _claimSessionService.GetPrintSessionsAsync(tenantId);

            return Ok(sessions);
        }

        // ================================
        // REVOKE SESSION
        // ================================
        [HttpPost("print/{sessionId}/revoke")]
        public async Task<IActionResult> Revoke(long sessionId)
        {
            int tenantId = User.Tenant();

            await _claimSessionService.RevokePrintSessionAsync(sessionId, tenantId);

            _logger.LogInformation("Print session revoked. SessionId={SessionId}", sessionId);

            return Ok(new { message = "Session revoked" });
        }
    }
}

