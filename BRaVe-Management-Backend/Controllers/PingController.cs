using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BRaVe_Management_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PingController : ControllerBase
    {

        [HttpGet]
        public bool Get()
        {
            return true;
        }
    }
}
