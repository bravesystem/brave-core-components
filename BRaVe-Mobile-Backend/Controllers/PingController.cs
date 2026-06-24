using BRaVe_Mobile_Backend.Controllers.server_operations;
using BRaVe_Mobile_Backend.Helpers;
using BRaVe_Mobile_Backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BRaVe_Mobile_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PingController : ControllerBase
    {
        private readonly IRequestContextValidator _contextValidator;
        private readonly ILogger<PingController> _logger;
        //private readonly IServiceBusSender _bus;

        public PingController(/*IServiceBusSender bus,*/ IRequestContextValidator contextValidator, ILogger<PingController> logger)
        {
            //_bus = bus;
            _contextValidator = contextValidator;
            _logger = logger;

        }

        /*[HttpGet("sb/{aaaa}")]
        public bool TestSB(string aaaa)
        {
            _bus.SendMessageAsync(aaaa);
            return true;
        }*/



        [HttpGet]
        public bool Get()
        {
            return true;
        }


        [HttpGet("secure")]
        [Authorize]
        public async Task<ActionResult<bool>> PingSecure()
        {
            
            var result = await _contextValidator.ValidateAsync(User, "");
            if (!result.Ok)
                return StatusCode(result.StatusCode, new { error = result.Error });


            return true;
        }
    }
}
