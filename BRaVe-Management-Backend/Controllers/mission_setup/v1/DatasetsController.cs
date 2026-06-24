using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Extensions;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text.Json;

namespace BRaVe_Management_Backend.Controllers.mission_setup.v1
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class DatasetsController : ControllerBase
    {
        private readonly ILogger<DatasetsController> _logger;
        private readonly IDatasetService _datasetService;

        public DatasetsController(ILogger<DatasetsController> logger, IDatasetService api)
        {
            _logger = logger;
            _datasetService = api;
        }

        // GET: api/v1/datasets
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var tenantId = User.Tenant();
                _logger.LogInformation("Fetching all datasets");

                var datasets = await _datasetService.GetAllAsync(tenantId);
                _logger.LogInformation("Fetched {Count} datasets", datasets.Count);
                return Ok(datasets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching datasets");
                return StatusCode(500, "An error occurred while fetching datasets");
            }
        }

        // GET: api/v1/datasets/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var tenantId = User.Tenant();
                _logger.LogInformation("Fetching dataset with ID {Id}", id);

                var dataset = await _datasetService.GetByIdAsync(id, tenantId);
              
                if (dataset == null)
                {
                    _logger.LogWarning("Dataset with ID {Id} not found", id);
                    return NotFound();
                }

                return Ok(dataset);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching dataset with ID {Id}", id);
                return StatusCode(500, "An error occurred while fetching the dataset");
            }
        }

        // POST: api/v1/datasets
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DatasetsDto newDataset)
        {
            var tenantId = User.Tenant();
            var userId = User.Identifier();

            _logger.LogInformation(
                "POST Create Dataset requested by User {UserId} for Tenant {TenantId}",
                userId,
                tenantId
            );

            if (newDataset == null)
            {
                _logger.LogWarning("Create dataset failed: payload is null");
                return BadRequest("Dataset cannot be null");
            }

            if (string.IsNullOrWhiteSpace(newDataset.SchemaJson))
            {
                _logger.LogWarning(
                    "Create dataset failed: SchemaJson missing for dataset '{Title}'",
                    newDataset.Title
                );
                return BadRequest("SchemaJson is required");
            }

            try
            {
                // Validate JSON schema
                using var doc = JsonDocument.Parse(newDataset.SchemaJson);

                _logger.LogInformation(
                    "Creating dataset '{Title}'",
                    newDataset.Title
                );

                var datasetId = await _datasetService.CreateAsync(
                    newDataset,
                    tenantId,
                    userId
                );

                _logger.LogInformation(
                    "Dataset '{Title}' created successfully with Id {DatasetId}",
                    newDataset.Title,
                    datasetId
                );

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = datasetId },
                    new
                    {
                        Id = datasetId,
                        newDataset.Title
                    }
                );
            }
            catch (JsonException ex)
            {
                _logger.LogError(
                    ex,
                    "Invalid SchemaJson for dataset '{Title}'",
                    newDataset.Title
                );

                return BadRequest("Invalid JSON format in SchemaJson");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unhandled error creating dataset '{Title}' for Tenant {TenantId}",
                    newDataset.Title,
                    tenantId
                );

                return StatusCode(500, "Failed to create dataset");
            }
        }
        // PUT: api/v1/datasets/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] DatasetsDto dataset)
        {
            var tenantId = User.Tenant();
            var userId = User.Identifier();

            if (dataset == null)
            {
                _logger.LogWarning("Update dataset called with null payload");
                return BadRequest("Dataset cannot be null");
            }

            if (id != dataset.Id)
            {
                _logger.LogWarning("Dataset ID mismatch. Route ID {RouteId}, Payload ID {PayloadId}", id, dataset.Id);
                return BadRequest("Dataset ID mismatch");
            }

            if (string.IsNullOrWhiteSpace(dataset.SchemaJson))
            {
                _logger.LogWarning("Update dataset called with empty SchemaJson");
                return BadRequest("SchemaJson is required");
            }

            try
            {
                // Validate JSON
                using var doc = JsonDocument.Parse(dataset.SchemaJson);

                _logger.LogInformation("Updating dataset {Id} - {Title}", id, dataset.Title);

                var updated = await _datasetService.UpdateAsync(dataset, tenantId, userId);

                if (!updated)
                {
                    _logger.LogWarning("Dataset {Id} not found or inactive", id);
                    return NotFound();
                }

                _logger.LogInformation("Dataset {Id} updated successfully", id);
                return NoContent();
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Invalid JSON provided for dataset update");
                return BadRequest("Invalid JSON format in SchemaJson");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating dataset {Id}", id);
                return StatusCode(500, "Failed to update dataset");
            }
        }


        // DELETE: api/v1/datasets/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var tenantId = User.Tenant();
                _logger.LogInformation("Deleting dataset with ID {Id}", id);

                await _datasetService.DeactivateAsync(id, tenantId, User.Identifier());

                _logger.LogInformation("Dataset {Id} deleted successfully", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting dataset with ID {Id}", id);
                return StatusCode(500, "Failed to delete dataset");
            }
        }
    }
}
