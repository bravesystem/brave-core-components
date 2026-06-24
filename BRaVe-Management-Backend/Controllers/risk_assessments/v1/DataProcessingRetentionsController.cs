using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BRaVe_Management_Backend.Controllers
{
    [Route("api/DataProcessing/Retentions")]
    [ApiController]
    [Authorize]
    public class DataProcessingRetentionsController : ControllerBase
    {
        public DataProcessingRetentionsController()
        {
        }

        [HttpGet("{assessmentId}")]
        public async Task<ActionResult<DataProcessingRetention>> GetById(int assessmentId)
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        public async Task<ActionResult<DataProcessingRetention>> Create(DataProcessingRetention data)
        {
            //get id generated

            return CreatedAtAction(nameof(GetById), new { assessmentId = data.AssessmentId }, data);
        }

        [HttpPut("{assessmentId}")]
        public async Task<IActionResult> Update(int assessmentId, DataProcessingRetention data)
        {


            return NoContent();
        }

        [HttpDelete("{assessmentId}")]
        public async Task<IActionResult> Delete(int assessmentId)
        {


            return NoContent();
        }
    }
}
