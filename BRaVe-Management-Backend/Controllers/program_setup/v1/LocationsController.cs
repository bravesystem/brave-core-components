using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Extensions;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.AspNetCore.Mvc;

namespace BRaVe_Management_Backend.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class LocationsController : ControllerBase
    {
        private readonly ILocationService _locationService;
        private readonly ILogger<LocationsController> _logger;

        public LocationsController(ILocationService locationService, ILogger<LocationsController> logger)
        {
            _locationService = locationService;
            _logger = logger;
        }
         [HttpGet]
        public async Task<ActionResult<IEnumerable<Location>>> GetAllLocation()
        {
            _logger.LogInformation("Fetching all Locations.");
            try
            {
                var result = await _locationService.GetAllLocations(User.Tenant());
                _logger.LogInformation("Fetched {Count} locations.", result?.Count() ?? 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching location.");
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpGet("{tenantId}")]

        public async Task<ActionResult<IEnumerable<Location>>> GetAllLocationbytenantId(int tenantId)
        {
            _logger.LogInformation("Fetching all Locations.");
            try
            {
                var result = await _locationService.GetAllLocationbytenantId(tenantId);
                _logger.LogInformation("Fetched {Count} locations.", result?.Count() ?? 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching location.");
                return StatusCode(500, "Internal server error.");
            }
        }


 
        [HttpPost]
        public async Task<ActionResult> CreateLocation(LocationDto data)

        {
            _logger.LogInformation("Updating Location with ID: {Id}", data.Id);
            try
            {
                await _locationService.CreateLocation(data, User.Identity.Name);
                _logger.LogInformation("Location updated successfully with ID: {Id}", data.Id);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Location with ID: {Id}", data.Id);
                return StatusCode(500, "Internal server error.");
            }
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLocation(int id, LocationDto data)
        {
            try
            {
                Location location = new Location()
                {
                    TenantId = Convert.ToInt32(User.Tenant()),
                    Id = id,
                    LocationName = data.LocationName,
                    ParentLocationId = data.ParentLocationId,
                    LevelId = data.LevelId,
                    OfficialCode = data.OfficialCode,
                    IsActive = data.IsActive
                };

                await _locationService.UpdateLocation(location, User.Identifier());

                return Ok();
            }
            catch (Exception ex)
            {
                // log if you want
                return StatusCode(500, ex.Message);
            }
        }


        //Uploading Administrative Level Location 
        [HttpPost("upload-excelAdministrativeLevelLocation")]
        public async Task<IActionResult> UploadAdministrativeLevelLocationExcelAsync(IFormFile file)
        {
            _logger.LogInformation("Received request to upload Administrative Level Location Excel.");

            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded." });

            try
            {
                var userId = User.Identifier();
                var tenantId = User.Tenant();

                var resultMessage = await _locationService
                    .UploadAdministrativeLevelLocationExcelAsync(file, userId, tenantId);

                _logger.LogInformation("Excel upload completed: {Message}", resultMessage);

                return Ok(new
                {
                    message = resultMessage
                });
            }
            catch (ApplicationException ex)
            {
               
                _logger.LogWarning("Upload validation error: {Error}", ex.Message);

                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during Excel upload.");

                return StatusCode(500, new
                {
                    message = "Unexpected error during upload. Please check your template and try again."
                });
            }
        }


        [HttpGet("download-excelAdministrativeLevelLocation")]
        public async Task<IActionResult> DownloadAdministrativeLevelLocationTemplate()
        {
            _logger.LogInformation("Generating Administrative Level Location Excel template.");

            try
            {
                var tenantId = User.Tenant();

                var fileBytes = await _locationService
                    .DownloadAdministrativeLevelLocationTemplateAsync(tenantId);

                var fileName = $"Administrative_Level_Location_Template_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";

                return File(
                    fileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Administrative Level Location template.");
                return StatusCode(500, "Failed to generate template.");
            }
        }


    }
}
