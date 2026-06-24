using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;

namespace BRaVe_Management_Backend.Controllers
{
    [Route("api/DataProcessing/LawfulBases")]
    [ApiController]
    [Authorize]
    public class DataProcessingLawfulBasesController : ControllerBase
    {

        public DataProcessingLawfulBasesController()
        {
        }
        
        [HttpGet("{assessmentId}")]
        public async Task<ActionResult<DataProcessingLawfulBasis>> GetById(int assessmentId)
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        public async Task<ActionResult<DataProcessingLawfulBasis>> Create(DataProcessingLawfulBasis data)
        {
            //get generated id
            //DataProcessingLawfulBasis lawfulBasis = new DataProcessingLawfulBasis();

            return CreatedAtAction(nameof(GetById), new { assessmentId = data.AssessmentId }, data);
        }

        [HttpPut("{assessmentId}")]
        public async Task<IActionResult> Update(int assessmentId, DataProcessingLawfulBasis data)
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
