using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using service_auth.Services;

namespace service_auth.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(ITokenService tokenService, IConfiguration configuration) : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("Hello from AuthController!");
        }
        [HttpGet("login")]
        public IActionResult Login()
        {
            AuthenticationProperties properties = new()
            {
                RedirectUri = "http://localhost:8082/api/auth/google-response"
            };

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }
        [HttpGet("google-response")]
        public async Task<IActionResult> GoogleResponse()
        {
            AuthenticateResult result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            string frontendUrl = configuration["FrontendUrl"]
                ?? throw new InvalidOperationException("Frontend URL not set in enviroment variables");

            if (!result.Succeeded)
                return Unauthorized("Google authentication failed.");

            string? googleUserId = result.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            string? email = result.Principal.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(googleUserId))
                return BadRequest("Could not retrieve User ID from Google.");
            if (string.IsNullOrEmpty(email))
                return BadRequest("Could not retrieve email from Google.");

            string token = await tokenService.CreateToken(googleUserId, email);

            // Set token in HTTP-only cookie
            Response.Cookies.Append("auth_token", token, new CookieOptions
            {
                HttpOnly = true,  // Prevents JavaScript access
                Secure = Request.IsHttps,    // Works in HTTP local dev and HTTPS environments
                SameSite = SameSiteMode.Lax,  // CSRF protection
                Expires = DateTimeOffset.UtcNow.AddHours(1),
                Path = "/"
            });

            // Redirect without token in URL
            return Redirect(frontendUrl);
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            // Delete the auth token cookie
            Response.Cookies.Delete("auth_token", new CookieOptions
            {
                Path = "/"
            });

            // Also delete the ASP.NET Core authentication cookie if present
            Response.Cookies.Delete(".AspNetCore.Cookies", new CookieOptions
            {
                Path = "/"
            });

            return Ok(new { message = "Logged out successfully" });
        }
    }
}
