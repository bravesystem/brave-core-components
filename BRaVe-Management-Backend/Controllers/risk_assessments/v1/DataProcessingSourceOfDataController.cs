using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BRaVe_Management_Backend.Controllers
{
    [Route("api/DataProcessing/SourceOfData")]
    [ApiController]
    [Authorize]
    public class DataProcessingSourceOfDataController : ControllerBase
    {

        public DataProcessingSourceOfDataController()
        {
        }

        [HttpGet("{assessmentId}")]
        public async Task<ActionResult<DataProcessingLawfulBasis>> GetById(int assessmentId)
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        public async Task<ActionResult<DataProcessingSourceOfData>> Create(DataProcessingSourceOfData data)
        {

            return CreatedAtAction(nameof(GetById), new { assessmentId = data.AssessmentId }, data);
        }

        [HttpPut("{assessmentId}")]
        public async Task<IActionResult> Update(int assessmentId, DataProcessingSourceOfData data)
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
