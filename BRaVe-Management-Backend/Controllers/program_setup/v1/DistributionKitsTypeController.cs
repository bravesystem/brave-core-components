using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Extensions;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BRaVe_Management_Backend.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class DistributionKitsTypeController : ControllerBase
    {
        private readonly IDistributionKitTypeService _distributionKitService;
        private readonly ILogger<DistributionKitsTypeController> _logger;

        public DistributionKitsTypeController(IDistributionKitTypeService distributionKitService, ILogger<DistributionKitsTypeController> logger)
        {
            _distributionKitService = distributionKitService;
            _logger = logger;
        }
       


        [HttpGet]
        public async Task<ActionResult<IEnumerable<KitType>>> GetAllDistributionKits()
        {
            _logger.LogInformation("Fetching all Distribution Kits.");
            try
            {
                int TenantId = User.Tenant();
                var result = await _distributionKitService.GetAllKitTypes(TenantId);
                _logger.LogInformation("Fetched {Count} Distribution Kit.", result?.Count() ?? 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Distribution Kit.");
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpGet("dist_mapping")]
        public async Task<ActionResult<IEnumerable<KitType>>> GetAllKitsWithItems()
        {
            _logger.LogInformation("Fetching all Distribution Kits.");
            try
            {
                int TenantId = User.Tenant();
                var result = await _distributionKitService.GetAllKitTypesWithItems(TenantId);
                _logger.LogInformation("Fetched {Count} Distribution Kit.", result?.Count() ?? 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Distribution Kit.");
                return StatusCode(500, "Internal server error.");
            }
        }



        [HttpPost]
        public async Task<ActionResult> CreateDistributionKit(KitType data)
        {
            _logger.LogInformation("Updating Distribution Kit with ID: {Id}", data.Id);
            try
            {
                await _distributionKitService.CreateKitAsync(data);
                _logger.LogInformation("Distribution Kit updated successfully with ID: {Id}", data.Id);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error creating Distribution Kit");

                return ex is ArgumentException
                    ? BadRequest(ex.Message)
                    : StatusCode(500, ex.Message);
            }
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDistributionKit(KitType data)
        {
          
            _distributionKitService.UpdateKitAsync(data);

            return Ok();
        }


    }
}
