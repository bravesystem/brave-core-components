using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Extensions;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace BRaVe_Management_Backend.Controllers.mission_setup.v1
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class DatapointsController : ControllerBase
    {
        private readonly IDatapointService _datapointService;
        private readonly ILogger<DatapointsController> _logger;

        public DatapointsController(IDatapointService datapointService, ILogger<DatapointsController> logger)
        {
            _datapointService = datapointService;
            _logger = logger;
        }

      
        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<DataPoint>>> GetAll()
        {
            var datapoints = await _datapointService.GetAllDataPoints();
            return Ok(datapoints);
        }

        [HttpGet("activity")]
        public async Task<ActionResult<List<DataPoint>>> GetDatapointForActivityByTenant(string languageCode)
        {
            int tenantId = User.Tenant();

            try
            {
                var dataPoints = await _datapointService.GetDataPointsForActivityByTenantAsync(tenantId, languageCode);

                if (dataPoints == null || !dataPoints.Any())
                    return Ok(new List<DataPoint>());

                return Ok(dataPoints);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching datapoints for tenant {TenantId}", tenantId);
                return StatusCode(500, "An error occurred retrieving datapoints.");
            }
        }

        [HttpGet]
        public async Task<ActionResult<List<DataPoint>>> GetByTenant( string languageCode)
        {
            int tenantId = User.Tenant();

            try
            {
                var dataPoints = await _datapointService.GetDataPointsByTenantAsync(tenantId, languageCode);

                if (dataPoints == null || !dataPoints.Any())
                    return Ok(new List<DataPoint>());

                return Ok(dataPoints);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching datapoints for tenant {TenantId}", tenantId);
                return StatusCode(500, "An error occurred retrieving datapoints.");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<DataPoint>> GetById(int id)
        {
            var datapoint = await _datapointService.GetDataPointById(id);
            if (datapoint == null)
                return NotFound();

            return Ok(datapoint);
        }



        [HttpPost]
        public async Task<IActionResult> CreateDataPoint([FromBody] DataPointDto newDataPoint)
        {
            if (newDataPoint == null)
                return BadRequest("DataPoint cannot be null.");

            try
            {
              
                //newDataPoint.Id = nextId; // <-- Assign the incremented ID
                //newDataPoint.DefaultLang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                newDataPoint.TenantId = User.Tenant();

                // Create
                await _datapointService.CreateDataPoint(newDataPoint, User.Identifier());

                _logger.LogInformation("Created new DataPoint with Id {Id}", newDataPoint.Id);

                return CreatedAtAction(nameof(GetById), new { id = newDataPoint.Id }, newDataPoint);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating datapoint");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }




        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, [FromBody] DataPointDto updatedDataPoint)
        {
            if (id != updatedDataPoint.Id)
                return BadRequest("DataPoint ID mismatch");

            await _datapointService.UpdateDataPoint(updatedDataPoint);
            _logger.LogInformation("Updated DataPoint with Id {Id}", updatedDataPoint.Id);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id, [FromQuery] int tenantId)
        {
            try
            {
                // Replace with your user context logic
                string userId = User?.Identity?.Name ?? "system";

                await _datapointService.DeleteDatapoint(userId, id, tenantId);

                _logger.LogInformation("Deleted datapoint {Id} for tenant {TenantId} by user {UserId}", id, tenantId, userId);

                return NoContent(); // 204
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting datapoint {Id} for tenant {TenantId}", id, tenantId);
                return StatusCode(500, new { message = $"Error deleting datapoint: {ex.Message}" });
            }
        }
        // GET: v1/DataPoints/Id/{dataPointId}
        [HttpGet("Id/{dataPointId:int}")]
        public async Task<ActionResult<DatapointTranslationDto>> GetTranslationByDataPointId(int dataPointId)
        {
            try
            {
                var translation = await _datapointService.GetAllTranslationsAsync(dataPointId);

                if (translation == null)
                    return NotFound($"No translation found for datapoint {dataPointId}");

                return Ok(translation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching translation for DataPoint ID {DataPointId}", dataPointId);
                return StatusCode(500, "Internal server error.");
            }
        }
        [HttpPost("translations/create")]
        public async Task<ActionResult> CreateOrUpdateTranslation([FromBody] DatapointTranslationDto translation)
        {
            if (translation == null)
                return BadRequest("Invalid translation payload.");

            try
            {
                // Ideally get logged-in user
                string userId = "system";

                await _datapointService.AddOrUpdateTranslationAsync(translation, userId);

                var action = translation.Id == 0 ? "created" : "updated";

                return Ok(new
                {
                    message = $"Translation successfully {action}.",
                    translation
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating/updating translation");
                return StatusCode(500, "Failed to create/update translation.");
            }
        }

        // DELETE v1/DataPoints/translations/delete/{dataPointId}/{languageCode}
        [HttpDelete("translations/delete/{dataPointId}/{languageCode}")]
        public async Task<IActionResult> DeleteTranslation(int dataPointId, string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
                return BadRequest("Language code is required.");

            try
            {
                await _datapointService.DeleteTranslationAsync(dataPointId, languageCode);
                return Ok(new { message = "Translation deleted successfully." });
            }
            catch
            {
                return StatusCode(500, "Failed to delete translation.");
            }
        }
    }

}

