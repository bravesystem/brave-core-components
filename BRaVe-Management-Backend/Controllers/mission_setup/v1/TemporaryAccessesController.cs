using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BRaVe_Management_Backend.Controllers
{
    [Route("v1/api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class TemporaryAccessesController : ControllerBase
    {
    }
}
