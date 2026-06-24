using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;

namespace BRaVe_Management_Backend.Controllers
{
    [Route("api/DataProcessing/PersonalData")]
    [ApiController]
    [Authorize]
    public class DataProcessingPersonalDataController : ControllerBase
    {


        public DataProcessingPersonalDataController()
        {
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DataProcessingPersonalData>>> GetAll(int assessmentId)
        {
            throw new NotImplementedException();
        }

        [HttpGet("{assessmentId}/{categoryId}")]
        public async Task<ActionResult<DataProcessingPersonalData>> GetById(int assessmentId, int categoryId)
        {
            throw new NotImplementedException();
        }

       

        [HttpPut("{assessmentId}/{categoryId}")]
        public async Task<IActionResult> Update(int assessmentId, int categoryId, DataProcessingPersonalData data)
        {
            return NoContent();
        }

        [HttpDelete("{assessmentId}/{categoryId}")]
        public async Task<IActionResult> Delete(int assessmentId, int categoryId)
        {

            return NoContent();
        }


    }
}
