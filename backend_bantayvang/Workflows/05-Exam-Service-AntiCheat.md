# Workflow 5: Enhanced Anti-Cheating System - Enterprise Edition

## 🎯 Mục tiêu
Xây dựng hệ thống chống gian lận tiên tiến với AI-powered detection, real-time monitoring và tuân thủ SOLID principles.

## 🔒 Security Requirements (OWASP Top 10)
- **A01 - Broken Access Control**: Secure anti-cheat API endpoints
- **A02 - Cryptographic Failures**: Encrypt behavioral data
- **A03 - Injection**: Prevent injection in monitoring scripts
- **A04 - Insecure Design**: Secure behavioral analysis
- **A06 - Vulnerable Components**: Secure monitoring libraries
- **A08 - Software Integrity**: Tamper-proof monitoring
- **A09 - Security Logging**: Comprehensive audit trails

## 🏗️ SOLID Principles Implementation

### Single Responsibility Principle (SRP)
- `IAntiCheatService`: Core anti-cheat functionality
- `IBehavioralAnalysisService`: Phân tích hành vi
- `IViolationDetectionService`: Phát hiện vi phạm
- `IRiskAssessmentService`: Đánh giá rủi ro

### Open/Closed Principle (OCP)
- Strategy pattern cho different detection algorithms
- Plugin architecture cho new violation types

### Interface Segregation Principle (ISP)
- Separate interfaces cho detection vs reporting

### Dependency Inversion Principle (DIP)
- Abstract detection algorithms and storage

## Bước 1: Enhanced Anti-Cheat Service Interfaces

### 1.1 Core Anti-Cheat Service Interface
```csharp
// Services/Interfaces/AntiCheat/IAntiCheatService.cs
public interface IAntiCheatService
{
    Task<Result> LogSuspiciousActivityAsync(SuspiciousActivityDto activityDto, CancellationToken cancellationToken = default);
    Task<Result<SecurityStatusDto>> GetSecurityStatusAsync(int baithiId, CancellationToken cancellationToken = default);
    Task<Result<ViolationAnalysisDto>> AnalyzeViolationPatternAsync(int baithiId, CancellationToken cancellationToken = default);
    Task<Result<bool>> ShouldTerminateExamAsync(int baithiId, CancellationToken cancellationToken = default);
    Task<Result<RiskAssessmentDto>> AssessExamRiskAsync(int baithiId, CancellationToken cancellationToken = default);
}

// Services/Interfaces/AntiCheat/IBehavioralAnalysisService.cs
public interface IBehavioralAnalysisService
{
    Task<Result<BehavioralProfileDto>> AnalyzeBehaviorAsync(int baithiId, BehavioralDataDto data, CancellationToken cancellationToken = default);
    Task<Result<AnomalyDetectionDto>> DetectAnomaliesAsync(int baithiId, CancellationToken cancellationToken = default);
    Task<Result<BehavioralTrendDto>> GetBehavioralTrendAsync(int baithiId, TimeSpan timeWindow, CancellationToken cancellationToken = default);
    Task<Result> UpdateBehavioralBaselineAsync(int userId, BehavioralDataDto data, CancellationToken cancellationToken = default);
}

// Services/Interfaces/AntiCheat/IViolationDetectionService.cs
public interface IViolationDetectionService
{
    Task<Result<ViolationDetectionDto>> DetectViolationAsync(ViolationEventDto eventDto, CancellationToken cancellationToken = default);
    Task<Result<List<ViolationPatternDto>>> AnalyzeViolationPatternsAsync(int baithiId, CancellationToken cancellationToken = default);
    Task<Result<ViolationSeverityDto>> AssessViolationSeverityAsync(ViolationEventDto eventDto, CancellationToken cancellationToken = default);
    Task<Result<bool>> ShouldTriggerActionAsync(int baithiId, ViolationType violationType, CancellationToken cancellationToken = default);
}
```

### 1.2 Enhanced Anti-Cheat DTOs
```csharp
// DTOs/AntiCheat/Enhanced/SuspiciousActivityDto.cs
public class SuspiciousActivityDto
{
    [Required(ErrorMessage = "Bài thi ID là bắt buộc")]
    [Range(1, int.MaxValue, ErrorMessage = "Bài thi ID không hợp lệ")]
    public int IdBaiThi { get; set; }
    
    [Required(ErrorMessage = "Loại vi phạm là bắt buộc")]
    public ViolationType ViolationType { get; set; }
    
    [Required(ErrorMessage = "Mô tả là bắt buộc")]
    [StringLength(1000, ErrorMessage = "Mô tả không được quá 1000 ký tự")]
    public string MoTa { get; set; } = string.Empty;
    
    public ViolationSeverity Severity { get; set; } = ViolationSeverity.Medium;
    public DateTime ThoiGian { get; set; } = DateTime.UtcNow;
    
    // Context information
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
    public string? ScreenResolution { get; set; }
    public int? QuestionId { get; set; }
    public TimeSpan? TimeSpentOnQuestion { get; set; }
    
    // Evidence data
    public Dictionary<string, object> Evidence { get; set; } = new();
    public List<string> Screenshots { get; set; } = new(); // Base64 encoded
    public BehavioralContextDto? BehavioralContext { get; set; }
}

public enum ViolationType
{
    // Navigation violations
    TabSwitch,
    WindowFocusLoss,
    BrowserNavigation,
    NewWindowOpened,
    FullscreenExit,
    
    // Input violations
    RightClick,
    CopyAttempt,
    PasteAttempt,
    CutAttempt,
    KeyboardShortcut,
    
    // Content violations
    TextSelection,
    ImageDownload,
    PrintAttempt,
    ScreenshotAttempt,
    
    // Technical violations
    DevToolsOpen,
    ConsoleAccess,
    NetworkInspection,
    JavaScriptDisabled,
    
    // Behavioral violations
    SuspiciousTypingPattern,
    UnusualMouseBehavior,
    AbnormalAnswerSpeed,
    PatternRecognition,
    
    // System violations
    VirtualMachine,
    RemoteDesktop,
    ScreenSharing,
    MultipleMonitors,
    
    // Advanced violations
    AIAssistance,
    CollaborativeAnswering,
    ExternalCommunication,
    DataExfiltration
}

public enum ViolationSeverity
{
    Info = 1,
    Low = 2,
    Medium = 3,
    High = 4,
    Critical = 5
}

// DTOs/AntiCheat/Enhanced/SecurityStatusDto.cs
public class SecurityStatusDto
{
    public int IdBaiThi { get; set; }
    public SecurityRiskLevel RiskLevel { get; set; }
    public double RiskScore { get; set; } // 0-100
    
    public int TotalViolations { get; set; }
    public int CriticalViolations { get; set; }
    public int HighViolations { get; set; }
    public int MediumViolations { get; set; }
    public int LowViolations { get; set; }
    
    public bool ShouldTerminate { get; set; }
    public bool RequiresReview { get; set; }
    public bool IsUnderMonitoring { get; set; }
    
    public List<ViolationSummaryDto> RecentViolations { get; set; } = new();
    public List<string> ActiveAlerts { get; set; } = new();
    public List<string> RecommendedActions { get; set; } = new();
    
    public DateTime LastViolation { get; set; }
    public DateTime LastAssessment { get; set; } = DateTime.UtcNow;
}

public enum SecurityRiskLevel
{
    Low,
    Medium,
    High,
    Critical
}
```

## Bước 2: Enhanced Anti-Cheat Service Implementation

### 2.1 Core Anti-Cheat Service
```csharp
// Services/Impl/AntiCheat/AntiCheatService.cs
public class AntiCheatService : IAntiCheatService
{
    private readonly ICanhbaogianlanRepository _canhbaoRepository;
    private readonly IBaithiRepository _baithiRepository;
    private readonly IBehavioralAnalysisService _behavioralService;
    private readonly IViolationDetectionService _violationService;
    private readonly IMapper _mapper;
    private readonly ILogger<AntiCheatService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHubContext<ExamMonitoringHub> _hubContext;

    public AntiCheatService(
        ICanhbaogianlanRepository canhbaoRepository,
        IBaithiRepository baithiRepository,
        IBehavioralAnalysisService behavioralService,
        IViolationDetectionService violationService,
        IMapper mapper,
        ILogger<AntiCheatService> logger,
        IConfiguration configuration,
        IHubContext<ExamMonitoringHub> hubContext)
    {
        _canhbaoRepository = canhbaoRepository;
        _baithiRepository = baithiRepository;
        _behavioralService = behavioralService;
        _violationService = violationService;
        _mapper = mapper;
        _logger = logger;
        _configuration = configuration;
        _hubContext = hubContext;
    }

    public async Task<Result> LogSuspiciousActivityAsync(
        SuspiciousActivityDto activityDto, 
        CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("LogSuspiciousActivity");
        activity?.SetTag("exam.session", activityDto.IdBaiThi.ToString());
        activity?.SetTag("violation.type", activityDto.ViolationType.ToString());

        try
        {
            // Security: Validate session exists and is active
            var baithi = await _baithiRepository.GetByIdAsync(activityDto.IdBaiThi);
            if (baithi == null)
            {
                return Result.Failure("Không tìm thấy phiên thi", "SESSION_NOT_FOUND");
            }

            if (baithi.TrangThai != "InProgress")
            {
                return Result.Failure("Phiên thi không còn hoạt động", "SESSION_INACTIVE");
            }

            // Detect violation severity and patterns
            var violationEvent = new ViolationEventDto
            {
                SessionId = activityDto.IdBaiThi,
                ViolationType = activityDto.ViolationType,
                EventData = activityDto.MoTa,
                Severity = activityDto.Severity,
                Timestamp = activityDto.ThoiGian,
                UserAgent = activityDto.UserAgent,
                IpAddress = activityDto.IpAddress,
                QuestionId = activityDto.QuestionId,
                TimeSpentOnQuestion = activityDto.TimeSpentOnQuestion,
                Evidence = activityDto.Evidence,
                BehavioralContext = activityDto.BehavioralContext
            };

            var detectionResult = await _violationService.DetectViolationAsync(violationEvent, cancellationToken);
            if (!detectionResult.IsSuccess)
            {
                _logger.LogWarning("Failed to analyze violation for session {SessionId}: {Error}", 
                    activityDto.IdBaiThi, detectionResult.ErrorMessage);
            }

            // Store violation record
            var canhbao = new Canhbaogianlan
            {
                IdBaiThi = activityDto.IdBaiThi,
                LoaiCanhBao = activityDto.ViolationType.ToString(),
                MoTa = activityDto.MoTa,
                ThoiGian = activityDto.ThoiGian,
                MucDoNghiemTrong = (int)activityDto.Severity,
                UserAgent = activityDto.UserAgent,
                IpAddress = activityDto.IpAddress,
                QuestionId = activityDto.QuestionId,
                Evidence = JsonSerializer.Serialize(activityDto.Evidence),
                CreatedAt = DateTime.UtcNow
            };

            await _canhbaoRepository.AddAsync(canhbao, cancellationToken);

            // Update exam session security metrics
            await UpdateSessionSecurityMetricsAsync(baithi, activityDto.Severity, cancellationToken);

            // Check if action should be triggered
            var shouldTriggerAction = await _violationService.ShouldTriggerActionAsync(
                activityDto.IdBaiThi, activityDto.ViolationType, cancellationToken);

            if (shouldTriggerAction.IsSuccess && shouldTriggerAction.Data)
            {
                await TriggerSecurityActionAsync(activityDto.IdBaiThi, activityDto.ViolationType, activityDto.Severity, cancellationToken);
            }

            // Real-time notification to monitoring dashboard
            await _hubContext.Clients.Group($"exam_{baithi.IdDeThi}").SendAsync("ViolationDetected", new
            {
                SessionId = activityDto.IdBaiThi,
                ViolationType = activityDto.ViolationType.ToString(),
                Severity = activityDto.Severity.ToString(),
                Timestamp = activityDto.ThoiGian,
                Description = activityDto.MoTa
            }, cancellationToken);

            _logger.LogWarning("Security violation logged for session {SessionId}: {ViolationType} - {Severity}", 
                activityDto.IdBaiThi, activityDto.ViolationType, activityDto.Severity);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging suspicious activity for session {SessionId}", activityDto.IdBaiThi);
            return Result.Failure("Có lỗi xảy ra khi ghi nhận hoạt động đáng ngờ", "LOGGING_ERROR");
        }
    }

    public async Task<Result<SecurityStatusDto>> GetSecurityStatusAsync(
        int baithiId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get all violations for this session
            var violations = await _canhbaoRepository.GetByBaiThiAsync(baithiId);
            
            var status = new SecurityStatusDto
            {
                IdBaiThi = baithiId,
                TotalViolations = violations.Count,
                CriticalViolations = violations.Count(v => v.MucDoNghiemTrong == (int)ViolationSeverity.Critical),
                HighViolations = violations.Count(v => v.MucDoNghiemTrong == (int)ViolationSeverity.High),
                MediumViolations = violations.Count(v => v.MucDoNghiemTrong == (int)ViolationSeverity.Medium),
                LowViolations = violations.Count(v => v.MucDoNghiemTrong == (int)ViolationSeverity.Low),
                LastViolation = violations.OrderByDescending(v => v.ThoiGian).FirstOrDefault()?.ThoiGian ?? DateTime.MinValue
            };

            // Calculate risk score
            status.RiskScore = CalculateRiskScore(status);
            status.RiskLevel = DetermineRiskLevel(status.RiskScore);

            // Determine if session should be terminated
            var terminationThresholds = _configuration.GetSection("AntiCheat:TerminationThresholds").Get<TerminationThresholdsDto>();
            status.ShouldTerminate = ShouldTerminateSession(status, terminationThresholds);

            // Get recent violations for detailed view
            status.RecentViolations = violations
                .OrderByDescending(v => v.ThoiGian)
                .Take(10)
                .Select(v => new ViolationSummaryDto
                {
                    ViolationType = v.LoaiCanhBao ?? "",
                    Severity = (ViolationSeverity)(v.MucDoNghiemTrong ?? 1),
                    Timestamp = v.ThoiGian ?? DateTime.MinValue,
                    Description = v.MoTa ?? ""
                })
                .ToList();

            // Generate recommendations
            status.RecommendedActions = GenerateRecommendations(status);

            return Result<SecurityStatusDto>.Success(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting security status for session {SessionId}", baithiId);
            return Result<SecurityStatusDto>.Failure("Có lỗi xảy ra khi lấy trạng thái bảo mật", "STATUS_ERROR");
        }
    }

    private async Task UpdateSessionSecurityMetricsAsync(
        Baithi baithi, 
        ViolationSeverity severity, 
        CancellationToken cancellationToken)
    {
        // Update violation count
        baithi.TongSoCanhBao = (baithi.TongSoCanhBao ?? 0) + 1;

        // Update security score (decrease based on severity)
        var scoreDeduction = severity switch
        {
            ViolationSeverity.Critical => 20,
            ViolationSeverity.High => 10,
            ViolationSeverity.Medium => 5,
            ViolationSeverity.Low => 2,
            _ => 1
        };

        baithi.DiemBaoMat = Math.Max(0, (baithi.DiemBaoMat ?? 100) - scoreDeduction);

        await _baithiRepository.UpdateAsync(baithi, cancellationToken);
    }

    private async Task TriggerSecurityActionAsync(
        int baithiId, 
        ViolationType violationType, 
        ViolationSeverity severity, 
        CancellationToken cancellationToken)
    {
        var actionConfig = _configuration.GetSection($"AntiCheat:Actions:{violationType}").Get<SecurityActionDto>();
        
        if (actionConfig?.AutoTerminate == true && severity >= ViolationSeverity.High)
        {
            await TerminateSessionAsync(baithiId, $"Auto-terminated due to {violationType}", cancellationToken);
        }
        else if (actionConfig?.SendAlert == true)
        {
            await SendSecurityAlertAsync(baithiId, violationType, severity, cancellationToken);
        }
    }

    private double CalculateRiskScore(SecurityStatusDto status)
    {
        var weights = _configuration.GetSection("AntiCheat:RiskWeights").Get<RiskWeightsDto>() ?? new RiskWeightsDto();
        
        return Math.Min(100, 
            (status.CriticalViolations * weights.CriticalWeight) +
            (status.HighViolations * weights.HighWeight) +
            (status.MediumViolations * weights.MediumWeight) +
            (status.LowViolations * weights.LowWeight));
    }

    private SecurityRiskLevel DetermineRiskLevel(double riskScore)
    {
        return riskScore switch
        {
            >= 80 => SecurityRiskLevel.Critical,
            >= 60 => SecurityRiskLevel.High,
            >= 30 => SecurityRiskLevel.Medium,
            _ => SecurityRiskLevel.Low
        };
    }

    // Implement other methods...
}
```

## 🔧 Performance & Security Enhancements

### Real-time Monitoring
- WebSocket connections for live violation alerts
- Real-time risk score updates
- Instant security action triggers

### AI-Powered Detection
- Machine learning models for behavioral analysis
- Pattern recognition algorithms
- Predictive risk assessment

## Tiếp theo: Workflow 06 - Enhanced Grading System