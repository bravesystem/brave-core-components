using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Extensions;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BRaVe_Management_Backend.Controllers.risk_assessments
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class PmDecisionsController : ControllerBase
    {
        private readonly IPmDecisionService _pmDecisionService;
        public PmDecisionsController(IPmDecisionService pmDecisionService)
        {
            _pmDecisionService = pmDecisionService;
        }


        [HttpGet("current/{assessmentId}")]
        public async Task<ActionResult<Decision>> GetCurrentDecision(int assessmentId)
        {
            //get the generated id
            try
            {
                Decision coMDecision = await _pmDecisionService.GetCurrentDecision(assessmentId, User.Tenant());
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
                _pmDecisionService.SaveDecision(data, User.Tenant(), User.Identifier());
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
