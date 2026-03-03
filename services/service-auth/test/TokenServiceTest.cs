using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using service_auth.Services;

namespace test
{
    public class TokenServiceTest
    {
        private readonly string jwtIssuer = "https://test-auth-service.com";
        private readonly string jwtAudience = "https://test-api.com";
        private readonly string userId = Guid.NewGuid().ToString();
        private readonly string email = "ok@ok.dk";
        private readonly string validApiKey = "this_is_a_very_long_secret_key_32_chars!!";
        [Fact]
        public void TestGenerateTokenSuccess()
        {
            
            Dictionary<string, string?> inMemorySettings = new()
            {
                {   
                    "SecretKeyJwt", validApiKey
                },
                {
                    "JwtIssuer", jwtIssuer
                },
                {
                    "JwtAudience", jwtAudience
                }
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
            TokenService tokenService = new(configuration);


            string token = tokenService.CreateToken(userId, email);

            Assert.NotNull(token);
            JwtSecurityTokenHandler handler = new();
            JwtSecurityToken jwtToken = handler.ReadJwtToken(token);

            string? emailClaim = jwtToken.Payload[JwtRegisteredClaimNames.Email].ToString();
            string? idClaim = jwtToken.Payload[JwtRegisteredClaimNames.NameId].ToString();
            string? issuerClaim = jwtToken.Payload[JwtRegisteredClaimNames.Iss].ToString();
            string? audienceClaim = jwtToken.Payload[JwtRegisteredClaimNames.Aud].ToString();

            Assert.Equal(email, emailClaim);
            Assert.Equal(userId, idClaim);
            Assert.Equal(jwtIssuer, issuerClaim);
            Assert.Equal(jwtAudience, audienceClaim);
        }
        [Fact]
        public void TestGenerateTokenApiKeyTooShortThrowEx()
        {
            Dictionary<string, string?> inMemorySettings = new()
            {
                {
                    "SecretKeyJwt", "this_y_32_chars!!"
                },
                {
                    "JwtIssuer", jwtIssuer
                },
                {
                    "JwtAudience", jwtAudience
                }
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
            TokenService tokenService = new(configuration);


            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => tokenService.CreateToken(userId, email));

            Assert.Equal("API key for JWT must be at least 32 bytes (256 bits). Current key is 17 bytes (136 bits). Please update your configuration with a longer key.", exception.Message);

        }
        [Fact]
        public void TestGenerateTokenNoApiKeyThrowEx()
        {
            Dictionary<string, string?> inMemorySettings = new()
            {
                {
                    "JwtIssuer", jwtIssuer
                },
                {
                    "JwtAudience", jwtAudience
                }
            };
            IConfiguration configuration = new ConfigurationBuilder()
                .Build();
            TokenService tokenService = new(configuration);
            string userId = Guid.NewGuid().ToString();
            string email = "ok@ok.dk";

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => tokenService.CreateToken(userId, email));
            Assert.Equal("API key for JWT is not configured.", exception.Message);
        }
        [Fact]
        public void TestGenerateTokenNoAudienceThrowEx()
        {
            Dictionary<string, string?> inMemorySettings = new()
            {
                {
                    "SecretKeyJwt", validApiKey
                },
                {
                    "JwtIssuer", jwtIssuer
                }
            };

            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
            TokenService tokenService = new(configuration);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => tokenService.CreateToken(userId, email));
            Assert.Equal("JWT audience is not configured.", exception.Message);
        }
        [Fact]
        public void TestGenerateTokenNoIssuerThrowEx()
        {
            Dictionary<string, string?> inMemorySettings = new()
            {
                {
                    "SecretKeyJwt", validApiKey
                },
                {
                    "JwtAudience", jwtAudience
                }
            };
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
            TokenService tokenService = new(configuration);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => tokenService.CreateToken(userId, email));
            Assert.Equal("JWT issuer is not configured.", exception.Message);
        }
    }
}
