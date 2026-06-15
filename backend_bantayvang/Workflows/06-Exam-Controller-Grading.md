# Workflow 6: Enhanced Grading System - Enterprise Edition

## 🎯 Mục tiêu
Xây dựng hệ thống chấm điểm tự động và thủ công với AI-powered analysis, statistical reporting và tuân thủ SOLID principles.

## 🔒 Security Requirements (OWASP Top 10)
- **A01 - Broken Access Control**: Secure grading API endpoints
- **A02 - Cryptographic Failures**: Encrypt sensitive grade data
- **A03 - Injection**: Prevent SQL injection in grading queries
- **A04 - Insecure Design**: Secure grade calculation algorithms
- **A05 - Security Misconfiguration**: Secure grading configurations
- **A07 - Authentication Failures**: Strong authentication for graders
- **A09 - Security Logging**: Comprehensive grading audit trails

## 🏗️ SOLID Principles Implementation

### Single Responsibility Principle (SRP)
- `IGradingService`: Core grading functionality
- `IStatisticalAnalysisService`: Statistical analysis
- `IReportGenerationService`: Report generation
- `IGradeValidationService`: Grade validation

### Open/Closed Principle (OCP)
- Strategy pattern cho different grading algorithms
- Plugin architecture cho custom grading rules

### Interface Segregation Principle (ISP)
- Separate interfaces cho automatic vs manual grading

### Dependency Inversion Principle (DIP)
- Abstract grading algorithms and storage

## Bước 1: Enhanced Grading Architecture

### 1.1 Segregated Grading Service Interfaces
```csharp
// Controllers/ExamController.cs
[ApiController]
[Route("api/[controller]")]
public class ExamController : ControllerBase
{
    private readonly IExamService _examService;
    private readonly IGradingService _gradingService;

    public ExamController(IExamService examService, IGradingService gradingService)
    {
        _examService = examService;
        _gradingService = gradingService;
    }

    [HttpPost("start")]
    public async Task<ActionResult<BaseResponseDto<BaithiDto>>> StartExam([FromBody] StartExamDto startDto)
    {
        // TODO: Lấy taikhoanId từ JWT token
        int taikhoanId = 1; // Temporary
        
        var result = await _examService.StartExamAsync(startDto, taikhoanId);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("{baithiId}/questions")]
    public async Task<ActionResult<BaseResponseDto<List<ExamQuestionDto>>>> GetExamQuestions(int baithiId)
    {
        // TODO: Lấy taikhoanId từ JWT token
        int taikhoanId = 1; // Temporary
        
        var result = await _examService.GetExamQuestionsAsync(baithiId, taikhoanId);
        return Ok(result);
    }

    [HttpPost("answer")]
    public async Task<ActionResult<BaseResponseDto>> SaveAnswer([FromBody] SubmitAnswerDto answerDto)
    {
        // TODO: Lấy taikhoanId từ JWT token
        int taikhoanId = 1; // Temporary
        
        var result = await _examService.SaveAnswerAsync(answerDto, taikhoanId);
        return Ok(result);
    }

    [HttpGet("{baithiId}/progress")]
    public async Task<ActionResult<BaseResponseDto<BaithiDto>>> GetExamProgress(int baithiId)
    {
        // TODO: Lấy taikhoanId từ JWT token
        int taikhoanId = 1; // Temporary
        
        var result = await _examService.GetExamProgressAsync(baithiId, taikhoanId);
        return Ok(result);
    }

    [HttpPost("submit")]
    public async Task<ActionResult<BaseResponseDto<BaithiDto>>> SubmitExam([FromBody] SubmitExamDto submitDto)
    {
        // TODO: Lấy taikhoanId từ JWT token
        int taikhoanId = 1; // Temporary
        
        var result = await _examService.SubmitExamAsync(submitDto, taikhoanId);
        return Ok(result);
    }

    [HttpPost("warning")]
    public async Task<ActionResult<BaseResponseDto>> LogCheatingWarning([FromBody] CheatingWarningDto warningDto)
    {
        var result = await _examService.LogSuspiciousActivityAsync(
            warningDto.IdBaiThi, 
            warningDto.LoaiCanhBao, 
            warningDto.MoTa ?? "");
        return Ok(result);
    }

    [HttpGet("{baithiId}/warnings")]
    public async Task<ActionResult<BaseResponseDto<int>>> GetWarningCount(int baithiId)
    {
        var result = await _examService.GetWarningCountAsync(baithiId);
        return Ok(result);
    }
}
```

## Bước 2: Grading System

### 2.1 Grading Service Interface
```csharp
// Services/Interfaces/IGradingService.cs
public interface IGradingService
{
    Task<BaseResponseDto<BaithiDto>> GradeExamAsync(int baithiId);
    Task<BaseResponseDto<List<BaithiDto>>> GradeMultipleExamsAsync(List<int> baithiIds);
    Task<BaseResponseDto<ExamResultDto>> GetExamResultAsync(int baithiId, int taikhoanId);
    Task<BaseResponseDto<List<ExamResultDto>>> GetExamResultsByExamAsync(int dethiId);
    Task<BaseResponseDto<ExamStatisticsDto>> GetExamStatisticsAsync(int dethiId);
    
    // Chấm thủ công cho câu tự luận
    Task<BaseResponseDto> GradeEssayQuestionAsync(int chitietId, double diem, string? nhanXet);
}
```

### 2.2 Grading DTOs
```csharp
// DTOs/Grading/ExamResultDto.cs
public class ExamResultDto
{
    public int IdBaiThi { get; set; }
    public int IdTaiKhoan { get; set; }
    public string? TenTaiKhoan { get; set; }
    public string? MaNhanVien { get; set; }
    
    public int IdDeThi { get; set; }
    public string? TenDeThi { get; set; }
    public string? MaDeThi { get; set; }
    
    public double? TongDiem { get; set; }
    public double? DiemToiDa { get; set; }
    public double? PhanTramDiem { get; set; }
    
    public int? SoCauDung { get; set; }
    public int? TongSoCau { get; set; }
    public int? TongSoCanhBao { get; set; }
    
    public DateTime? ThoiGianNop { get; set; }
    public string? TrangThai { get; set; }
    public string? XepLoai { get; set; } // Xuất sắc, Giỏi, Khá, Trung bình, Yếu
    
    public List<QuestionResultDto> ChiTietCauHoi { get; set; } = new();
}

// DTOs/Grading/QuestionResultDto.cs
public class QuestionResultDto
{
    public int IdCauHoi { get; set; }
    public string? NoiDungCauHoi { get; set; }
    public double? DiemToiDa { get; set; }
    public double? DiemDatDuoc { get; set; }
    public bool? DungHaySai { get; set; }
    
    public int? IdLuaChonDaChon { get; set; }
    public string? NoiDungLuaChonDaChon { get; set; }
    public int? IdLuaChonDung { get; set; }
    public string? NoiDungLuaChonDung { get; set; }
    
    public string? CauTraLoiTuLuan { get; set; }
    public string? NhanXetGiaoVien { get; set; }
}

// DTOs/Grading/ExamStatisticsDto.cs
public class ExamStatisticsDto
{
    public int IdDeThi { get; set; }
    public string? TenDeThi { get; set; }
    public int TongSoThiSinh { get; set; }
    public int SoThiSinhDaLam { get; set; }
    public int SoThiSinhChuaLam { get; set; }
    
    public double? DiemTrungBinh { get; set; }
    public double? DiemCaoNhat { get; set; }
    public double? DiemThapNhat { get; set; }
    
    public int SoThiSinhXuatSac { get; set; } // >= 9
    public int SoThiSinhGioi { get; set; }     // 8-8.9
    public int SoThiSinhKha { get; set; }      // 6.5-7.9
    public int SoThiSinhTrungBinh { get; set; } // 5-6.4
    public int SoThiSinhYeu { get; set; }      // < 5
    
    public List<QuestionStatisticsDto> ThongKeCauHoi { get; set; } = new();
}

// DTOs/Grading/QuestionStatisticsDto.cs
public class QuestionStatisticsDto
{
    public int IdCauHoi { get; set; }
    public string? NoiDungCauHoi { get; set; }
    public int SoLuotTraLoi { get; set; }
    public int SoLuotTraLoiDung { get; set; }
    public double TiLeTraLoiDung { get; set; }
    public string? DoKho { get; set; }
    
    public List<ChoiceStatisticsDto> ThongKeLuaChon { get; set; } = new();
}

// DTOs/Grading/ChoiceStatisticsDto.cs
public class ChoiceStatisticsDto
{
    public int IdLuaChon { get; set; }
    public string? NoiDungLuaChon { get; set; }
    public int SoLuotChon { get; set; }
    public double TiLeChon { get; set; }
    public bool? LaDapAnDung { get; set; }
}
```

### 2.3 Grading Service Implementation
```csharp
// Services/Impl/GradingService.cs
public class GradingService : IGradingService
{
    private readonly IBaithiRepository _baithiRepository;
    private readonly IChitietlambaiRepository _chitietRepository;
    private readonly ICauhoiRepository _cauhoiRepository;
    private readonly ILuachonRepository _luachonRepository;
    private readonly IMapper _mapper;

    public GradingService(
        IBaithiRepository baithiRepository,
        IChitietlambaiRepository chitietRepository,
        ICauhoiRepository cauhoiRepository,
        ILuachonRepository luachonRepository,
        IMapper mapper)
    {
        _baithiRepository = baithiRepository;
        _chitietRepository = chitietRepository;
        _cauhoiRepository = cauhoiRepository;
        _luachonRepository = luachonRepository;
        _mapper = mapper;
    }

    public async Task<BaseResponseDto<BaithiDto>> GradeExamAsync(int baithiId)
    {
        try
        {
            var baithi = await _baithiRepository.GetWithDetailsAsync(baithiId);
            if (baithi == null)
            {
                return new BaseResponseDto<BaithiDto>
                {
                    Success = false,
                    Message = "Không tìm thấy bài thi"
                };
            }

            var chitietList = await _chitietRepository.GetByBaiThiAsync(baithiId);
            double tongDiem = 0;
            int soCauDung = 0;

            foreach (var chitiet in chitietList)
            {
                var cauhoi = await _cauhoiRepository.GetByIdAsync(chitiet.IdCauHoi ?? 0);
                if (cauhoi == null) continue;

                // Chấm câu trắc nghiệm
                if (chitiet.IdLuaChonDaChon.HasValue)
                {
                    var luachon = await _luachonRepository.GetByIdAsync(chitiet.IdLuaChonDaChon.Value);
                    if (luachon?.LaDapAnDung == true)
                    {
                        chitiet.DiemDatDuoc = cauhoi.Diem;
                        soCauDung++;
                    }
                    else
                    {
                        chitiet.DiemDatDuoc = 0;
                    }
                    
                    await _chitietRepository.UpdateAsync(chitiet);
                }
                // Câu tự luận sẽ được chấm thủ công sau

                tongDiem += chitiet.DiemDatDuoc ?? 0;
            }

            // Cập nhật kết quả bài thi
            baithi.TongDiem = tongDiem;
            baithi.SoCauDung = soCauDung;
            baithi.TrangThai = "Completed";
            
            await _baithiRepository.UpdateAsync(baithi);

            var result = _mapper.Map<BaithiDto>(baithi);
            return new BaseResponseDto<BaithiDto>
            {
                Success = true,
                Message = "Chấm điểm thành công",
                Data = result
            };
        }
        catch (Exception ex)
        {
            return new BaseResponseDto<BaithiDto>
            {
                Success = false,
                Message = "Có lỗi xảy ra khi chấm điểm",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    private string GetXepLoai(double? phanTramDiem)
    {
        if (!phanTramDiem.HasValue) return "Chưa xếp loại";
        
        return phanTramDiem.Value switch
        {
            >= 90 => "Xuất sắc",
            >= 80 => "Giỏi",
            >= 65 => "Khá",
            >= 50 => "Trung bình",
            _ => "Yếu"
        };
    }

    // Implement other methods...
}
```

## Tiếp theo: Grading Controller và Final Setup