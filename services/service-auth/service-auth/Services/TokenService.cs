using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace service_auth.Services
{
    /// <summary>
    /// Service for creating and managing JWT tokens for authenticated users.
    /// </summary>
    public class TokenService(IOptions<AppSettings> options) : ITokenService
    {
        private readonly AppSettings _appSettings = options.Value;

        /// <summary>
        /// Creates a JWT token containing user claims from Google authentication.
        /// </summary>
        /// <param name="googleUserId">The unique user identifier from Google OAuth.</param>
        /// <param name="email">The user's email address from Google.</param>
        /// <returns>A signed JWT token string valid for 10 minutes.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the JWT secret key is less than 32 bytes.</exception>
        public string CreateToken(string googleUserId, string email)
        {
            if (string.IsNullOrEmpty(_appSettings.AuthServiceUrl))
                throw new InvalidOperationException("JWT issuer is not configured.");
            if (string.IsNullOrEmpty(_appSettings.BalanceServiceUrl))
                throw new InvalidOperationException("Balance service URL is not configured for audience claim.");
            if (string.IsNullOrEmpty(_appSettings.AccountServiceUrl))
                throw new InvalidOperationException("Account service URL is not configured for audience claim.");

            byte[] key = Encoding.UTF8.GetBytes(_appSettings.SecretKeyJwt);

            JwtSecurityTokenHandler tokenHandler = new ();
            SecurityTokenDescriptor tokenDescriptor = new()
            {
                Subject = new ClaimsIdentity(
                    [
                        new(ClaimTypes.Email, email),
                        new(ClaimTypes.NameIdentifier, googleUserId),
                        new(JwtRegisteredClaimNames.Aud, _appSettings.BalanceServiceUrl),
                        new(JwtRegisteredClaimNames.Aud, _appSettings.AccountServiceUrl),
                    ]),
                Expires = DateTime.UtcNow.AddMinutes(10),
                Issuer = _appSettings.AuthServiceUrl,
                // Remove the Audience property — it's handled by the claims above
                SigningCredentials = new SigningCredentials(
        new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);
            string jwtToken = tokenHandler.WriteToken(token);

            return jwtToken;
        }
    }
}
