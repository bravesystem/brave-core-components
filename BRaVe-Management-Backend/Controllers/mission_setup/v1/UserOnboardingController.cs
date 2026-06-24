using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BRaVe_Management_Backend.Controllers.mission_setup
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class UserOnboardingController : ControllerBase
    {
    }
}
