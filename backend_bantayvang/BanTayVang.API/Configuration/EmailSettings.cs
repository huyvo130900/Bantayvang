namespace BanTayVang.API.Configuration
{
    /// <summary>
    /// Email/SMTP settings
    /// OWASP A02: Cryptographic Failures - Use TLS for SMTP
    /// </summary>
    public class EmailSettings
    {
        public const string SectionName = "EmailSettings";

        public string SmtpHost { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public string SmtpUsername { get; set; } = string.Empty;
        public string SmtpPassword { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = "BanTayVang System";
        public bool UseSsl { get; set; } = true;
        public bool EnableEmailSending { get; set; } = false; // Default off for dev
    }
}