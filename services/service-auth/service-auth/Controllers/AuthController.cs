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
        /// <summary>
        /// Initiates Google OAuth login flow.
        /// </summary>
        /// <returns>Redirect to Google authentication</returns>
        [HttpGet("login")]
        public IActionResult Login()
        {
            string gatewayUrl = configuration["ApiGatewayUrl"]
                ?? throw new InvalidOperationException("Gateway URL not set in environment variables");
            AuthenticationProperties properties = new()
            {
                RedirectUri = $"{gatewayUrl}/api/auth/google-response"
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


            string frontendUrl = configuration["FrontendUrl"]
                ?? throw new InvalidOperationException("Frontend URL not set in environment variables");

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

            return Redirect(frontendUrl);
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
