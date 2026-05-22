using BanTayVang.API.DTOs.Auth;
using BanTayVang.API.DTOs.Common;
using BanTayVang.API.Models;
using BanTayVang.API.Repositories.Interfaces;
using BanTayVang.API.Services.Interfaces.Auth;

namespace BanTayVang.API.Services.Impl.Auth
{
    public class AuthService : IAuthService
    {
        private readonly ITaikhoanRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IUserSessionRepository _sessionRepository;
        private readonly IJwtService _jwtService;
        private readonly IPasswordService _passwordService;
        private readonly Services.Interfaces.IEmailService _emailService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            ITaikhoanRepository userRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IUserSessionRepository sessionRepository,
            IJwtService jwtService,
            IPasswordService passwordService,
            Services.Interfaces.IEmailService emailService,
            ILogger<AuthService> logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
            _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
            _jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
            _passwordService = passwordService ?? throw new ArgumentNullException(nameof(passwordService));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<BaseResponseDto<AuthResponseDto>> LoginAsync(LoginDto loginDto, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Login attempt for username: {Username}", loginDto.Username);

                // Input validation
                if (string.IsNullOrWhiteSpace(loginDto.Username) || string.IsNullOrWhiteSpace(loginDto.Password))
                {
                    return new BaseResponseDto<AuthResponseDto>
                    {
                        Success = false,
                        Message = "Tên đăng nhập và mật khẩu không được để trống"
                    };
                }

                // Get user by username or email
                var user = await _userRepository.GetByUsernameOrEmailAsync(loginDto.Username);
                if (user == null)
                {
                    return new BaseResponseDto<AuthResponseDto>
                    {
                        Success = false,
                        Message = "Tên đăng nhập hoặc mật khẩu không đúng"
                    };
                }

                // Check if user is active
                if (user.TrangThai != true)
                {
                    return new BaseResponseDto<AuthResponseDto>
                    {
                        Success = false,
                        Message = "Tài khoản đã bị vô hiệu hóa"
                    };
                }

                // Verify password
                if (!_passwordService.VerifyPassword(loginDto.Password, user.MatKhau ?? string.Empty))
                {
                    return new BaseResponseDto<AuthResponseDto>
                    {
                        Success = false,
                        Message = "Tên đăng nhập hoặc mật khẩu không đúng"
                    };
                }

                // Generate tokens
                var accessToken = _jwtService.GenerateAccessToken(user, loginDto.RememberMe);
                var refreshToken = _jwtService.GenerateRefreshToken();

                // Save refresh token
                var refreshTokenEntity = new RefreshToken
                {
                    Token = refreshToken,
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(loginDto.RememberMe ? 90 : 30),
                    IpAddress = loginDto.IpAddress,
                    UserAgent = loginDto.UserAgent
                };

                await _refreshTokenRepository.AddAsync(refreshTokenEntity);

                // Create user session
                var sessionId = Guid.NewGuid().ToString();
                var userSession = new UserSession
                {
                    SessionId = sessionId,
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddHours(loginDto.RememberMe ? 24 * 90 : 24),
                    IpAddress = loginDto.IpAddress,
                    UserAgent = loginDto.UserAgent,
                    IsActive = true
                };

                await _sessionRepository.AddAsync(userSession);

                // Update user last login
                user.LanDangNhapCuoi = DateTime.Now;
                await _userRepository.UpdateAsync(user);

                // Create response
                var userInfo = new UserInfoDto
                {
                    Id = user.Id,
                    Username = user.TenDangNhap ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    FullName = user.HoTen ?? string.Empty,
                    Role = GetRoleName(user.IdVaiTro),
                    IsActive = user.TrangThai ?? false,
                    LastLoginAt = user.LanDangNhapCuoi ?? DateTime.Now
                };

                var authResponse = new AuthResponseDto
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = _jwtService.GetTokenExpiration(accessToken) ?? DateTime.UtcNow.AddHours(1),
                    TokenType = "Bearer",
                    User = userInfo
                };

                return new BaseResponseDto<AuthResponseDto>
                {
                    Success = true,
                    Message = "Đăng nhập thành công",
                    Data = authResponse
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for username: {Username}", loginDto.Username);
                
                return new BaseResponseDto<AuthResponseDto>
                {
                    Success = false,
                    Message = "Có lỗi xảy ra trong quá trình đăng nhập",
                    Errors = new List<string> { ex.Message, ex.InnerException?.Message ?? "" }
                };
            }
        }

        public async Task<BaseResponseDto<AuthResponseDto>> RegisterAsync(RegisterDto registerDto, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Register attempt for username: {Username}, email: {Email}", 
                    registerDto.Username, registerDto.Email);

                // Input validation
                if (string.IsNullOrWhiteSpace(registerDto.Username) || 
                    string.IsNullOrWhiteSpace(registerDto.Password) ||
                    string.IsNullOrWhiteSpace(registerDto.Email))
                {
                    return new BaseResponseDto<AuthResponseDto>
                    {
                        Success = false,
                        Message = "Tên đăng nhập, mật khẩu và email không được để trống"
                    };
                }

                // Check if username already exists
                var existingUser = await _userRepository.GetByUsernameOrEmailAsync(registerDto.Username);
                if (existingUser != null)
                {
                    return new BaseResponseDto<AuthResponseDto>
                    {
                        Success = false,
                        Message = "Tên đăng nhập đã tồn tại"
                    };
                }

                // Check if email already exists
                var existingEmail = await _userRepository.GetByUsernameOrEmailAsync(registerDto.Email);
                if (existingEmail != null)
                {
                    return new BaseResponseDto<AuthResponseDto>
                    {
                        Success = false,
                        Message = "Email đã được sử dụng"
                    };
                }

                // Validate role
                if (registerDto.IdVaiTro < 1 || registerDto.IdVaiTro > 4)
                {
                    registerDto.IdVaiTro = 3; // Default to Student
                }

                // Hash password
                var hashedPassword = _passwordService.HashPassword(registerDto.Password);

                // Create new user
                var newUser = new Taikhoan
                {
                    TenDangNhap = registerDto.Username,
                    MatKhau = hashedPassword,
                    Email = registerDto.Email,
                    HoTen = registerDto.HoTen,
                    IdVaiTro = registerDto.IdVaiTro,
                    MaNhanVien = registerDto.MaNhanVien,
                    ChucDanh = registerDto.ChucDanh,
                    KhoaPhong = registerDto.KhoaPhong,
                    TrangThai = true,
                    NgayTao = DateTime.Now
                };

                var savedUser = await _userRepository.AddAsync(newUser);

                // Auto-login after registration: generate tokens
                var accessToken = _jwtService.GenerateAccessToken(savedUser, false);
                var refreshToken = _jwtService.GenerateRefreshToken();

                // Save refresh token
                var refreshTokenEntity = new RefreshToken
                {
                    Token = refreshToken,
                    UserId = savedUser.Id,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(30)
                };
                await _refreshTokenRepository.AddAsync(refreshTokenEntity);

                // Create user session
                var sessionId = Guid.NewGuid().ToString();
                var userSession = new UserSession
                {
                    SessionId = sessionId,
                    UserId = savedUser.Id,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddHours(24),
                    IsActive = true
                };
                await _sessionRepository.AddAsync(userSession);

                // Update last login
                savedUser.LanDangNhapCuoi = DateTime.Now;
                await _userRepository.UpdateAsync(savedUser);

                _logger.LogInformation("User registered successfully: {Username}, ID: {UserId}", 
                    savedUser.TenDangNhap, savedUser.Id);

                // Build response
                var userInfo = new UserInfoDto
                {
                    Id = savedUser.Id,
                    Username = savedUser.TenDangNhap ?? string.Empty,
                    Email = savedUser.Email ?? string.Empty,
                    FullName = savedUser.HoTen ?? string.Empty,
                    Role = GetRoleName(savedUser.IdVaiTro),
                    IsActive = savedUser.TrangThai ?? true,
                    LastLoginAt = savedUser.LanDangNhapCuoi ?? DateTime.Now
                };

                var authResponse = new AuthResponseDto
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = _jwtService.GetTokenExpiration(accessToken) ?? DateTime.UtcNow.AddHours(1),
                    TokenType = "Bearer",
                    User = userInfo
                };

                return new BaseResponseDto<AuthResponseDto>
                {
                    Success = true,
                    Message = "Đăng ký thành công",
                    Data = authResponse
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for username: {Username}", registerDto.Username);
                
                return new BaseResponseDto<AuthResponseDto>
                {
                    Success = false,
                    Message = "Có lỗi xảy ra trong quá trình đăng ký",
                    Errors = new List<string> { ex.Message, ex.InnerException?.Message ?? "" }
                };
            }
        }

        public async Task<BaseResponseDto<AuthResponseDto>> RefreshTokenAsync(RefreshTokenDto refreshTokenDto, CancellationToken cancellationToken = default)
        {
            try
            {
                // Get refresh token from database
                var refreshToken = await _refreshTokenRepository.GetByTokenAsync(refreshTokenDto.RefreshToken);
                if (refreshToken == null || !refreshToken.IsValid)
                {
                    return new BaseResponseDto<AuthResponseDto>
                    {
                        Success = false,
                        Message = "Refresh token không hợp lệ"
                    };
                }

                // Get user
                var user = refreshToken.User;
                if (user == null || user.TrangThai != true)
                {
                    return new BaseResponseDto<AuthResponseDto>
                    {
                        Success = false,
                        Message = "Tài khoản không còn hoạt động"
                    };
                }

                // Mark old refresh token as used
                refreshToken.IsUsed = true;
                await _refreshTokenRepository.UpdateAsync(refreshToken);

                // Generate new tokens
                var newAccessToken = _jwtService.GenerateAccessToken(user, false);
                var newRefreshToken = _jwtService.GenerateRefreshToken();

                // Save new refresh token
                var newRefreshTokenEntity = new RefreshToken
                {
                    Token = newRefreshToken,
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddDays(30),
                    IpAddress = refreshTokenDto.IpAddress,
                    UserAgent = refreshToken.UserAgent
                };

                await _refreshTokenRepository.AddAsync(newRefreshTokenEntity);

                // Create response
                var userInfo = new UserInfoDto
                {
                    Id = user.Id,
                    Username = user.TenDangNhap ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    FullName = user.HoTen ?? string.Empty,
                    Role = GetRoleName(user.IdVaiTro),
                    IsActive = user.TrangThai ?? false,
                    LastLoginAt = user.LanDangNhapCuoi ?? DateTime.Now
                };

                var authResponse = new AuthResponseDto
                {
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken,
                    ExpiresAt = _jwtService.GetTokenExpiration(newAccessToken) ?? DateTime.UtcNow.AddHours(1),
                    TokenType = "Bearer",
                    User = userInfo
                };

                return new BaseResponseDto<AuthResponseDto>
                {
                    Success = true,
                    Message = "Làm mới token thành công",
                    Data = authResponse
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                
                return new BaseResponseDto<AuthResponseDto>
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi làm mới token",
                    Errors = new List<string> { "Lỗi hệ thống" }
                };
            }
        }

        public async Task<BaseResponseDto> LogoutAsync(LogoutDto logoutDto, int userId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (logoutDto.LogoutFromAllDevices)
                {
                    // Revoke all refresh tokens
                    await _refreshTokenRepository.RevokeAllUserTokensAsync(userId);
                    
                    // End all sessions
                    await _sessionRepository.EndAllUserSessionsAsync(userId, "LogoutAll");
                }
                else
                {
                    // Revoke specific refresh token if provided
                    if (!string.IsNullOrEmpty(logoutDto.RefreshToken))
                    {
                        await _refreshTokenRepository.RevokeTokenAsync(logoutDto.RefreshToken);
                    }
                }

                return new BaseResponseDto
                {
                    Success = true,
                    Message = "Đăng xuất thành công"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout for user: {UserId}", userId);
                
                return new BaseResponseDto
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi đăng xuất",
                    Errors = new List<string> { "Lỗi hệ thống" }
                };
            }
        }

        public async Task<BaseResponseDto> ChangePasswordAsync(ChangePasswordDto changePasswordDto, int userId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Get user
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null || user.TrangThai != true)
                {
                    return new BaseResponseDto
                    {
                        Success = false,
                        Message = "Tài khoản không tồn tại hoặc đã bị vô hiệu hóa"
                    };
                }

                // Verify current password
                if (!_passwordService.VerifyPassword(changePasswordDto.CurrentPassword, user.MatKhau ?? string.Empty))
                {
                    return new BaseResponseDto
                    {
                        Success = false,
                        Message = "Mật khẩu hiện tại không đúng"
                    };
                }

                // Validate new password strength
                var passwordValidation = _passwordService.ValidatePasswordStrength(changePasswordDto.NewPassword);
                if (!passwordValidation.IsValid)
                {
                    return new BaseResponseDto
                    {
                        Success = false,
                        Message = "Mật khẩu mới không đủ mạnh",
                        Errors = passwordValidation.Errors
                    };
                }

                // Hash new password
                var hashedPassword = _passwordService.HashPassword(changePasswordDto.NewPassword);
                
                // Update user password
                user.MatKhau = hashedPassword;
                user.NgayCapNhat = DateTime.Now;
                await _userRepository.UpdateAsync(user);

                // Revoke all existing tokens to force re-login
                await _refreshTokenRepository.RevokeAllUserTokensAsync(userId);
                await _sessionRepository.EndAllUserSessionsAsync(userId, "PasswordChanged");

                return new BaseResponseDto
                {
                    Success = true,
                    Message = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user: {UserId}", userId);
                
                return new BaseResponseDto
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi đổi mật khẩu",
                    Errors = new List<string> { "Lỗi hệ thống" }
                };
            }
        }

        public async Task<BaseResponseDto<UserInfoDto>> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                {
                    return new BaseResponseDto<UserInfoDto>
                    {
                        Success = false,
                        Message = "Token không được để trống"
                    };
                }

                // Validate JWT token
                var principal = _jwtService.ValidateToken(token);
                if (principal == null)
                {
                    return new BaseResponseDto<UserInfoDto>
                    {
                        Success = false,
                        Message = "Token không hợp lệ"
                    };
                }

                // Extract user ID
                var userId = _jwtService.GetUserIdFromToken(token);
                if (!userId.HasValue)
                {
                    return new BaseResponseDto<UserInfoDto>
                    {
                        Success = false,
                        Message = "Token không chứa thông tin người dùng hợp lệ"
                    };
                }

                // Get user from database
                var user = await _userRepository.GetByIdAsync(userId.Value);
                if (user == null || user.TrangThai != true)
                {
                    return new BaseResponseDto<UserInfoDto>
                    {
                        Success = false,
                        Message = "Tài khoản không tồn tại hoặc đã bị vô hiệu hóa"
                    };
                }

                var userInfo = new UserInfoDto
                {
                    Id = user.Id,
                    Username = user.TenDangNhap ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    FullName = user.HoTen ?? string.Empty,
                    Role = GetRoleName(user.IdVaiTro),
                    IsActive = user.TrangThai ?? false,
                    LastLoginAt = user.LanDangNhapCuoi ?? DateTime.Now
                };

                return new BaseResponseDto<UserInfoDto>
                {
                    Success = true,
                    Message = "Token hợp lệ",
                    Data = userInfo
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token");
                
                return new BaseResponseDto<UserInfoDto>
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi xác thực token",
                    Errors = new List<string> { "Lỗi hệ thống" }
                };
            }
        }

        public async Task<BaseResponseDto<UserInfoDto>> GetCurrentUserAsync(int userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null || user.TrangThai != true)
                {
                    return new BaseResponseDto<UserInfoDto>
                    {
                        Success = false,
                        Message = "Tài khoản không tồn tại hoặc đã bị vô hiệu hóa"
                    };
                }

                var userInfo = new UserInfoDto
                {
                    Id = user.Id,
                    Username = user.TenDangNhap ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    FullName = user.HoTen ?? string.Empty,
                    Role = GetRoleName(user.IdVaiTro),
                    IsActive = user.TrangThai ?? false,
                    LastLoginAt = user.LanDangNhapCuoi ?? DateTime.Now
                };

                return new BaseResponseDto<UserInfoDto>
                {
                    Success = true,
                    Message = "Lấy thông tin người dùng thành công",
                    Data = userInfo
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user: {UserId}", userId);
                
                return new BaseResponseDto<UserInfoDto>
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi lấy thông tin người dùng",
                    Errors = new List<string> { "Lỗi hệ thống" }
                };
            }
        }

        public async Task<BaseResponseDto> RevokeAllUserSessionsAsync(int userId, CancellationToken cancellationToken = default)
        {
            try
            {
                // Revoke all refresh tokens
                var revokedTokens = await _refreshTokenRepository.RevokeAllUserTokensAsync(userId);
                
                // End all sessions
                var endedSessions = await _sessionRepository.EndAllUserSessionsAsync(userId, "AdminRevoked");

                return new BaseResponseDto
                {
                    Success = true,
                    Message = $"Đã thu hồi {revokedTokens} token và kết thúc {endedSessions} phiên đăng nhập"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking all sessions for user: {UserId}", userId);
                
                return new BaseResponseDto
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi thu hồi phiên đăng nhập",
                    Errors = new List<string> { "Lỗi hệ thống" }
                };
            }
        }

        public async Task<BaseResponseDto> RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default)
        {
            try
            {
                // Get user by email
                var user = await _userRepository.GetByUsernameOrEmailAsync(email);
                if (user == null || user.TrangThai != true)
                {
                    // Don't reveal if email exists or not for security
                    return new BaseResponseDto
                    {
                        Success = true,
                        Message = "Nếu email tồn tại, bạn sẽ nhận được hướng dẫn đặt lại mật khẩu"
                    };
                }

                // Generate password reset token
                var resetToken = _jwtService.GeneratePasswordResetToken(user);

                // Send email with reset token
                await _emailService.SendPasswordResetEmailAsync(
                    user.Email ?? email,
                    user.HoTen ?? user.TenDangNhap ?? "User",
                    resetToken);

                _logger.LogInformation("Password reset email sent for user {UserId}", user.Id);

                return new BaseResponseDto
                {
                    Success = true,
                    Message = "Nếu email tồn tại, bạn sẽ nhận được hướng dẫn đặt lại mật khẩu"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting password reset for email: {Email}", email);
                
                return new BaseResponseDto
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi yêu cầu đặt lại mật khẩu",
                    Errors = new List<string> { "Lỗi hệ thống" }
                };
            }
        }

        public async Task<BaseResponseDto> ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate reset token
                var principal = _jwtService.ValidateToken(token);
                if (principal == null)
                {
                    return new BaseResponseDto
                    {
                        Success = false,
                        Message = "Token đặt lại mật khẩu không hợp lệ hoặc đã hết hạn"
                    };
                }

                // Extract user ID from token
                var userIdClaim = principal.FindFirst("user_id")?.Value;
                if (!int.TryParse(userIdClaim, out var userId))
                {
                    return new BaseResponseDto
                    {
                        Success = false,
                        Message = "Token không chứa thông tin người dùng hợp lệ"
                    };
                }

                // Get user
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null || user.TrangThai != true)
                {
                    return new BaseResponseDto
                    {
                        Success = false,
                        Message = "Tài khoản không tồn tại hoặc đã bị vô hiệu hóa"
                    };
                }

                // Validate new password strength
                var passwordValidation = _passwordService.ValidatePasswordStrength(newPassword);
                if (!passwordValidation.IsValid)
                {
                    return new BaseResponseDto
                    {
                        Success = false,
                        Message = "Mật khẩu mới không đủ mạnh",
                        Errors = passwordValidation.Errors
                    };
                }

                // Hash new password
                var hashedPassword = _passwordService.HashPassword(newPassword);
                
                // Update user password
                user.MatKhau = hashedPassword;
                user.NgayCapNhat = DateTime.Now;
                await _userRepository.UpdateAsync(user);

                // Revoke all existing tokens to force re-login
                await _refreshTokenRepository.RevokeAllUserTokensAsync(userId);
                await _sessionRepository.EndAllUserSessionsAsync(userId, "PasswordReset");

                return new BaseResponseDto
                {
                    Success = true,
                    Message = "Đặt lại mật khẩu thành công. Vui lòng đăng nhập lại."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password");
                
                return new BaseResponseDto
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi đặt lại mật khẩu",
                    Errors = new List<string> { "Lỗi hệ thống" }
                };
            }
        }

        private static string GetRoleName(int? roleId)
        {
            return roleId switch
            {
                1 => "Admin",
                2 => "Teacher",
                3 => "Student",
                4 => "Supervisor",
                _ => "Student"
            };
        }
    }
}