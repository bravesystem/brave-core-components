using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;

namespace BRaVe_Management_Backend.Controllers
{
    [Route("api/DataProcessing/DataSubjects")]
    [ApiController]
    [Authorize]
    public class DataProcessingDataSubjectsController : ControllerBase
    {

        public DataProcessingDataSubjectsController()
        {
           
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DataProcessingDataSubject>>> GetAll(int assessmentId)
        {
            throw new NotImplementedException();
        }

        [HttpGet("{assessmentId}/{subjectTypeId}")]
        public async Task<ActionResult<DataProcessingDataSubject>> GetById(int assessmentId, int subjectTypeId)
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        public async Task<ActionResult<DataProcessingDataSubject>> Create(DataProcessingDataSubject data)
        {
            //get generated id
            //DataProcessingDataSubject dataSubject = new DataProcessingDataSubject();

            return CreatedAtAction(nameof(GetById), new { assessmentId = data.AssessmentId, subjectTypeId = data.SubjectTypeId }, data);
        }

        [HttpPut("{assessmentId}/{subjectTypeId}")]
        public async Task<IActionResult> Update(int assessmentId, int subjectTypeId, DataProcessingDataSubject data)
        {     
            return NoContent();
        }

        [HttpDelete("{assessmentId}/{subjectTypeId}")]
        public async Task<IActionResult> Delete(int assessmentId, int subjectTypeId)
        {
            
            return NoContent();
        }


    }
}
