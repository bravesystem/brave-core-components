using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BRaVe_Management_Backend.Controllers
{
    [Route("api/DataProcessing/SecurityMeasures")]
    [ApiController]
    [Authorize]
    public class DataProcessingSecurityMeasuresController : ControllerBase
    {

        public DataProcessingSecurityMeasuresController()
        {

        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DataProcessingSecurityMeasure>>> GetAll(int assessmentId)
        {
            throw new NotImplementedException();
        }

        [HttpGet("{assessmentId}/{securityMeasureId}")]
        public async Task<ActionResult<DataProcessingSecurityMeasure>> GetById(int assessmentId, int securityMeasureId)
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        public async Task<ActionResult<DataProcessingSecurityMeasure>> Create(DataProcessingSecurityMeasure data)
        {

            return CreatedAtAction(nameof(GetById), new { assessmentId = data.AssessmentId, securityMeasureId = data.SecurityMeasureId }, data);
        }

        [HttpPut("{assessmentId}/{securityMeasureId}")]
        public async Task<IActionResult> Update(int assessmentId, int securityMeasureId, DataProcessingSecurityMeasure data)
        {
            return NoContent();
        }

        [HttpDelete("{assessmentId}/{securityMeasureId}")]
        public async Task<IActionResult> Delete(int assessmentId, int securityMeasureId)
        {

            return NoContent();
        }
    }
}
