using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Extensions;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BRaVe_Management_Backend.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class DistributionsTypeController : ControllerBase
    {
        private readonly IDistributionTypesService _distributionService;
        private readonly ILogger<DistributionsTypeController> _logger;

        public DistributionsTypeController(IDistributionTypesService distributionService, ILogger<DistributionsTypeController> logger)
        {
            _distributionService = distributionService;
            _logger = logger;
        }

      
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DistributionTypes>>> GetAllDistributionTypes()
        {
            _logger.LogInformation("Fetching all Distribution types.");
            try
            {

                int TenantId = User.Tenant();
                var result = await _distributionService.GetAllDistributionTypes(TenantId);
                _logger.LogInformation("Fetched {Count} Distribution types. ", result?.Count() ?? 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Distribution types.");
                return StatusCode(500, "Internal server error.");
            }
        }




        [HttpGet("{id}")]
        public async Task<ActionResult<DistributionTypes>> GetDistributionTypeById(int id)
        {
            _logger.LogInformation("Fetching Distribution type  with ID: {Id}", id);
            try
            {
                var distribution = await _distributionService.GetDistributionTypesById(id);
                if (distribution == null)
                {
                    _logger.LogWarning("Distribution  type not found with ID: {Id}", id);
                    return NotFound();
                }
                return Ok(distribution);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Distribution type with ID: {Id}", id);
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DistributionTypes Distribution)
        {
            try
            {
                await _distributionService.AddDistributionType(Distribution);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating distribution");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] DistributionTypes UpdatedDistribution)
        {
            try
            {
                await _distributionService.UpdateDistributionType(id, UpdatedDistribution);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating distribution");
                return StatusCode(500, ex.Message);
            }
        }


     

    }
}
