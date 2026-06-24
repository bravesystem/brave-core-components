using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;

namespace BRaVe_Management_Backend.Controllers
{
    [Route("api/DataProcessing/DataSharingRecipients")]
    [ApiController]
    [Authorize]
    public class DataProcessingDataSharingRecipientsController : ControllerBase
    {

        public DataProcessingDataSharingRecipientsController()
        {
            
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DataProcessingDataSharingRecipient>>> GetAll(int assessmentId)
        {
            throw new NotImplementedException();
        }

        [HttpGet("{assessmentId}/{recipientId}")]
        public async Task<ActionResult<DataProcessingDataSharingRecipient>> GetById(int assessmentId, int recipientId)
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        public async Task<ActionResult<DataProcessingDataSharingRecipient>> Create(DataProcessingDataSharingRecipient data)
        {
            //get generated id
            //DataProcessingDataSharingRecipient sharingRecipient = new DataProcessingDataSharingRecipient();

            return CreatedAtAction(nameof(GetById), new { assessmentId = data.AssessmentId, recipientId = data.RecipientId }, data);
        }

        [HttpPut("{assessmentId}/{recipientId}")]
        public async Task<IActionResult> Update(int assessmentId, int recipientId, DataProcessingDataSharingRecipient data)
        {
           
            return NoContent();
        }

        [HttpDelete("{assessmentId}/{recipientId}")]
        public async Task<IActionResult> Delete(int assessmentId, int recipientId)
        {
         
            return NoContent();
        }

    }
}
