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
});


// Add Health Checks
builder.Services.AddHealthChecks();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.SetIsOriginAllowed(origin => true)
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// Add SignalR for real-time monitoring
builder.Services.AddSignalR();
builder.Services.AddSingleton<BanTayVang.API.Hubs.IExamMonitorNotifier, BanTayVang.API.Hubs.ExamMonitorNotifier>();

builder.Services.AddDbContext<BanTayVangDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Authentication Repositories
builder.Services.AddScoped<ITaikhoanRepository, TaikhoanRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IUserSessionRepository, UserSessionRepository>();

// Core Repositories
builder.Services.AddScoped<ICauhoiRepository, CauhoiRepository>();
builder.Services.AddScoped<ILuachonRepository, LuachonRepository>();
builder.Services.AddScoped<IDethiRepository, DethiRepository>();
builder.Services.AddScoped<IBaithiRepository, BaithiRepository>();
builder.Services.AddScoped<IChitietlambaiRepository, ChitietlambaiRepository>();
builder.Services.AddScoped<ICanhbaogianlanRepository, CanhbaogianlanRepository>();
builder.Services.AddScoped<ILoaicauhoiRepository, LoaicauhoiRepository>();

// JWT Authentication Services
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Question Service
builder.Services.AddScoped<BanTayVang.API.Services.Interfaces.ICauhoiService, BanTayVang.API.Services.Impl.CauhoiService>();

// Question Import Strategies
builder.Services.AddScoped<BanTayVang.API.Services.Interfaces.Import.IQuestionImportStrategy, BanTayVang.API.Services.Impl.Import.MultipleChoiceImportStrategy>();
builder.Services.AddScoped<BanTayVang.API.Services.Interfaces.Import.IQuestionImportStrategy, BanTayVang.API.Services.Impl.Import.EssayImportStrategy>();
builder.Services.AddScoped<BanTayVang.API.Services.Interfaces.Import.IQuestionImportStrategyFactory, BanTayVang.API.Services.Impl.Import.QuestionImportStrategyFactory>();

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

// Exam Assignment Service
builder.Services.AddScoped<BanTayVang.API.Services.Interfaces.IExamAssignmentService, BanTayVang.API.Services.Impl.ExamAssignmentService>();

// Ky Thi Service
builder.Services.AddScoped<BanTayVang.API.Services.Interfaces.IKyThiService, BanTayVang.API.Services.Impl.KyThiService>();

// File Upload Service
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<BanTayVang.API.Services.Interfaces.IFileUploadService, BanTayVang.API.Services.Impl.FileUploadService>();

// Audit Log Service
builder.Services.AddScoped<BanTayVang.API.Services.Interfaces.IAuditLogService, BanTayVang.API.Services.Impl.AuditLogService>();

// Background Jobs
builder.Services.AddHostedService<BanTayVang.API.BackgroundJobs.AutoSubmitExpiredExamsJob>();

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

        // 1. Ensure Email and SoDienThoai columns exist in the database
        await context.Database.ExecuteSqlRawAsync(@"
            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'TAIKHOAN' AND COLUMN_NAME = 'Email')
            BEGIN
                ALTER TABLE TAIKHOAN ADD Email nvarchar(255) NULL;
            END
        ");

        await context.Database.ExecuteSqlRawAsync(@"
            IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'TAIKHOAN' AND COLUMN_NAME = 'SoDienThoai')
            BEGIN
                ALTER TABLE TAIKHOAN ADD SoDienThoai nvarchar(50) NULL;
            END
        ");

        // 2. Ensure an Admin user exists in the system
        var adminExists = await context.Taikhoans.AnyAsync(u => u.IdVaiTro == 1);
        if (!adminExists)
        {
            var adminUser = new Taikhoan
            {
                TenDangNhap = "admin",
                MaNhanVien = "admin",
                MatKhau = passwordService.HashPassword("admin123"),
                HoTen = "Quản trị viên hệ thống",
                IdVaiTro = 1,
                TrangThai = true,
                NgayTao = DateTime.Now
            };
            context.Taikhoans.Add(adminUser);
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