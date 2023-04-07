using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NOAM_ASISTENCIA_V2.Server.Controllers.Authentication
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        [HttpGet("[controller]/[action]")]
        public IActionResult ForgotPassword()
        {
            return Ok();
        }
    }
}
