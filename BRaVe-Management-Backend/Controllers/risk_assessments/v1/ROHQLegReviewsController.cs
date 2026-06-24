using BRaVe_Management_Backend.DTOs;
using BRaVe_Management_Backend.Extensions;
using BRaVe_Management_Backend.Interfaces;
using BRaVe_Management_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BRaVe_Management_Backend.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class ROHQLegReviewsController : ControllerBase
    {
        private readonly IRecommendationService _recommendationService;
        public ROHQLegReviewsController(IRecommendationService recommendationService)
        {
            _recommendationService = recommendationService;
        }

        [HttpGet("current/{assessmentId}/{isleg}")]
        public async Task<ActionResult<Recommendation>> GetCurrentRecommendation(int assessmentId, bool isleg)
        {
            //get the generated id
            try
            {
                Recommendation recommendation = await _recommendationService.GetCurrentRecommendation(assessmentId, isleg, User.Tenant());
                return Ok(recommendation);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<ActionResult> SaveRecommendation(RecommendationDto data)
        {
            //get the generated id
            try
            {
                _recommendationService.SaveRecommendation(data, User.Tenant(), User.Identifier());
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
