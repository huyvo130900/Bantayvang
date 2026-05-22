# Workflow 2: Question Bank Management - DTOs & Repository

## 🎯 Mục tiêu
Xây dựng DTOs và Repository layer cho hệ thống quản lý câu hỏi với validation và performance optimization.

## 📋 Core Features
- CRUD operations với validation
- Advanced search và filtering
- Soft delete với audit trail
- Performance-optimized queries

## 🔧 Enhanced DTOs

### 1. Core Question DTOs

```csharp
// DTOs/Question/CauhoiDto.cs
public class CauhoiDto
{
    public int Id { get; set; }
    public int? IdDanhMuc { get; set; }
    public string? TenDanhMuc { get; set; }
    public int? IdLoaiCauHoi { get; set; }
    public string? TenLoaiCauHoi { get; set; }
    public string? NoiDung { get; set; }
    public double? Diem { get; set; }
    public string? DoKho { get; set; }
    public string? KhoaPhong { get; set; }
    public string? HinhAnh { get; set; }
    public DateTime? NgayTao { get; set; }
    public DateTime? NgayCapNhat { get; set; }
    public List<LuachonDto> DanhSachLuaChon { get; set; } = new();
}

// DTOs/Question/CreateCauhoiDto.cs
public class CreateCauhoiDto
{
    [Required(ErrorMessage = "Danh mục là bắt buộc")]
    public int IdDanhMuc { get; set; }
    
    [Required(ErrorMessage = "Loại câu hỏi là bắt buộc")]
    public int IdLoaiCauHoi { get; set; }
    
    [Required(ErrorMessage = "Nội dung câu hỏi là bắt buộc")]
    [StringLength(5000, MinimumLength = 10, ErrorMessage = "Nội dung từ 10-5000 ký tự")]
    public string NoiDung { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Điểm số là bắt buộc")]
    [Range(0.1, 10.0, ErrorMessage = "Điểm từ 0.1 đến 10")]
    public double Diem { get; set; }
    
    public string? DoKho { get; set; }
    public string? KhoaPhong { get; set; }
    public string? HinhAnh { get; set; }
    public int NguoiTao { get; set; }
    
    public List<CreateLuachonDto> DanhSachLuaChon { get; set; } = new();
}

// DTOs/Question/UpdateCauhoiDto.cs
public class UpdateCauhoiDto : CreateCauhoiDto
{
    [Required]
    public int Id { get; set; }
    public int NguoiCapNhat { get; set; }
}
```

### 2. Choice DTOs

```csharp
// DTOs/Question/LuachonDto.cs
public class LuachonDto
{
    public int Id { get; set; }
    public string? NoiDung { get; set; }
    public bool? LaDapAnDung { get; set; }
    public int? ThuTu { get; set; }
}

// DTOs/Question/CreateLuachonDto.cs
public class CreateLuachonDto
{
    [Required(ErrorMessage = "Nội dung lựa chọn là bắt buộc")]
    [StringLength(1000, ErrorMessage = "Nội dung không quá 1000 ký tự")]
    public string NoiDung { get; set; } = string.Empty;
    
    public bool LaDapAnDung { get; set; }
    
    [Range(1, 8, ErrorMessage = "Thứ tự từ 1 đến 8")]
    public int ThuTu { get; set; }
}
```

### 3. Filter DTOs

```csharp
// DTOs/Question/QuestionFilterDto.cs
public class QuestionFilterDto : PaginationDto
{
    public int? IdDanhMuc { get; set; }
    public int? IdLoaiCauHoi { get; set; }
    public string? DoKho { get; set; }
    public string? KhoaPhong { get; set; }
    public int? NguoiTao { get; set; }
    public DateTime? NgayTaoTu { get; set; }
    public DateTime? NgayTaoDen { get; set; }
    public bool IncludeDeleted { get; set; } = false;
}
```

## 🔧 Repository Layer

### 1. Enhanced Repository Interface

```csharp
// Repositories/Interfaces/ICauhoiRepository.cs
public interface ICauhoiRepository : IBaseRepository<Cauhoi>
{
    Task<PagedResultDto<Cauhoi>> GetFilteredAsync(QuestionFilterDto filter);
    Task<IEnumerable<Cauhoi>> GetByCategoryWithChoicesAsync(int categoryId);
    Task<IEnumerable<Cauhoi>> GetRandomQuestionsAsync(int count, int? categoryId = null, string? difficulty = null);
    Task<bool> SoftDeleteAsync(int id, int deletedBy);
    Task<bool> RestoreAsync(int id, int restoredBy);
    Task<IEnumerable<Cauhoi>> SearchAsync(string keyword);
}
```

### 2. Repository Implementation

```csharp
// Repositories/Impl/CauhoiRepository.cs
public class CauhoiRepository : BaseRepository<Cauhoi>, ICauhoiRepository
{
    private readonly BanTayVangDbContext _context;
    private readonly ILogger<CauhoiRepository> _logger;

    public CauhoiRepository(BanTayVangDbContext context, ILogger<CauhoiRepository> logger) 
        : base(context)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PagedResultDto<Cauhoi>> GetFilteredAsync(QuestionFilterDto filter)
    {
        try
        {
            var query = _context.Cauhois
                .Include(c => c.IdDanhMucNavigation)
                .Include(c => c.IdLoaiCauHoiNavigation)
                .Include(c => c.Luachons.OrderBy(l => l.ThuTu))
                .AsQueryable();

            // Base filter
            if (!filter.IncludeDeleted)
                query = query.Where(c => c.DaXoa != true);

            // Apply filters
            if (filter.IdDanhMuc.HasValue)
                query = query.Where(c => c.IdDanhMuc == filter.IdDanhMuc);

            if (filter.IdLoaiCauHoi.HasValue)
                query = query.Where(c => c.IdLoaiCauHoi == filter.IdLoaiCauHoi);

            if (!string.IsNullOrEmpty(filter.DoKho))
                query = query.Where(c => c.DoKho == filter.DoKho);

            if (!string.IsNullOrEmpty(filter.KhoaPhong))
                query = query.Where(c => c.KhoaPhong == filter.KhoaPhong);

            if (!string.IsNullOrEmpty(filter.Search))
                query = query.Where(c => c.NoiDung!.Contains(filter.Search));

            if (filter.NgayTaoTu.HasValue)
                query = query.Where(c => c.NgayTao >= filter.NgayTaoTu);

            if (filter.NgayTaoDen.HasValue)
                query = query.Where(c => c.NgayTao <= filter.NgayTaoDen);

            // Sorting
            query = filter.SortBy?.ToLower() switch
            {
                "ngaytao" => filter.SortDirection == "desc" 
                    ? query.OrderByDescending(c => c.NgayTao)
                    : query.OrderBy(c => c.NgayTao),
                "diem" => filter.SortDirection == "desc"
                    ? query.OrderByDescending(c => c.Diem)
                    : query.OrderBy(c => c.Diem),
                _ => query.OrderByDescending(c => c.NgayTao)
            };

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(filter.Skip)
                .Take(filter.PageSize)
                .AsNoTracking()
                .ToListAsync();

            return PagedResultDto<Cauhoi>.Create(items, totalCount, filter.Page, filter.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error filtering questions");
            throw;
        }
    }

    public async Task<IEnumerable<Cauhoi>> GetRandomQuestionsAsync(int count, int? categoryId = null, string? difficulty = null)
    {
        var query = _context.Cauhois
            .Include(c => c.Luachons.OrderBy(l => l.ThuTu))
            .Where(c => c.DaXoa != true);

        if (categoryId.HasValue)
            query = query.Where(c => c.IdDanhMuc == categoryId);

        if (!string.IsNullOrEmpty(difficulty))
            query = query.Where(c => c.DoKho == difficulty);

        return await query
            .OrderBy(c => Guid.NewGuid())
            .Take(count)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<bool> SoftDeleteAsync(int id, int deletedBy)
    {
        var question = await _context.Cauhois.FindAsync(id);
        if (question == null) return false;

        question.DaXoa = true;
        question.NguoiCapNhat = deletedBy;
        question.NgayCapNhat = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }
}
```

## 📋 Implementation Checklist

### ✅ DTOs Layer
- [x] CauhoiDto với complete information
- [x] CreateCauhoiDto với comprehensive validation
- [x] UpdateCauhoiDto với audit fields
- [x] LuachonDto và CreateLuachonDto
- [x] QuestionFilterDto với advanced filtering

### ✅ Repository Layer
- [x] ICauhoiRepository interface với domain methods
- [x] CauhoiRepository implementation với performance optimization
- [x] Error handling và logging
- [x] Soft delete functionality
- [x] Random question selection
- [x] Advanced filtering và sorting

### ✅ Quality Standards
- [x] Input validation với Data Annotations
- [x] Performance optimization với AsNoTracking
- [x] Proper error handling
- [x] Logging integration
- [x] SOLID principles compliance

## 🎯 Next Steps

1. **Service Layer** - Business logic implementation
2. **Controller Layer** - RESTful API endpoints
3. **AutoMapper Configuration** - Object mapping
4. **Unit Testing** - Repository và service tests

---

**Status:** DTOs và Repository Complete
**Next:** Workflow 03 - Service Layer Implementation