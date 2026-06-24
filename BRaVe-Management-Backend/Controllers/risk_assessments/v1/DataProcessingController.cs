using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog; 
using System;

namespace BRaVe_Management_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DataProcessingController : ControllerBase
    {
        public DataProcessingController()
        {
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<DataProcessing>> GetById(int id)
        {
            try
            {
                Log.Information("Fetching DataProcessing by Id: {Id}", id);

                throw new NotImplementedException();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error fetching DataProcessing by Id: {Id}", id);
                return StatusCode(500, "An error occurred while fetching the data.");
            }
        }

        [HttpPost]
        public async Task<ActionResult<DataProcessing>> Create(DataProcessing data)
        {
            try
            {
                Log.Information("Creating new DataProcessing with data: {@Data}", data);

                return CreatedAtAction(nameof(GetById), new { id = data.AssessmentId }, data);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating DataProcessing with data: {@Data}", data);
                return StatusCode(500, "An error occurred while creating the data.");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, DataProcessing data)
        {
            try
            {
                Log.Information("Updating DataProcessing with Id: {Id} and data: {@Data}", id, data);

                return NoContent();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error updating DataProcessing with Id: {Id} and data: {@Data}", id, data);
                return StatusCode(500, "An error occurred while updating the data.");
            }
        }
    }
}
