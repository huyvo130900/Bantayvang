namespace BanTayVang.API.Configuration
{
    /// <summary>
    /// JWT configuration settings
    /// OWASP A05: Security Misconfiguration prevention
    /// </summary>
    public class JwtSettings
    {
        public const string SectionName = "JwtSettings";

        /// <summary>
        /// Secret key for signing JWT tokens (should be at least 256 bits)
        /// </summary>
        public string SecretKey { get; set; } = string.Empty;

        /// <summary>
        /// Token issuer (your application name)
        /// </summary>
        public string Issuer { get; set; } = "BanTayVang.API";

        /// <summary>
        /// Token audience (your application URL)
        /// </summary>
        public string Audience { get; set; } = "BanTayVang.Client";

        /// <summary>
        /// Access token expiration time in minutes
        /// </summary>
        public int AccessTokenExpirationMinutes { get; set; } = 60; // 1 hour

        /// <summary>
        /// Refresh token expiration time in days
        /// </summary>
        public int RefreshTokenExpirationDays { get; set; } = 30; // 30 days

        /// <summary>
        /// Remember me token expiration time in days
        /// </summary>
        public int RememberMeExpirationDays { get; set; } = 90; // 3 months

        /// <summary>
        /// Whether to require HTTPS for token operations
        /// </summary>
        public bool RequireHttps { get; set; } = true;

        /// <summary>
        /// Whether to validate token audience
        /// </summary>
        public bool ValidateAudience { get; set; } = true;

        /// <summary>
        /// Whether to validate token issuer
        /// </summary>
        public bool ValidateIssuer { get; set; } = true;

        /// <summary>
        /// Whether to validate token lifetime
        /// </summary>
        public bool ValidateLifetime { get; set; } = true;

        /// <summary>
        /// Clock skew tolerance in minutes
        /// </summary>
        public int ClockSkewMinutes { get; set; } = 5;
    }
}