using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BRaVe_Management_Backend.Controllers
{
    [Route("api/DataProcessing/DataDisclosureRecipients")]
    [ApiController]
    [Authorize]
    public class DataProcessingDataDisclosureRecipientsController : ControllerBase
    {
        public DataProcessingDataDisclosureRecipientsController()
        {
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DataProcessingDataDisclosureRecipient>>> GetAll(int assessmentId)
        {
            Log.Information("GetAll called for assessmentId {AssessmentId}", assessmentId);
            throw new NotImplementedException();
        }

        [HttpGet("{assessmentId}/{recipientId}")]
        public async Task<ActionResult<DataProcessingDataDisclosureRecipient>> GetById(int assessmentId, int recipientId)
        {
            Log.Information("GetById called for assessmentId {AssessmentId}, recipientId {RecipientId}", assessmentId, recipientId);
            throw new NotImplementedException();
        }

        [HttpPost]
        public async Task<ActionResult<DataProcessingDataDisclosureRecipient>> Create(DataProcessingDataDisclosureRecipientDto data)
        {
            Log.Information("Create called for assessmentId {AssessmentId}", data.AssessmentId);

            DataProcessingDataDisclosureRecipient recipient = new DataProcessingDataDisclosureRecipient();
            return CreatedAtAction(nameof(GetById), new { assessmentId = data.AssessmentId, recipientId = recipient.RecipientId }, recipient);
        }

        [HttpPut("{assessmentId}/{recipientId}")]
        public async Task<IActionResult> Update(int assessmentId, int recipientId, DataProcessingDataDisclosureRecipientDto data)
        {
            Log.Information("Update called for assessmentId {AssessmentId}, recipientId {RecipientId}", assessmentId, recipientId);
            return NoContent();
        }

        [HttpDelete("{assessmentId}/{recipientId}")]
        public async Task<IActionResult> Delete(int assessmentId, int recipientId)
        {
            Log.Information("Delete called for assessmentId {AssessmentId}, recipientId {RecipientId}", assessmentId, recipientId);
            return NoContent();
        }
    }
}
