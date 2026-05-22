# Workflow 4: Hệ thống Thi trực tuyến - Enterprise Edition

## 🎯 Mục tiêu
Xây dựng hệ thống thi trực tuyến an toàn, hiệu suất cao với khả năng chống gian lận và tuân thủ SOLID principles.

## 🔒 Security Requirements (OWASP Top 10)
- **A01 - Broken Access Control**: Session-based exam access control
- **A02 - Cryptographic Failures**: Encrypt sensitive exam data
- **A03 - Injection**: Prevent SQL injection in exam queries
- **A04 - Insecure Design**: Secure exam session management
- **A06 - Vulnerable Components**: Secure real-time monitoring
- **A07 - Authentication Failures**: Strong exam authentication
- **A08 - Software Integrity**: Tamper-proof exam submissions

## 🏗️ SOLID Principles Implementation

### Single Responsibility Principle (SRP)
- `IExamSessionService`: Quản lý phiên thi
- `IExamSecurityService`: Bảo mật và chống gian lận
- `IExamTimerService`: Quản lý thời gian thi
- `IExamSubmissionService`: Xử lý nộp bài

### Open/Closed Principle (OCP)
- Strategy pattern cho different exam types
- Plugin architecture cho anti-cheat mechanisms

### Liskov Substitution Principle (LSP)
- Consistent interfaces cho exam operations

### Interface Segregation Principle (ISP)
- Separate interfaces cho exam creation vs taking

### Dependency Inversion Principle (DIP)
- Abstract exam storage and security services

## Bước 1: Enhanced Exam System Architecture

### 1.1 Segregated Exam Service Interfaces
```csharp
// Services/Interfaces/Exams/IExamSessionService.cs
public interface IExamSessionService
{
    Task<Result<ExamSessionDto>> StartExamSessionAsync(StartExamDto startDto, int taikhoanId, CancellationToken cancellationToken = default);
    Task<Result<ExamSessionDto>> GetActiveSessionAsync(int taikhoanId, string maDeThi, CancellationToken cancellationToken = default);
    Task<Result<ExamSessionDto>> ResumeExamSessionAsync(int sessionId, int taikhoanId, CancellationToken cancellationToken = default);
    Task<Result> PauseExamSessionAsync(int sessionId, int taikhoanId, CancellationToken cancellationToken = default);
    Task<Result<ExamSessionDto>> GetSessionProgressAsync(int sessionId, int taikhoanId, CancellationToken cancellationToken = default);
    Task<Result> ValidateSessionAccessAsync(int sessionId, int taikhoanId, CancellationToken cancellationToken = default);
}

// Services/Interfaces/Exams/IExamSecurityService.cs
public interface IExamSecurityService
{
    Task<Result> LogSecurityEventAsync(SecurityEventDto eventDto, CancellationToken cancellationToken = default);
    Task<Result<SecurityStatusDto>> GetSecurityStatusAsync(int sessionId, CancellationToken cancellationToken = default);
    Task<Result> ValidateExamEnvironmentAsync(ExamEnvironmentDto environment, CancellationToken cancellationToken = default);
    Task<Result<bool>> ShouldTerminateSessionAsync(int sessionId, CancellationToken cancellationToken = default);
    Task<Result> RecordSuspiciousActivityAsync(int sessionId, SuspiciousActivityDto activity, CancellationToken cancellationToken = default);
}

// Services/Interfaces/Exams/IExamTimerService.cs
public interface IExamTimerService
{
    Task<Result<TimeRemainingDto>> GetTimeRemainingAsync(int sessionId, CancellationToken cancellationToken = default);
    Task<Result> ExtendTimeAsync(int sessionId, TimeSpan extension, int authorizedBy, CancellationToken cancellationToken = default);
    Task<Result<List<int>>> GetExpiredSessionsAsync(CancellationToken cancellationToken = default);
    Task<Result> HandleTimeWarningAsync(int sessionId, TimeWarningType warningType, CancellationToken cancellationToken = default);
}

// Services/Interfaces/Exams/IExamSubmissionService.cs
public interface IExamSubmissionService
{
    Task<Result> SaveAnswerAsync(SaveAnswerDto answerDto, int taikhoanId, CancellationToken cancellationToken = default);
    Task<Result<SubmissionResultDto>> SubmitExamAsync(SubmitExamDto submitDto, int taikhoanId, CancellationToken cancellationToken = default);
    Task<Result> AutoSubmitExpiredExamsAsync(CancellationToken cancellationToken = default);
    Task<Result<AnswerStatusDto>> GetAnswerStatusAsync(int sessionId, int questionId, int taikhoanId, CancellationToken cancellationToken = default);
    Task<Result<List<AnswerSummaryDto>>> GetAnswerSummaryAsync(int sessionId, int taikhoanId, CancellationToken cancellationToken = default);
}
```

### 1.2 Enhanced Security DTOs
```csharp
// DTOs/Exams/Security/SecurityEventDto.cs
public class SecurityEventDto
{
    [Required]
    public int SessionId { get; set; }
    
    [Required]
    public SecurityEventType EventType { get; set; }
    
    [Required]
    public string EventData { get; set; } = string.Empty;
    
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public SecuritySeverity Severity { get; set; } = SecuritySeverity.Medium;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public enum SecurityEventType
{
    TabSwitch,
    WindowFocusLoss,
    RightClick,
    CopyAttempt,
    PasteAttempt,
    KeyboardShortcut,
    DevToolsOpen,
    FullscreenExit,
    MultipleWindows,
    SuspiciousNavigation,
    UnauthorizedAccess,
    SessionHijacking
}

public enum SecuritySeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

// DTOs/Exams/Security/ExamEnvironmentDto.cs
public class ExamEnvironmentDto
{
    [Required]
    public string UserAgent { get; set; } = string.Empty;
    
    [Required]
    public string IpAddress { get; set; } = string.Empty;
    
    public string? ScreenResolution { get; set; }
    public string? TimeZone { get; set; }
    public string? Language { get; set; }
    public bool IsFullscreen { get; set; }
    public int WindowCount { get; set; }
    public List<string> InstalledPlugins { get; set; } = new();
    public Dictionary<string, object> BrowserFingerprint { get; set; } = new();
}

// DTOs/Exams/Security/SecurityStatusDto.cs
public class SecurityStatusDto
{
    public int SessionId { get; set; }
    public int TotalViolations { get; set; }
    public int CriticalViolations { get; set; }
    public int HighViolations { get; set; }
    public int MediumViolations { get; set; }
    public int LowViolations { get; set; }
    public SecurityRiskLevel RiskLevel { get; set; }
    public bool ShouldTerminate { get; set; }
    public List<SecurityEventSummaryDto> RecentEvents { get; set; } = new();
    public DateTime LastViolation { get; set; }
}

public enum SecurityRiskLevel
{
    Low,
    Medium,
    High,
    Critical
}
```

### 1.3 Enhanced Exam Session DTOs
```csharp
// DTOs/Exams/Sessions/ExamSessionDto.cs
public class ExamSessionDto
{
    public int Id { get; set; }
    public int TaikhoanId { get; set; }
    public int DethiId { get; set; }
    public string? MaDeThi { get; set; }
    public string? TenDeThi { get; set; }
    
    public ExamSessionStatus Status { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public DateTime? SubmittedAt { get; set; }
    
    public int TotalQuestions { get; set; }
    public int AnsweredQuestions { get; set; }
    public int FlaggedQuestions { get; set; }
    
    public TimeSpan AllocatedTime { get; set; }
    public TimeSpan ElapsedTime { get; set; }
    public TimeSpan RemainingTime { get; set; }
    
    public SecurityStatusDto SecurityStatus { get; set; } = new();
    public ExamEnvironmentDto Environment { get; set; } = new();
    
    public bool CanResume { get; set; }
    public bool CanSubmit { get; set; }
    public bool IsExpired { get; set; }
    
    public List<ExamQuestionDto> Questions { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public enum ExamSessionStatus
{
    NotStarted,
    InProgress,
    Paused,
    Submitted,
    Expired,
    Terminated,
    UnderReview
}

// DTOs/Exams/Sessions/StartExamDto.cs
public class StartExamDto
{
    [Required(ErrorMessage = "Mã đề thi là bắt buộc")]
    [StringLength(50, ErrorMessage = "Mã đề thi không được quá 50 ký tự")]
    [RegularExpression(@"^[A-Z0-9_-]+$", ErrorMessage = "Mã đề thi chỉ chứa chữ hoa, số, gạch dưới và gạch ngang")]
    public string MaDeThi { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Thông tin môi trường là bắt buộc")]
    [ValidateComplexType]
    public ExamEnvironmentDto Environment { get; set; } = new();
    
    public string? AccessCode { get; set; }
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}

// DTOs/Exams/Sessions/TimeRemainingDto.cs
public class TimeRemainingDto
{
    public int SessionId { get; set; }
    public TimeSpan RemainingTime { get; set; }
    public TimeSpan ElapsedTime { get; set; }
    public TimeSpan TotalTime { get; set; }
    public bool IsExpired { get; set; }
    public bool HasWarning { get; set; }
    public TimeWarningType? WarningType { get; set; }
    public DateTime ServerTime { get; set; } = DateTime.UtcNow;
}

public enum TimeWarningType
{
    FifteenMinutes,
    FiveMinutes,
    OneMinute,
    ThirtySeconds
}
```

```

### 1.4 Answer Submission DTOs
```csharp
// DTOs/Exams/Submissions/SaveAnswerDto.cs
public class SaveAnswerDto
{
    [Required(ErrorMessage = "Session ID là bắt buộc")]
    [Range(1, int.MaxValue, ErrorMessage = "Session ID không hợp lệ")]
    public int SessionId { get; set; }
    
    [Required(ErrorMessage = "Question ID là bắt buộc")]
    [Range(1, int.MaxValue, ErrorMessage = "Question ID không hợp lệ")]
    public int QuestionId { get; set; }
    
    public int? SelectedChoiceId { get; set; } // Cho câu trắc nghiệm
    
    [StringLength(5000, ErrorMessage = "Câu trả lời tự luận không được quá 5000 ký tự")]
    public string? EssayAnswer { get; set; } // Cho câu tự luận
    
    public bool IsFlagged { get; set; } = false;
    public bool IsConfident { get; set; } = true;
    
    public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;
    public TimeSpan TimeSpent { get; set; }
    
    // Security: Track answer changes
    public int ChangeCount { get; set; } = 0;
    public string? PreviousAnswer { get; set; }
}

// DTOs/Exams/Submissions/SubmitExamDto.cs
public class SubmitExamDto
{
    [Required(ErrorMessage = "Session ID là bắt buộc")]
    [Range(1, int.MaxValue, ErrorMessage = "Session ID không hợp lệ")]
    public int SessionId { get; set; }
    
    public SubmissionType SubmissionType { get; set; } = SubmissionType.Manual;
    
    [StringLength(500, ErrorMessage = "Ghi chú không được quá 500 ký tự")]
    public string? Notes { get; set; }
    
    public bool ConfirmSubmission { get; set; } = false;
    
    // Final answers (optional - for verification)
    public List<FinalAnswerDto> FinalAnswers { get; set; } = new();
    
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> SubmissionMetadata { get; set; } = new();
}

public enum SubmissionType
{
    Manual,
    Automatic,
    TimeExpired,
    SecurityViolation,
    SystemForced
}

// DTOs/Exams/Submissions/FinalAnswerDto.cs
public class FinalAnswerDto
{
    public int QuestionId { get; set; }
    public int? SelectedChoiceId { get; set; }
    public string? EssayAnswer { get; set; }
    public bool IsFlagged { get; set; }
    public TimeSpan TimeSpent { get; set; }
    public int ChangeCount { get; set; }
}

// DTOs/Exams/Submissions/SubmissionResultDto.cs
public class SubmissionResultDto
{
    public int SessionId { get; set; }
    public bool IsSuccessful { get; set; }
    public string? Message { get; set; }
    public DateTime SubmittedAt { get; set; }
    public SubmissionType SubmissionType { get; set; }
    
    public int TotalQuestions { get; set; }
    public int AnsweredQuestions { get; set; }
    public int UnansweredQuestions { get; set; }
    public int FlaggedQuestions { get; set; }
    
    public TimeSpan TotalTimeSpent { get; set; }
    public SecurityStatusDto SecurityStatus { get; set; } = new();
    
    public string? SubmissionId { get; set; } // For tracking
    public Dictionary<string, object> AdditionalData { get; set; } = new();
}
```

## Bước 2: Enhanced Exam Repository Layer

### 2.1 Secure Exam Repository Interfaces
```csharp
// Repositories/Interfaces/Exams/IExamSessionRepository.cs
public interface IExamSessionRepository : IBaseRepository<ExamSession>
{
    Task<ExamSession?> GetActiveSessionAsync(int taikhoanId, int dethiId, CancellationToken cancellationToken = default);
    Task<List<ExamSession>> GetExpiredSessionsAsync(CancellationToken cancellationToken = default);
    Task<ExamSession?> GetSessionWithDetailsAsync(int sessionId, CancellationToken cancellationToken = default);
    Task<bool> UpdateSessionStatusAsync(int sessionId, ExamSessionStatus status, CancellationToken cancellationToken = default);
    Task<List<ExamSession>> GetSessionsByUserAsync(int taikhoanId, int? dethiId = null, CancellationToken cancellationToken = default);
    Task<bool> ValidateSessionAccessAsync(int sessionId, int taikhoanId, CancellationToken cancellationToken = default);
}

// Repositories/Interfaces/Exams/IExamAnswerRepository.cs
public interface IExamAnswerRepository : IBaseRepository<ExamAnswer>
{
    Task<List<ExamAnswer>> GetSessionAnswersAsync(int sessionId, CancellationToken cancellationToken = default);
    Task<ExamAnswer?> GetAnswerAsync(int sessionId, int questionId, CancellationToken cancellationToken = default);
    Task<bool> SaveAnswerAsync(ExamAnswer answer, CancellationToken cancellationToken = default);
    Task<int> CountAnsweredQuestionsAsync(int sessionId, CancellationToken cancellationToken = default);
    Task<List<ExamAnswer>> GetAnswerHistoryAsync(int sessionId, int questionId, CancellationToken cancellationToken = default);
    Task<Dictionary<int, int>> GetAnswerChangeCountsAsync(int sessionId, CancellationToken cancellationToken = default);
}

// Repositories/Interfaces/Exams/ISecurityEventRepository.cs
public interface ISecurityEventRepository : IBaseRepository<SecurityEvent>
{
    Task<List<SecurityEvent>> GetSessionEventsAsync(int sessionId, CancellationToken cancellationToken = default);
    Task<int> CountViolationsByTypeAsync(int sessionId, SecurityEventType eventType, CancellationToken cancellationToken = default);
    Task<int> CountViolationsBySeverityAsync(int sessionId, SecuritySeverity severity, CancellationToken cancellationToken = default);
    Task<SecurityEvent?> GetLastViolationAsync(int sessionId, CancellationToken cancellationToken = default);
    Task<List<SecurityEvent>> GetRecentEventsAsync(int sessionId, int count = 10, CancellationToken cancellationToken = default);
    Task<bool> HasCriticalViolationsAsync(int sessionId, CancellationToken cancellationToken = default);
}
```

### 2.2 Enhanced Exam Session Service Implementation
```csharp
// Services/Impl/Exams/ExamSessionService.cs
public class ExamSessionService : IExamSessionService
{
    private readonly IExamSessionRepository _sessionRepository;
    private readonly IDethiRepository _examRepository;
    private readonly IExamSecurityService _securityService;
    private readonly IExamTimerService _timerService;
    private readonly IMapper _mapper;
    private readonly ILogger<ExamSessionService> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDistributedCache _cache;

    public ExamSessionService(
        IExamSessionRepository sessionRepository,
        IDethiRepository examRepository,
        IExamSecurityService securityService,
        IExamTimerService timerService,
        IMapper mapper,
        ILogger<ExamSessionService> logger,
        ICurrentUserService currentUserService,
        IDistributedCache cache)
    {
        _sessionRepository = sessionRepository;
        _examRepository = examRepository;
        _securityService = securityService;
        _timerService = timerService;
        _mapper = mapper;
        _logger = logger;
        _currentUserService = currentUserService;
        _cache = cache;
    }

    public async Task<Result<ExamSessionDto>> StartExamSessionAsync(
        StartExamDto startDto, 
        int taikhoanId, 
        CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("StartExamSession");
        activity?.SetTag("user.id", taikhoanId.ToString());
        activity?.SetTag("exam.code", startDto.MaDeThi);

        try
        {
            // Security: Validate environment
            var environmentValidation = await _securityService.ValidateExamEnvironmentAsync(startDto.Environment, cancellationToken);
            if (!environmentValidation.IsSuccess)
            {
                _logger.LogWarning("Invalid exam environment for user {UserId}, exam {ExamCode}", taikhoanId, startDto.MaDeThi);
                return Result<ExamSessionDto>.Failure(environmentValidation.ErrorMessage ?? "Môi trường thi không hợp lệ", "INVALID_ENVIRONMENT");
            }

            // Get exam details
            var exam = await _examRepository.GetByMaDeThiAsync(startDto.MaDeThi);
            if (exam == null)
            {
                return Result<ExamSessionDto>.Failure("Không tìm thấy đề thi", "EXAM_NOT_FOUND");
            }

            // Validate exam availability
            if (exam.ThoiGianBatDau > DateTime.UtcNow)
            {
                return Result<ExamSessionDto>.Failure("Chưa đến thời gian thi", "EXAM_NOT_STARTED");
            }

            if (exam.TrangThai != "Active")
            {
                return Result<ExamSessionDto>.Failure("Đề thi không khả dụng", "EXAM_INACTIVE");
            }

            // Check for existing session
            var existingSession = await _sessionRepository.GetActiveSessionAsync(taikhoanId, exam.Id, cancellationToken);
            if (existingSession != null)
            {
                if (existingSession.Status == ExamSessionStatus.Submitted)
                {
                    return Result<ExamSessionDto>.Failure("Bạn đã hoàn thành bài thi này", "EXAM_ALREADY_COMPLETED");
                }

                if (existingSession.Status == ExamSessionStatus.Terminated)
                {
                    return Result<ExamSessionDto>.Failure("Phiên thi đã bị chấm dứt do vi phạm", "SESSION_TERMINATED");
                }

                // Resume existing session
                return await ResumeExamSessionAsync(existingSession.Id, taikhoanId, cancellationToken);
            }

            // Create new session
            using var transaction = await _sessionRepository.BeginTransactionAsync(cancellationToken);
            
            try
            {
                var session = new ExamSession
                {
                    TaikhoanId = taikhoanId,
                    DethiId = exam.Id,
                    MaDeThi = exam.MaDeThi,
                    Status = ExamSessionStatus.InProgress,
                    StartTime = DateTime.UtcNow,
                    AllocatedTimeMinutes = exam.ThoiGianLamBai ?? 60,
                    Environment = JsonSerializer.Serialize(startDto.Environment),
                    SecurityScore = 100, // Start with perfect score
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = taikhoanId
                };

                var savedSession = await _sessionRepository.AddAsync(session, cancellationToken);

                // Log session start
                await _securityService.LogSecurityEventAsync(new SecurityEventDto
                {
                    SessionId = savedSession.Id,
                    EventType = SecurityEventType.SessionStart,
                    EventData = "Exam session started",
                    UserAgent = startDto.Environment.UserAgent,
                    IpAddress = startDto.Environment.IpAddress,
                    Severity = SecuritySeverity.Low
                }, cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                // Cache session for quick access
                var cacheKey = $"exam_session:{savedSession.Id}";
                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(savedSession), 
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6)
                    }, cancellationToken);

                var result = _mapper.Map<ExamSessionDto>(savedSession);
                result.TenDeThi = exam.TenDeThi;
                result.TotalQuestions = exam.DethiCauhois.Count;

                _logger.LogInformation("Exam session {SessionId} started for user {UserId}, exam {ExamCode}", 
                    savedSession.Id, taikhoanId, startDto.MaDeThi);

                return Result<ExamSessionDto>.Success(result);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting exam session for user {UserId}, exam {ExamCode}", taikhoanId, startDto.MaDeThi);
            return Result<ExamSessionDto>.Failure("Có lỗi xảy ra khi bắt đầu thi", "INTERNAL_ERROR");
        }
    }

    public async Task<Result<ExamSessionDto>> GetSessionProgressAsync(
        int sessionId, 
        int taikhoanId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Security: Validate session access
            var accessValidation = await ValidateSessionAccessAsync(sessionId, taikhoanId, cancellationToken);
            if (!accessValidation.IsSuccess)
            {
                return Result<ExamSessionDto>.Failure(accessValidation.ErrorMessage ?? "Không có quyền truy cập", "ACCESS_DENIED");
            }

            // Try cache first
            var cacheKey = $"exam_session_progress:{sessionId}";
            var cachedProgress = await _cache.GetStringAsync(cacheKey, cancellationToken);
            
            if (!string.IsNullOrEmpty(cachedProgress))
            {
                var cached = JsonSerializer.Deserialize<ExamSessionDto>(cachedProgress);
                if (cached != null)
                {
                    // Update real-time data
                    var timeRemaining = await _timerService.GetTimeRemainingAsync(sessionId, cancellationToken);
                    if (timeRemaining.IsSuccess)
                    {
                        cached.RemainingTime = timeRemaining.Data!.RemainingTime;
                        cached.ElapsedTime = timeRemaining.Data.ElapsedTime;
                        cached.IsExpired = timeRemaining.Data.IsExpired;
                    }

                    return Result<ExamSessionDto>.Success(cached);
                }
            }

            // Get from database
            var session = await _sessionRepository.GetSessionWithDetailsAsync(sessionId, cancellationToken);
            if (session == null)
            {
                return Result<ExamSessionDto>.Failure("Không tìm thấy phiên thi", "SESSION_NOT_FOUND");
            }

            var result = _mapper.Map<ExamSessionDto>(session);
            
            // Get security status
            var securityStatus = await _securityService.GetSecurityStatusAsync(sessionId, cancellationToken);
            if (securityStatus.IsSuccess)
            {
                result.SecurityStatus = securityStatus.Data!;
            }

            // Get time remaining
            var timeResult = await _timerService.GetTimeRemainingAsync(sessionId, cancellationToken);
            if (timeResult.IsSuccess)
            {
                result.RemainingTime = timeResult.Data!.RemainingTime;
                result.ElapsedTime = timeResult.Data.ElapsedTime;
                result.IsExpired = timeResult.Data.IsExpired;
            }

            // Cache the result
            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(result),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1) // Short cache for progress
                }, cancellationToken);

            return Result<ExamSessionDto>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session progress for session {SessionId}, user {UserId}", sessionId, taikhoanId);
            return Result<ExamSessionDto>.Failure("Có lỗi xảy ra khi lấy tiến độ thi", "INTERNAL_ERROR");
        }
    }

    // Implement other methods with similar security and performance patterns...
}
```

## 🔧 Performance & Security Enhancements

### Real-time Features
- WebSocket connections for live monitoring
- Server-sent events for time updates
- Real-time security violation alerts

### Caching Strategy
- Session data caching with Redis
- Question caching for faster loading
- Security event aggregation

### Security Measures
- Environment fingerprinting
- Behavioral analysis
- Real-time violation detection
- Secure session management

## Tiếp theo: Workflow 05 - Enhanced Anti-Cheating System