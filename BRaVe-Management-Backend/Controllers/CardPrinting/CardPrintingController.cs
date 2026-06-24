using BRaVe_Management_Backend.DTOs.PrintService;
using BRaVe_Management_Backend.Extensions;
using BRaVe_Management_Backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace BRaVe_Management_Backend.Controllers
{
    [Route("api/v1/card-printing")]
    [ApiController]
    public class CardPrintingController : ControllerBase
    {
        private readonly IPrintService _printService;
        private readonly ILogger<CardPrintingController> _logger;

        public CardPrintingController(IPrintService printService,
                                      ILogger<CardPrintingController> logger)
        {
            _printService = printService;
            _logger = logger;
        }

        // ==========================================
        // CLAIM (LOGIN FOR PRINTING APP)
        // ==========================================
        [HttpPost("claim")]
        public async Task<ActionResult<PrintDeviceClaimResult>> Claim([FromBody] PrintClaimRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.SessionCode) ||
                string.IsNullOrWhiteSpace(req.DeviceId))
            {
                return Problem(statusCode: 400, title: "invalid_request");
            }

            try
            {
                var result = await _printService.ClaimPrintAsync(req.SessionCode, req.DeviceId);

                _logger.LogInformation("Printing device {DeviceId} claimed session {SessionId}",
                    req.DeviceId, result.SessionId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Print claim failed for device {DeviceId}", req.DeviceId);
                return Problem(statusCode: 400, title: ex.Message);
            }
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<RefreshResponse>> Refresh([FromBody] RefreshPrintRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.RefreshToken))
                return Problem(statusCode: 400, title: "invalid_request");

            try
            {
                var result = await _printService.RefreshAsync(req.RefreshToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Refresh failed");
                return Problem(statusCode: 401, title: ex.Message);
            }
        }



        //LOAD PRINTING RECORDS

        [HttpPost("load")]
        [Authorize]
        public async Task<ActionResult<List<CardPrintQueueItem>>> LoadQueue([FromBody] CardPrintQueueRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.DeviceId))
                return Problem(statusCode: 400, title: "device_id_required");

            try
            {

                //--------------------------------------------------
                //  GET TENANT FROM TOKEN
                //--------------------------------------------------
                var tenantClaim = User.FindFirst("tenantId")?.Value;

                if (tenantClaim == null)
                    return Problem(statusCode: 401, title: "invalid_token");

                int tenantId = int.Parse(tenantClaim);

                //--------------------------------------------------
                // CALL SERVICE
                //--------------------------------------------------
                var result = await _printService.LoadPrintQueueAsync(
                    tenantId: tenantId,
                    householdIds: req.HouseholdIds,
                    activityCode: req.ActivityCode,
                    deviceId: req.DeviceId,
                    user: req.WindowsUser
                );

                _logger.LogInformation(
                    "Loaded {Count} print records for device {DeviceId}",
                    result.Count,
                    req.DeviceId
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load print queue");
                return Problem(statusCode: 500, title: ex.Message);
            }
        }


        //UPDATE PRINTED RECORDS

        [HttpPost("sync")]
        [Authorize]
        public async Task<ActionResult> SyncPrinted([FromBody] SyncPrintedRequest req)
        {
            if (req == null || req.HouseholdIds == null || req.HouseholdIds.Count == 0)
                return Problem(statusCode: 400, title: "no_records_to_sync");

            if (string.IsNullOrWhiteSpace(req.DeviceId))
                return Problem(statusCode: 400, title: "device_id_required");

            try
            {
                //--------------------------------------------------
                // GET TENANT FROM TOKEN
                //--------------------------------------------------
                var tenantClaim = User.FindFirst("tenantId")?.Value;

                if (tenantClaim == null)
                    return Problem(statusCode: 401, title: "invalid_token");

                int tenantId = int.Parse(tenantClaim);

                //--------------------------------------------------
                // USER (who printed)
                //--------------------------------------------------
                var printedBy = req.WindowsUser;

                //--------------------------------------------------
                // CALL SERVICE
                //--------------------------------------------------
                await _printService.SyncPrintedRecordsAsync(
                    tenantId,
                    req.HouseholdIds,
                    printedBy
                );

                _logger.LogInformation(
                    "Synced {Count} printed records for device {DeviceId}",
                    req.HouseholdIds.Count,
                    req.DeviceId
                );

                return Ok(new { message = "sync_success" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync printed records");
                return Problem(statusCode: 500, title: ex.Message);
            }
        }


        [HttpGet("printed-records")]
        [Authorize]
        public async Task<ActionResult<List<PrintedCardSummaryDto>>> GetPrintedRecords([FromQuery] DateTime? startDate,[FromQuery] DateTime? endDate)
        {
            try
            {
                //--------------------------------------------------
                // GET TENANT 
                //--------------------------------------------------
                int tenantId = User.Tenant();

                //--------------------------------------------------
                // CALL SERVICE
                //--------------------------------------------------
                var result = await _printService.GetPrintedRecordsAsync(
                    tenantId,
                    startDate,
                    endDate
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load printed records");
                return Problem(statusCode: 500, title: ex.Message);
            }
        }



        // ==========================================
        // UNLOCK PRINT QUEUE RECORDS
        // ==========================================
        [HttpPost("unlock")]
        [Authorize]
        public async Task<ActionResult> UnlockQueue(
            [FromBody] SyncPrintedRequest req)
        {
            if (req == null ||
                req.HouseholdIds == null ||
                req.HouseholdIds.Count == 0)
            {
                return Problem(
                    statusCode: 400,
                    title: "no_records_provided");
            }

            try
            {
                //--------------------------------------------------
                // CALL SERVICE
                //--------------------------------------------------
                await _printService.UnlockPrintQueueRecordsAsync(
                    req.HouseholdIds,req.WindowsUser
                );

                _logger.LogInformation(
                    "Unlocked {Count} queue records",
                    req.HouseholdIds.Count
                );

                return Ok(new
                {
                    message = "unlock_success"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to unlock print queue records");

                return Problem(
                    statusCode: 500,
                    title: ex.Message);
            }
        }

        [HttpPost("authorize-reprint")]
        [Authorize]
        public async Task<ActionResult> AuthorizeReprint([FromBody] AuthorizeReprintRequest req)
        {
            if (req == null || req.CardId <= 0 || string.IsNullOrWhiteSpace(req.Reason))
                return Problem(statusCode: 400, title: "invalid_request");

            try
            {
                //--------------------------------------------------
                // Get user 
                //--------------------------------------------------
                var user = User.Identity?.Name ?? req.AuthorizedBy;

                //--------------------------------------------------
                // CALL SERVICE
                //--------------------------------------------------
                await _printService.AuthorizeReprintAsync(
                    req.CardId,
                    user,
                    req.Reason
                );

                _logger.LogInformation(
                    "Reprint authorized for CardId {CardId} by {User}",
                    req.CardId,
                    user
                );

                return Ok(new { message = "authorization_success" });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to authorize reprint for CardId {CardId}", req.CardId);

                return Problem(statusCode: 400, title: ex.Message);
            }
        }
    }
}