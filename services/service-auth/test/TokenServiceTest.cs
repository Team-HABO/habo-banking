using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using service_auth;
using service_auth.Services;

namespace test
{
    public class TokenServiceTest
    {
        private readonly string authServiceUrl = "https://test-auth-service.com";
        private readonly string balanceUrl = "https://test-balance-service.com";
        private readonly string accountUrl = "https://test-account-service.com";
        private readonly string userId = Guid.NewGuid().ToString();
        private readonly string email = "ok@ok.dk";
        private readonly string validApiKey = "this_is_a_very_long_secret_key_32_chars!!";

        [Fact]
        public void TestGenerateTokenSuccess()
        {
            AppSettings appSettings = new()
            {
                SecretKeyJwt = validApiKey,
                AuthServiceUrl = authServiceUrl,
                BalanceServiceUrl = balanceUrl,
                AccountServiceUrl = accountUrl
            };

            TokenService tokenService = new(Options.Create(appSettings));

            string token = tokenService.CreateToken(userId, email);

            Assert.NotNull(token);
            JwtSecurityTokenHandler handler = new();
            JwtSecurityToken jwtToken = handler.ReadJwtToken(token);

            string? emailClaim = jwtToken.Payload[JwtRegisteredClaimNames.Email].ToString();
            string? idClaim = jwtToken.Payload[JwtRegisteredClaimNames.NameId].ToString();
            string? issuerClaim = jwtToken.Payload[JwtRegisteredClaimNames.Iss].ToString();
            List<string> audienceClaims = [.. jwtToken.Audiences];

            Assert.Equal(email, emailClaim);
            Assert.Equal(userId, idClaim);
            Assert.Equal(authServiceUrl, issuerClaim);
            Assert.Equal(2, audienceClaims.Count);
            Assert.Contains(balanceUrl, audienceClaims);
            Assert.Contains(accountUrl, audienceClaims);
        }
        [Fact]
        public void TestGenerateTokenApiKeyTooShortThrowEx()
        {
            AppSettings appSettings = new()
            {
                SecretKeyJwt = "this_is_not_32_chars!!",
                AuthServiceUrl = authServiceUrl,
                BalanceServiceUrl = balanceUrl,
                AccountServiceUrl = accountUrl
            };

            TokenService tokenService = new(Options.Create(appSettings));

            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => tokenService.CreateToken(userId, email));

            Assert.Equal("IDX10720: Unable to create KeyedHashAlgorithm for algorithm 'http://www.w3.org/2001/04/xmldsig-more#hmac-sha256', " +
                "the key size must be greater than: '256' bits, key has '176' bits. (Parameter 'keyBytes')", exception.Message);

        }
        [Fact]
        public void TestGenerateTokenNoApiKeyThrowEx()
        {
            AppSettings appSettings = new()
            {
                AuthServiceUrl = authServiceUrl,
                BalanceServiceUrl = balanceUrl,
                AccountServiceUrl = accountUrl
            };

            TokenService tokenService = new(Options.Create(appSettings));

            ArgumentException exception = Assert.Throws<ArgumentException>(() => tokenService.CreateToken(userId, email));

            Assert.Equal("IDX10703: Cannot create a 'Microsoft.IdentityModel.Tokens.SymmetricSecurityKey', key length is zero.", exception.Message);
        }
        [Fact]
        public void TestGenerateTokenNoBalanceAudienceThrowEx()
        {
            AppSettings appSettings = new()
            {
                SecretKeyJwt = validApiKey,
                AuthServiceUrl = authServiceUrl,
                AccountServiceUrl = accountUrl

            };

            TokenService tokenService = new(Options.Create(appSettings));

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => tokenService.CreateToken(userId, email));

            Assert.Equal("Balance service URL is not configured for audience claim.", exception.Message);
        }
        [Fact]
        public void TestGenerateTokenNoAccountAudienceThrowEx()
        {
            AppSettings appSettings = new()
            {
                SecretKeyJwt = validApiKey,
                AuthServiceUrl = authServiceUrl,
                BalanceServiceUrl = balanceUrl
            };

            TokenService tokenService = new(Options.Create(appSettings));

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => tokenService.CreateToken(userId, email));

            Assert.Equal("Account service URL is not configured for audience claim.", exception.Message);
        }
        [Fact]
        public void TestGenerateTokenNoIssuerThrowEx()
        {
            AppSettings appSettings = new()
            {
                SecretKeyJwt = validApiKey,
                BalanceServiceUrl = balanceUrl,
                AccountServiceUrl = accountUrl
            };

            TokenService tokenService = new(Options.Create(appSettings));

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => tokenService.CreateToken(userId, email));

            Assert.Equal("JWT issuer is not configured.", exception.Message);
        }
    }
}
