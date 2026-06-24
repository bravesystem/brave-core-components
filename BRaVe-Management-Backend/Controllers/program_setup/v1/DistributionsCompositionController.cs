using Azure.Core;
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
    public class DistributionsCompositionController : ControllerBase
    {
        private readonly IDistributionCompositionService _distributionService;
        private readonly ILogger<DistributionsCompositionController> _logger;

        public DistributionsCompositionController(
            IDistributionCompositionService distributionService,
            ILogger<DistributionsCompositionController> logger)
        {
            _distributionService = distributionService;
            _logger = logger;
        }

        // GET: api/v1/DistributionsComposition
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DistributionComposition>>> GetAll()
        {
            try
            {
                int tenantId = User.Tenant();

                var results = await _distributionService.GetDistributionCompositionsAsync(tenantId);

                _logger.LogInformation("Fetched {Count} distribution compositions.", results?.Count() ?? 0);

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving distribution compositions");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: v1/DistributionsComposition/by-distribution/5
        [HttpGet("by-distribution/{distributionId:int}")]
        public async Task<ActionResult<IEnumerable<DistributionComposition>>> GetByDistribution(int distributionId)
        {
            _logger.LogInformation("Fetching distribution compositions for DistributionId={DistributionId}", distributionId);

            try
            {
                int tenantId = User.Tenant();

                var result = await _distributionService.GetAllDistributionComposition(distributionId, tenantId);

                _logger.LogInformation("Fetched {Count} records.", result?.Count() ?? 0);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching distribution compositions for DistributionId={DistributionId}", distributionId);
                return StatusCode(500, "Internal server error.");
            }
        }


        // GET: api/v1/DistributionsComposition/items
        [HttpGet("items")]
        public async Task<ActionResult<IEnumerable<DistributionItemTypes>>> GetAllItems()
        {
            try
            {
                int tenantId = User.Tenant();

                _logger.LogInformation("Fetching distribution items for TenantId={TenantId}", tenantId);

                var results = await _distributionService.GetDistributionItemsAsync(tenantId);

                _logger.LogInformation("Fetched {Count} distribution items.", results?.Count() ?? 0);

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving distribution items");
                return StatusCode(500, "Internal server error");
            }
        }

        // POST: api/v1/DistributionsComposition
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DistributionItemTypes item)
        {
            try
            {
                int tenantId = User.Tenant();
                item.TenantId = tenantId;

                var id = await _distributionService.CreateDistributionItemAsync(item);

                return Ok(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating distribution item");
                return StatusCode(500, "Internal server error");
            }
        }


        // PUT: api/v1/DistributionsComposition/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] DistributionItemTypes item)
        {
            try
            {
                int tenantId = User.Tenant();
                item.Id = id;
                item.TenantId = tenantId;

                await _distributionService.UpdateDistributionItemAsync(item);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating distribution item");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("AddDistributionItemAsync")]
        public async Task<IActionResult> AddDistributionItemAsync([FromBody] List<DistributionItemCreateDto> dtoList)
        {
            try
            {
                int tenantId = User.Tenant();

                foreach (var dto in dtoList)
                    dto.TenantId = tenantId;

                await _distributionService.AddDistributionItemsAsync(dtoList); // new bulk method

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding distribution items");
                //return StatusCode(500, "Internal server error");
                return StatusCode(500, ex.Message);
            }
        }
        // POST multiple kits (bulk)
        [HttpPost("kits/bulk")]
        public async Task<IActionResult> AddDistributionKitsAsync([FromBody] List<DistributionKitCreateDto> kits)
        {
            if (kits == null || !kits.Any())
                return BadRequest("No kits supplied.");

            var tenantId = User.Tenant();
            foreach (var kit in kits)
                kit.TenantId = tenantId;

            try
            {
                await _distributionService.AddDistributionKitsAsync(kits);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding distribution kits");
                return StatusCode(500, ex.Message);
            }
        }

        // POST multiple items (bulk)
        [HttpPost("items/bulk")]
        public async Task<IActionResult>AddKitItemsBulkAsync([FromBody] List<KitItemInsertDto> items)
        {
            if (items == null || !items.Any())
                return BadRequest("No items supplied.");

      
            try
            {
                var tenantId = User.Tenant();
                var userId = User.Identifier();
                //var KitId = items.KitId;
                await _distributionService.AddKitItemsAsync(tenantId, userId, items);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding distribution kits");
                return StatusCode(500, ex.Message);
            }
        }



        // GET: api/v1/DistributionsComposition/items
        [HttpGet("Kititems")]
        public async Task<ActionResult<IEnumerable<KitTypeItem>>> GetKitItemsByKitId()
        {
            try
            {
                // Fetch all items for this kit
                var results = await _distributionService.GetAllKitItemsAsync();

                if (results == null || !results.Any())
                    return NotFound($"No items found for KitId.");

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving items ");
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: v1/DistributionsComposition/by-kit/10
        [HttpGet("by-kit/{kitId:long}")]
        public async Task<ActionResult<IEnumerable<KitItemsDto>>> GetKitItemsByKitId(long kitId)
        {
            try
            {
                var tenantId = User.Tenant();// however you're resolving tenant

                var results = await _distributionService.GetKitItemByKitIdAsync(kitId, tenantId);

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving kit items for KitId {KitId}", kitId);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("inline/{id}")]
        public async Task<IActionResult> InlineUpdate(int id, KitItemInsertDto dto)
        {
            await _distributionService.UpdateInline(id, dto.Measure, dto.UoM);
            return Ok();
        }


        [HttpPut("inline_item_distribution/{id}")]
        public async Task<IActionResult> UpdateInlineItemDistribution( int id, [FromBody] DistributionItemCreateDto dto)
        {
            if (dto == null)
                return BadRequest();

            try
            {
                await _distributionService.UpdateInlineItemDistribution(id, dto);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Inline distribution update failed for Id {Id}", id);
                return StatusCode(500, new { success = false });
            }
        }

        [HttpPut("kit-inline")]
        public async Task<IActionResult> UpdateInlineKitDistribution([FromBody] DistributionKitCreateDto dto)
        {
            if (dto == null)
                return BadRequest(new { success = false, message = "Invalid payload" });

            try
            {
                await _distributionService.UpdateInlineKitDistribution(dto);

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Inline kit update failed for KitTypeId {KitTypeId}, DistributionId {DistributionId}",
                    dto.KitTypeId, dto.KitTypeId);

                return StatusCode(500, new { success = false });
            }
        }

    }
}