using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Extensions;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace BRaVe_Management_Backend.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class RegionsController : ControllerBase
    {
        private readonly IRegionService _regionService;
        private readonly ILogger<RegionsController> _logger;

        public RegionsController(IRegionService regionService, ILogger<RegionsController> logger)
        {
            _regionService = regionService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Region>>> GetAllRegions()
        {
            _logger.LogInformation("Fetching all regions.");
            try
            {
                var regions = await _regionService.GetAllRegions();
                _logger.LogInformation("Fetched {Count} regions.", regions?.Count() ?? 0);
                return Ok(regions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all regions.");
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Region>> GetRegionById(int id)
        {
            _logger.LogInformation("Fetching region with ID: {RegionId}", id);
            try
            {
                var region = await _regionService.GetRegion(id);
                if (region == null)
                {
                    _logger.LogWarning("Region not found with ID: {RegionId}", id);
                    return NotFound();
                }

                _logger.LogInformation("Fetched region with ID: {RegionId}", id);
                return Ok(region);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching region with ID: {RegionId}", id);
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpPost]
        public async Task<ActionResult> CreateRegion(RegionDto data)
        {
            var userId = User.Identifier();
            _logger.LogInformation("Creating new region: {RegionName} by user {UserId}", data.Name, userId);

            try
            {
                await _regionService.CreateRegion(data, userId);
                _logger.LogInformation("Region {RegionName} created successfully.", data.Name);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating region: {RegionName}", data.Name);
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRegion(int id, RegionDto data)
        {
            var userId = User.Identifier();
            _logger.LogInformation("Updating region with ID: {RegionId} by user {UserId}", id, userId);

            try
            {
                var region = new Region
                {
                    RegionId = id,
                    Name = data.Name,
                    Note = data.Note
                };

                await _regionService.UpdateRegion(region, userId);
                _logger.LogInformation("Region with ID: {RegionId} updated successfully.", id);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating region with ID: {RegionId}", id);
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRegion(int id)
        {
            _logger.LogInformation("Deleting region with ID: {RegionId}", id);
            try
            {
                await _regionService.DeleteRegion(id);
                _logger.LogInformation("Region with ID: {RegionId} deleted successfully.", id);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting region with ID: {RegionId}", id);
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpGet("countries/all")]
        public async Task<ActionResult<IEnumerable<Country>>> GetAllCountries()
        {
            _logger.LogInformation("Fetching all countries.");
            try
            {
                var countries = await _regionService.GetAllCountries();
                _logger.LogInformation("Fetched {Count} countries.", countries?.Count() ?? 0);
                return Ok(countries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all countries.");
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpGet("countries/byregion/{id}")]
        public async Task<ActionResult<IEnumerable<Country>>> GetAllCountriesByRegionId(int id)
        {
            _logger.LogInformation("Fetching countries for region ID: {RegionId}", id);
            try
            {
                var countries = await _regionService.GetAllCountriesByRegionId(id);
                _logger.LogInformation("Fetched {Count} countries for region ID: {RegionId}", countries?.Count() ?? 0, id);
                return Ok(countries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching countries for region ID: {RegionId}", id);
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpGet("countries/{id}")]
        public async Task<ActionResult<Country>> GetCountryById(string id)
        {
            _logger.LogInformation("Fetching country with ISO code: {CountryId}", id);
            try
            {
                var country = await _regionService.GetCountry(id);
                if (country == null)
                {
                    _logger.LogWarning("Country not found with ISO code: {CountryId}", id);
                    return NotFound();
                }

                _logger.LogInformation("Fetched country with ISO code: {CountryId}", id);
                return Ok(country);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching country with ISO code: {CountryId}", id);
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpPost("countries")]
        public async Task<ActionResult<Country>> CreateCountry(Country data)
        {
            var userId = User.Identifier();
            _logger.LogInformation("Creating country {CountryId} by user {UserId}", data.CountryIso2, userId);

            try
            {
                await _regionService.CreateCountry(data, userId);
                _logger.LogInformation("Country {CountryId} created successfully.", data.CountryIso2);
                return Ok();
            }
            catch (SqlException e)
            {
                _logger.LogError(e, "SQL error while creating country {CountryId}", data.CountryIso2);
                if (e.Number == 2627 || e.Number == 2601)
                {
                    return Conflict("A Country with the same Iso code already exists.");
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating country {CountryId}", data.CountryIso2);
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpPut("countries/{id}")]
        public async Task<IActionResult> UpdateCountry(string id, CountryDto data)
        {
            var userId = User.Identifier();
            _logger.LogInformation("Updating country {CountryId} by user {UserId}", id, userId);

            try
            {
                var country = new Country
                {
                    CountryIso2 = id,
                    Name = data.Name,
                    PhoneCode = data.PhoneCode,
                    RegionId = data.RegionId
                };

                await _regionService.UpdateCountry(country, userId);
                _logger.LogInformation("Country {CountryId} updated successfully.", id);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating country {CountryId}", id);
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpDelete("countries/{id}")]
        public async Task<IActionResult> DeleteCountry(string id)
        {
            _logger.LogInformation("Deleting country {CountryId}", id);
            try
            {
                await _regionService.DeleteCountry(id);
                _logger.LogInformation("Country {CountryId} deleted successfully.", id);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting country {CountryId}", id);
                return StatusCode(500, "Internal server error.");
            }
        }
    }
}
