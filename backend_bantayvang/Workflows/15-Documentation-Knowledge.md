# Workflow 15: Documentation & Knowledge Management

## 📚 Tổng quan Documentation

Workflow này thiết lập hệ thống documentation toàn diện và knowledge management cho dự án BanTayVang.

## 🎯 Documentation Goals

### Target Audiences
- **Developers:** Technical documentation, API docs, architecture
- **DevOps:** Deployment guides, infrastructure docs
- **QA:** Testing procedures, test cases
- **Business Users:** User manuals, feature specifications
- **Stakeholders:** Project overview, progress reports

### Documentation Types
- **Technical Documentation:** Code docs, API specs, architecture
- **User Documentation:** User guides, tutorials, FAQs
- **Process Documentation:** Workflows, procedures, standards
- **Knowledge Base:** Troubleshooting, best practices

## 📖 Technical Documentation

### API Documentation with Swagger

```csharp
// Program.cs - Enhanced Swagger configuration
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BanTayVang API",
        Version = "v1.0",
        Description = "Hệ thống thi trắc nghiệm trực tuyến cho điều dưỡng viên",
        Contact = new OpenApiContact
        {
            Name = "BanTayVang Development Team",
            Email = "dev@bantayvang.hospital.vn",
            Url = new Uri("https://bantayvang.hospital.vn")
        },
        License = new OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });
    
    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
    
    // Add JWT authentication
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
    
    // Group by tags
    c.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] });
    c.DocInclusionPredicate((name, api) => true);
});

// Enable XML documentation generation
builder.Services.Configure<MvcOptions>(options =>
{
    options.Filters.Add(new ProducesResponseTypeAttribute(typeof(BaseResponseDto<object>), 200));
    options.Filters.Add(new ProducesResponseTypeAttribute(typeof(BaseResponseDto<object>), 400));
    options.Filters.Add(new ProducesResponseTypeAttribute(typeof(BaseResponseDto<object>), 500));
});
```

### Enhanced Controller Documentation

```csharp
// Controllers/ExamController.cs - Well-documented controller
/// <summary>
/// Controller for managing exam operations
/// </summary>
/// <remarks>
/// This controller handles all exam-related operations including:
/// - Creating and managing exams
/// - Starting exam sessions
/// - Submitting answers and completing exams
/// - Anti-cheat monitoring
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Exam Management")]
public class ExamController : ControllerBase
{
    private readonly IExamService _examService;
    private readonly ILogger<ExamController> _logger;
    
    /// <summary>
    /// Initializes a new instance of the ExamController
    /// </summary>
    /// <param name="examService">Service for exam operations</param>
    /// <param name="logger">Logger instance</param>
    public ExamController(IExamService examService, ILogger<ExamController> logger)
    {
        _examService = examService;
        _logger = logger;
    }
    
    /// <summary>
    /// Creates a new exam
    /// </summary>
    /// <param name="createDto">Exam creation data</param>
    /// <returns>Created exam information</returns>
    /// <response code="201">Exam created successfully</response>
    /// <response code="400">Invalid input data</response>
    /// <response code="401">Unauthorized access</response>
    /// <response code="500">Internal server error</response>
    /// <example>
    /// POST /api/exam
    /// {
    ///   "maDeThi": "EXAM001",
    ///   "tenDeThi": "Kiểm tra kiến thức cơ bản",
    ///   "thoiGianLamBai": 60,
    ///   "nguoiTao": 1
    /// }
    /// </example>
    [HttpPost]
    [ProducesResponseType(typeof(BaseResponseDto<DethiDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(BaseResponseDto<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(BaseResponseDto<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateExam([FromBody] CreateDethiDto createDto)
    {
        try
        {
            _logger.LogInformation("Creating exam with code: {ExamCode}", createDto.MaDeThi);
            
            var result = await _examService.CreateExamAsync(createDto);
            
            if (result.Success)
            {
                _logger.LogInformation("Exam created successfully with ID: {ExamId}", result.Data?.Id);
                return CreatedAtAction(nameof(GetExam), new { id = result.Data?.Id }, result);
            }
            
            _logger.LogWarning("Failed to create exam: {Message}", result.Message);
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating exam");
            return StatusCode(500, BaseResponseDto<object>.Failure("Internal server error"));
        }
    }
    
    /// <summary>
    /// Starts an exam session for a student
    /// </summary>
    /// <param name="startDto">Exam start data including exam code and student ID</param>
    /// <returns>Exam session information</returns>
    /// <remarks>
    /// This endpoint:
    /// 1. Validates the exam code and student eligibility
    /// 2. Creates a new exam session
    /// 3. Returns exam session details for the student to begin
    /// 
    /// **Important:** Once started, the exam timer begins and cannot be paused.
    /// </remarks>
    /// <response code="200">Exam session started successfully</response>
    /// <response code="400">Invalid exam code or student not eligible</response>
    /// <response code="404">Exam not found</response>
    /// <response code="409">Student already has an active session for this exam</response>
    [HttpPost("start")]
    [ProducesResponseType(typeof(BaseResponseDto<BaithiDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BaseResponseDto<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BaseResponseDto<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(BaseResponseDto<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> StartExam([FromBody] StartExamDto startDto)
    {
        var result = await _examService.StartExamAsync(startDto);
        
        return result.Success switch
        {
            true => Ok(result),
            false when result.Message.Contains("không tìm thấy") => NotFound(result),
            false when result.Message.Contains("đã có phiên thi") => Conflict(result),
            _ => BadRequest(result)
        };
    }
}
```

### Architecture Documentation

```markdown
<!-- docs/architecture/README.md -->
# BanTayVang API Architecture

## Overview

BanTayVang API is built using Clean Architecture principles with ASP.NET Core 8.0, following SOLID principles and implementing comprehensive security measures.

## Architecture Layers

### 1. Presentation Layer (Controllers)
- **Responsibility:** HTTP request/response handling, input validation, authentication
- **Components:** Controllers, Middleware, Filters
- **Dependencies:** Service Layer only

### 2. Service Layer (Business Logic)
- **Responsibility:** Business rules, workflow orchestration, data transformation
- **Components:** Services, DTOs, Validators, Mappers
- **Dependencies:** Repository Layer, External Services

### 3. Repository Layer (Data Access)
- **Responsibility:** Data persistence, query optimization, transaction management
- **Components:** Repositories, Entity Framework DbContext
- **Dependencies:** Database, Models

### 4. Cross-Cutting Concerns
- **Logging:** Serilog with structured logging
- **Caching:** Redis distributed cache with memory cache fallback
- **Security:** JWT authentication, OWASP compliance
- **Monitoring:** Prometheus metrics, health checks

## Design Patterns Used

### Repository Pattern
```csharp
public interface IExamRepository : IBaseRepository<Exam>
{
    Task<Exam?> GetByCodeAsync(string examCode);
    Task<IEnumerable<Exam>> GetActiveExamsAsync();
}
```

### Service Pattern
```csharp
public interface IExamService
{
    Task<BaseResponseDto<ExamDto>> CreateExamAsync(CreateExamDto createDto);
    Task<BaseResponseDto<ExamSessionDto>> StartExamAsync(StartExamDto startDto);
}
```

### Strategy Pattern (Grading)
```csharp
public interface IGradingStrategy
{
    bool CanHandle(QuestionType questionType);
    float CalculateScore(Answer answer, Question question);
}
```

## Data Flow

```
HTTP Request → Controller → Service → Repository → Database
                    ↓
Response ← DTO ← Mapper ← Entity ← Query Result
```

## Security Architecture

### Authentication Flow
1. User submits credentials
2. JWT token generated with claims
3. Token included in subsequent requests
4. Middleware validates token and sets user context

### Authorization Levels
- **Admin:** Full system access
- **Teacher:** Exam management, grading
- **Student:** Take assigned exams only

## Performance Considerations

### Caching Strategy
- **L1 Cache:** In-memory for frequently accessed data
- **L2 Cache:** Redis for distributed caching
- **Cache Keys:** Hierarchical naming for easy invalidation

### Database Optimization
- **Indexes:** Strategic indexing on query columns
- **Pagination:** All list endpoints support pagination
- **Async Operations:** All I/O operations are asynchronous

## Deployment Architecture

```
Internet → Load Balancer → API Instances → Database
                      ↓
                   Redis Cache
```

## Monitoring & Observability

### Metrics Collected
- Request/response times
- Error rates
- Active user sessions
- Database query performance
- Cache hit/miss rates

### Logging Strategy
- **Structured Logging:** JSON format with correlation IDs
- **Log Levels:** Debug, Info, Warning, Error, Critical
- **Log Aggregation:** Centralized logging with ELK stack

## Future Enhancements

### Planned Features
- Real-time exam monitoring with SignalR
- Advanced analytics and reporting
- Mobile app support
- Integration with hospital HR systems

### Scalability Roadmap
- Microservices decomposition
- Event-driven architecture
- Container orchestration with Kubernetes
```

## 📋 User Documentation

### API Usage Guide

```markdown
<!-- docs/api/getting-started.md -->
# BanTayVang API - Getting Started Guide

## Authentication

### 1. Obtain Access Token

```http
POST /api/auth/login
Content-Type: application/json

{
  "username": "your-username",
  "password": "your-password"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "refresh-token-here",
    "expiresAt": "2024-01-01T12:00:00Z",
    "user": {
      "id": 1,
      "username": "teacher01",
      "role": "Teacher"
    }
  }
}
```

### 2. Use Token in Requests

Include the token in the Authorization header:

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

## Common Workflows

### Creating and Managing Exams

#### 1. Create an Exam

```http
POST /api/exam
Authorization: Bearer {token}
Content-Type: application/json

{
  "maDeThi": "EXAM001",
  "tenDeThi": "Basic Nursing Knowledge Test",
  "thoiGianLamBai": 60,
  "nguoiTao": 1
}
```

#### 2. Add Questions to Exam

```http
POST /api/exam/{examId}/questions
Authorization: Bearer {token}
Content-Type: application/json

{
  "questionId": 1,
  "weight": 1.0
}
```

#### 3. Activate Exam

```http
PUT /api/exam/{examId}/status
Authorization: Bearer {token}
Content-Type: application/json

{
  "status": "Active"
}
```

### Taking an Exam (Student Flow)

#### 1. Start Exam Session

```http
POST /api/exam/start
Authorization: Bearer {token}
Content-Type: application/json

{
  "maDeThi": "EXAM001",
  "idTaiKhoan": 123
}
```

#### 2. Get Exam Questions

```http
GET /api/exam/{examId}/questions
Authorization: Bearer {token}
```

#### 3. Submit Answer

```http
POST /api/exam/answer
Authorization: Bearer {token}
Content-Type: application/json

{
  "idBaiThi": 456,
  "idCauHoi": 1,
  "idLuaChonDaChon": 2
}
```

#### 4. Submit Complete Exam

```http
POST /api/exam/submit
Authorization: Bearer {token}
Content-Type: application/json

{
  "idBaiThi": 456
}
```

## Error Handling

All API responses follow a consistent format:

### Success Response
```json
{
  "success": true,
  "data": { /* response data */ },
  "message": "Operation completed successfully"
}
```

### Error Response
```json
{
  "success": false,
  "data": null,
  "message": "Error description",
  "errors": ["Detailed error 1", "Detailed error 2"]
}
```

### Common HTTP Status Codes

- **200 OK:** Request successful
- **201 Created:** Resource created successfully
- **400 Bad Request:** Invalid input data
- **401 Unauthorized:** Authentication required
- **403 Forbidden:** Insufficient permissions
- **404 Not Found:** Resource not found
- **409 Conflict:** Resource conflict (e.g., duplicate)
- **500 Internal Server Error:** Server error

## Rate Limiting

API requests are rate-limited to prevent abuse:

- **General endpoints:** 100 requests per minute
- **Authentication endpoints:** 10 requests per minute
- **Exam submission:** 1 request per second

Rate limit headers are included in responses:
```
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1640995200
```

## Pagination

List endpoints support pagination:

```http
GET /api/questions?page=1&pageSize=20
```

**Response:**
```json
{
  "success": true,
  "data": {
    "items": [/* array of items */],
    "totalCount": 150,
    "page": 1,
    "pageSize": 20,
    "totalPages": 8
  }
}
```

## Best Practices

### 1. Always Check Response Status
```javascript
if (response.success) {
  // Handle success
  console.log(response.data);
} else {
  // Handle error
  console.error(response.message);
}
```

### 2. Handle Token Expiration
```javascript
if (response.status === 401) {
  // Token expired, refresh or redirect to login
  refreshToken();
}
```

### 3. Use Appropriate HTTP Methods
- **GET:** Retrieve data
- **POST:** Create new resources
- **PUT:** Update entire resources
- **PATCH:** Partial updates
- **DELETE:** Remove resources

### 4. Include Correlation IDs
For debugging, include a correlation ID in requests:
```http
X-Correlation-ID: 12345678-1234-1234-1234-123456789012
```
```

## 🔧 Development Documentation

### Setup Guide

```markdown
<!-- docs/development/setup.md -->
# Development Environment Setup

## Prerequisites

### Required Software
- **.NET 8.0 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **SQL Server 2022** - [Download](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
- **Redis** - [Download](https://redis.io/download)
- **Visual Studio 2022** or **VS Code** - [Download](https://visualstudio.microsoft.com/)
- **Git** - [Download](https://git-scm.com/)

### Optional Tools
- **Docker Desktop** - For containerized development
- **Postman** - For API testing
- **SQL Server Management Studio** - For database management

## Project Setup

### 1. Clone Repository
```bash
git clone https://github.com/hospital/bantayvang-api.git
cd bantayvang-api
```

### 2. Database Setup
```bash
# Create database
sqlcmd -S localhost -E -Q "CREATE DATABASE HeThongBanTayVang"

# Run database script
sqlcmd -S localhost -E -d HeThongBanTayVang -i db.sql

# Run sample data
sqlcmd -S localhost -E -d HeThongBanTayVang -i TestData/01-SampleData-Categories-Types.sql
sqlcmd -S localhost -E -d HeThongBanTayVang -i TestData/02-SampleData-Questions.sql
```

### 3. Configuration
```bash
# Copy configuration template
cp appsettings.Development.json.template appsettings.Development.json

# Update connection strings and settings
```

**appsettings.Development.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=HeThongBanTayVang;Trusted_Connection=true;TrustServerCertificate=true;",
    "Redis": "localhost:6379"
  },
  "JWT": {
    "SecretKey": "your-secret-key-here-must-be-at-least-32-characters",
    "Issuer": "BanTayVang.Development",
    "Audience": "BanTayVang.Users",
    "ExpirationMinutes": 60
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### 4. Build and Run
```bash
# Restore packages
dotnet restore

# Build solution
dotnet build

# Run application
dotnet run --project BanTayVang.API

# Or use Visual Studio F5
```

### 5. Verify Setup
- Navigate to `https://localhost:7001/swagger`
- You should see the Swagger UI with API documentation
- Test the `/health` endpoint to verify database connectivity

## Development Workflow

### 1. Feature Development
```bash
# Create feature branch
git checkout -b feature/exam-grading

# Make changes
# ...

# Run tests
dotnet test

# Commit changes
git add .
git commit -m "feat: implement exam grading functionality"

# Push and create PR
git push origin feature/exam-grading
```

### 2. Database Changes
```bash
# Add migration (if using EF migrations)
dotnet ef migrations add AddNewTable

# Update database
dotnet ef database update

# Or manually update db.sql for schema changes
```

### 3. Testing
```bash
# Run all tests
dotnet test

# Run specific test category
dotnet test --filter Category=Unit

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Debugging

### 1. Visual Studio
- Set breakpoints in code
- Press F5 to start debugging
- Use Debug Console for immediate evaluation

### 2. VS Code
- Install C# extension
- Configure launch.json
- Use integrated debugger

### 3. Logging
- Check console output for structured logs
- Use correlation IDs to trace requests
- Configure log levels in appsettings.json

## Common Issues

### Database Connection Issues
```bash
# Check SQL Server is running
net start MSSQLSERVER

# Test connection
sqlcmd -S localhost -E -Q "SELECT 1"

# Check firewall settings
```

### Redis Connection Issues
```bash
# Start Redis server
redis-server

# Test connection
redis-cli ping
```

### Build Issues
```bash
# Clean and rebuild
dotnet clean
dotnet build

# Clear NuGet cache
dotnet nuget locals all --clear
```
```

## 📊 Knowledge Management

### Decision Records

```markdown
<!-- docs/decisions/001-architecture-pattern.md -->
# ADR-001: Architecture Pattern Selection

## Status
Accepted

## Context
We need to choose an architecture pattern for the BanTayVang API that supports:
- Maintainability and testability
- Clear separation of concerns
- Scalability for future growth
- Team productivity

## Decision
We will use **Clean Architecture** with the following layers:
- Presentation (Controllers)
- Application (Services)
- Infrastructure (Repositories, External Services)
- Domain (Models, Interfaces)

## Consequences

### Positive
- Clear separation of concerns
- High testability with dependency injection
- Framework independence
- Easy to understand and maintain

### Negative
- More initial setup complexity
- Additional abstraction layers
- Learning curve for team members

## Implementation
- Use Repository pattern for data access
- Implement Service pattern for business logic
- Apply Dependency Inversion principle
- Use AutoMapper for object mapping

## Date
2024-01-15

## Participants
- Lead Developer
- Senior Developer
- Architecture Team
```

### Troubleshooting Guide

```markdown
<!-- docs/troubleshooting/common-issues.md -->
# Common Issues & Solutions

## Database Issues

### Issue: Connection Timeout
**Symptoms:** Database queries taking too long or timing out

**Solutions:**
1. Check database server performance
2. Review query execution plans
3. Add missing indexes
4. Increase connection timeout in connection string

```sql
-- Check slow queries
SELECT TOP 10 
    total_elapsed_time/execution_count AS avg_time,
    text
FROM sys.dm_exec_query_stats 
CROSS APPLY sys.dm_exec_sql_text(sql_handle)
ORDER BY avg_time DESC
```

### Issue: Deadlocks
**Symptoms:** Occasional database deadlock errors

**Solutions:**
1. Review transaction scope and duration
2. Ensure consistent lock ordering
3. Use appropriate isolation levels
4. Implement retry logic

## Performance Issues

### Issue: High Memory Usage
**Symptoms:** Application consuming excessive memory

**Solutions:**
1. Check for memory leaks in services
2. Review caching strategy
3. Dispose resources properly
4. Use memory profiler tools

### Issue: Slow API Response
**Symptoms:** API endpoints responding slowly

**Solutions:**
1. Enable response caching
2. Optimize database queries
3. Use async/await properly
4. Implement pagination

## Authentication Issues

### Issue: JWT Token Expired
**Symptoms:** 401 Unauthorized errors

**Solutions:**
1. Implement token refresh mechanism
2. Check token expiration settings
3. Verify system clock synchronization
4. Handle token renewal in client

## Deployment Issues

### Issue: Docker Container Won't Start
**Symptoms:** Container exits immediately

**Solutions:**
1. Check Dockerfile configuration
2. Verify environment variables
3. Review container logs
4. Test locally first

```bash
# Check container logs
docker logs bantayvang-api

# Run container interactively
docker run -it bantayvang-api /bin/bash
```

## Monitoring & Alerts

### Setting Up Alerts
1. Configure Prometheus alerts
2. Set up Grafana dashboards
3. Implement health checks
4. Monitor key metrics

### Key Metrics to Monitor
- Response time (95th percentile < 2s)
- Error rate (< 1%)
- Memory usage (< 80%)
- CPU usage (< 70%)
- Database connections (< 80% of pool)
```

## 📋 Documentation Checklist

### Technical Documentation
- [ ] API documentation (Swagger/OpenAPI)
- [ ] Architecture documentation
- [ ] Database schema documentation
- [ ] Code documentation (XML comments)
- [ ] Deployment guides
- [ ] Configuration reference

### User Documentation
- [ ] Getting started guide
- [ ] API usage examples
- [ ] Error handling guide
- [ ] Best practices
- [ ] FAQ section
- [ ] Troubleshooting guide

### Process Documentation
- [ ] Development workflow
- [ ] Code review process
- [ ] Testing procedures
- [ ] Deployment process
- [ ] Incident response
- [ ] Change management

### Knowledge Management
- [ ] Architecture decision records
- [ ] Lessons learned
- [ ] Best practices repository
- [ ] Team knowledge sharing
- [ ] Documentation maintenance process

### Automation
- [ ] Auto-generated API docs
- [ ] Documentation CI/CD
- [ ] Link checking
- [ ] Documentation versioning
- [ ] Search functionality

---
**Status:** Documentation framework established
**Estimated Time:** 1-2 weeks for initial setup
**Priority:** Medium for long-term success