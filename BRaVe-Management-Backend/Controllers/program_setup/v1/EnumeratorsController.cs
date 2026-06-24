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
    public class EnumeratorsController : ControllerBase
    {
        private readonly IEnumeratorService _enumeratorService;
        private readonly ILogger<EnumeratorsController> _logger;

        public EnumeratorsController(IEnumeratorService enumeratorService, ILogger<EnumeratorsController> logger)
        {
            _enumeratorService = enumeratorService;
            _logger = logger;
        }
         [HttpGet]
        public async Task<ActionResult<IEnumerable<Enumerator>>> GetAllEnumerators()
        {
            _logger.LogInformation("Fetching all enumerators.");
            try
            {
                var result = await _enumeratorService.GetAllEnumerators(User.Tenant());
                _logger.LogInformation("Fetched {Count} enumerators.", result?.Count() ?? 0);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching enumerators.");
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpGet("EnumeratorCodes")] 
        public async Task<ActionResult<IEnumerable<Enumerator>>> GetAllEnumeratorCode()
        {
            return Ok(await _enumeratorService.GetAllEnumeratorCode(User.Tenant()));
        }




        [HttpGet("{id}")]
        public async Task<ActionResult<Mission>> GetEnumeratorById(int id)
        {
            _logger.LogInformation("Fetching enumerator with ID: {EnumeratorId}", id);
            try
            {
                var enumerator = await _enumeratorService.GetEnumeratorById(id);
                if (enumerator == null)
                {
                    _logger.LogWarning("Enumerator not found with ID: {EnumeratorId}", id);
                    return NotFound();
                }
                return Ok(enumerator);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching enumerator with ID: {EnumeratorId}", id);
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpPost("resetpin/{id}")]
        public async Task<ActionResult> ResetEnumeratorPin(int id)
        {
            _logger.LogInformation("Resetting PIN for enumerator with ID: {EnumeratorId}", id);
            try
            {
                await _enumeratorService.ResetEnumeratorPin(id, User.Identity?.Name);
                _logger.LogInformation("Successfully reset PIN for enumerator ID: {EnumeratorId}", id);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting PIN for enumerator ID: {EnumeratorId}", id);
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpPost]
        public async Task<ActionResult> CreateEnumerator(EnumeratorDto data)
        {
            _logger.LogInformation("Updating enumerator with ID: {EnumeratorId}", data.EnumeratorId);
            try
            {
                await _enumeratorService.CreateEnumerator(data, User.Identity.Name);
                _logger.LogInformation("Enumerator updated successfully with ID: {EnumeratorId}", data.EnumeratorId);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating enumerator with ID: {EnumeratorId}", data.EnumeratorId);
                return StatusCode(500, "Internal server error.");
            }
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEnumerator(int id, EnumeratorDto data)
        {
            Enumerator enumerator = new Enumerator()
            {
                //TenantId = Convert.ToInt32(User.Tenant),
                EnumeratorId = id,
                FullName = data.FullName,
                EnumeratorCode = data.EnumeratorCode,
                EnumeratorType = data.EnumeratorType,
                IsPinUpdated = data.IsPinUpdated,
                IsSupervisor = data.IsSupervisor,
                IsActive = data.IsActive,
                Note = data.Note
            };

            _enumeratorService.UpdateEnumerator(enumerator, User.Identifier());

            return Ok();
        }
        [HttpGet("batches")]
        public async Task<ActionResult<IEnumerable<EnumeratorCodeBatch>>> GetAllCodeBatches()
        {
            _logger.LogInformation("Fetching all enumerator code batches.");
            try
            {
                var batches = await _enumeratorService.GetAllCodeBatches(User.Tenant());
                _logger.LogInformation("Fetched {Count} code batches.", batches?.Count() ?? 0);
                return Ok(batches);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching enumerator code batches.");
                return StatusCode(500, "Internal server error.");
            }
        }


        [HttpPost("generate")]
        public async Task<ActionResult> CreateEnumeratorCodeBatch(EnumeratorCodeBatchDto data)
        {
            data.TenantId = User.Tenant();
            await _enumeratorService.CreateEnumeratorCodeBatch(data, User.Identity.Name);

            return Ok();
        }


        [HttpPost("generate/codes")]
        public async Task<ActionResult> GetAllEnumerators(EnumeratorCodeBatchDto data)
        {
            _logger.LogInformation("Generating enumerator codes for batch: {Mission}", data.TenantId);
            try
            {
                data.TenantId = User.Tenant();
                await _enumeratorService.CreateEnumeratorCodeBatch(data, User.Identity?.Name);
                _logger.LogInformation("Enumerator code batch {Mission} generated successfully.", data.TenantId);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating enumerator code batch: {Mission}", data.TenantId);
                return StatusCode(500, "Internal server error.");
            }
        }
    }
}
