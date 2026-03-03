using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace service_auth.Services
{
    public class TokenService(IConfiguration configuration) : ITokenService
    {
        public string CreateToken(string googleUserId, string email)
        {
            string apiKey = configuration["SecretKeyJwt"]
                ?? throw new InvalidOperationException("API key for JWT is not configured.");
            byte[] key = Encoding.UTF8.GetBytes(apiKey);

            if (key.Length < 32)
                throw new InvalidOperationException($"API key for JWT must be at least 32 bytes (256 bits). Current key is {key.Length} bytes ({key.Length * 8} bits). Please update your configuration with a longer key.");

            JwtSecurityTokenHandler tokenHandler = new ();
            SecurityTokenDescriptor tokenDescriptor = new()
            {
                Subject = new ClaimsIdentity(
                [
                new(ClaimTypes.Email, email),
                new(ClaimTypes.NameIdentifier, googleUserId)
                ]),
                Expires = DateTime.UtcNow.AddMinutes(10),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);
            string jwtToken = tokenHandler.WriteToken(token);

            return jwtToken;
        }
    }
}
