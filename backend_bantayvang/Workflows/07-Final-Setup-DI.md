# Workflow 7: Enterprise Final Setup và Dependency Injection

## 🎯 Mục tiêu
Hoàn thiện cấu hình enterprise-grade với comprehensive DI setup, security configuration, performance optimization và monitoring.

## 🔒 Security Requirements (OWASP Top 10)
- **A01 - Broken Access Control**: JWT authentication và role-based authorization
- **A02 - Cryptographic Failures**: Secure configuration management
- **A05 - Security Misconfiguration**: Hardened application settings
- **A06 - Vulnerable Components**: Secure dependency management
- **A09 - Security Logging**: Comprehensive audit logging
- **A10 - Server-Side Request Forgery**: Input validation và sanitization

## 🏗️ SOLID Principles Implementation

### Dependency Inversion Principle (DIP)
- Complete abstraction của all dependencies
- Interface-based service registration
- Configuration-driven implementations

### Single Responsibility Principle (SRP)
- Separate configuration classes
- Dedicated service registration extensions
- Modular middleware setup

## Bước 1: Enhanced Controllers với Security

### 1.1 Secure Grading Controller
```csharp
// Controllers/GradingController.cs
[ApiController]
[Route("api/[controller]")]
[Authorize] // Security: Require authentication
[ApiVersion("1.0")]
[Produces("application/json")]
public class GradingController : ControllerBase
{
    private readonly IGradingService _gradingService;
    private readonly IManualGradingService _manualGradingService;
    private readonly IStatisticalAnalysisService _statisticalService;
    private readonly IReportGenerationService _reportService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GradingController> _logger;
    private readonly IRateLimitService _rateLimitService;

    public GradingController(
        IGradingService gradingService,
        IManualGradingService manualGradingService,
        IStatisticalAnalysisService statisticalService,
        IReportGenerationService reportService,
        ICurrentUserService currentUserService,
        ILogger<GradingController> logger,
        IRateLimitService rateLimitService)
    {
        _gradingService = gradingService;
        _manualGradingService = manualGradingService;
        _statisticalService = statisticalService;
        _reportService = reportService;
        _currentUserService = currentUserService;
        _logger = logger;
        _rateLimitService = rateLimitService;
    }

    /// <summary>
    /// Chấm điểm tự động cho bài thi
    /// </summary>
    [HttpPost("grade/{baithiId:int}")]
    [Authorize(Roles = "Admin,Teacher,Grader")]
    [ProducesResponseType(typeof(ApiResponse<GradingResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<GradingResultDto>>> GradeExam(
        int baithiId,
        CancellationToken cancellationToken = default)
    {
        // Security: Rate limiting
        var rateLimitResult = await _rateLimitService.CheckRateLimitAsync(
            _currentUserService.UserId, "GradeExam", 20, TimeSpan.FromMinutes(1));
        
        if (!rateLimitResult.IsAllowed)
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, 
                ApiResponse.Failure("Quá nhiều yêu cầu chấm điểm"));
        }

        if (baithiId <= 0)
        {
            return BadRequest(ApiResponse.Failure("ID bài thi không hợp lệ"));
        }

        var result = await _gradingService.GradeExamAsync(baithiId, cancellationToken);
        
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse.Failure(result.ErrorMessage ?? "Có lỗi xảy ra khi chấm điểm"));
        }

        _logger.LogInformation("Exam {ExamId} graded by user {UserId}", baithiId, _currentUserService.UserId);
        return Ok(ApiResponse.Success(result.Data));
    }

    /// <summary>
    /// Chấm thủ công câu tự luận
    /// </summary>
    [HttpPost("grade-essay")]
    [Authorize(Roles = "Admin,Teacher,Grader")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse>> GradeEssayQuestion(
        [FromBody] GradeEssayDto gradeDto,
        CancellationToken cancellationToken = default)
    {
        var result = await _manualGradingService.GradeEssayQuestionAsync(
            gradeDto, _currentUserService.UserId, cancellationToken);
        
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse.Failure(result.ErrorMessage ?? "Có lỗi xảy ra khi chấm câu tự luận"));
        }

        return Ok(ApiResponse.Success("Chấm câu tự luận thành công"));
    }

    /// <summary>
    /// Lấy kết quả thi của thí sinh
    /// </summary>
    [HttpGet("result/{baithiId:int}")]
    [ProducesResponseType(typeof(ApiResponse<ExamResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ExamResultDto>>> GetExamResult(
        int baithiId,
        CancellationToken cancellationToken = default)
    {
        var result = await _gradingService.GetExamResultAsync(baithiId, _currentUserService.UserId, cancellationToken);
        
        if (!result.IsSuccess)
        {
            return NotFound(ApiResponse.Failure(result.ErrorMessage ?? "Không tìm thấy kết quả thi"));
        }

        return Ok(ApiResponse.Success(result.Data));
    }

    /// <summary>
    /// Lấy thống kê đề thi
    /// </summary>
    [HttpGet("statistics/{dethiId:int}")]
    [Authorize(Roles = "Admin,Teacher")]
    [ProducesResponseType(typeof(ApiResponse<ExamStatisticsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ExamStatisticsDto>>> GetExamStatistics(
        int dethiId,
        CancellationToken cancellationToken = default)
    {
        var result = await _statisticalService.GetExamStatisticsAsync(dethiId, cancellationToken);
        
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse.Failure(result.ErrorMessage ?? "Có lỗi xảy ra khi lấy thống kê"));
        }

        return Ok(ApiResponse.Success(result.Data));
    }
}

```

## Bước 2: Enhanced AutoMapper Configuration

### 2.1 Comprehensive AutoMapper Profiles
```csharp
// Mappings/AutoMapperProfile.cs
public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        // Question mappings with security considerations
        CreateMap<Cauhoi, CauhoiDto>()
            .ForMember(dest => dest.TenDanhMuc, opt => opt.MapFrom(src => src.IdDanhMucNavigation!.TenDanhMuc))
            .ForMember(dest => dest.TenLoaiCauHoi, opt => opt.MapFrom(src => src.IdLoaiCauHoiNavigation!.TenLoai))
            .ForMember(dest => dest.DanhSachLuaChon, opt => opt.MapFrom(src => src.Luachons))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.NgayTao))
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.NguoiTao));

        CreateMap<CreateCauhoiDto, Cauhoi>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.NgayTao, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.DaXoa, opt => opt.MapFrom(src => false))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.Version, opt => opt.MapFrom(src => 1));

        CreateMap<Luachon, LuachonDto>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt));
        
        CreateMap<CreateLuachonDto, Luachon>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

        // Exam mappings with enhanced security
        CreateMap<Dethi, DethiDto>()
            .ForMember(dest => dest.SoCauHoi, opt => opt.MapFrom(src => src.DethiCauhois.Count))
            .ForMember(dest => dest.DanhSachCauHoi, opt => opt.MapFrom(src => 
                src.DethiCauhois.Select(dc => dc.IdCauHoiNavigation)))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.NgayTao))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.TrangThai == "Active"));

        CreateMap<CreateDethiDto, Dethi>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.NgayTao, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.Version, opt => opt.MapFrom(src => 1));

        CreateMap<Baithi, BaithiDto>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.ThoiGianBatDau))
            .ForMember(dest => dest.SubmittedAt, opt => opt.MapFrom(src => src.ThoiGianNop))
            .ForMember(dest => dest.SecurityScore, opt => opt.MapFrom(src => src.DiemBaoMat));

        // Enhanced grading mappings
        CreateMap<Baithi, ExamResultDto>()
            .ForMember(dest => dest.TenTaiKhoan, opt => opt.MapFrom(src => src.IdTaiKhoanNavigation!.TenDangNhap))
            .ForMember(dest => dest.MaNhanVien, opt => opt.MapFrom(src => src.IdTaiKhoanNavigation!.MaNhanVien))
            .ForMember(dest => dest.TenDeThi, opt => opt.MapFrom(src => src.IdDeThiNavigation!.TenDeThi))
            .ForMember(dest => dest.MaDeThi, opt => opt.MapFrom(src => src.IdDeThiNavigation!.MaDeThi))
            .ForMember(dest => dest.DiemToiDa, opt => opt.MapFrom(src => src.IdDeThiNavigation!.TongDiem))
            .ForMember(dest => dest.PhanTramDiem, opt => opt.MapFrom(src => 
                src.TongDiem.HasValue && src.IdDeThiNavigation!.TongDiem.HasValue && src.IdDeThiNavigation.TongDiem > 0
                    ? (src.TongDiem.Value / src.IdDeThiNavigation.TongDiem.Value) * 100
                    : 0))
            .ForMember(dest => dest.XepLoai, opt => opt.MapFrom(src => src.XepLoai))
            .ForMember(dest => dest.SecurityScore, opt => opt.MapFrom(src => src.DiemBaoMat))
            .ForMember(dest => dest.ViolationCount, opt => opt.MapFrom(src => src.TongSoCanhBao));

        // Anti-cheat mappings
        CreateMap<Canhbaogianlan, ViolationSummaryDto>()
            .ForMember(dest => dest.ViolationType, opt => opt.MapFrom(src => src.LoaiCanhBao))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.MoTa))
            .ForMember(dest => dest.Timestamp, opt => opt.MapFrom(src => src.ThoiGian))
            .ForMember(dest => dest.Severity, opt => opt.MapFrom(src => (ViolationSeverity)(src.MucDoNghiemTrong ?? 1)));
    }
}

// Mappings/SecurityMappingProfile.cs
public class SecurityMappingProfile : Profile
{
    public SecurityMappingProfile()
    {
        CreateMap<SuspiciousActivityDto, Canhbaogianlan>()
            .ForMember(dest => dest.LoaiCanhBao, opt => opt.MapFrom(src => src.ViolationType.ToString()))
            .ForMember(dest => dest.MoTa, opt => opt.MapFrom(src => src.MoTa))
            .ForMember(dest => dest.ThoiGian, opt => opt.MapFrom(src => src.ThoiGian))
            .ForMember(dest => dest.MucDoNghiemTrong, opt => opt.MapFrom(src => (int)src.Severity))
            .ForMember(dest => dest.UserAgent, opt => opt.MapFrom(src => src.UserAgent))
            .ForMember(dest => dest.IpAddress, opt => opt.MapFrom(src => src.IpAddress))
            .ForMember(dest => dest.Evidence, opt => opt.MapFrom(src => JsonSerializer.Serialize(src.Evidence)))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));
    }
}
```

## Bước 3: Enterprise Dependency Injection Setup

### 3.1 Enhanced Program.cs với Security và Performance
```csharp
// Program.cs
using BanTayVang.API.Models;
using BanTayVang.API.Extensions;
using BanTayVang.API.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog;
using Microsoft.OpenApi.Models;
using System.Reflection;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/bantayvang-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Add Serilog
builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers(options =>
{
    options.Filters.Add<GlobalExceptionFilter>();
    options.Filters.Add<ValidationFilter>();
});

// API Versioning
builder.Services.AddApiVersioning(opt =>
{
    opt.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
    opt.AssumeDefaultVersionWhenUnspecified = true;
    opt.ApiVersionReader = Microsoft.AspNetCore.Mvc.ApiVersionReader.Combine(
        new Microsoft.AspNetCore.Mvc.QueryStringApiVersionReader("version"),
        new Microsoft.AspNetCore.Mvc.HeaderApiVersionReader("X-Version"));
});

// Swagger/OpenAPI with security
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "BanTayVang API", 
        Version = "v1",
        Description = "Enterprise Exam Management System API",
        Contact = new OpenApiContact
        {
            Name = "BanTayVang Team",
            Email = "support@bantayvang.com"
        }
    });

    // JWT Authentication in Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

// Database with connection resilience
builder.Services.AddDbContext<BanTayVangDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
        sqlOptions.CommandTimeout(30);
    });
    
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// Redis Cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "BanTayVang";
});

// Memory Cache as fallback
builder.Services.AddMemoryCache();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Encoding.ASCII.GetBytes(jwtSettings["SecretKey"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(secretKey),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("TeacherOrAdmin", policy => policy.RequireRole("Admin", "Teacher"));
    options.AddPolicy("GraderAccess", policy => policy.RequireRole("Admin", "Teacher", "Grader"));
});

// CORS with security
builder.Services.AddCors(options =>
{
    options.AddPolicy("Production", builder =>
    {
        builder.WithOrigins(
                "https://bantayvang.com",
                "https://app.bantayvang.com"
            )
            .AllowedMethods("GET", "POST", "PUT", "DELETE")
            .AllowedHeaders("Content-Type", "Authorization", "X-Version")
            .AllowCredentials();
    });

    options.AddPolicy("Development", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContext<BanTayVangDbContext>()
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!)
    .AddCheck<CustomHealthCheck>("custom");

// SignalR for real-time features
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("Api", limiterOptions =>
    {
        limiterOptions.PermitLimit = 100;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 10;
    });
});

// Register custom services
builder.Services.RegisterRepositories();
builder.Services.RegisterBusinessServices();
builder.Services.RegisterInfrastructureServices();
builder.Services.RegisterSecurityServices();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BanTayVang API V1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Security middleware
app.UseHttpsRedirection();
app.UseSecurityHeaders();
app.UseRateLimiter();

// CORS
app.UseCors(app.Environment.IsDevelopment() ? "Development" : "Production");

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Custom middleware
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ExamSecurityMiddleware>();

// Health checks
app.MapHealthChecks("/health");

// Controllers
app.MapControllers();

// SignalR Hubs
app.MapHub<ExamMonitoringHub>("/hubs/exam-monitoring");
app.MapHub<GradingHub>("/hubs/grading");

// Database migration and seeding
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<BanTayVangDbContext>();
    
    if (app.Environment.IsDevelopment())
    {
        await context.Database.EnsureCreatedAsync();
        await SeedData.SeedAsync(context);
    }
    else
    {
        await context.Database.MigrateAsync();
    }
}

app.Run();
```

### 3.2 Service Registration Extensions
```csharp
// Extensions/ServiceCollectionExtensions.cs
public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterRepositories(this IServiceCollection services)
    {
        // Base repositories
        services.AddScoped<ICauhoiRepository, CauhoiRepository>();
        services.AddScoped<ILuachonRepository, LuachonRepository>();
        services.AddScoped<IDethiRepository, DethiRepository>();
        services.AddScoped<IBaithiRepository, BaithiRepository>();
        services.AddScoped<IChitietlambaiRepository, ChitietlambaiRepository>();
        services.AddScoped<ICanhbaogianlanRepository, CanhbaogianlanRepository>();
        services.AddScoped<IDanhmucauhoiRepository, DanhmucauhoiRepository>();
        services.AddScoped<ILoaicauhoiRepository, LoaicauhoiRepository>();
        services.AddScoped<ITaikhoanRepository, TaikhoanRepository>();

        // Enhanced repositories
        services.AddScoped<IExamSessionRepository, ExamSessionRepository>();
        services.AddScoped<IExamAnswerRepository, ExamAnswerRepository>();
        services.AddScoped<ISecurityEventRepository, SecurityEventRepository>();
        services.AddScoped<IBehavioralDataRepository, BehavioralDataRepository>();
        services.AddScoped<IUserBaselineRepository, UserBaselineRepository>();

        return services;
    }

    public static IServiceCollection RegisterBusinessServices(this IServiceCollection services)
    {
        // Question services
        services.AddScoped<IQuestionReadService, QuestionReadService>();
        services.AddScoped<IQuestionWriteService, QuestionWriteService>();
        services.AddScoped<IQuestionImportService, QuestionImportService>();
        services.AddScoped<IQuestionValidator, QuestionValidator>();
        services.AddScoped<IQuestionCacheService, QuestionCacheService>();

        // Exam services
        services.AddScoped<IExamManagementService, ExamManagementService>();
        services.AddScoped<IExamSessionService, ExamSessionService>();
        services.AddScoped<IExamSubmissionService, ExamSubmissionService>();
        services.AddScoped<IExamTimerService, ExamTimerService>();

        // Grading services
        services.AddScoped<IGradingService, GradingService>();
        services.AddScoped<IManualGradingService, ManualGradingService>();
        services.AddScoped<IStatisticalAnalysisService, StatisticalAnalysisService>();
        services.AddScoped<IReportGenerationService, ReportGenerationService>();
        services.AddScoped<IGradeValidationService, GradeValidationService>();

        // Anti-cheat services
        services.AddScoped<IAntiCheatService, AntiCheatService>();
        services.AddScoped<IBehavioralAnalysisService, BehavioralAnalysisService>();
        services.AddScoped<IViolationDetectionService, ViolationDetectionService>();
        services.AddScoped<IRiskAssessmentService, RiskAssessmentService>();

        return services;
    }

    public static IServiceCollection RegisterInfrastructureServices(this IServiceCollection services)
    {
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IRateLimitService, RateLimitService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IFileStorageService, FileStorageService>();
        services.AddScoped<IEncryptionService, EncryptionService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }

    public static IServiceCollection RegisterSecurityServices(this IServiceCollection services)
    {
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHashService, PasswordHashService>();
        services.AddScoped<ISecurityHeadersService, SecurityHeadersService>();
        services.AddScoped<IInputSanitizationService, InputSanitizationService>();

        return services;
    }
}
```

## Bước 4: Enhanced Configuration Files

### 4.1 Comprehensive appsettings.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=BanTayVangDB;Trusted_Connection=true;TrustServerCertificate=true;MultipleActiveResultSets=true;",
    "Redis": "localhost:6379"
  },
  "JwtSettings": {
    "SecretKey": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "BanTayVang.API",
    "Audience": "BanTayVang.Client",
    "ExpirationInMinutes": 60,
    "RefreshTokenExpirationInDays": 7
  },
  "ExamSettings": {
    "MaxWarningsAllowed": 5,
    "AutoSubmitOnMaxWarnings": true,
    "DefaultExamTimeMinutes": 60,
    "MaxConcurrentExams": 1000,
    "SessionTimeoutMinutes": 30,
    "AutoSaveIntervalSeconds": 30
  },
  "AntiCheat": {
    "Enabled": true,
    "TerminationThresholds": {
      "CriticalViolations": 3,
      "HighViolations": 5,
      "TotalViolations": 10,
      "RiskScoreThreshold": 80
    },
    "RiskWeights": {
      "CriticalWeight": 25,
      "HighWeight": 15,
      "MediumWeight": 8,
      "LowWeight": 3,
      "TypingWeight": 0.2,
      "MouseWeight": 0.15,
      "NavigationWeight": 0.25,
      "TimingWeight": 0.2,
      "ConsistencyWeight": 0.1,
      "AnomalyWeight": 0.1
    },
    "Actions": {
      "TabSwitch": {
        "AutoTerminate": false,
        "SendAlert": true,
        "MaxOccurrences": 3
      },
      "DevToolsOpen": {
        "AutoTerminate": true,
        "SendAlert": true,
        "MaxOccurrences": 1
      },
      "CopyAttempt": {
        "AutoTerminate": false,
        "SendAlert": true,
        "MaxOccurrences": 2
      }
    }
  },
  "Grading": {
    "PassingThreshold": 50.0,
    "UseAIAssistance": true,
    "AutoGradeMultipleChoice": true,
    "RequireManualReview": false,
    "GradingTimeoutMinutes": 30
  },
  "FileUpload": {
    "MaxFileSizeMB": 10,
    "AllowedExtensions": [".xlsx", ".xls", ".pdf", ".doc", ".docx"],
    "UploadPath": "uploads",
    "ScanForViruses": true
  },
  "Email": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "EnableSsl": true,
    "FromEmail": "noreply@bantayvang.com",
    "FromName": "BanTayVang System"
  },
  "Security": {
    "EnableSecurityHeaders": true,
    "RequireHttps": true,
    "EnableCsrfProtection": true,
    "MaxLoginAttempts": 5,
    "LockoutDurationMinutes": 15,
    "PasswordPolicy": {
      "MinLength": 8,
      "RequireUppercase": true,
      "RequireLowercase": true,
      "RequireDigit": true,
      "RequireSpecialChar": true
    }
  },
  "Performance": {
    "EnableCaching": true,
    "CacheExpirationMinutes": 30,
    "EnableCompression": true,
    "MaxConcurrentRequests": 1000
  },
  "Monitoring": {
    "EnableHealthChecks": true,
    "EnableMetrics": true,
    "LogSensitiveData": false
  },
  "AllowedHosts": "*"
}
```

### 4.2 Production appsettings.Production.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Error",
      "BanTayVang": "Information"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "#{DatabaseConnectionString}#",
    "Redis": "#{RedisConnectionString}#"
  },
  "JwtSettings": {
    "SecretKey": "#{JwtSecretKey}#",
    "ExpirationInMinutes": 30
  },
  "Security": {
    "RequireHttps": true,
    "EnableCsrfProtection": true
  },
  "Performance": {
    "EnableCaching": true,
    "EnableCompression": true
  },
  "Monitoring": {
    "LogSensitiveData": false
  }
}
```

## Bước 5: Enhanced Repository Interfaces

### 5.1 Missing Repository Interfaces với Security
```csharp
// Repositories/Interfaces/ILuachonRepository.cs
public interface ILuachonRepository : IBaseRepository<Luachon>
{
    Task<List<Luachon>> GetByCauHoiAsync(int cauhoiId, CancellationToken cancellationToken = default);
    Task<Luachon?> GetCorrectAnswerAsync(int cauhoiId, CancellationToken cancellationToken = default);
    Task<List<Luachon>> GetShuffledChoicesAsync(int cauhoiId, CancellationToken cancellationToken = default);
    Task<bool> ValidateChoiceAsync(int choiceId, int questionId, CancellationToken cancellationToken = default);
}

// Repositories/Interfaces/ICanhbaogianlanRepository.cs
public interface ICanhbaogianlanRepository : IBaseRepository<Canhbaogianlan>
{
    Task<List<Canhbaogianlan>> GetByBaiThiAsync(int baithiId, CancellationToken cancellationToken = default);
    Task<int> CountWarningsByBaiThiAsync(int baithiId, CancellationToken cancellationToken = default);
    Task<int> CountViolationsByTypeAsync(int baithiId, string violationType, CancellationToken cancellationToken = default);
    Task<List<Canhbaogianlan>> GetViolationsByTimeRangeAsync(int baithiId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<Dictionary<string, int>> GetViolationStatisticsAsync(int baithiId, CancellationToken cancellationToken = default);
}

// Repositories/Interfaces/IDanhmucauhoiRepository.cs
public interface IDanhmucauhoiRepository : IBaseRepository<Danhmucauhoi>
{
    Task<List<Danhmucauhoi>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<List<Danhmucauhoi>> GetByUserPermissionAsync(int userId, CancellationToken cancellationToken = default);
    Task<bool> ValidateAccessAsync(int categoryId, int userId, CancellationToken cancellationToken = default);
}

// Repositories/Interfaces/ILoaicauhoiRepository.cs
public interface ILoaicauhoiRepository : IBaseRepository<Loaicauhoi>
{
    Task<List<Loaicauhoi>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<List<Loaicauhoi>> GetSupportedTypesAsync(CancellationToken cancellationToken = default);
}

// Repositories/Interfaces/ITaikhoanRepository.cs
public interface ITaikhoanRepository : IBaseRepository<Taikhoan>
{
    Task<Taikhoan?> GetByTenDangNhapAsync(string tenDangNhap, CancellationToken cancellationToken = default);
    Task<bool> ValidatePasswordAsync(string tenDangNhap, string matKhau, CancellationToken cancellationToken = default);
    Task<List<string>> GetUserRolesAsync(int userId, CancellationToken cancellationToken = default);
    Task<bool> IsAccountLockedAsync(int userId, CancellationToken cancellationToken = default);
    Task<int> GetFailedLoginAttemptsAsync(int userId, CancellationToken cancellationToken = default);
    Task IncrementFailedLoginAttemptsAsync(int userId, CancellationToken cancellationToken = default);
    Task ResetFailedLoginAttemptsAsync(int userId, CancellationToken cancellationToken = default);
}
```

## Bước 6: API Testing và Documentation

### 6.1 Enhanced API Testing Endpoints
```http
@BanTayVang.API_HostAddress = https://localhost:7293
@AuthToken = Bearer {{auth_token}}

### Authentication
POST {{BanTayVang.API_HostAddress}}/api/auth/login
Content-Type: application/json

{
  "tenDangNhap": "admin",
  "matKhau": "Admin@123"
}

### Test Question Management with Security
GET {{BanTayVang.API_HostAddress}}/api/cauhoi?page=1&pageSize=10
Authorization: {{AuthToken}}
Accept: application/json

### Create Question with Validation
POST {{BanTayVang.API_HostAddress}}/api/cauhoi
Authorization: {{AuthToken}}
Content-Type: application/json

{
  "idDanhMuc": 1,
  "idLoaiCauHoi": 1,
  "noiDung": "Câu hỏi test với security validation",
  "diem": 1.0,
  "doKho": "Trung bình",
  "khoaPhong": "CNTT",
  "danhSachLuaChon": [
    {
      "noiDung": "Đáp án A - Đúng",
      "laDapAnDung": true,
      "thuTu": 1
    },
    {
      "noiDung": "Đáp án B - Sai",
      "laDapAnDung": false,
      "thuTu": 2
    }
  ]
}

### Test Exam System with Anti-Cheat
POST {{BanTayVang.API_HostAddress}}/api/exam/start
Authorization: {{AuthToken}}
Content-Type: application/json

{
  "maDeThi": "TEST001",
  "environment": {
    "userAgent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
    "ipAddress": "192.168.1.100",
    "screenResolution": "1920x1080",
    "timeZone": "Asia/Ho_Chi_Minh",
    "isFullscreen": true,
    "windowCount": 1,
    "isVirtualMachine": false,
    "hasRemoteDesktop": false,
    "multipleMonitors": false
  }
}

### Test Anti-Cheat Logging
POST {{BanTayVang.API_HostAddress}}/api/exam/violation
Authorization: {{AuthToken}}
Content-Type: application/json

{
  "idBaiThi": 1,
  "violationType": "TabSwitch",
  "moTa": "User switched to another tab",
  "severity": "Medium",
  "userAgent": "Mozilla/5.0...",
  "ipAddress": "192.168.1.100",
  "evidence": {
    "timestamp": "2024-01-01T10:30:00Z",
    "windowTitle": "Google Chrome"
  }
}

### Test Grading System
POST {{BanTayVang.API_HostAddress}}/api/grading/grade/1
Authorization: {{AuthToken}}
Accept: application/json

### Get Exam Statistics
GET {{BanTayVang.API_HostAddress}}/api/grading/statistics/1
Authorization: {{AuthToken}}
Accept: application/json

### Health Check
GET {{BanTayVang.API_HostAddress}}/health
Accept: application/json
```

## 🎯 Tổng kết Enterprise Implementation

### ✅ Hoàn thành
1. **Security-First Architecture** - JWT, OWASP compliance, input validation
2. **SOLID Principles** - Interface segregation, dependency inversion
3. **Performance Optimization** - Caching, connection pooling, async operations
4. **Comprehensive Monitoring** - Health checks, logging, metrics
5. **Anti-Cheat System** - Real-time detection, behavioral analysis
6. **Advanced Grading** - AI-assisted, statistical analysis
7. **Enterprise Configuration** - Environment-specific settings
8. **API Documentation** - Swagger with security schemas

### 🚀 Production Ready Features
- JWT Authentication & Authorization
- Rate Limiting & CORS
- Health Checks & Monitoring
- Comprehensive Logging
- Error Handling & Validation
- Real-time SignalR Hubs
- Redis Caching
- Database Resilience

### 📋 Next Steps
- Deploy to production environment
- Configure CI/CD pipelines
- Set up monitoring dashboards
- Implement backup strategies
- Performance testing
- Security penetration testing