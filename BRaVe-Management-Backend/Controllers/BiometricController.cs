using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Extensions;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BRaVe_Management_Backend.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class BiometricMatchesController : ControllerBase
    {
        private readonly ISqlBiometricService _biometricService;
        private readonly ILogger<BiometricMatchesController> _logger;

        public BiometricMatchesController(
            ISqlBiometricService biometricService,
            ILogger<BiometricMatchesController> logger)
        {
            _biometricService = biometricService;
            _logger = logger;
        }


        // GET: api/v1/biometricmatches/details?sourceUuid=xxx&matchedUuid=yyy
        [HttpGet("details")]
        public async Task<ActionResult<BiometricMatchResultDto>> GetDetails(
            [FromQuery] Guid sourceUuid,
            [FromQuery] Guid matchedUuid)
        {
            int tenantId = User.Tenant();

            try
            {
                var result =
                    await _biometricService.GetBiometricMatchDetailsAsync(
                        tenantId,
                        sourceUuid,
                        matchedUuid
                    );

                if (result == null)
                    return NotFound("Match not found.");

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error loading match details | Tenant {TenantId} | Source {SourceUuid} | Matched {MatchedUuid}",
                    tenantId,
                    sourceUuid,
                    matchedUuid
                );

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    "Failed to load match details"
                );
            }
        }

        // GET: api/v1/biometricmatches/manual-details?sourceUuid=xxx&matchedUuid=yyy
        [HttpGet("manual-details")]
        public async Task<ActionResult<BiometricMatchResultDto>> GetManualDetails(
            [FromQuery] Guid sourceUuid,
            [FromQuery] Guid matchedUuid)
        {
            int tenantId = User.Tenant();

            try
            {
                var result =
                    await _biometricService.GetManualMatchDetailsAsync(
                        tenantId,
                        sourceUuid,
                        matchedUuid
                    );

                if (result == null)
                    return NotFound("Manual match not found.");

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error loading manual match details | Tenant {TenantId} | Source {SourceUuid} | Matched {MatchedUuid}",
                    tenantId,
                    sourceUuid,
                    matchedUuid
                );

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    "Failed to load manual match details"
                );
            }
        }


        // GET: api/v1/biometricmatches/list?pageNumber=1&pageSize=10
        [HttpGet("list")]
        public async Task<ActionResult<PagedResult<BiometricMatchResultDto>>> GetMatchList([FromQuery] int pageNumber = 1,[FromQuery] int pageSize = 10,[FromQuery] string? activity = null,[FromQuery] DateTime? dateFrom = null, [FromQuery] DateTime? dateTo = null, [FromQuery] int? minScore = null)
        {
            int tenantId = User.Tenant();

            try
            {
                var result =
                      await _biometricService.GetBiometricMatchListAsync(
                          tenantId,
                          pageNumber,
                          pageSize,
                          activity,
                          dateFrom,
                          dateTo,
                          minScore);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error loading biometric match list for Tenant {TenantId}",
                    tenantId
                );

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    "Failed to load biometric match list"
                );
            }
        }


        // GET: api/v1/biometricmatches/verifications?pageNumber=1&pageSize=10
        [HttpGet("verifications")]
        public async Task<ActionResult<PagedResult<BiometricVerificationResultDto>>> GetVerifications(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            int tenantId = User.Tenant();

            try
            {
                var result =
                    await _biometricService.GetBiometricVerificationsAsync(
                        tenantId,
                        pageNumber,
                        pageSize
                    );

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error loading biometric verifications for Tenant {TenantId}",
                    tenantId
                );

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    "Failed to load biometric verifications"
                );
            }
        }

        [HttpGet("duplicate-indicators")]
        public async Task<IActionResult> GetDuplicateIndicators(int? tenantId,string languageCode = "en")
        {
            try
            {
                _logger.LogInformation(
                    "GetDuplicateIndicators called | TenantId={TenantId}, LanguageCode={LanguageCode}",
                    tenantId, languageCode);

                var result = await _biometricService
                    .GetDuplicateIndicatorChecklistAsync(tenantId, languageCode);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrieving duplicate indicators | TenantId={TenantId}",
                    tenantId);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        message = "Failed to retrieve duplicate indicators."
                    });
            }
        }


        [HttpGet("programmatic-data")]
        public async Task<IActionResult> GetProgrammaticData([FromQuery] string householdId)
        {


            var tenantId = User.Tenant();
            try
            {
                _logger.LogInformation(
                    "GetProgrammaticData called | TenantId={TenantId}, HouseholdId={HouseholdId}",
                    tenantId, householdId);

                var result = await _biometricService
                    .GetProgrammaticDataAsync(tenantId, householdId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrieving programmatic data | TenantId={TenantId}, HouseholdId={HouseholdId}",
                    tenantId, householdId);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        message = "Failed to retrieve programmatic data."
                    });
            }
        }


        [HttpGet("activities")]
        public async Task<ActionResult<List<string>>> GetAvailableActivities()
        {
            int tenantId = User.Tenant();

            try
            {
                _logger.LogInformation(
                    "GetAvailableActivities called | TenantId={TenantId}",
                    tenantId);

                var activities =
                    await _biometricService.GetAvailableMatchActivitiesAsync(tenantId);

                return Ok(activities);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrieving biometric match activities | TenantId={TenantId}",
                    tenantId);

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    "Failed to load activity codes"
                );
            }
        }


    }
}
