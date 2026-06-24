using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Exceptions;
using BRaVe_Management_Backend.Extensions;
using BRaVe_Management_Backend.Helpers;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

/// i will dispose this once i have my logic inplace
/// // incase i forget any person who comes across it in to consult me for deletion   BERNARD
namespace BRaVe_Management_Backend.Controller
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class TargetingRulesController : ControllerBase
    {
        private readonly ITargetingRulesService _targetingService;
        private readonly ILogger<TargetingRulesController> _logger;

        public TargetingRulesController(
            ITargetingRulesService targetingService,
            ILogger<TargetingRulesController> logger)
        {
            _targetingService = targetingService;
            _logger = logger;
        }

        // ===================== CREATE =====================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TargetingRuleDto dto)
        {
            _logger.LogInformation(
                "Creating targeting rule. Name={RuleName}, TenantId={TenantId}, WeightBound={WeightBound}",
                dto.RuleName,
                dto.TenantId,
                dto.WeightBound);

            var ruleId = await _targetingService.CreateAsync(dto);

            _logger.LogInformation(
                "Targeting rule created successfully. RuleId={RuleId}",
                ruleId);

            return Ok(ruleId);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] TargetingRuleDto dto)
        {
            _logger.LogInformation(
                "Updating targeting rule. RuleId={RuleId}, Name={RuleName}, TenantId={TenantId}, WeightBound={WeightBound}",
                id,
                dto.RuleName,
                dto.TenantId,
                dto.WeightBound);

            await _targetingService.UpdateAsync(id, dto);

            _logger.LogInformation(
                "Targeting rule updated successfully. RuleId={RuleId}",
                id);

            return NoContent();
        }

        // ===================== READ =====================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("Fetching all targeting rules");

            var rules = await _targetingService.GetAllAsync(User.Tenant());

            _logger.LogInformation(
                "Retrieved {Count} targeting rules",
                rules?.Count() ?? 0);

            return Ok(rules);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            _logger.LogInformation("Fetching targeting rule by id {RuleId}", id);

            var rule = await _targetingService.GetByIdAsync(id);

            if (rule == null)
            {
                _logger.LogWarning("Targeting rule not found. RuleId={RuleId}", id);
                return NotFound();
            }

            _logger.LogInformation("Targeting rule retrieved. RuleId={RuleId}", id);
            return Ok(rule);
        }

        // ===================== UPDATE =====================
      
        // ===================== DELETE =====================
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogWarning(
                "Deleting targeting rule. RuleId={RuleId}",
                id);

            await _targetingService.DeleteAsync(id);

            _logger.LogInformation(
                "Targeting rule deleted successfully. RuleId={RuleId}",
                id);

            return NoContent();
        }

        // ===================== LOOKUPS =====================
        [HttpGet("fields")]
        public async Task<IActionResult> GetTargetingFields()
        {
            var tenantId = User.Tenant();
            _logger.LogInformation("Fetching targeting rule fields");

            var fields = await _targetingService.GetAllTargetingFieldsAsync(tenantId);

          
            _logger.LogInformation(
                "Retrieved {Count} targeting fields",
                fields?.Count() ?? 0);

            return Ok(fields);
        }

        [HttpGet("field-data")]
        public async Task<IActionResult> GetFieldData([FromQuery] string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return BadRequest("Field name is required.");

            try
            {
                var values = await _targetingService.GetDistinctValuesForFieldAsync(displayName);
                return Ok(values);
            }
            catch (ArgumentException argEx)
            {
                _logger.LogWarning(argEx, "Invalid field requested: {Field}", displayName);
                return BadRequest(argEx.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving values for field {Field}", displayName);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("preview")]
        public async Task<IActionResult> Preview([FromBody] JsonElement ruleJson)
        {
            var results = await _targetingService.PreviewAsync(ruleJson);
            return Ok(results);
        }

        [HttpPost("enroll-beneficiaries")]

        public async Task<IActionResult> EnrollBeneficiaries([FromBody] EnrollBeneficiariesDto dto)
        {

            var tenantId = User.Tenant();
            _logger.LogInformation(
                "Enroll beneficiaries request received. TenantId={TenantId}, DistributionId={DistributionId}, HouseholdCount={HouseholdCount}",
                dto.TenantId=tenantId,
                dto.DistributionId,
                dto.HouseholdIds?.Count ?? 0
            );

            var userId = User.Identifier();
            dto.CreatedBy = userId;

            try
            {
                await _targetingService.EnrollBeneficiariesAsync(dto);
                return Ok();
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Validation error during enrollment. TenantId={TenantId}, DistributionId={DistributionId}",
                    dto.TenantId,
                    dto.DistributionId
                );

                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Unknown error during enrollment. TenantId={TenantId}, DistributionId={DistributionId}",
                    dto.TenantId,
                    dto.DistributionId
                );

                return StatusCode(500, ex.Message);
            }
        }

    }
}
