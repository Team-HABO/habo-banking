using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace service_auth
{
    /// <summary>
    /// Application configuration settings loaded from environment variables or appsettings.json.
    /// All required settings are validated at application startup.
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// The URL of the frontend application. Used for CORS policy and redirects after authentication.
        /// </summary>
        [Required]
        
        public string FrontendUrl { get; set; } = string.Empty;

        /// <summary>
        /// The URL of the authentication service. Used as the JWT issuer and OAuth redirect URI.
        /// </summary>
        [Required]
        [NotNull]
        public string AuthServiceUrl { get; set; } = string.Empty;

        /// <summary>
        /// The URL of the balance service. Used as the JWT audience.
        /// </summary>
        [Required]
        [NotNull]
        public string BalanceServiceUrl { get; set; } = string.Empty;

        /// <summary>
        /// The URL of the account service. Used as the JWT audience.
        /// </summary>
        [Required]
        [NotNull]
        public string AccountServiceUrl { get; set; } = string.Empty;

        /// <summary>
        /// Secret key used for signing JWT tokens. Must be at least 32 characters (256 bits) for security.
        /// </summary>
        [Required]
        [MinLength(32)]
        public string SecretKeyJwt { get; set; } = string.Empty;

        /// <summary>
        /// External authentication provider settings.
        /// </summary>
        [Required]
        public AuthenticationSettings Authentication { get; set; } = new();
    }

    /// <summary>
    /// Configuration settings for external authentication providers.
    /// </summary>
    public class AuthenticationSettings
    {
        /// <summary>
        /// Google OAuth authentication settings.
        /// </summary>
        [Required]
        public GoogleSettings Google { get; set; } = new();
    }

    /// <summary>
    /// Google OAuth 2.0 authentication configuration.
    /// Obtain ClientId and ClientSecret from the Google Cloud Console.
    /// </summary>
    public class GoogleSettings
    {
        /// <summary>
        /// Google OAuth 2.0 Client ID from Google Cloud Console.
        /// </summary>
        [Required]
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// Google OAuth 2.0 Client Secret from Google Cloud Console.
        /// </summary>
        [Required]
        public string ClientSecret { get; set; } = string.Empty;
    }
}
