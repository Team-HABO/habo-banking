using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using service_auth.Services;

namespace service_auth.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(ITokenService tokenService, IOptions<AppSettings> options) : ControllerBase
    {
        private readonly AppSettings _appSettings = options.Value;

        /// <summary>
        /// Initiates Google OAuth login flow.
        /// </summary>
        /// <returns>Redirect to Google authentication</returns>
        [HttpGet("login")]
        public IActionResult Login()
        {
            AuthenticationProperties properties = new()
            {
                RedirectUri = $"{_appSettings.AuthServiceUrl}/api/auth/google-response"
            };

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }
        /// <summary>
        /// Handles the OAuth callback from Google, creates JWT token, and sets authentication cookie.
        /// </summary>
        /// <returns>Redirect to frontend URL with authentication cookie set</returns>
        [HttpGet("google-response")]
        public async Task<IActionResult> GoogleResponse()
        {
            AuthenticateResult result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!result.Succeeded)
                return Unauthorized("Google authentication failed.");

            string? googleUserId = result.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            string? email = result.Principal.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(googleUserId))
                return BadRequest("Could not retrieve User ID from Google.");
            if (string.IsNullOrEmpty(email))
                return BadRequest("Could not retrieve email from Google.");

            string token = tokenService.CreateToken(googleUserId, email);

            // Set token in HTTP-only cookie
            Response.Cookies.Append("auth_token", token, new CookieOptions
            {
                HttpOnly = true,  // Prevents JavaScript access
                Secure = true,
                SameSite = SameSiteMode.None, 
                Expires = DateTimeOffset.UtcNow.AddMinutes(10),
                Path = "/"
            });

            // .AddCookie() in Program.cs generates a cookie we don't use, so we sign out of that to clear it from the browser. 
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return Redirect(_appSettings.FrontendUrl);
        }
        /// <summary>
        /// Deletes the authentication cookie to log the user out. 
        /// </summary>
        /// <returns>200 OK response with success message</returns>
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("auth_token", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax
            });

            return Ok(new { message = "Logged out successfully" });
        }
    }
}
