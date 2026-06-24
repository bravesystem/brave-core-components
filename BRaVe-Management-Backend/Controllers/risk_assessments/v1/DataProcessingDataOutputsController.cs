using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BRaVe_Management_Backend.Controllers
{
    [Route("api/DataProcessing/DataOutputs")]
    [ApiController]
    [Authorize]
    public class DataProcessingDataOutputsController : ControllerBase
    {
        public DataProcessingDataOutputsController()
        {

        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DataProcessingDataOutput>>> GetAll(int assessmentId)
        {
            throw new NotImplementedException();
        }

        [HttpGet("{assessmentId}/{dataOutputId}/{recipientId}")]
        public async Task<ActionResult<DataProcessingDataOutput>> GetById(int assessmentId,int dataOutputId, int recipientId)
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        public async Task<ActionResult<DataProcessingDataOutput>> Create(DataProcessingDataOutputDto data)
        {
            //get generated id
            DataProcessingDataOutput dataOutput = new DataProcessingDataOutput();

            return CreatedAtAction(nameof(GetById), new { assessmentId = data.AssessmentId, dataOutputId= dataOutput.DataOutputId, recipientId = data.RecipientId }, dataOutput);
        }

        [HttpPut("{assessmentId}/{dataOutputId}/{recipientId}")]
        public async Task<IActionResult> Update(int assessmentId, int dataOutputId, int recipientId, DataProcessingDataOutputDto data)
        {

            return NoContent();
        }

        [HttpDelete("{assessmentId}/{dataOutputId}/{recipientId}")]
        public async Task<IActionResult> Delete(int assessmentId, int dataOutputId, int recipientId)
        {

            return NoContent();
        }
    }
}
