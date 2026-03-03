using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using service_auth.Services;

namespace test
{
    public class TokenServiceTest
    {
        [Fact]
        public void TestGenerateTokenSuccess()
        {
            Dictionary<string, string?> inMemorySettings = new()
            {
                {   
                    "SecretKeyJwt", "this_is_a_very_long_secret_key_32_chars!!"
                }
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
            TokenService tokenService = new(configuration);
            string userId = Guid.NewGuid().ToString();
            string email = "ok@ok.dk";

            string token = tokenService.CreateToken(userId, email);

            Assert.NotNull(token);
            JwtSecurityTokenHandler handler = new();
            JwtSecurityToken jwtToken = handler.ReadJwtToken(token);

            string? emailClaim = jwtToken.Payload[JwtRegisteredClaimNames.Email].ToString();
            string? idClaim = jwtToken.Payload[JwtRegisteredClaimNames.NameId].ToString();

            Assert.Equal(email, emailClaim);
            Assert.Equal(userId, idClaim);
        }
        [Fact]
        public void TestGenerateTokenApiKeyTooShortThrowEx()
        {
            Dictionary<string, string?> inMemorySettings = new()
            {
                {
                    "SecretKeyJwt", "this_y_32_chars!!"
                }
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
            TokenService tokenService = new(configuration);
            string userId = Guid.NewGuid().ToString();
            string email = "ok@ok.dk";

            Assert.Throws<InvalidOperationException>(() => tokenService.CreateToken(userId, email));
        }
        [Fact]
        public void TestGenerateTokenNoApiKeyThrowEx()
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .Build();
            TokenService tokenService = new(configuration);
            string userId = Guid.NewGuid().ToString();
            string email = "ok@ok.dk";

            Assert.Throws<InvalidOperationException>(() => tokenService.CreateToken(userId, email));
        }
    }
}
