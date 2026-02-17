using Microsoft.AspNetCore.Mvc;

namespace service_auth.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("Hello from AuthController!");
        }
    }
}
