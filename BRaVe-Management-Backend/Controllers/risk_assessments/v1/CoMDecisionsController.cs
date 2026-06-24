using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Extensions;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BRaVe_Management_Backend.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class CoMDecisionsController : ControllerBase
    {
        private readonly ICoMDecisionService _coMDecisionService;
        public CoMDecisionsController(ICoMDecisionService coMDecisionService)
        {
            _coMDecisionService = coMDecisionService; 
        }


        [HttpGet("current/{assessmentId}")]
        public async Task<ActionResult<Decision>> GetCurrentDecision(int assessmentId)
        {
            //get the generated id
            try
            {
                Decision  coMDecision= await _coMDecisionService.GetCurrentDecision(assessmentId, User.Tenant());
                return Ok(coMDecision);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<ActionResult<Decision>> SaveDecision(DecisionDto data)
        {
            //get the generated id
            try
            {
                _coMDecisionService.SaveDecision(data, User.Tenant(), User.Identifier());
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

    }
}
