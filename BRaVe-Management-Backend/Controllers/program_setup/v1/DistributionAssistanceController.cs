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
    public class DistributionAssistancesController : ControllerBase
    {
        private readonly IDistributionAssistanceService _distributionAssistanceService;
        private readonly ILogger<DistributionAssistancesController> _logger;

        public DistributionAssistancesController(IDistributionAssistanceService distributionAssistanceService, ILogger<DistributionAssistancesController> logger)
        {
            _distributionAssistanceService = distributionAssistanceService;
            _logger = logger;
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DistributionAssistance>>> GetAllDistributionAssistances()
        {
            _logger.LogInformation("Fetching all Distribution Assistance.");
            try
            {

                int TenantId = User.Tenant();
                var result = await _distributionAssistanceService.GetAllDistributionAssistance(User.Tenant());
                _logger.LogInformation("Fetched {Count} Distribution Assistance.", result?.Count() ?? 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Distribution Assistance.");
                return StatusCode(500, "Internal server error.");
            }
        }

         


      

        [HttpGet("{id}")]
        public async Task<ActionResult<DistributionAssistance>> GetDistributionAssistanceById(int id)
        {
            _logger.LogInformation("Fetching Distribution Assistance  with ID: {Id}", id);
            try
            {
                var distributionAssistance = await _distributionAssistanceService.GetDistributionAssistanceById(id);
                if (distributionAssistance == null)
                {
                    _logger.LogWarning("Distribution Assistance not found with ID: {Id}", id);
                    return NotFound();
                }
                return Ok(distributionAssistance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Distribution Assistance with ID: {Id}", id);
                return StatusCode(500, "Internal server error.");
            }
        }
 
        [HttpPost]
        public async Task<ActionResult> CreateDistributionAssistance(DistributionAssistanceDto data)
        {
            _logger.LogInformation("Updating Distribution Assistance  with ID: {Id}", data.Id);
            try
            {
                await _distributionAssistanceService.CreateDistributionAssistance(data, User.Identity.Name);
                _logger.LogInformation("Distribution Assistance  updated successfully with ID: {Id}", data.Id);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Distribution Assistance with ID: {Id}", data.Id);
                return StatusCode(500, "Internal server error.");
            }
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDistributionAssistance(int id, DistributionAssistanceDto data)
        {
            DistributionAssistance distributionAssistance = new DistributionAssistance()
            {
                Id = id,
                KitId = data.KitId,
                Notes = data.Notes,
                Quantity = data.Quantity,
                ItemTag = data.ItemTag,
                IsActive = data.IsActive

            };

            _distributionAssistanceService.UpdateDistributionAssistance(distributionAssistance, User.Identifier());

            return Ok();
        }


       

         
    }
}
