# Workflow 1: Project Setup & Foundation

## 🎯 Mục tiêu
Thiết lập foundation cho BanTayVang API với Clean Architecture, SOLID principles, và enterprise-grade standards.

## 📋 Prerequisites
- .NET 8.0 SDK
- SQL Server 2022 (hoặc SQL Server Express)
- Visual Studio 2022 hoặc VS Code
- Git for version control

## 🏗️ Project Structure Setup

### 1. Verify Current Structure
```
BanTayVang.API/
├── Controllers/           # API Controllers (✅ exists)
├── DTOs/                 # Data Transfer Objects (✅ exists)
│   ├── Common/           # Base DTOs
│   ├── Question/         # Question-related DTOs
│   ├── Exam/            # Exam-related DTOs
│   └── AntiCheat/       # Anti-cheat DTOs
├── Services/            # Business Logic Layer (✅ exists)
│   ├── Interfaces/      # Service contracts
│   └── Impl/           # Service implementations
├── Repositories/        # Data Access Layer (✅ exists)
│   ├── Interfaces/      # Repository contracts
│   └── Impl/           # Repository implementations
├── Models/              # Entity Models (✅ exists)
├── Mappings/           # AutoMapper Profiles (✅ exists)
└── Middleware/         # Custom Middleware (to be created)
```

## 🔧 Enhanced Base Infrastructure

### 1. Enhanced Base Response DTO

```csharp
// DTOs/Common/BaseResponseDto.cs
using System.Text.Json.Serialization;

/// <summary>
/// Base response wrapper for all API responses
/// Provides consistent response format across the application
/// </summary>
/// <typeparam name="T">Type of data being returned</typeparam>
public class BaseResponseDto<T>
{
    /// <summary>
    /// Indicates if the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The actual data payload
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Human-readable message describing the result
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Collection of detailed error messages (if any)
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Timestamp when the response was generated
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Correlation ID for request tracking
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Creates a successful response with data
    /// </summary>
    public static BaseResponseDto<T> Success(T data, string message = "Operation completed successfully")
    {
        return new BaseResponseDto<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }

    /// <summary>
    /// Creates a failure response with error message
    /// </summary>
    public static BaseResponseDto<T> Failure(string message, List<string>? errors = null)
    {
        return new BaseResponseDto<T>
        {
            Success = false,
            Message = message,
            Errors = errors ?? new List<string>()
        };
    }

    /// <summary>
    /// Creates a failure response with single error
    /// </summary>
    public static BaseResponseDto<T> Failure(string message, string error)
    {
        return new BaseResponseDto<T>
        {
            Success = false,
            Message = message,
            Errors = new List<string> { error }
        };
    }
}
```

### 2. Enhanced Pagination Support

```csharp
// DTOs/Common/PaginationDto.cs
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Standard pagination parameters for list requests
/// </summary>
public class PaginationDto
{
    private int _page = 1;
    private int _pageSize = 20;

    /// <summary>
    /// Page number (1-based)
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")]
    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    /// <summary>
    /// Number of items per page
    /// </summary>
    [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            < 1 => 1,
            > 100 => 100,
            _ => value
        };
    }

    /// <summary>
    /// Optional search keyword
    /// </summary>
    [StringLength(255, ErrorMessage = "Search keyword cannot exceed 255 characters")]
    public string? Search { get; set; }

    /// <summary>
    /// Optional sort field
    /// </summary>
    [StringLength(50, ErrorMessage = "Sort field cannot exceed 50 characters")]
    public string? SortBy { get; set; }

    /// <summary>
    /// Sort direction (asc/desc)
    /// </summary>
    [RegularExpression("^(asc|desc)$", ErrorMessage = "Sort direction must be 'asc' or 'desc'")]
    public string SortDirection { get; set; } = "asc";

    /// <summary>
    /// Calculate skip count for database queries
    /// </summary>
    [JsonIgnore]
    public int Skip => (Page - 1) * PageSize;
}

// DTOs/Common/PagedResultDto.cs
/// <summary>
/// Paginated result wrapper
/// </summary>
/// <typeparam name="T">Type of items in the collection</typeparam>
public class PagedResultDto<T>
{
    /// <summary>
    /// Collection of items for current page
    /// </summary>
    public IEnumerable<T> Items { get; set; } = new List<T>();

    /// <summary>
    /// Total number of items across all pages
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// Indicates if there is a previous page
    /// </summary>
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Indicates if there is a next page
    /// </summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Creates a paginated result
    /// </summary>
    public static PagedResultDto<T> Create(IEnumerable<T> items, int totalCount, int page, int pageSize)
    {
        return new PagedResultDto<T>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
```

### 3. Enhanced Base Repository Interface

```csharp
// Repositories/Interfaces/IBaseRepository.cs
using System.Linq.Expressions;

/// <summary>
/// Generic repository interface for common CRUD operations
/// Follows Repository pattern with async operations
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public interface IBaseRepository<T> where T : class
{
    // Read operations
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<PagedResultDto<T>> GetPagedAsync(int page, int pageSize);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);

    // Write operations
    Task<T> CreateAsync(T entity);
    Task<IEnumerable<T>> CreateRangeAsync(IEnumerable<T> entities);
    Task<T> UpdateAsync(T entity);
    Task<IEnumerable<T>> UpdateRangeAsync(IEnumerable<T> entities);
    Task<bool> DeleteAsync(int id);
    Task<bool> DeleteAsync(T entity);
    Task<bool> DeleteRangeAsync(IEnumerable<T> entities);

    // Soft delete operations (if applicable)
    Task<bool> SoftDeleteAsync(int id);
    Task<bool> RestoreAsync(int id);

    // Transaction support
    Task<int> SaveChangesAsync();
    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation);
}
```

## 🔧 Enhanced Program.cs Configuration

```csharp
// Program.cs - Production-ready configuration
using BanTayVang.API.Models;
using BanTayVang.API.Repositories.Interfaces;
using BanTayVang.API.Repositories.Impl;
using BanTayVang.API.Services.Interfaces;
using BanTayVang.API.Services.Impl;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers(options =>
{
    // Global model validation
    options.Filters.Add<ValidationFilter>();
    
    // Global exception handling
    options.Filters.Add<GlobalExceptionFilter>();
    
    // Consistent API responses
    options.Filters.Add(new ProducesResponseTypeAttribute(typeof(BaseResponseDto<object>), 200));
    options.Filters.Add(new ProducesResponseTypeAttribute(typeof(BaseResponseDto<object>), 400));
    options.Filters.Add(new ProducesResponseTypeAttribute(typeof(BaseResponseDto<object>), 500));
})
.AddJsonOptions(options =>
{
    // Configure JSON serialization
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// API Explorer for Swagger
builder.Services.AddEndpointsApiExplorer();

// Enhanced Swagger configuration
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BanTayVang API",
        Version = "v1.0",
        Description = "Enterprise-grade exam management system for healthcare professionals",
        Contact = new OpenApiContact
        {
            Name = "Development Team",
            Email = "dev@bantayvang.hospital.vn"
        }
    });

    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Group by controller
    c.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] });
    c.DocInclusionPredicate((name, api) => true);
});

// CORS configuration - restrictive for production
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentPolicy", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });

    options.AddPolicy("ProductionPolicy", builder =>
    {
        builder.WithOrigins("https://bantayvang.hospital.vn", "https://app.bantayvang.vn")
               .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH")
               .WithHeaders("Content-Type", "Authorization", "X-Correlation-ID")
               .AllowCredentials();
    });
});

// AutoMapper configuration
builder.Services.AddAutoMapper(typeof(Program));

// Database configuration with optimizations
builder.Services.AddDbContext<BanTayVangDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.CommandTimeout(30);
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorNumbersToAdd: null);
        })
        .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
        .EnableDetailedErrors(builder.Environment.IsDevelopment());
});

// Repository registration
builder.Services.AddScoped<ICauhoiRepository, CauhoiRepository>();
builder.Services.AddScoped<ILuachonRepository, LuachonRepository>();
builder.Services.AddScoped<IDethiRepository, DethiRepository>();
builder.Services.AddScoped<IBaithiRepository, BaithiRepository>();
builder.Services.AddScoped<IChitietlambaiRepository, ChitietlambaiRepository>();
builder.Services.AddScoped<ICanhbaogianlanRepository, CanhbaogianlanRepository>();

// Service registration
builder.Services.AddScoped<ICauhoiService, CauhoiService>();
builder.Services.AddScoped<IExamService, ExamService>();

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<BanTayVangDbContext>("database");

// Build application
var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BanTayVang API v1");
        c.RoutePrefix = "swagger";
        c.DisplayRequestDuration();
        c.EnableDeepLinking();
    });
    
    app.UseCors("DevelopmentPolicy");
}
else
{
    app.UseHsts();
    app.UseCors("ProductionPolicy");
}

// Security headers middleware
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    
    await next();
});

app.UseHttpsRedirection();

// Health check endpoint
app.MapHealthChecks("/health");

app.UseAuthorization();

app.MapControllers();

app.Run();
```

## 🔧 Middleware Infrastructure

### 1. Global Exception Handler

```csharp
// Middleware/GlobalExceptionFilter.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;

/// <summary>
/// Global exception filter for consistent error handling
/// </summary>
public class GlobalExceptionFilter : IExceptionFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger;

    public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
    {
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        var correlationId = context.HttpContext.TraceIdentifier;
        
        _logger.LogError(context.Exception, 
            "Unhandled exception occurred. CorrelationId: {CorrelationId}", 
            correlationId);

        var response = BaseResponseDto<object>.Failure(
            "An error occurred while processing your request",
            new List<string> { "Please contact support if the problem persists" }
        );
        
        response.CorrelationId = correlationId;

        context.Result = new ObjectResult(response)
        {
            StatusCode = (int)HttpStatusCode.InternalServerError
        };

        context.ExceptionHandled = true;
    }
}
```

### 2. Validation Filter

```csharp
// Middleware/ValidationFilter.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// Global validation filter for model state validation
/// </summary>
public class ValidationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors)
                .Select(x => x.ErrorMessage)
                .ToList();

            var response = BaseResponseDto<object>.Failure(
                "Validation failed",
                errors
            );

            context.Result = new BadRequestObjectResult(response);
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        // No implementation needed
    }
}
```

## 📋 Foundation Checklist

### ✅ Project Structure
- [x] Controllers folder exists
- [x] DTOs folder with subfolders
- [x] Services with Interfaces/Impl structure
- [x] Repositories with Interfaces/Impl structure
- [x] Models folder with EF entities
- [x] Mappings folder for AutoMapper

### ✅ Base Infrastructure
- [x] Enhanced BaseResponseDto with correlation ID
- [x] Comprehensive PaginationDto
- [x] PagedResultDto for list responses
- [x] IBaseRepository with full CRUD operations
- [x] Global exception handling
- [x] Model validation filter

### ✅ Configuration
- [x] Enhanced Program.cs with production settings
- [x] Swagger documentation setup
- [x] CORS policies for dev/prod
- [x] Database context with optimizations
- [x] Health checks configuration
- [x] Security headers middleware

### ✅ Quality Standards
- [x] XML documentation enabled
- [x] Consistent error handling
- [x] Proper async/await patterns
- [x] SOLID principles foundation
- [x] Enterprise-grade configuration

## 🎯 Next Steps

1. **Verify Database Connection** - Test connection string
2. **Run Initial Migration** - Ensure database schema is current
3. **Test Health Endpoint** - Verify `/health` returns 200 OK
4. **Test Swagger UI** - Navigate to `/swagger` and verify documentation
5. **Proceed to Workflow 02** - Question Bank Management implementation

---

**Status:** Foundation Enhanced - Ready for Core Feature Implementation
**Next:** Workflow 02 - Question Bank Management with enhanced DTOs and validation