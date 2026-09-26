using BanTayVang.API.DTOs.Auth;
using BanTayVang.API.DTOs.Common;
using BanTayVang.API.Models;
using BanTayVang.API.Repositories.Interfaces;
using BanTayVang.API.Services.Interfaces;
using BanTayVang.API.Services.Interfaces.Auth;
using Microsoft.EntityFrameworkCore;

namespace BanTayVang.API.Services.Impl.Auth
{
    public class AuthService : IAuthService
    {
        // SECURITY (username enumeration via timing side-channel): BCrypt.Verify is
        // deliberately slow (~250-300ms at workFactor 12 on this machine, measured directly -
        // a non-existent username returned in ~8ms by comparison). Returning early when the
        // user lookup misses, without ever calling VerifyPassword, let an attacker enumerate
        // valid usernames purely from response time, no password guessing needed. This dummy
        // hash (never a real user's password) is verified against on the "user not found" path
        // too, so both paths pay the same BCrypt cost and are no longer distinguishable by timing.
        private static readonly string DummyPasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString(), workFactor: 12);

        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IUserSessionRepository _sessionRepository;
        private readonly IJwtService _jwtService;
        private readonly IPasswordService _passwordService;
        private readonly Services.Interfaces.IEmailService _emailService;
        private readonly Services.Interfaces.IEmailVerificationService _emailVerificationService;
        private readonly IAuditLogService _auditLogService;
        private readonly BanTayVangDbContext _context;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserRepository userRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IUserSessionRepository sessionRepository,
            IJwtService jwtService,
            IPasswordService passwordService,
            Services.Interfaces.IEmailService emailService,
            Services.Interfaces.IEmailVerificationService emailVerificationService,
            IAuditLogService auditLogService,
            BanTayVangDbContext context,
            ILogger<AuthService> logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
            _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
            _jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
            _passwordService = passwordService ?? throw new ArgumentNullException(nameof(passwordService));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _emailVerificationService = emailVerificationService ?? throw new ArgumentNullException(nameof(emailVerificationService));
            _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
            _context = context ?? throw new ArgumentNullException(nameof(context));
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
                        Message = "Mã nhân viên và mật khẩu không được để trống"
                    };
                }

                // Get user by username or email
                var user = await _userRepository.GetByUsernameOrEmailAsync(loginDto.Username);
                if (user == null)
                {
                    // Pay the same BCrypt cost as the "wrong password" path below so response
                    // time doesn't leak whether this username exists.
                    _passwordService.VerifyPassword(loginDto.Password, DummyPasswordHash);
                    return new BaseResponseDto<AuthResponseDto>
                    {
                        Success = false,
                        Message = "Mã nhân viên hoặc mật khẩu không đúng"
                    };
                }

                // Check if user is active
                if (user.Status != true)
                {
                    return new BaseResponseDto<AuthResponseDto>
                    {
                        Success = false,
                        Message = "Tài khoản đã bị vô hiệu hóa"
                    };
                }

                // SECURITY (OWASP A07): account lockout after repeated failed logins.
                // Use true UTC (not the DateTime.UtcNow.AddHours(7) "fake VN time" convention used
                // for display timestamps elsewhere) since this is purely a server-side security
                // check never shown to the user - true UTC avoids the whole class of timezone-
                // comparison bugs already found and fixed elsewhere in this codebase.
                if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
                {
                    var minutesLeft = Math.Max(1, (int)Math.Ceiling((user.LockoutEnd.Value - DateTime.UtcNow).TotalMinutes));
                    _logger.LogWarning("Login blocked for user {Username} - account locked for {Minutes} more minute(s)", loginDto.Username, minutesLeft);
                    return new BaseResponseDto<AuthResponseDto>
                    {
                        Success = false,
                        Message = $"Tài khoản tạm khóa do đăng nhập sai nhiều lần. Vui lòng thử lại sau {minutesLeft} phút."
                    };
                }

                // Verify password
                if (!_passwordService.VerifyPassword(loginDto.Password, user.Password ?? string.Empty))
                {
                    // SECURITY: track failed attempts and lock the account for 15 minutes after
                    // 5 consecutive failures, to slow down online password-guessing/brute-force.
                    // BUG FIX: this used to be `user.FailedLoginAttempts += 1;` on the in-memory
                    // tracked entity, then a plain UpdateAsync - a classic non-atomic read-modify-
                    // write. Firing several wrong-password attempts in parallel let each request
                    // read the same starting count before any of them saved, so the counter never
                    // actually reached 5 and the lockout could be bypassed entirely by brute-forcing
                    // concurrently instead of sequentially - defeating the one control this code
                    // exists for. Same class of bug already fixed elsewhere (WarningCount, OTP
                    // FailedAttempts) - ExecuteUpdateAsync computes the new count AND the lockout
                    // decision in one atomic SQL UPDATE, so concurrent attempts correctly stack.
                    const int maxFailedAttempts = 5;
                    await _context.Users
                        .Where(u => u.Id == user.Id)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(u => u.FailedLoginAttempts, u => u.FailedLoginAttempts + 1)
                            .SetProperty(u => u.LockoutEnd, u => u.FailedLoginAttempts + 1 >= maxFailedAttempts
                                ? DateTime.UtcNow.AddMinutes(15)
                                : u.LockoutEnd));

                    return new BaseResponseDto<AuthResponseDto>
                    {
                        Success = false,
                        Message = "Mã nhân viên hoặc mật khẩu không đúng"
                    };
                }

                // Successful login: reset failed-attempt counter/lockout state.
                user.FailedLoginAttempts = 0;
                user.LockoutEnd = null;

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
                user.LastLoginAt = DateTime.UtcNow.AddHours(7);
                await _userRepository.UpdateAsync(user);

                // Create response
                var userInfo = new UserInfoDto
                {
                    Id = user.Id,
                    Username = user.Username ?? string.Empty,
                    Email = string.Empty,
                    FullName = user.FullName ?? string.Empty,
                    Role = GetRoleName(user.RoleId),
                    IsActive = user.Status ?? false,
                    LastLoginAt = user.LastLoginAt ?? DateTime.UtcNow.AddHours(7),
                    Department = user.Department,
                    DeptManagerDeptId = user.DeptManagerDeptId,
                    // BUG FIX: Gán DeptManagerDeptName từ navigation property ManagedDepartment.
                    // Field này bị thiếu khiến frontend (DeptManager) không biết mình thuộc khoa nào
                    // → filter department = null → GET /api/Question trả 0 kết quả.
                    DeptManagerDeptName = user.ManagedDepartment?.DepartmentName ?? user.Department
                };

                var authResponse = new AuthResponseDto
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = _jwtService.GetTokenExpiration(accessToken) ?? DateTime.UtcNow.AddHours(1),
                    TokenType = "Bearer",
                    User = userInfo
                };

                await _auditLogService.LogActionAsync(
                    actionType: "POST /api/Auth/login",
                    description: $"User {user.Username} logged in successfully",
                    userId: user.Id,
                    username: user.Username,
                    ipAddress: loginDto.IpAddress,
                    userAgent: loginDto.UserAgent,
                    method: "POST",
                    path: "/api/Auth/login",
                    statusCode: 200,
                    department: user.Department);

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
                _logger.LogInformation("Register attempt for username: {Username}", 
                    registerDto.Username);

                // Input validation
                if (string.IsNullOrWhiteSpace(registerDto.Username) || 
                    string.IsNullOrWhiteSpace(registerDto.Password))
                {
                    return new BaseResponseDto<AuthResponseDto>
                    {
                        Success = false,
                        Message = "Tên đăng nhập và mật khẩu không được để trống"
                    };
                }

                // Check if username already exists
                var existingUser = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Username == registerDto.Username || u.EmployeeCode == registerDto.Username);
                if (existingUser != null)
                {
                    return new BaseResponseDto<AuthResponseDto>
                    {
                        Success = false,
                        Message = "Tên đăng nhập đã tồn tại"
                    };
                }

                // BUG FIX: registration only checked IsNullOrWhiteSpace on the password, so
                // anything meeting the DTO's bare [StringLength(6,100)] attribute - e.g. "111111"
                // - was accepted, even though PasswordService.ValidatePasswordStrength already
                // implements the real complexity/repeat/common-pattern rules used everywhere else
                // (change-password, reset-password) - it just was never called here.
                var passwordValidation = _passwordService.ValidatePasswordStrength(registerDto.Password);
                if (!passwordValidation.IsValid)
                {
                    return new BaseResponseDto<AuthResponseDto>
                    {
                        Success = false,
                        Message = "Mật khẩu không đủ mạnh",
                        Errors = passwordValidation.Errors
                    };
                }

                // Hash password
                var hashedPassword = _passwordService.HashPassword(registerDto.Password);

                // Create new user
                var newUser = new User
                {
                    Username = registerDto.Username,
                    Password = hashedPassword,
                    FullName = registerDto.FullName,
                    Email = registerDto.Email, // BUG FIX: was never persisted, User.Email stayed blank
                    // BUG FIX: RoleId used to come from the client-facing RegisterDto (anonymous
                    // endpoint) with a runtime override forcing it to 3 - one accidental refactor
                    // away from a public self-registration -> Admin privilege escalation. Removed
                    // the field from the DTO entirely and hardcode Student here instead.
                    RoleId = 3,
                    EmployeeCode = registerDto.EmployeeCode,
                    JobTitle = registerDto.JobTitle,
                    Department = registerDto.Department,
                    Status = true,
                    CreatedAt = DateTime.UtcNow.AddHours(7)
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
                savedUser.LastLoginAt = DateTime.UtcNow.AddHours(7);
                await _userRepository.UpdateAsync(savedUser);

                _logger.LogInformation("User registered successfully: {Username}, ID: {UserId}", 
                    savedUser.Username, savedUser.Id);

                // Build response
                var userInfo = new UserInfoDto
                {
                    Id = savedUser.Id,
                    Username = savedUser.Username ?? string.Empty,
                    Email = string.Empty,
                    FullName = savedUser.FullName ?? string.Empty,
                    Role = GetRoleName(savedUser.RoleId),
                    IsActive = savedUser.Status ?? true,
                    LastLoginAt = savedUser.LastLoginAt ?? DateTime.UtcNow.AddHours(7),
                    Department = savedUser.Department,
                    DeptManagerDeptId = savedUser.DeptManagerDeptId
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

                // SECURITY (OWASP A07): refresh token rotation reuse detection.
                // Each refresh token can only be exchanged once (IsUsed=true after use, see below).
                // If a token that was ALREADY used is presented again, that is a strong signal the
                // token was stolen and is now racing the legitimate client (attacker used it first,
                // or vice versa). The correct response is to assume compromise and revoke every
                // refresh token for that user, forcing re-login everywhere - not just reject this call.
                if (refreshToken != null && refreshToken.IsUsed && !refreshToken.IsRevoked)
                {
                    var revokedCount = await _refreshTokenRepository.RevokeAllUserTokensAsync(refreshToken.UserId);
                    _logger.LogWarning(
                        "SECURITY: Refresh token reuse detected for user {UserId} from IP {IpAddress} - revoked {Count} active token(s)",
                        refreshToken.UserId, refreshTokenDto.IpAddress, revokedCount);
                    await _auditLogService.LogActionAsync(
                        actionType: "SECURITY_REFRESH_TOKEN_REUSE",
                        description: $"Refresh token reuse detected for user {refreshToken.UserId} - all sessions revoked",
                        userId: refreshToken.UserId,
                        username: refreshToken.User?.Username,
                        ipAddress: refreshTokenDto.IpAddress,
                        method: "POST",
                        path: "/api/Auth/refresh-token",
                        statusCode: 401);

                    return new BaseResponseDto<AuthResponseDto>
                    {
                        Success = false,
                        Message = "Phiên đăng nhập không hợp lệ, vui lòng đăng nhập lại"
                    };
                }

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
                if (user == null || user.Status != true)
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
                    Username = user.Username ?? string.Empty,
                    Email = string.Empty,
                    FullName = user.FullName ?? string.Empty,
                    Role = GetRoleName(user.RoleId),
                    IsActive = user.Status ?? false,
                    LastLoginAt = user.LastLoginAt ?? DateTime.UtcNow.AddHours(7),
                    Department = user.Department,
                    DeptManagerDeptId = user.DeptManagerDeptId,
                    DeptManagerDeptName = user.ManagedDepartment?.DepartmentName ?? user.Department
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
                if (user == null || user.Status != true)
                {
                    return new BaseResponseDto
                    {
                        Success = false,
                        Message = "Tài khoản không tồn tại hoặc đã bị vô hiệu hóa"
                    };
                }

                // Verify current password
                if (!_passwordService.VerifyPassword(changePasswordDto.CurrentPassword, user.Password ?? string.Empty))
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
                user.Password = hashedPassword;
                user.UpdatedAt = DateTime.UtcNow.AddHours(7);
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
                if (user == null || user.Status != true)
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
                    Username = user.Username ?? string.Empty,
                    Email = string.Empty,
                    FullName = user.FullName ?? string.Empty,
                    Role = GetRoleName(user.RoleId),
                    IsActive = user.Status ?? false,
                    LastLoginAt = user.LastLoginAt ?? DateTime.UtcNow.AddHours(7),
                    Department = user.Department,
                    DeptManagerDeptId = user.DeptManagerDeptId,
                    DeptManagerDeptName = user.ManagedDepartment?.DepartmentName ?? user.Department
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
                if (user == null || user.Status != true)
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
                    Username = user.Username ?? string.Empty,
                    Email = string.Empty,
                    FullName = user.FullName ?? string.Empty,
                    Role = GetRoleName(user.RoleId),
                    IsActive = user.Status ?? false,
                    LastLoginAt = user.LastLoginAt ?? DateTime.UtcNow.AddHours(7),
                    Department = user.Department,
                    DeptManagerDeptId = user.DeptManagerDeptId,
                    DeptManagerDeptName = user.ManagedDepartment?.DepartmentName ?? user.Department
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
                if (user == null || user.Status != true)
                {
                    // BUG FIX (timing attack, measured live: ~10ms vs ~7.7ms across 20 samples):
                    // this returned immediately while the found-user path below calls SendCodeAsync,
                    // which hashes the OTP with BCrypt (workFactor 10) - same class of side-channel
                    // already fixed on login. Pay the same BCrypt cost here so the "don't reveal
                    // whether this email exists" message the comment below promises isn't undermined
                    // by response time.
                    BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString(), workFactor: 10);

                    // Don't reveal if email exists or not for security
                    return new BaseResponseDto
                    {
                        Success = true,
                        Message = "Nếu email tồn tại, bạn sẽ nhận được hướng dẫn đặt lại mật khẩu"
                    };
                }

                // Gửi mã OTP 6 số qua email thay vì link JWT
                await _emailVerificationService.SendCodeAsync(email, "PasswordReset", ipAddress: null, cancellationToken);

                _logger.LogInformation("Password reset OTP sent for user {UserId}", user.Id);

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

                // OWASP A01/A07: this token must actually be a password-reset token, not just
                // any valid JWT (e.g. a normal login access token, which also carries "user_id").
                // Without this check, any authenticated user could call this endpoint with their
                // own access token to change their password without knowing the current one, and
                // anyone who stole someone else's live access token could lock that account out.
                var purposeClaim = principal.FindFirst("purpose")?.Value;
                if (purposeClaim != "password_reset")
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
                if (user == null || user.Status != true)
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
                user.Password = hashedPassword;
                user.UpdatedAt = DateTime.UtcNow.AddHours(7);
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
                2 => "Teacher",      // Obsolete
                3 => "Student",
                4 => "Supervisor",   // Obsolete
                5 => "DeptManager",
                6 => "ThiSinhNgoai",
                _ => "Student"
            };
        }

        public async Task<BaseResponseDto<string>> VerifyPasswordResetCodeAsync(string email, string code, CancellationToken cancellationToken = default)
        {
            try
            {
                var (success, message) = await _emailVerificationService.VerifyCodeAsync(email, code, "PasswordReset", cancellationToken);
                if (!success)
                {
                    return BaseResponseDto<string>.FailureResult(message);
                }
                
                // Tiêu thụ mã OTP ngay sau khi xác thực thành công để tránh việc dùng lại mã
                await _emailVerificationService.ConsumeVerifiedCodeAsync(email, "PasswordReset", cancellationToken);

                var user = await _userRepository.GetByUsernameOrEmailAsync(email);
                if (user == null || user.Status != true)
                {
                    return BaseResponseDto<string>.FailureResult("Người dùng không tồn tại hoặc đã bị khóa.");
                }

                var resetToken = _jwtService.GeneratePasswordResetToken(user);

                return BaseResponseDto<string>.SuccessResult(resetToken, "Xác thực mã thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying password reset code for email: {Email}", email);

                return BaseResponseDto<string>.FailureResult("Có lỗi xảy ra khi xác thực mã");
            }
        }
    }
}
