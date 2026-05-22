# Workflow 3: Question Service và Controller - Enterprise Edition

## 🎯 Mục tiêu
Xây dựng Question Service và Controller tuân thủ SOLID principles, OWASP Top 10 security, và performance optimization.

## 🔒 Security Requirements (OWASP Top 10)
- **A01 - Broken Access Control**: Implement role-based authorization
- **A03 - Injection**: Use parameterized queries, input validation
- **A05 - Security Misconfiguration**: Secure file upload validation
- **A07 - Identification/Authentication**: JWT token validation
- **A10 - Server-Side Request Forgery**: Validate file sources

## 🏗️ SOLID Principles Implementation

### Single Responsibility Principle (SRP)
- `ICauhoiService`: Business logic cho câu hỏi
- `IQuestionValidator`: Validation logic
- `IFileProcessor`: File processing logic
- `IQuestionCacheService`: Caching logic

### Open/Closed Principle (OCP)
- Strategy pattern cho different question types
- Factory pattern cho question creation

### Liskov Substitution Principle (LSP)
- Interface segregation cho different operations

### Interface Segregation Principle (ISP)
- Separate interfaces cho read/write operations

### Dependency Inversion Principle (DIP)
- Depend on abstractions, not concretions

## Bước 1: Enhanced Question Service Architecture

### 1.1 Segregated Service Interfaces
```csharp
// Services/Interfaces/Questions/IQuestionReadService.cs
public interface IQuestionReadService
{
    Task<Result<PagedResultDto<CauhoiDto>>> GetFilteredQuestionsAsync(QuestionFilterDto filter, CancellationToken cancellationToken = default);
    Task<Result<CauhoiDto>> GetQuestionByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Result<List<CauhoiDto>>> GetRandomQuestionsAsync(int count, int? danhMucId = null, CancellationToken cancellationToken = default);
    Task<Result<QuestionStatisticsDto>> GetQuestionStatisticsAsync(int id, CancellationToken cancellationToken = default);
}

// Services/Interfaces/Questions/IQuestionWriteService.cs
public interface IQuestionWriteService
{
    Task<Result<CauhoiDto>> CreateQuestionAsync(CreateCauhoiDto createDto, int nguoiTao, CancellationToken cancellationToken = default);
    Task<Result<CauhoiDto>> UpdateQuestionAsync(UpdateCauhoiDto updateDto, int nguoiCapNhat, CancellationToken cancellationToken = default);
    Task<Result> DeleteQuestionAsync(int id, int nguoiCapNhat, CancellationToken cancellationToken = default);
    Task<Result> SoftDeleteQuestionAsync(int id, int nguoiCapNhat, CancellationToken cancellationToken = default);
}

// Services/Interfaces/Questions/IQuestionImportService.cs
public interface IQuestionImportService
{
    Task<Result<ImportResultDto<CauhoiDto>>> ImportQuestionsFromExcelAsync(IFormFile file, int nguoiTao, CancellationToken cancellationToken = default);
    Task<Result<byte[]>> ExportQuestionsToExcelAsync(QuestionFilterDto filter, CancellationToken cancellationToken = default);
    Task<Result<ImportValidationDto>> ValidateImportFileAsync(IFormFile file, CancellationToken cancellationToken = default);
}

// Services/Interfaces/Questions/IQuestionValidator.cs
public interface IQuestionValidator
{
    Task<ValidationResult> ValidateCreateQuestionAsync(CreateCauhoiDto dto, CancellationToken cancellationToken = default);
    Task<ValidationResult> ValidateUpdateQuestionAsync(UpdateCauhoiDto dto, CancellationToken cancellationToken = default);
    ValidationResult ValidateFileUpload(IFormFile file);
    Task<ValidationResult> ValidateQuestionPermissionAsync(int questionId, int userId, string operation, CancellationToken cancellationToken = default);
}

// Services/Interfaces/Questions/IQuestionCacheService.cs
public interface IQuestionCacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);
}
```

```

### 1.2 Enhanced DTOs with Security & Validation
```csharp
// DTOs/Common/Result.cs
public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T? Data { get; private set; }
    public string? ErrorMessage { get; private set; }
    public List<string> ValidationErrors { get; private set; } = new();
    public string? ErrorCode { get; private set; }

    public static Result<T> Success(T data) => new() { IsSuccess = true, Data = data };
    public static Result<T> Failure(string errorMessage, string? errorCode = null) => 
        new() { IsSuccess = false, ErrorMessage = errorMessage, ErrorCode = errorCode };
    public static Result<T> ValidationFailure(List<string> validationErrors) => 
        new() { IsSuccess = false, ValidationErrors = validationErrors };
}

public class Result
{
    public bool IsSuccess { get; private set; }
    public string? ErrorMessage { get; private set; }
    public List<string> ValidationErrors { get; private set; } = new();
    public string? ErrorCode { get; private set; }

    public static Result Success() => new() { IsSuccess = true };
    public static Result Failure(string errorMessage, string? errorCode = null) => 
        new() { IsSuccess = false, ErrorMessage = errorMessage, ErrorCode = errorCode };
}

// DTOs/Questions/Enhanced/CreateCauhoiDto.cs
public class CreateCauhoiDto
{
    [Required(ErrorMessage = "Danh mục câu hỏi là bắt buộc")]
    [Range(1, int.MaxValue, ErrorMessage = "Danh mục câu hỏi không hợp lệ")]
    public int IdDanhMuc { get; set; }

    [Required(ErrorMessage = "Loại câu hỏi là bắt buộc")]
    [Range(1, int.MaxValue, ErrorMessage = "Loại câu hỏi không hợp lệ")]
    public int IdLoaiCauHoi { get; set; }

    [Required(ErrorMessage = "Nội dung câu hỏi là bắt buộc")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Nội dung câu hỏi phải từ 10-2000 ký tự")]
    [RegularExpression(@"^[^<>""'%;()&+]*$", ErrorMessage = "Nội dung chứa ký tự không hợp lệ")]
    public string NoiDung { get; set; } = string.Empty;

    [Range(0.1, 10.0, ErrorMessage = "Điểm phải từ 0.1 đến 10.0")]
    public double Diem { get; set; }

    [Required(ErrorMessage = "Độ khó là bắt buộc")]
    [RegularExpression("^(Dễ|Trung bình|Khó)$", ErrorMessage = "Độ khó không hợp lệ")]
    public string DoKho { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Khoa phòng không được quá 100 ký tự")]
    [RegularExpression(@"^[a-zA-ZÀ-ỹ0-9\s]*$", ErrorMessage = "Khoa phòng chứa ký tự không hợp lệ")]
    public string? KhoaPhong { get; set; }

    [Url(ErrorMessage = "Đường dẫn hình ảnh không hợp lệ")]
    [StringLength(500, ErrorMessage = "Đường dẫn hình ảnh quá dài")]
    public string? HinhAnh { get; set; }

    [ValidateComplexType]
    public List<CreateLuachonDto> DanhSachLuaChon { get; set; } = new();

    // Security: Prevent mass assignment
    public Dictionary<string, object> AdditionalProperties { get; set; } = new();
}

// DTOs/Questions/Enhanced/ImportResultDto.cs
public class ImportResultDto<T>
{
    public List<T> SuccessfulItems { get; set; } = new();
    public List<ImportErrorDto> FailedItems { get; set; } = new();
    public int TotalProcessed { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public string? FileName { get; set; }
    public DateTime ImportedAt { get; set; }
    public int ImportedBy { get; set; }
}

public class ImportErrorDto
{
    public int RowNumber { get; set; }
    public string? RowData { get; set; }
    public List<string> Errors { get; set; } = new();
    public string? ErrorCode { get; set; }
}

// DTOs/Questions/Enhanced/ValidationResult.cs
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public Dictionary<string, List<string>> FieldErrors { get; set; } = new();
    public string? ErrorCode { get; set; }

    public static ValidationResult Success() => new() { IsValid = true };
    public static ValidationResult Failure(string error, string? errorCode = null) => 
        new() { IsValid = false, Errors = new List<string> { error }, ErrorCode = errorCode };
    public static ValidationResult Failure(List<string> errors, string? errorCode = null) => 
        new() { IsValid = false, Errors = errors, ErrorCode = errorCode };
}
```

### 1.3 Secure Question Service Implementation
```csharp
// Services/Impl/Questions/QuestionWriteService.cs
public class QuestionWriteService : IQuestionWriteService
{
    private readonly ICauhoiRepository _cauhoiRepository;
    private readonly ILuachonRepository _luachonRepository;
    private readonly IQuestionValidator _validator;
    private readonly IQuestionCacheService _cacheService;
    private readonly IMapper _mapper;
    private readonly ILogger<QuestionWriteService> _logger;
    private readonly ICurrentUserService _currentUserService;

    public QuestionWriteService(
        ICauhoiRepository cauhoiRepository,
        ILuachonRepository luachonRepository,
        IQuestionValidator validator,
        IQuestionCacheService cacheService,
        IMapper mapper,
        ILogger<QuestionWriteService> logger,
        ICurrentUserService currentUserService)
    {
        _cauhoiRepository = cauhoiRepository;
        _luachonRepository = luachonRepository;
        _validator = validator;
        _cacheService = cacheService;
        _mapper = mapper;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<Result<CauhoiDto>> CreateQuestionAsync(
        CreateCauhoiDto createDto, 
        int nguoiTao, 
        CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("CreateQuestion");
        activity?.SetTag("user.id", nguoiTao.ToString());
        
        try
        {
            // Security: Validate permissions
            var permissionResult = await _validator.ValidateQuestionPermissionAsync(0, nguoiTao, "CREATE", cancellationToken);
            if (!permissionResult.IsValid)
            {
                _logger.LogWarning("User {UserId} attempted to create question without permission", nguoiTao);
                return Result<CauhoiDto>.Failure("Không có quyền tạo câu hỏi", "PERMISSION_DENIED");
            }

            // Validation
            var validationResult = await _validator.ValidateCreateQuestionAsync(createDto, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result<CauhoiDto>.ValidationFailure(validationResult.Errors);
            }

            // Business logic validation
            if (createDto.DanhSachLuaChon.Any() && !createDto.DanhSachLuaChon.Any(x => x.LaDapAnDung))
            {
                return Result<CauhoiDto>.Failure("Câu hỏi trắc nghiệm phải có ít nhất 1 đáp án đúng", "INVALID_CHOICES");
            }

            // Security: Sanitize input
            createDto.NoiDung = SanitizeInput(createDto.NoiDung);
            createDto.KhoaPhong = SanitizeInput(createDto.KhoaPhong);

            using var transaction = await _cauhoiRepository.BeginTransactionAsync(cancellationToken);
            
            try
            {
                var cauhoi = new Cauhoi
                {
                    IdDanhMuc = createDto.IdDanhMuc,
                    IdLoaiCauHoi = createDto.IdLoaiCauHoi,
                    NoiDung = createDto.NoiDung,
                    Diem = createDto.Diem,
                    DoKho = createDto.DoKho,
                    KhoaPhong = createDto.KhoaPhong,
                    HinhAnh = createDto.HinhAnh,
                    NguoiTao = nguoiTao,
                    NgayTao = DateTime.UtcNow,
                    DaXoa = false,
                    // Security: Add audit fields
                    CreatedBy = nguoiTao,
                    CreatedAt = DateTime.UtcNow,
                    Version = 1
                };

                var savedCauhoi = await _cauhoiRepository.AddAsync(cauhoi, cancellationToken);

                // Add choices with validation
                foreach (var luachonDto in createDto.DanhSachLuaChon)
                {
                    var luachon = new Luachon
                    {
                        IdCauHoi = savedCauhoi.Id,
                        NoiDung = SanitizeInput(luachonDto.NoiDung),
                        LaDapAnDung = luachonDto.LaDapAnDung,
                        ThuTu = luachonDto.ThuTu,
                        CreatedBy = nguoiTao,
                        CreatedAt = DateTime.UtcNow
                    };
                    await _luachonRepository.AddAsync(luachon, cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);

                // Clear cache
                await _cacheService.RemoveByPatternAsync("questions:*", cancellationToken);

                var result = _mapper.Map<CauhoiDto>(savedCauhoi);
                
                _logger.LogInformation("Question {QuestionId} created successfully by user {UserId}", savedCauhoi.Id, nguoiTao);
                
                return Result<CauhoiDto>.Success(result);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating question for user {UserId}", nguoiTao);
            return Result<CauhoiDto>.Failure("Có lỗi xảy ra khi tạo câu hỏi", "INTERNAL_ERROR");
        }
    }

    private static string? SanitizeInput(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        // Remove potentially dangerous characters
        return input
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#x27;")
            .Replace("/", "&#x2F;")
            .Trim();
    }

    // Implement other methods with similar security patterns...
}
```

```

### 1.4 Secure File Import Service
```csharp
// Services/Impl/Questions/QuestionImportService.cs
public class QuestionImportService : IQuestionImportService
{
    private readonly IQuestionWriteService _questionWriteService;
    private readonly IQuestionValidator _validator;
    private readonly ILogger<QuestionImportService> _logger;
    private readonly IConfiguration _configuration;
    
    private readonly string[] _allowedExtensions = { ".xlsx", ".xls" };
    private readonly long _maxFileSize = 10 * 1024 * 1024; // 10MB

    public async Task<Result<ImportResultDto<CauhoiDto>>> ImportQuestionsFromExcelAsync(
        IFormFile file, 
        int nguoiTao, 
        CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("ImportQuestions");
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Security: Validate file
            var fileValidation = ValidateFileUpload(file);
            if (!fileValidation.IsValid)
            {
                return Result<ImportResultDto<CauhoiDto>>.ValidationFailure(fileValidation.Errors);
            }

            var result = new ImportResultDto<CauhoiDto>
            {
                FileName = file.FileName,
                ImportedAt = DateTime.UtcNow,
                ImportedBy = nguoiTao
            };

            using var stream = file.OpenReadStream();
            using var package = new ExcelPackage(stream);
            
            var worksheet = package.Workbook.Worksheets.FirstOrDefault();
            if (worksheet == null)
            {
                return Result<ImportResultDto<CauhoiDto>>.Failure("File Excel không chứa worksheet hợp lệ");
            }

            var rowCount = worksheet.Dimension?.Rows ?? 0;
            if (rowCount <= 1) // Header row
            {
                return Result<ImportResultDto<CauhoiDto>>.Failure("File Excel không chứa dữ liệu");
            }

            // Process rows (skip header)
            for (int row = 2; row <= rowCount; row++)
            {
                try
                {
                    var questionDto = ExtractQuestionFromRow(worksheet, row);
                    if (questionDto == null)
                    {
                        result.FailedItems.Add(new ImportErrorDto
                        {
                            RowNumber = row,
                            Errors = new List<string> { "Dữ liệu hàng không hợp lệ" }
                        });
                        continue;
                    }

                    var createResult = await _questionWriteService.CreateQuestionAsync(questionDto, nguoiTao, cancellationToken);
                    if (createResult.IsSuccess)
                    {
                        result.SuccessfulItems.Add(createResult.Data!);
                        result.SuccessCount++;
                    }
                    else
                    {
                        result.FailedItems.Add(new ImportErrorDto
                        {
                            RowNumber = row,
                            Errors = createResult.ValidationErrors.Any() ? createResult.ValidationErrors : new List<string> { createResult.ErrorMessage ?? "Unknown error" }
                        });
                        result.FailureCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing row {Row} in import file", row);
                    result.FailedItems.Add(new ImportErrorDto
                    {
                        RowNumber = row,
                        Errors = new List<string> { "Lỗi xử lý dữ liệu hàng" }
                    });
                    result.FailureCount++;
                }
                
                result.TotalProcessed++;
            }

            stopwatch.Stop();
            result.ProcessingTime = stopwatch.Elapsed;

            _logger.LogInformation("Import completed: {Success}/{Total} questions imported successfully", 
                result.SuccessCount, result.TotalProcessed);

            return Result<ImportResultDto<CauhoiDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing questions from file {FileName}", file.FileName);
            return Result<ImportResultDto<CauhoiDto>>.Failure("Có lỗi xảy ra khi import câu hỏi");
        }
    }

    public ValidationResult ValidateFileUpload(IFormFile file)
    {
        var errors = new List<string>();

        if (file == null || file.Length == 0)
        {
            errors.Add("File không được để trống");
            return ValidationResult.Failure(errors, "INVALID_FILE");
        }

        // Security: Check file size
        if (file.Length > _maxFileSize)
        {
            errors.Add($"File không được vượt quá {_maxFileSize / (1024 * 1024)}MB");
        }

        // Security: Check file extension
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_allowedExtensions.Contains(extension))
        {
            errors.Add("Chỉ chấp nhận file Excel (.xlsx, .xls)");
        }

        // Security: Check filename for path traversal
        if (file.FileName.Contains("..") || file.FileName.Contains("/") || file.FileName.Contains("\\"))
        {
            errors.Add("Tên file không hợp lệ");
        }

        // Security: Basic content type check
        var allowedContentTypes = new[] { 
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "application/vnd.ms-excel"
        };
        
        if (!allowedContentTypes.Contains(file.ContentType))
        {
            errors.Add("Loại file không được hỗ trợ");
        }

        return errors.Any() ? ValidationResult.Failure(errors, "INVALID_FILE") : ValidationResult.Success();
    }

    private CreateCauhoiDto? ExtractQuestionFromRow(ExcelWorksheet worksheet, int row)
    {
        try
        {
            var noiDung = worksheet.Cells[row, 1].Text?.Trim();
            if (string.IsNullOrWhiteSpace(noiDung))
                return null;

            var diem = double.TryParse(worksheet.Cells[row, 2].Text, out var d) ? d : 1.0;
            var doKho = worksheet.Cells[row, 3].Text?.Trim() ?? "Trung bình";
            var khoaPhong = worksheet.Cells[row, 4].Text?.Trim();
            
            var danhSachLuaChon = new List<CreateLuachonDto>();
            
            // Extract choices (columns 5-8: A, B, C, D)
            for (int col = 5; col <= 8; col++)
            {
                var luaChonText = worksheet.Cells[row, col].Text?.Trim();
                if (!string.IsNullOrWhiteSpace(luaChonText))
                {
                    danhSachLuaChon.Add(new CreateLuachonDto
                    {
                        NoiDung = luaChonText,
                        ThuTu = col - 4,
                        LaDapAnDung = false // Will be set based on correct answer column
                    });
                }
            }

            // Get correct answer (column 9)
            var correctAnswer = worksheet.Cells[row, 9].Text?.Trim().ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(correctAnswer) && correctAnswer.Length == 1)
            {
                var correctIndex = correctAnswer[0] - 'A';
                if (correctIndex >= 0 && correctIndex < danhSachLuaChon.Count)
                {
                    danhSachLuaChon[correctIndex].LaDapAnDung = true;
                }
            }

            return new CreateCauhoiDto
            {
                IdDanhMuc = 1, // Default category - should be configurable
                IdLoaiCauHoi = 1, // Default type - should be configurable
                NoiDung = noiDung,
                Diem = diem,
                DoKho = doKho,
                KhoaPhong = khoaPhong,
                DanhSachLuaChon = danhSachLuaChon
            };
        }
        catch
        {
            return null;
        }
    }
}
```

## Bước 2: Enhanced Question Controller với Security

### 2.1 Secure Question Controller
```csharp
// Controllers/CauhoiController.cs
[ApiController]
[Route("api/[controller]")]
[Authorize] // Security: Require authentication
[ApiVersion("1.0")]
[Produces("application/json")]
public class CauhoiController : ControllerBase
{
    private readonly IQuestionReadService _questionReadService;
    private readonly IQuestionWriteService _questionWriteService;
    private readonly IQuestionImportService _questionImportService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CauhoiController> _logger;
    private readonly IRateLimitService _rateLimitService;

    public CauhoiController(
        IQuestionReadService questionReadService,
        IQuestionWriteService questionWriteService,
        IQuestionImportService questionImportService,
        ICurrentUserService currentUserService,
        ILogger<CauhoiController> logger,
        IRateLimitService rateLimitService)
    {
        _questionReadService = questionReadService;
        _questionWriteService = questionWriteService;
        _questionImportService = questionImportService;
        _currentUserService = currentUserService;
        _logger = logger;
        _rateLimitService = rateLimitService;
    }

    /// <summary>
    /// Lấy danh sách câu hỏi với phân trang và lọc
    /// </summary>
    /// <param name="filter">Bộ lọc câu hỏi</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Danh sách câu hỏi</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResultDto<CauhoiDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<ApiResponse<PagedResultDto<CauhoiDto>>>> GetQuestions(
        [FromQuery] QuestionFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        // Security: Rate limiting
        var rateLimitResult = await _rateLimitService.CheckRateLimitAsync(
            _currentUserService.UserId, "GetQuestions", 100, TimeSpan.FromMinutes(1));
        
        if (!rateLimitResult.IsAllowed)
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, 
                ApiResponse.Failure("Quá nhiều yêu cầu, vui lòng thử lại sau"));
        }

        var result = await _questionReadService.GetFilteredQuestionsAsync(filter, cancellationToken);
        
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse.Failure(result.ErrorMessage ?? "Có lỗi xảy ra"));
        }

        return Ok(ApiResponse.Success(result.Data));
    }

    /// <summary>
    /// Lấy thông tin chi tiết câu hỏi
    /// </summary>
    /// <param name="id">ID câu hỏi</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Thông tin câu hỏi</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<CauhoiDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<CauhoiDto>>> GetQuestion(
        int id,
        CancellationToken cancellationToken = default)
    {
        // Security: Input validation
        if (id <= 0)
        {
            return BadRequest(ApiResponse.Failure("ID câu hỏi không hợp lệ"));
        }

        var result = await _questionReadService.GetQuestionByIdAsync(id, cancellationToken);
        
        if (!result.IsSuccess)
        {
            return NotFound(ApiResponse.Failure(result.ErrorMessage ?? "Không tìm thấy câu hỏi"));
        }

        return Ok(ApiResponse.Success(result.Data));
    }

    /// <summary>
    /// Tạo câu hỏi mới
    /// </summary>
    /// <param name="createDto">Thông tin câu hỏi</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Câu hỏi đã tạo</returns>
    [HttpPost]
    [Authorize(Roles = "Admin,Teacher")] // Security: Role-based authorization
    [ProducesResponseType(typeof(ApiResponse<CauhoiDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<CauhoiDto>>> CreateQuestion(
        [FromBody] CreateCauhoiDto createDto,
        CancellationToken cancellationToken = default)
    {
        // Security: Rate limiting for write operations
        var rateLimitResult = await _rateLimitService.CheckRateLimitAsync(
            _currentUserService.UserId, "CreateQuestion", 10, TimeSpan.FromMinutes(1));
        
        if (!rateLimitResult.IsAllowed)
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, 
                ApiResponse.Failure("Quá nhiều yêu cầu tạo câu hỏi"));
        }

        var result = await _questionWriteService.CreateQuestionAsync(
            createDto, _currentUserService.UserId, cancellationToken);
        
        if (!result.IsSuccess)
        {
            if (result.ValidationErrors.Any())
            {
                return BadRequest(ApiResponse.ValidationFailure(result.ValidationErrors));
            }
            return BadRequest(ApiResponse.Failure(result.ErrorMessage ?? "Có lỗi xảy ra khi tạo câu hỏi"));
        }

        _logger.LogInformation("Question {QuestionId} created by user {UserId}", 
            result.Data!.Id, _currentUserService.UserId);

        return CreatedAtAction(nameof(GetQuestion), 
            new { id = result.Data.Id }, 
            ApiResponse.Success(result.Data));
    }

    /// <summary>
    /// Import câu hỏi từ file Excel
    /// </summary>
    /// <param name="file">File Excel</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Kết quả import</returns>
    [HttpPost("import")]
    [Authorize(Roles = "Admin,Teacher")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10MB limit
    [ProducesResponseType(typeof(ApiResponse<ImportResultDto<CauhoiDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<ImportResultDto<CauhoiDto>>>> ImportQuestions(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        // Security: Rate limiting for import operations
        var rateLimitResult = await _rateLimitService.CheckRateLimitAsync(
            _currentUserService.UserId, "ImportQuestions", 3, TimeSpan.FromHours(1));
        
        if (!rateLimitResult.IsAllowed)
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, 
                ApiResponse.Failure("Quá nhiều yêu cầu import, vui lòng thử lại sau 1 giờ"));
        }

        var result = await _questionImportService.ImportQuestionsFromExcelAsync(
            file, _currentUserService.UserId, cancellationToken);
        
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse.Failure(result.ErrorMessage ?? "Có lỗi xảy ra khi import"));
        }

        _logger.LogInformation("Questions imported by user {UserId}: {Success}/{Total}", 
            _currentUserService.UserId, result.Data!.SuccessCount, result.Data.TotalProcessed);

        return Ok(ApiResponse.Success(result.Data));
    }

    /// <summary>
    /// Lấy câu hỏi ngẫu nhiên
    /// </summary>
    /// <param name="count">Số lượng câu hỏi</param>
    /// <param name="danhMucId">ID danh mục (tùy chọn)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Danh sách câu hỏi ngẫu nhiên</returns>
    [HttpGet("random")]
    [ProducesResponseType(typeof(ApiResponse<List<CauhoiDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<CauhoiDto>>>> GetRandomQuestions(
        [FromQuery] int count = 10,
        [FromQuery] int? danhMucId = null,
        CancellationToken cancellationToken = default)
    {
        // Security: Limit random question count
        if (count <= 0 || count > 100)
        {
            return BadRequest(ApiResponse.Failure("Số lượng câu hỏi phải từ 1 đến 100"));
        }

        var result = await _questionReadService.GetRandomQuestionsAsync(count, danhMucId, cancellationToken);
        
        if (!result.IsSuccess)
        {
            return BadRequest(ApiResponse.Failure(result.ErrorMessage ?? "Có lỗi xảy ra"));
        }

        return Ok(ApiResponse.Success(result.Data));
    }
}
```

## 🔧 Performance Optimizations

### Caching Strategy
- Redis cache cho frequently accessed questions
- Memory cache cho metadata (categories, types)
- Cache invalidation patterns

### Database Optimizations
- Proper indexing strategy
- Query optimization with EF Core
- Connection pooling
- Read replicas for read operations

### Security Enhancements
- Input sanitization
- SQL injection prevention
- File upload security
- Rate limiting
- Audit logging

## Tiếp theo: Workflow 04 - Enhanced Online Exam System