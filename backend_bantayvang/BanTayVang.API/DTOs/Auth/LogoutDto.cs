namespace BanTayVang.API.DTOs.Auth
{
    /// <summary>
    /// Logout request DTO
    /// </summary>
    public class LogoutDto
    {
        /// <summary>
        /// Refresh token to revoke (optional)
        /// </summary>
        public string? RefreshToken { get; set; }

        /// <summary>
        /// Whether to logout from all devices
        /// </summary>
        public bool LogoutFromAllDevices { get; set; } = false;
    }
}