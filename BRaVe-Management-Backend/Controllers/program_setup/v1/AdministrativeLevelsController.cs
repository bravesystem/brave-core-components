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
    public class AdministrativeLevelsController : ControllerBase
    {
        private readonly IAdministrativeLevelService _administrativeLevelService;
        private readonly ILogger<AdministrativeLevelsController> _logger;

        public AdministrativeLevelsController(IAdministrativeLevelService administrativeLevelService, ILogger<AdministrativeLevelsController> logger)
        {
            _administrativeLevelService = administrativeLevelService;
            _logger = logger;
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AdministrativeLevel>>> GetAllAdministrativeLevel()
        {
            _logger.LogInformation("Fetching all Administrative Levels.");
            try
            {
                var result = await _administrativeLevelService.GetAllAdministrativeLevel(User.Tenant());
                _logger.LogInformation("Fetched {Count} administrative levels.", result?.Count() ?? 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching administrative Level.");
                return StatusCode(500, "Internal server error.");
            }
        }

         


        //[HttpGet("available")]
        //public async Task<ActionResult<IEnumerable<LookupItemDto>>> GetAvailableEnumeratorCodes()
        //{
        //    _logger.LogInformation("Fetching available enumerator codes.");
        //    try
        //    {
        //        var codes = await _enumeratorService.GetAvailableEnumeratorCodes();
        //        _logger.LogInformation("Fetched {Count} available codes.", codes?.Count() ?? 0);
        //        return Ok(codes);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error fetching available enumerator codes.");
        //        return StatusCode(500, "Internal server error.");
        //    }
        //}

        [HttpGet("{id}")]
        public async Task<ActionResult<Mission>> GetAdministrativeLevelById(int id)
        {
            _logger.LogInformation("Fetching administrative Level  with ID: {Id}", id);
            try
            {
                var administrativeLevel = await _administrativeLevelService.GetAdministrativeLevelById(id);
                if (administrativeLevel == null)
                {
                    _logger.LogWarning("administrativeLevel not found with ID: {Id}", id);
                    return NotFound();
                }
                return Ok(administrativeLevel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching administrativeLevel with ID: {Id}", id);
                return StatusCode(500, "Internal server error.");
            }
        }
 
        [HttpPost]
        public async Task<ActionResult> CreateAdministrativeLevel(AdministrativeLevelDto data)
        {
            _logger.LogInformation("Updating Administrative Level with ID: {Id}", data.Id);
            try
            {
                await _administrativeLevelService.CreateAdministrativeLevel(data, User.Identity.Name);
                _logger.LogInformation("Administrative Level updated successfully with ID: {Id}", data.Id);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Administrative Level with ID: {Id}", data.Id);
                return StatusCode(500, "Internal server error.");
            }
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAdministrativeLevel(int id, AdministrativeLevelDto data)
        {
            AdministrativeLevel administrativeLevel = new AdministrativeLevel()
            {
                //TenantId = Convert.ToInt32(User.Tenant),
                Id = id,
                LevelName = data.LevelName,
                OfficialCode = data.OfficialCode,
                IsActive = data.IsActive
            };

            _administrativeLevelService.UpdateAdministrativeLevel(administrativeLevel, User.Identifier());

            return Ok();
        }

        [HttpPost("upload-excel")]
        public async Task<IActionResult> UploadAdministrativeLevelExcel(IFormFile file)
        {
            _logger.LogInformation("Received request to upload Administrative Level Excel.");

            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded." });

            try
            {
                var userId = User.Identifier();
                var tenantId = User.Tenant();

                await _administrativeLevelService.UploadAdministrativeLevelExcelAsync(file, userId, tenantId);

                _logger.LogInformation(" Administrative Level  Excel uploaded successfully by {UserId}", userId);
                return Ok(new { message = " Administrative Level  Excel uploaded and processed successfully." });
            }
            catch (ApplicationException ex)
            {
                // Return a controlled 400 response with message
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while uploading  Administrative Level  Excel.");
                return StatusCode(500, "Unexpected error during upload. Make sure you use the template provided.");
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

                await _administrativeLevelService.UploadAdministrativeLevelLocationExcelAsync(file, userId, tenantId);

                _logger.LogInformation(" Administrative Level Location  Excel uploaded successfully by {UserId}", userId);
                return Ok(new { message = " Administrative Level Location Excel uploaded and processed successfully." });
            }
            catch (ApplicationException ex)
            {
                // Return a controlled 400 response with message
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while uploading  Administrative Level Location Excel.");
                return StatusCode(500, "Unexpected error during upload. Make sure you use the template provided.");
            }
        }
    }
}
