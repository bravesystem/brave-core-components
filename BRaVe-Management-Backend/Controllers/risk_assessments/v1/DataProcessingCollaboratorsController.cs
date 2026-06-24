using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System;

namespace BRaVe_Management_Backend.Controllers
{
    [Route("api/DataProcessing/Collaborators")]
    [ApiController]
    [Authorize]
    public class DataProcessingCollaboratorsController : ControllerBase
    {
        public DataProcessingCollaboratorsController()
        {

        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DataProcessingCollaborator>>> GetAll(int assessmentId)
        {
            throw new NotImplementedException();
        }

        [HttpGet("{assessmentId}/{partnerId}")]
        public async Task<ActionResult<DataProcessingCollaborator>> GetById(int assessmentId, int partnerId)
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        public async Task<ActionResult<DataProcessingCollaborator>> Create(DataProcessingCollaboratorDto data)
        {
            Log.Information("Creating a new DataProcessingCollaborator for AssessmentId: {AssessmentId}", data.AssessmentId);

            try
            {
                DataProcessingCollaborator dataProcessingCollaborator = new DataProcessingCollaborator();
                Log.Information("Successfully created DataProcessingCollaborator with PartnerId: {PartnerId}", dataProcessingCollaborator.PartnerId);

                return CreatedAtAction(nameof(GetById), new { assessmentId = data.AssessmentId, partnerId = dataProcessingCollaborator.PartnerId }, dataProcessingCollaborator);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while creating DataProcessingCollaborator for AssessmentId: {AssessmentId}", data.AssessmentId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred.");
            }
        }

        [HttpPut("{assessmentId}/{partnerId}")]
        public async Task<IActionResult> Update(int assessmentId, int partnerId, DataProcessingCollaboratorDto data)
        {
            Log.Information("Updating DataProcessingCollaborator with AssessmentId: {AssessmentId}, PartnerId: {PartnerId}", assessmentId, partnerId);

            try
            {
                return NoContent();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while updating DataProcessingCollaborator with AssessmentId: {AssessmentId}, PartnerId: {PartnerId}", assessmentId, partnerId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred.");
            }
        }

        [HttpDelete("{assessmentId}/{partnerId}")]
        public async Task<IActionResult> Delete(int assessmentId, int partnerId)
        {
            Log.Information("Deleting DataProcessingCollaborator with AssessmentId: {AssessmentId}, PartnerId: {PartnerId}", assessmentId, partnerId);

            try
            {
                return NoContent();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while deleting DataProcessingCollaborator with AssessmentId: {AssessmentId}, PartnerId: {PartnerId}", assessmentId, partnerId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred.");
            }
        }
    }
}

