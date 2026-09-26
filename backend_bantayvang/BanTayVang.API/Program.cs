using BanTayVang.API.Models;
using BanTayVang.API.Repositories.Interfaces;
using BanTayVang.API.Repositories.Impl;
using BanTayVang.API.Services.Interfaces.Auth;
using BanTayVang.API.Services.Impl.Auth;
using BanTayVang.API.Configuration;
using BanTayVang.API.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "BanTayVang.API",
        Version = "v1",
        Description = "API for BanTayVang Exam Management System"
    });

    // Add JWT Bearer authentication to Swagger
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Chỉ cần paste token vào đây, KHÔNG cần thêm 'Bearer'. Ví dụ: eyJhbGciOiJI..."
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// JWT Configuration
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));

// Email Configuration
builder.Services.Configure<BanTayVang.API.Configuration.EmailSettings>(
    builder.Configuration.GetSection(BanTayVang.API.Configuration.EmailSettings.SectionName));

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>();
if (jwtSettings == null || string.IsNullOrEmpty(jwtSettings.SecretKey))
{
    throw new InvalidOperationException("JWT settings are not configured properly");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = jwtSettings.RequireHttps;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = jwtSettings.ValidateIssuer,
        ValidateAudience = jwtSettings.ValidateAudience,
        ValidateLifetime = jwtSettings.ValidateLifetime,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        ClockSkew = TimeSpan.FromMinutes(jwtSettings.ClockSkewMinutes)
    };

    // Custom JWT events for better error handling
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
            {
                context.Response.Headers.Append("Token-Expired", "true");
            }
            return Task.CompletedTask;
        },
        // BUG FIX: special-purpose tokens (currently only the 15-minute password-reset token,
        // marked with a "purpose" claim - see JwtService.GeneratePasswordResetToken) were only
        // rejected by the custom JwtAuthenticationMiddleware, which just skips populating
        // HttpContext.Items["UserId"] and lets the request continue down the pipeline. This
        // standard AddJwtBearer handler runs independently right after that middleware and does
        // its OWN signature/issuer/audience/lifetime validation with no knowledge of "purpose" -
        // so it still authenticates the same token and sets HttpContext.User, meaning any action
        // gated by a bare [Authorize] (no role/policy) that reads the caller via
        // User.FindFirst("user_id")/ClaimTypes.NameIdentifier directly - rather than the app's own
        // HttpContext.Items["UserId"] convention - would accept a leaked/stolen password-reset
        // token as a full bearer credential. Checked every controller: none currently do this
        // unsafely (the only direct-claims reads are gated by role policies a reset token can't
        // satisfy, since it carries no "role" claim) - but that's a fragile convention to rely on,
        // not a structural guarantee. Reject purpose-claimed tokens at the source instead, so
        // HttpContext.User.Identity.IsAuthenticated is false for them regardless of which
        // extraction pattern any future endpoint happens to use.
        OnTokenValidated = context =>
        {
            if (context.Principal?.FindFirst("purpose")?.Value is string purpose && !string.IsNullOrEmpty(purpose))
            {
                context.Fail("Special-purpose token cannot be used for general API authentication.");
            }
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            var result = System.Text.Json.JsonSerializer.Serialize(new
            {
                success = false,
                message = "Token không hợp lệ hoặc đã hết hạn",
                statusCode = 401
            });
            return context.Response.WriteAsync(result);
        },
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// Add Authorization with role-based policies
builder.Services.AddAuthorization(options =>
{
    // AdminOnly: full system administrators
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));

    // ManagementOnly: Admin + Department Managers
    options.AddPolicy("ManagementOnly", policy =>
        policy.RequireRole("Admin", "DeptManager"));
});

// OWASP A04: Rate limiting (built-in .NET 7+)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(
            "{\"success\":false,\"message\":\"Quá nhiều request. Vui lòng thử lại sau.\"}", token);
    };

    var isDev = builder.Environment.IsDevelopment();

    // Global limit: 100 requests per minute per IP (1000 in Dev)
    options.AddPolicy("global", httpContext =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = isDev ? 1000 : 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // Strict for auth endpoints: 10 requests per minute (1000 in Dev for testing)
    options.AddPolicy("auth", httpContext =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = isDev ? 1000 : 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // BUG FIX: the "global" policy above was defined but never actually attached anywhere -
    // only AuthController opts into rate limiting via [EnableRateLimiting("auth")], so every
    // other controller (Question import, Grading, User, Exam generation, ...) had zero request
    // throttling despite UseRateLimiter() being wired into the pipeline. GlobalLimiter applies to
    // every request by default (endpoints can still stack a stricter named policy on top, as
    // AuthController already does), giving the rest of the API the same baseline protection.
    options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = isDev ? 1000 : 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});


// Add Health Checks
builder.Services.AddHealthChecks();

// Add CORS
// BUG FIX (OWASP A05): SetIsOriginAllowed(origin => true) reflects back whatever Origin header
// the caller sends as Access-Control-Allow-Origin, combined with AllowCredentials() - so ANY
// website could make credentialed cross-origin calls to this API from a victim's browser. This
// app currently authenticates via a Bearer token the frontend attaches manually (not a cookie a
// browser would auto-send), which limits today's blast radius, but the settings still fail an
// OWASP scan and would become a full CSRF hole the moment anything cookie-based is added (e.g.
// the httpOnly-cookie token migration already being discussed). Restrict to configured origins.
var corsAllowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:3000", "https://localhost:3000" };
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policyBuilder =>
    {
        policyBuilder.WithOrigins(corsAllowedOrigins)
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});

// Add AutoMapper
builder.Services.AddAutoMapper(cfg => {
    cfg.AddMaps(typeof(Program).Assembly);
});

// Add SignalR for real-time monitoring
builder.Services.AddSignalR();
builder.Services.AddSingleton<BanTayVang.API.Hubs.IExamMonitorNotifier, BanTayVang.API.Hubs.ExamMonitorNotifier>();

builder.Services.AddDbContext<BanTayVangDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Authentication Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IUserSessionRepository, UserSessionRepository>();

// Core Repositories
builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
builder.Services.AddScoped<IQuestionOptionRepository, QuestionOptionRepository>();
builder.Services.AddScoped<IExamPaperRepository, ExamPaperRepository>();
builder.Services.AddScoped<IExamSubmissionRepository, ExamSubmissionRepository>();
builder.Services.AddScoped<ISubmissionDetailRepository, SubmissionDetailRepository>();
builder.Services.AddScoped<ICheatWarningRepository, CheatWarningRepository>();
builder.Services.AddScoped<IQuestionCategoryRepository, QuestionCategoryRepository>();

// JWT Authentication Services
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<BanTayVang.API.Services.Interfaces.IExamRegistrationService, BanTayVang.API.Services.Impl.ExamRegistrationService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Question Service
builder.Services.AddScoped<BanTayVang.API.Services.Interfaces.IQuestionService, BanTayVang.API.Services.Impl.QuestionService>();

// Question Import Strategies
builder.Services.AddScoped<BanTayVang.API.Services.Interfaces.Import.IQuestionImportStrategy, BanTayVang.API.Services.Impl.Import.MultipleChoiceImportStrategy>();
builder.Services.AddScoped<BanTayVang.API.Services.Interfaces.Import.IQuestionImportStrategy, BanTayVang.API.Services.Impl.Import.EssayImportStrategy>();
builder.Services.AddScoped<BanTayVang.API.Services.Interfaces.Import.IQuestionImportStrategyFactory, BanTayVang.API.Services.Impl.Import.QuestionImportStrategyFactory>();
// Word Import Service (Chuẩn Azota – nhận diện đáp án qua Bold/Underline)
builder.Services.AddScoped<BanTayVang.API.Services.Interfaces.Import.IWordQuestionImportService, BanTayVang.API.Services.Impl.Import.WordQuestionImportService>();

// Category Service
builder.Services.AddScoped<BanTayVang.API.Services.Interfaces.ICategoryService, BanTayVang.API.Services.Impl.CategoryService>();

// User Management Service
builder.Services.AddScoped<BanTayVang.API.Services.Interfaces.IUserManagementService, BanTayVang.API.Services.Impl.UserManagementService>();

// Statistics Service
builder.Services.AddScoped<BanTayVang.API.Services.Interfaces.IStatisticsService, BanTayVang.API.Services.Impl.StatisticsService>();

// Notification Service
builder.Services.AddScoped<BanTayVang.API.Services.Interfaces.INotificationService, BanTayVang.API.Services.Impl.NotificationService>();

// Grading Service
builder.Services.AddScoped<BanTayVang.API.Services.Interfaces.IGradingService, BanTayVang.API.Services.Impl.GradingService>();

// Email Service
builder.Services.AddScoped<BanTayVang.API.Services.Interfaces.IEmailService, BanTayVang.API.Services.Impl.EmailService>();

// Email Verification Service (OTP cho đăng ký thí sinh ngoại và quên mật khẩu)
builder.Services.AddScoped<BanTayVang.API.Services.Interfaces.IEmailVerificationService, BanTayVang.API.Services.Impl.EmailVerificationService>();

// Exam Assignment Service
builder.Services.AddScoped<BanTayVang.API.Services.Interfaces.IExamAssignmentService, BanTayVang.API.Services.Impl.ExamAssignmentService>();

// Ky Thi Service
builder.Services.AddScoped<BanTayVang.API.Services.Interfaces.IExamCampaignService, BanTayVang.API.Services.Impl.ExamCampaignService>();

// File Upload Service
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<BanTayVang.API.Services.Interfaces.IFileUploadService, BanTayVang.API.Services.Impl.FileUploadService>();

// Audit Log Service
builder.Services.AddScoped<BanTayVang.API.Services.Interfaces.IAuditLogService, BanTayVang.API.Services.Impl.AuditLogService>();

// Background Jobs
builder.Services.AddHostedService<BanTayVang.API.BackgroundJobs.AutoSubmitExpiredExamsJob>();

// AI Grading
builder.Services.Configure<BanTayVang.API.Configuration.AiGradingSettings>(
    builder.Configuration.GetSection(BanTayVang.API.Configuration.AiGradingSettings.SectionName));
builder.Services.AddHttpClient<BanTayVang.API.Services.Interfaces.IAIGradingService, BanTayVang.API.Services.Impl.GeminiAIGradingService>();
builder.Services.AddSingleton<BanTayVang.API.Services.Impl.AiGradingQueue>();
builder.Services.AddHostedService<BanTayVang.API.BackgroundJobs.AiGradingWorker>();

// SOLID-compliant Exam Services (Interface Segregation Principle)
builder.Services.AddScoped<BanTayVang.API.Services.Interfaces.Validation.IExamValidationService, BanTayVang.API.Services.Impl.Validation.ExamValidationService>();
builder.Services.AddScoped<BanTayVang.API.Services.Interfaces.Security.IExamSecurityService, BanTayVang.API.Services.Impl.Security.ExamSecurityService>();
builder.Services.AddScoped<BanTayVang.API.Services.Interfaces.Exams.IExamManagementService, BanTayVang.API.Services.Impl.Exams.ExamManagementService>();
builder.Services.AddScoped<BanTayVang.API.Services.Interfaces.Exams.IExamSessionService, BanTayVang.API.Services.Impl.Exams.ExamSessionService>();
builder.Services.AddScoped<BanTayVang.API.Services.Interfaces.Exams.IExamSubmissionService, BanTayVang.API.Services.Impl.Exams.ExamSubmissionService>();

// Main Exam Service (Facade pattern - delegates to segregated services)
builder.Services.AddScoped<BanTayVang.API.Services.Interfaces.IExamService, BanTayVang.API.Services.Impl.ExamService>();

var app = builder.Build();

// Seed Admin user and database schema updates
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<BanTayVangDbContext>();
        var passwordService = services.GetRequiredService<IPasswordService>();

        // 1. Ensure Email and PhoneNumber columns exist in the database
        await context.Database.ExecuteSqlRawAsync(@"
            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'Email')
            BEGIN
                ALTER TABLE [Users] ADD Email nvarchar(255) NULL;
            END
        ");

        await context.Database.ExecuteSqlRawAsync(@"
            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'PhoneNumber')
            BEGIN
                ALTER TABLE [Users] ADD PhoneNumber nvarchar(50) NULL;
            END
        ");

        // AI Grading columns for SubmissionDetails
        await context.Database.ExecuteSqlRawAsync(@"
            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'SubmissionDetails' AND COLUMN_NAME = 'AiScore')
            BEGIN
                ALTER TABLE [SubmissionDetails] ADD AiScore float NULL;
            END
        ");
        await context.Database.ExecuteSqlRawAsync(@"
            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'SubmissionDetails' AND COLUMN_NAME = 'AiComment')
            BEGIN
                ALTER TABLE [SubmissionDetails] ADD AiComment nvarchar(MAX) NULL;
            END
        ");
        await context.Database.ExecuteSqlRawAsync(@"
            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'SubmissionDetails' AND COLUMN_NAME = 'AiGradingStatus')
            BEGIN
                ALTER TABLE [SubmissionDetails] ADD AiGradingStatus nvarchar(20) NULL;
            END
        ");


        // BUG FIX: these 3 columns were added by later EF migrations but this manual bootstrap
        // (which is what actually runs, since Database.Migrate() is never called - see comments
        // above) never got the matching patches added. On a fresh/un-migrated DB, code that reads
        // or writes these columns (essay teacher comments, suggested answers, essay image URLs)
        // would throw "Invalid column name" at runtime.
        await context.Database.ExecuteSqlRawAsync(@"
            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'SubmissionDetails' AND COLUMN_NAME = 'TeacherComment')
            BEGIN
                ALTER TABLE [SubmissionDetails] ADD TeacherComment nvarchar(MAX) NULL;
            END
        ");
        await context.Database.ExecuteSqlRawAsync(@"
            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'SubmissionDetails' AND COLUMN_NAME = 'EssayImageUrl')
            BEGIN
                ALTER TABLE [SubmissionDetails] ADD EssayImageUrl nvarchar(MAX) NULL;
            END
        ");
        await context.Database.ExecuteSqlRawAsync(@"
            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Questions' AND COLUMN_NAME = 'SuggestedAnswer')
            BEGIN
                ALTER TABLE [Questions] ADD SuggestedAnswer nvarchar(MAX) NULL;
            END
        ");

        await context.Database.ExecuteSqlRawAsync(@"
            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'EmailVerificationCodes')
            BEGIN
                CREATE TABLE [EmailVerificationCodes] (
                    [Id] int NOT NULL IDENTITY,
                    [Email] nvarchar(255) NOT NULL,
                    [CodeHash] nvarchar(255) NOT NULL,
                    [Purpose] nvarchar(50) NOT NULL,
                    [CreatedAt] datetime2 NOT NULL DEFAULT (getdate()),
                    [ExpiresAt] datetime2 NOT NULL,
                    [IsVerified] bit NOT NULL DEFAULT CAST(0 AS bit),
                    [VerifiedAt] datetime2 NULL,
                    [IsUsed] bit NOT NULL DEFAULT CAST(0 AS bit),
                    [FailedAttempts] int NOT NULL DEFAULT 0,
                    [IpAddress] nvarchar(50) NULL,
                    CONSTRAINT [PK_EmailVerificationCodes] PRIMARY KEY ([Id])
                );
            END
        ");

        // BUG FIX (Đợt 3): account-lockout tracking columns for repeated failed logins,
        // and defensive unique indexes to close TOCTOU duplicate-registration/username races.
        // These are added the same idempotent bootstrap-SQL way as the other schema patches
        // above, since Database.Migrate() is never called in this project.
        await context.Database.ExecuteSqlRawAsync(@"
            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'FailedLoginAttempts')
            BEGIN
                ALTER TABLE [Users] ADD FailedLoginAttempts int NOT NULL DEFAULT 0;
            END
        ");
        await context.Database.ExecuteSqlRawAsync(@"
            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'LockoutEnd')
            BEGIN
                ALTER TABLE [Users] ADD LockoutEnd datetime2 NULL;
            END
        ");

        // Unique index on Users.Username (active users only - soft-deleted rows are already
        // invisible to the app's own duplicate check via the global IsDeleted query filter, so
        // mirroring that scope here avoids blocking legitimate username reuse after a soft-delete).
        // Created defensively: if duplicate active usernames already exist in this DB, creating
        // the index would fail and crash startup, so we skip it and just log a warning instead -
        // an admin needs to manually resolve the duplicates first.
        try
        {
            var dupUsernames = await context.Database.SqlQuery<int>($@"
                SELECT COUNT(*) AS Value FROM (
                    SELECT Username FROM [Users] WHERE Username IS NOT NULL AND IsDeleted = 0
                    GROUP BY Username HAVING COUNT(*) > 1
                ) dup
            ").FirstOrDefaultAsync();
            if (dupUsernames == 0)
            {
                await context.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Users_Username' AND object_id = OBJECT_ID('Users'))
                    BEGIN
                        CREATE UNIQUE INDEX UX_Users_Username ON [Users](Username) WHERE Username IS NOT NULL AND IsDeleted = 0;
                    END
                ");
            }
            else
            {
                var startupLogger = services.GetRequiredService<ILogger<Program>>();
                startupLogger.LogWarning("Skipped creating UX_Users_Username: {Count} duplicate active username(s) found - resolve manually then restart.", dupUsernames);
            }
        }
        catch (Exception idxEx)
        {
            var startupLogger = services.GetRequiredService<ILogger<Program>>();
            startupLogger.LogWarning(idxEx, "Could not verify/create UX_Users_Username (non-fatal, continuing startup).");
        }

        // Filtered unique index: at most one Pending ExamRegistration per IdCardNumber at a time.
        // This matches the app's own duplicate check in ExamRegistrationService (which only blocks
        // a new registration when an existing Pending row has the same CCCD, allowing re-registration
        // after a prior request was Approved/Rejected) - so a plain table-wide unique index would be
        // wrong here and would break that legitimate resubmission flow. Also created defensively.
        try
        {
            var dupPendingRegs = await context.Database.SqlQuery<int>($@"
                SELECT COUNT(*) AS Value FROM (
                    SELECT IdCardNumber FROM [ExamRegistrations] WHERE Status = 'Pending'
                    GROUP BY IdCardNumber HAVING COUNT(*) > 1
                ) dup
            ").FirstOrDefaultAsync();
            if (dupPendingRegs == 0)
            {
                await context.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_ExamRegistrations_IdCardNumber_Pending' AND object_id = OBJECT_ID('ExamRegistrations'))
                    BEGIN
                        CREATE UNIQUE INDEX UX_ExamRegistrations_IdCardNumber_Pending ON [ExamRegistrations](IdCardNumber) WHERE Status = 'Pending';
                    END
                ");
            }
            else
            {
                var startupLogger = services.GetRequiredService<ILogger<Program>>();
                startupLogger.LogWarning("Skipped creating UX_ExamRegistrations_IdCardNumber_Pending: {Count} duplicate pending registration(s) found - resolve manually then restart.", dupPendingRegs);
            }
        }
        catch (Exception idxEx)
        {
            var startupLogger = services.GetRequiredService<ILogger<Program>>();
            startupLogger.LogWarning(idxEx, "Could not verify/create UX_ExamRegistrations_IdCardNumber_Pending (non-fatal, continuing startup).");
        }

        // Filtered unique index: at most one InProgress ExamSubmission per (UserId, ExamPaperId).
        // BUG FIX: StartExamAsync (ExamSessionService) does a plain "check active session, else
        // insert" with no transaction/lock - a double-click or 2-tab retry on "start exam" can
        // read "no active session" twice and insert 2 InProgress rows for the same user+exam,
        // each with its own randomly-selected question set, both independently submittable.
        // This index makes the race fail fast at the DB instead of silently duplicating data;
        // StartExamAsync catches the resulting unique-violation and resumes the winning row.
        try
        {
            var dupActiveSessions = await context.Database.SqlQuery<int>($@"
                SELECT COUNT(*) AS Value FROM (
                    SELECT UserId, ExamPaperId FROM [ExamSubmissions] WHERE Status = 'InProgress'
                    GROUP BY UserId, ExamPaperId HAVING COUNT(*) > 1
                ) dup
            ").FirstOrDefaultAsync();
            if (dupActiveSessions == 0)
            {
                await context.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_ExamSubmissions_User_Exam_InProgress' AND object_id = OBJECT_ID('ExamSubmissions'))
                    BEGIN
                        CREATE UNIQUE INDEX UX_ExamSubmissions_User_Exam_InProgress ON [ExamSubmissions](UserId, ExamPaperId) WHERE Status = 'InProgress';
                    END
                ");
            }
            else
            {
                var startupLogger = services.GetRequiredService<ILogger<Program>>();
                startupLogger.LogWarning("Skipped creating UX_ExamSubmissions_User_Exam_InProgress: {Count} duplicate active session(s) found - resolve manually then restart.", dupActiveSessions);
            }
        }
        catch (Exception idxEx)
        {
            var startupLogger = services.GetRequiredService<ILogger<Program>>();
            startupLogger.LogWarning(idxEx, "Could not verify/create UX_ExamSubmissions_User_Exam_InProgress (non-fatal, continuing startup).");
        }

        // Unique index: at most one ExamAssignment row per (UserId, ExamId) - the row is meant to be
        // reused across assign/unassign/extend-time cycles (IsActive toggles it, never deleted), so a
        // plain per-pair uniqueness constraint is correct here (see BUG FIX comments in
        // ExamAssignmentService.AssignUsersToExamAsync/ExtendExamTimeAsync for the races this closes).
        try
        {
            var dupAssignments = await context.Database.SqlQuery<int>($@"
                SELECT COUNT(*) AS Value FROM (
                    SELECT UserId, ExamId FROM [ExamAssignments]
                    GROUP BY UserId, ExamId HAVING COUNT(*) > 1
                ) dup
            ").FirstOrDefaultAsync();
            if (dupAssignments == 0)
            {
                await context.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_ExamAssignments_User_Exam' AND object_id = OBJECT_ID('ExamAssignments'))
                    BEGIN
                        CREATE UNIQUE INDEX UX_ExamAssignments_User_Exam ON [ExamAssignments](UserId, ExamId);
                    END
                ");
            }
            else
            {
                var startupLogger = services.GetRequiredService<ILogger<Program>>();
                startupLogger.LogWarning("Skipped creating UX_ExamAssignments_User_Exam: {Count} duplicate (UserId, ExamId) pair(s) found - resolve manually then restart.", dupAssignments);
            }
        }
        catch (Exception idxEx)
        {
            var startupLogger = services.GetRequiredService<ILogger<Program>>();
            startupLogger.LogWarning(idxEx, "Could not verify/create UX_ExamAssignments_User_Exam (non-fatal, continuing startup).");
        }

        // 2. Ensure an Admin user exists in the system
        var adminExists = await context.Users.AnyAsync(u => u.RoleId == 1);
        if (!adminExists)
        {
            var adminUser = new User
            {
                Username = "admin",
                EmployeeCode = "admin",
                Password = passwordService.HashPassword("admin123"),
                FullName = "Quản trị viên hệ thống",
                RoleId = 1,
                Status = true,
                CreatedAt = DateTime.UtcNow.AddHours(7)
            };
            context.Users.Add(adminUser);
            await context.SaveChangesAsync();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding or updating database schema.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

app.UseHttpsRedirection();

// Serve static files (for uploaded images)
app.UseStaticFiles();

// Use Global Exception Middleware FIRST
app.UseMiddleware<GlobalExceptionMiddleware>();

// Use JWT Authentication Middleware
app.UseMiddleware<JwtAuthenticationMiddleware>();

// Use Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// OWASP A04: Rate Limiting
app.UseRateLimiter();

// Audit Log middleware (after auth so we have UserId)
app.UseMiddleware<AuditLogMiddleware>();

// Health check endpoint
app.MapHealthChecks("/health");

// SignalR Hub
app.MapHub<BanTayVang.API.Hubs.ExamMonitorHub>("/hubs/exam-monitor");
app.MapHub<BanTayVang.API.Hubs.NotificationHub>("/hubs/notifications");

app.MapControllers();

app.Run();
