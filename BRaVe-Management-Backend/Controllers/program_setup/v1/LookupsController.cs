using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Extensions;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace BRaVe_Management_Backend.Controllers.program_setup.v1
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class LookupsController : ControllerBase
    {
        private readonly ILookupService _lookupService;
        private readonly ILogger<LookupsController> _logger;

        public LookupsController(ILookupService lookupService, ILogger<LookupsController> logger)
        {
            _lookupService = lookupService;
            _logger = logger;
        }

        // ---------------------------------------------------------
        // 1) Core lookups 
        // ---------------------------------------------------------
        [HttpGet("core/{lang}")]
        public async Task<ActionResult<CoreLookups>> CoreLookups(string lang = null)
        {
            _logger.LogInformation("Fetching core lookups. Language: {Lang}", lang ?? "default");

            try
            {
                var result = await _lookupService.GetCoreLookups(lang);
                _logger.LogInformation("Successfully fetched core lookups. Items count: {Count}",
                    result?.GetType().GetProperties().Length ?? 0);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching core lookups for language {Lang}", lang ?? "default");
                return BadRequest(ex.Message);
            }
        }




        // ---------------------------------------------------------
       // 2) Uploads an Excel file (2 tabs: LookupNames + LookupValues)
        // and processes both in one transaction.
        // ---------------------------------------------------------

        [HttpPost("upload-excel-v2")]
        public async Task<IActionResult> UploadLookupExcelV2(IFormFile file)
        {
            _logger.LogInformation("Received request to upload lookup Excel V2.");

            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded." });

            try
            {
                var userId = User.Identifier();
                var tenantId = User.Tenant();

                await _lookupService.UploadLookupExcelV2Async(file, userId, tenantId);

                _logger.LogInformation("Lookup Excel V2 uploaded successfully by {UserId}", userId);

                return Ok(new
                {
                    message = "Lookup Excel uploaded and processed successfully."
                });
            }
            catch (SqlException ex) when (ex.Number>=60000 && ex.Number < 60006)
            {

                return BadRequest(new
                {
                    errorcode = ex.Number,
                    message = ex.Message
                });

            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while uploading lookup Excel V2.");

                return StatusCode(500, new
                {
                    message = "Unexpected error during upload. Make sure you use the correct template with both sheets (LookupNames and LookupValues)."
                });
            }
        }

        // ---------------------------------------------------------
        // 3) GET all lookups fromlookup table 
        // ---------------------------------------------------------
        [HttpGet("lookups")]
        public async Task<IActionResult> GetAllLookupNames()
        {
            _logger.LogInformation("Fetching all lookup table names.");

            var tenantId = User.Tenant();

            try
            {
                var list = await _lookupService.GetAllLookupNamesAsync(tenantId);

                if (list == null || !list.Any())
                {
                    _logger.LogWarning("No lookup names found in the database.");
                    return NotFound(new { message = "No lookup names found." });
                }

                _logger.LogInformation("Fetched {Count} lookup names successfully.", list.Count());
                return Ok(list);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error while fetching lookup names. ErrorNumber={ErrorNumber}", ex.Number);
                return StatusCode(500, new { message = "Database error occurred while fetching lookup names." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching lookup names.");
                return StatusCode(500, new { message = "Unexpected error occurred while fetching lookup names." });
            }
        }



        // ---------------------------------------------------------
        // 4) CREATE new Lookups
        // ---------------------------------------------------------
        [HttpPost("lookups")]
        public async Task<IActionResult> CreateLookupName([FromBody] LookupTableNameDto dto)
        {

            if (dto == null || string.IsNullOrWhiteSpace(dto.LookupName))
                return BadRequest(new { message = "LookupName is required." });

            var userId = User.Identifier();
            var tenantId = User.Tenant();
            _logger.LogInformation("Creating new lookup name {LookupName} by user {UserId}", dto.LookupName, userId);

            try
            {
                await _lookupService.CreateLookupNameAsync(dto, userId, tenantId);
                _logger.LogInformation("Lookup name {LookupName} created successfully.", dto.LookupName);
                return Ok(new { message = "Lookup name created successfully." });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error while creating lookup name {LookupName}. ErrorNumber={ErrorNumber}", dto.LookupName, ex.Number);
                return StatusCode(500, new { message = "Database error occurred while creating lookup name." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating lookup name {LookupName}.", dto.LookupName);
                return StatusCode(500, new { message = "Unexpected error occurred while creating lookup name." });
            }
        }

        // ---------------------------------------------------------
        // 5) UPDATE Lookup 
        // ---------------------------------------------------------
        [HttpPut("lookups/{id:int}")]
        public async Task<IActionResult> UpdateLookupName(int id, [FromBody] LookupTableNameDto dto)
        {
            if (dto == null || id != dto.Id)
                return BadRequest(new { message = "Invalid lookup name update request." });

            var userId = User.Identifier();
            _logger.LogInformation("Updating lookup name Id={Id} by user {UserId}", id, userId);

            try
            {
                dto.TenantId = User.Tenant();

                await _lookupService.UpdateLookupNameAsync(dto, userId);
                _logger.LogInformation("Lookup name Id={Id} updated successfully.", id);
                return Ok(new { message = "Lookup name updated successfully." });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error while updating lookup name Id={Id}. ErrorNumber={ErrorNumber}", id, ex.Number);
                return StatusCode(500, new { message = "Database error occurred while updating lookup name." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating lookup name Id={Id}.", id);
                return StatusCode(500, new { message = "Unexpected error occurred while updating lookup name." });
            }
        }

        // ---------------------------------------------------------
        // 6) DELETE lookup 
        // ---------------------------------------------------------
        [HttpDelete("lookups/{id:int}")]
        public async Task<IActionResult> DeleteLookupName(int id)
        {
            _logger.LogInformation("Deleting lookup name Id={Id}", id);

            try
            {
                await _lookupService.DeleteLookupNameAsync(id);
                _logger.LogInformation("Lookup name Id={Id} deleted successfully.", id);
                return Ok(new { message = "Lookup name deleted successfully." });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error while deleting lookup name Id={Id}. ErrorNumber={ErrorNumber}", id, ex.Number);
                return StatusCode(500, new { message = "Database error occurred while deleting lookup name." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting lookup name Id={Id}.", id);
                return StatusCode(500, new { message = "Unexpected error occurred while deleting lookup name." });
            }
        }

        // ---------------------------------------------------------
        //  6) GET LOOKUP TABLE VALUES
        // ---------------------------------------------------------
        [HttpGet("values")]
        public async Task<IActionResult> GetLookupValues()
        {
            _logger.LogInformation("Fetching all lookup table values.");
            var tenantId = User.Tenant();

            try
            {
                var result = await _lookupService.GetAllLookupValuesAsync(tenantId);

                if (result == null || !result.Any())
                {
                    _logger.LogWarning("No lookup table values found in the database.");
                    return NotFound(new { message = "No lookup values found." });
                }

                _logger.LogInformation("Fetched {Count} lookup table values successfully.", result.Count());
                return Ok(result);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error while fetching lookup table values. ErrorNumber={ErrorNumber}", ex.Number);
                return StatusCode(500, new { message = "Database error occurred while fetching lookup values." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching lookup table values.");
                return StatusCode(500, new { message = "Unexpected error occurred while fetching lookup values." });
            }
        }


        // ---------------------------------------------------------
        //  7) CREATE LOOKUP TABLE VALUE
        // ---------------------------------------------------------
        [HttpPost("values")]
        public async Task<IActionResult> CreateLookupValue([FromBody] LookupTableValueDto dto)
        {
            var userId = User.Identifier();
            var tenantId = User.Tenant();
            _logger.LogInformation("Creating new lookup value {ItemName} for LookupId={LookupId} by user {UserId}", dto.ItemName, dto.LookupId, userId);

            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.ItemName))
                {
                    _logger.LogWarning("Invalid lookup value creation request received.");
                    return BadRequest(new { message = "Item name is required." });
                }

                await _lookupService.CreateLookupValueAsync(dto, userId,tenantId);

                _logger.LogInformation("Lookup value {ItemName} created successfully by {UserId}.", dto.ItemName, userId);
                return Ok(new { message = "Lookup value created successfully." });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error while creating lookup value {ItemName}. ErrorNumber={ErrorNumber}", dto.ItemName, ex.Number);
                return StatusCode(500, new { message = "Database error occurred while creating lookup value." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating lookup value {ItemName}.", dto.ItemName);
                return StatusCode(500, new { message = "Unexpected error occurred while creating lookup value." });
            }
        }


        // ---------------------------------------------------------
        //  8) UPDATE LOOKUP TABLE VALUE
        // ---------------------------------------------------------
        [HttpPut("values/{id:int}")]
        public async Task<IActionResult> UpdateLookupValue(int id, [FromBody] LookupTableValueDto dto)
        {
            var userId = User.Identifier();
            _logger.LogInformation("Updating lookup value Id={Id} by user {UserId}", id, userId);

            try
            {
                if (dto == null || id != dto.Id)
                {
                    _logger.LogWarning("Invalid update request for lookup value. Id={Id} does not match payload.", id);
                    return BadRequest(new { message = "Invalid lookup value update request." });
                }

                dto.TenantId = User.Tenant();

                await _lookupService.UpdateLookupValueAsync(dto, userId);

                _logger.LogInformation("Lookup value Id={Id} updated successfully by {UserId}.", id, userId);
                return Ok(new { message = "Lookup value updated successfully." });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error while updating lookup value Id={Id}. ErrorNumber={ErrorNumber}", id, ex.Number);
                return StatusCode(500, new { message = "Database error occurred while updating lookup value." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating lookup value Id={Id}.", id);
                return StatusCode(500, new { message = "Unexpected error occurred while updating lookup value." });
            }
        }


        // ---------------------------------------------------------
        //  9) DELETE LOOKUP TABLE VALUE
        // ---------------------------------------------------------
        [HttpDelete("values/{id:int}")]
        public async Task<IActionResult> DeleteLookupValue(int id)
        {
            _logger.LogInformation("Deleting lookup value Id={Id}", id);

            try
            {
                await _lookupService.DeleteLookupValueAsync(id);
                _logger.LogInformation("Lookup value Id={Id} deleted successfully.", id);
                return Ok(new { message = "Lookup value deleted successfully." });
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error while deleting lookup value Id={Id}. ErrorNumber={ErrorNumber}", id, ex.Number);
                return StatusCode(500, new { message = "Database error occurred while deleting lookup value." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting lookup value Id={Id}.", id);
                return StatusCode(500, new { message = "Unexpected error occurred while deleting lookup value." });
            }
        }

        // ---------------------------------------------------------
        // 10) GET LOOKUP VALUES BY LOOKUP ID
        // ---------------------------------------------------------
        [HttpGet("values/by-lookup/{lookupId:int}")]
        public async Task<IActionResult> GetLookupValuesByLookupId(int lookupId)
        {
            if (lookupId <= 0)
                return BadRequest(new { message = "Invalid lookup id." });

            var tenantId = User.Tenant();
            _logger.LogInformation("Fetching lookup values for LookupId={LookupId}, TenantId={TenantId}", lookupId, tenantId);

            try
            {
                var values = await _lookupService.GetLookupValuesByLookupIdAsync(lookupId, tenantId);

                if (values == null || !values.Any())
                    return Ok(new List<LookupTableValueDto>());

                return Ok(values);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error fetching lookup values for LookupId={LookupId}", lookupId);
                return StatusCode(500, new { message = "Database error occurred while fetching lookup values." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching lookup values for LookupId={LookupId}", lookupId);
                return StatusCode(500, new { message = "Unexpected error occurred while fetching lookup values." });
            }
        }



    }
}
