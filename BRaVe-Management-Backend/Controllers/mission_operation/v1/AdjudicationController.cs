using BRaVe_Management_Backend.Interfaces;
using Microsoft.AspNetCore.Mvc;
using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Extensions;
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces.jobs;

namespace BRaVe_Management_Backend.Controllers.search_analytics
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class AdjudicationController : ControllerBase
    {
        private readonly IAdjudicationService _adjudicationService;
        private readonly ISqlBiometricService _biometricService;
        //private readonly IProcessStagingJobQueue _queue;
        private readonly IServiceBusSender _bus;
        private readonly ILogger<AdjudicationController> _logger;

        public AdjudicationController(
            IAdjudicationService adjudicationService,
            ISqlBiometricService biometricService,
            IServiceBusSender bus,
            ILogger<AdjudicationController> logger)
        {
            _adjudicationService = adjudicationService;
            _biometricService = biometricService;
            _bus = bus;
            _logger = logger;
        }

        [HttpGet("decisions")]
        public async Task<IActionResult> GetDecisions(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("API request: Fetch adjudication decisions");

                var data = await _adjudicationService
                    .GetAdjudicationDecisionsAsync(cancellationToken);

                return Ok(new
                {
                    success = true,
                    data = data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"Error while fetching adjudication decisions. {ex.Message}");

                return StatusCode(StatusCodes.Status500InternalServerError,
                    new
                    {
                        success = false,
                        ErrorMessage = "Error while fetching adjudication decisions."
                    });
            }
        }


        [HttpPost("adjudicate")]
        public async Task<IActionResult> Adjudicate([FromBody] AdjudicationRequestDto request,CancellationToken cancellationToken)
        {
            try
            {
                request.TenantId =User.Tenant();

                request.AdjudicatedBy =User.GetUserEmailLike();

                var result = await _adjudicationService.AdjudicateAsync(request, cancellationToken);

                //send job to matcher
                if (request.DecisionId == (int)AdjudicationDecision.TrueDuplicateSwap) //swap
                {
                    //remove MatchedUuid  from matcher
                    _logger.LogInformation($"Adjudication - Removing Biometric Id {request.MatchedUuid.ToString()} from matcher");
                    Guid removalJob = await _biometricService.RemoveBiometricMatchingRequest(request.TenantId, request.MatchedUuid);
                    //_queue.Enqueue(removalJob);
                    _bus.SendMessageAsync(removalJob.ToString());

                    //add SourceUuid to matcher
                    _logger.LogInformation($"Adjudication - Adding Biometric Id {request.SourceUuid.ToString()} to matcher");
                    Guid reprocessJob = await _biometricService.ReprocessBiometricMatchingRequest(request.TenantId, request.SourceUuid);
                    _bus.SendMessageAsync(reprocessJob.ToString());

                }

                return Ok(new
                {
                    success = true,
                    data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Adjudication failed");

                return StatusCode(500, new
                {
                    success = false,
                    message = "Adjudication failed."
                });
            }
        }


        [HttpPost("adjudicate-bulk")]
        public async Task<IActionResult> AdjudicateBulk([FromBody] List<AdjudicationRequestDto> requests,CancellationToken cancellationToken)
        {
            try
            {
                if (requests == null || requests.Count == 0)
                    return BadRequest(new
                    {
                        success = false,
                        message = "No adjudication records provided."
                    });

                var tenantId = User.Tenant();
                var user = User.GetUserEmailLike();

                foreach (var r in requests)
                {
                    r.TenantId = tenantId;
                    r.AdjudicatedBy = user;
                }

                _logger.LogInformation(
                    "API request: BULK adjudication {Count} records JobId={JobId}",
                    requests.Count,
                    requests.First().JobId);

                await _adjudicationService.BulkAdjudicateAsync(requests, cancellationToken);

                return Ok(new
                {
                    success = true,
                    message = $"{requests.Count} records adjudicated successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bulk adjudication failed");

                return StatusCode(500, new
                {
                    success = false,
                    message = "Bulk adjudication failed."
                });
            }
        }

        [HttpGet("member-match-history/{memberUuid}")]
        public async Task<IActionResult> GetMemberMatchHistory(Guid memberUuid,CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "API request: Fetch member match history for {MemberUuid}",
                    memberUuid);

                var data = await _adjudicationService
                    .GetMemberMatchHistoryAsync(memberUuid, cancellationToken);

                return Ok(new
                {
                    success = true,
                    data = data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error fetching match history for {MemberUuid}",
                    memberUuid);

                return StatusCode(StatusCodes.Status500InternalServerError,
                    new
                    {
                        success = false,
                        message = "Error fetching member match history."
                    });
            }
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetAdjudicationHistory(
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] int? decisionId = null,
    [FromQuery] string? dedupMode = null,
    [FromQuery] DateTime? dateFrom = null,
    [FromQuery] DateTime? dateTo = null,
    [FromQuery] string? adjudicatedBy = null,
    CancellationToken cancellationToken = default)
        {
            try
            {
                int tenantId = User.Tenant();

                _logger.LogInformation(
                    "API request: Fetch adjudication history Page={PageNumber}",
                    pageNumber);

                var result = await _adjudicationService
                    .GetAdjudicationHistoryAsync(
                        tenantId,
                        pageNumber,
                        pageSize,
                        decisionId,
                        dedupMode,
                        dateFrom,
                        dateTo,
                        adjudicatedBy,
                        cancellationToken);

                return Ok(new
                {
                    success = true,
                    data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error fetching adjudication history");

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        success = false,
                        message = "Error fetching adjudication history."
                    });
            }
        }
    }
}