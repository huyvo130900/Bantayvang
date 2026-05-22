namespace BanTayVang.API.Services.Interfaces
{
    /// <summary>
    /// Email service interface
    /// OWASP A02 / A09: Secure email transmission and logging
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Send a generic email
        /// </summary>
        Task<bool> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true);

        /// <summary>
        /// Send password reset email
        /// </summary>
        Task<bool> SendPasswordResetEmailAsync(string toEmail, string fullName, string resetToken);

        /// <summary>
        /// Send welcome email after registration
        /// </summary>
        Task<bool> SendWelcomeEmailAsync(string toEmail, string username, string fullName);

        /// <summary>
        /// Send exam result notification
        /// </summary>
        Task<bool> SendExamResultEmailAsync(string toEmail, string fullName, string examName, double score, bool pass);
    }
}