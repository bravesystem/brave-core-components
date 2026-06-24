using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Extensions;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BRaVe_Management_Backend.Controllers.mission_operation
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class FlaggedBeneficiariesController : ControllerBase
    {
        private readonly IFlaggedBeneficiariesService _flaggedBeneficiariesService;
        private readonly ILogger<FlaggedBeneficiariesController> _logger;

        public FlaggedBeneficiariesController(IFlaggedBeneficiariesService flaggedBeneficiariesService, ILogger<FlaggedBeneficiariesController> logger)
        {
            _flaggedBeneficiariesService = flaggedBeneficiariesService;
            _logger = logger;
        }


        [HttpGet]
        public async Task<ActionResult<IEnumerable<FlaggedBeneficiary>>> GetAll()
        {
            try
            {
                int TenantId = User.Tenant();
                _logger.LogInformation("Fetching all flagged beneficiaries");
                var beneficiaries = await _flaggedBeneficiariesService.GetAll(TenantId);
                return Ok(beneficiaries);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error fetching all flagged beneficiaries");
                return BadRequest(ex.Message);
            }
            
        }

        [HttpPost]
        public async Task<IActionResult> BulkCreate(
           [FromBody] CreateFlaggedBeneficiaryRequest request)
        { 
            try
            {
                await _flaggedBeneficiariesService
                    .InsertFlaggedBeneficiaries(request);

                return Ok(new
                {
                    success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error creating flagged beneficiaries.");

                return BadRequest(ex.Message);
            }
        }

        [HttpPost("GroupSelected")]
        public async Task<IActionResult> GroupSelected(
            [FromBody] List<string> beneficiaryIds)
        {
            try
            {
                int tenantId = User.Tenant();

                _logger.LogInformation(
                    "Grouping selected beneficiaries. Total: {Count}",
                    beneficiaryIds.Count);

                string code = await _flaggedBeneficiariesService.Group(
                    tenantId,
                    beneficiaryIds);

                return Ok(new
                {
                    success = true,
                    data = code
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while grouping selected beneficiaries.");

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    "Failed to group beneficiaries.");
            }
        }

        [HttpPost("AutoPartition")]
        public async Task<IActionResult> AutoPartition([FromBody] AutoPartitionParams autoPartition)
        {
            try
            {
                int tenantId = User.Tenant();

                _logger.LogInformation(
                    "Auto partitioning beneficiaries. " +
                    "Groups: {Groups}, Criterion: {Criterion}",
                    autoPartition.Groups,
                    autoPartition.Criterion);

                await _flaggedBeneficiariesService.AutoPartition(
                    tenantId,
                    autoPartition);

                return Ok(new
                {
                    success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while auto partitioning beneficiaries.");

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    "Failed to auto partition beneficiaries.");
            }
        }

    }
}
