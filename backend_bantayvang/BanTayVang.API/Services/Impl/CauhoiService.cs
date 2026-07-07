using AutoMapper;
using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Question;
using BanTayVang.API.Models;
using BanTayVang.API.Repositories.Interfaces;
using BanTayVang.API.Services.Interfaces;
using BanTayVang.API.Services.Interfaces.Security;
using BanTayVang.API.Services.Interfaces.Validation;
using BanTayVang.API.Services.Interfaces.Import;
using Microsoft.Extensions.Logging;

namespace BanTayVang.API.Services.Impl
{
    /// <summary>
    /// Question service implementation following SOLID principles and OWASP security
    /// </summary>
    public class CauhoiService : ICauhoiService
    {
        private readonly ICauhoiRepository _cauhoiRepository;
        private readonly ILuachonRepository _luachonRepository;
        private readonly IExamSecurityService _securityService;
        private readonly IMapper _mapper;
        private readonly ILogger<CauhoiService> _logger;
        private readonly IQuestionImportStrategyFactory _strategyFactory;
        private readonly ILoaicauhoiRepository _loaiRepository;

        public CauhoiService(
            ICauhoiRepository cauhoiRepository,
            ILuachonRepository luachonRepository,
            IExamSecurityService securityService,
            IMapper mapper,
            ILogger<CauhoiService> logger,
            IQuestionImportStrategyFactory strategyFactory,
            ILoaicauhoiRepository loaiRepository)
        {
            _cauhoiRepository = cauhoiRepository ?? throw new ArgumentNullException(nameof(cauhoiRepository));
            _luachonRepository = luachonRepository ?? throw new ArgumentNullException(nameof(luachonRepository));
            _securityService = securityService ?? throw new ArgumentNullException(nameof(securityService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _strategyFactory = strategyFactory ?? throw new ArgumentNullException(nameof(strategyFactory));
            _loaiRepository = loaiRepository ?? throw new ArgumentNullException(nameof(loaiRepository));
        }

        public async Task<BaseResponseDto<PagedResultDto<CauhoiDto>>> GetFilteredQuestionsAsync(QuestionFilterDto filter)
        {
            try
            {
                _logger.LogInformation("Getting filtered questions with filter: {@Filter}", filter);

                // OWASP A03: Injection - Input validation
                if (filter.PageSize > 100)
                {
                    filter.PageSize = 100; // Limit page size to prevent DoS
                }

                if (filter.PageNumber < 1)
                {
                    filter.PageNumber = 1;
                }

                var questions = await _cauhoiRepository.GetFilteredAsync(filter);
                var totalCount = await _cauhoiRepository.GetFilteredCountAsync(filter);

                var questionDtos = _mapper.Map<List<CauhoiDto>>(questions.Items);

                var result = new PagedResultDto<CauhoiDto>
                {
                    Items = questionDtos,
                    Pagination = new PaginationDto
                    {
                        PageNumber = filter.PageNumber,
                        PageSize = filter.PageSize,
                        TotalRecords = totalCount,
                        TotalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize)
                    }
                };

                return new BaseResponseDto<PagedResultDto<CauhoiDto>>
                {
                    Success = true,
                    Message = "Lấy danh sách câu hỏi thành công",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting filtered questions");
                
                await _securityService.LogSecurityEventAsync("QUESTION_FILTER_ERROR", 
                    $"System error during question filtering: {ex.Message}", 
                    null, "Medium");

                return new BaseResponseDto<PagedResultDto<CauhoiDto>>
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi lấy danh sách câu hỏi",
                    Errors = new List<string> { ex.Message, ex.InnerException?.Message ?? "" }
                };
            }
        }

        public async Task<BaseResponseDto<CauhoiDto>> GetQuestionByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Getting question by ID: {QuestionId}", id);

                // OWASP A01: Broken Access Control - Input validation
                if (id <= 0)
                {
                    return new BaseResponseDto<CauhoiDto>
                    {
                        Success = false,
                        Message = "ID câu hỏi không hợp lệ"
                    };
                }

                var question = await _cauhoiRepository.GetWithChoicesAsync(id);
                if (question == null)
                {
                    return new BaseResponseDto<CauhoiDto>
                    {
                        Success = false,
                        Message = "Không tìm thấy câu hỏi"
                    };
                }

                var result = _mapper.Map<CauhoiDto>(question);
                
                return new BaseResponseDto<CauhoiDto>
                {
                    Success = true,
                    Message = "Lấy thông tin câu hỏi thành công",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting question by ID: {QuestionId}", id);
                
                await _securityService.LogSecurityEventAsync("QUESTION_GET_ERROR", 
                    $"System error getting question {id}: {ex.Message}", 
                    null, "Medium");

                return new BaseResponseDto<CauhoiDto>
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi lấy thông tin câu hỏi",
                    Errors = new List<string> { "Lỗi hệ thống" }
                };
            }
        }

        public async Task<BaseResponseDto<CauhoiDto>> CreateQuestionAsync(CreateCauhoiDto createDto, int nguoiTao)
        {
            try
            {
                _logger.LogInformation("Creating question for user {UserId}", nguoiTao);

                // OWASP A03: Injection - Input validation and sanitization
                if (string.IsNullOrWhiteSpace(createDto.NoiDung))
                {
                    return new BaseResponseDto<CauhoiDto>
                    {
                        Success = false,
                        Message = "Nội dung câu hỏi không được để trống"
                    };
                }

                var loaiId = createDto.IdLoaiCauHoi ?? 1;
                bool requiresChoices = true;
                if (loaiId > 0)
                {
                    var loai = await _loaiRepository.GetByIdAsync(loaiId);
                    if (loai != null)
                    {
                        var loaiName = loai.TenLoai?.ToLower() ?? "";
                        if (loaiName.Contains("tự luận") || loaiName.Contains("tu luan"))
                        {
                            requiresChoices = false;
                        }
                    }
                }

                if (requiresChoices && (createDto.DanhSachLuaChon == null || createDto.DanhSachLuaChon.Count < 2))
                {
                    return new BaseResponseDto<CauhoiDto>
                    {
                        Success = false,
                        Message = "Câu hỏi trắc nghiệm phải có ít nhất 2 lựa chọn"
                    };
                }

                // Kiểm tra câu hỏi trùng nội dung (so sánh không phân biệt hoa thường, bỏ khoảng trắng thừa)
                var noiDungChuan = createDto.NoiDung.Trim().ToLower();
                var duplicate = await _cauhoiRepository.FindDuplicateAsync(noiDungChuan, createDto.KhoaPhong);
                if (duplicate != null)
                {
                    return new BaseResponseDto<CauhoiDto>
                    {
                        Success = false,
                        Message = $"Câu hỏi đã tồn tại trong ngân hàng (Id: {duplicate.Id}): \"{duplicate.NoiDung}\""
                    };
                }

                // OWASP A04: Insecure Design - Transaction integrity
                using var transaction = await _cauhoiRepository.BeginTransactionAsync();
                try
                {
                    var cauhoi = new Cauhoi
                    {
                        NoiDung = SanitizeHtmlContent(createDto.NoiDung),
                        IdLoaiCauHoi = loaiId,
                        DoKho = MapDifficultyToDb(createDto.DoKho ?? createDto.MucDo),
                        HinhAnh = createDto.HinhAnh,
                        NguoiTao = nguoiTao,
                        NgayTao = DateTime.Now,
                        DaXoa = false,
                        KhoaPhong = createDto.KhoaPhong
                    };

                    var savedQuestion = await _cauhoiRepository.AddAsync(cauhoi);

                    // Add choices
                    if (createDto.DanhSachLuaChon != null)
                    {
                        foreach (var choiceDto in createDto.DanhSachLuaChon)
                        {
                            var luachon = new Luachon
                            {
                                IdCauHoi = savedQuestion.Id,
                                NoiDung = SanitizeHtmlContent(choiceDto.NoiDung),
                                ThuTu = choiceDto.ThuTu,
                                LaDapAnDung = choiceDto.LaDapAnDung
                            };

                            await _luachonRepository.AddAsync(luachon);
                        }
                    }

                    await transaction.CommitAsync();

                    await _securityService.LogSecurityEventAsync("QUESTION_CREATED", 
                        $"User {nguoiTao} created question {savedQuestion.Id}", 
                        nguoiTao, "Info");

                    // Get the complete question with choices
                    var completeQuestion = await _cauhoiRepository.GetWithChoicesAsync(savedQuestion.Id);
                    var result = _mapper.Map<CauhoiDto>(completeQuestion);

                    return new BaseResponseDto<CauhoiDto>
                    {
                        Success = true,
                        Message = "Tạo câu hỏi thành công",
                        Data = result
                    };
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating question for user {UserId}", nguoiTao);
                
                await _securityService.LogSecurityEventAsync("QUESTION_CREATE_ERROR", 
                    $"System error creating question for user {nguoiTao}: {ex.Message}", 
                    nguoiTao, "High");

                return new BaseResponseDto<CauhoiDto>
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi tạo câu hỏi",
                    Errors = new List<string> { "Lỗi hệ thống" }
                };
            }
        }

        public async Task<BaseResponseDto<CauhoiDto>> UpdateQuestionAsync(UpdateCauhoiDto updateDto, int nguoiCapNhat)
        {
            try
            {
                _logger.LogInformation("Updating question {QuestionId} by user {UserId}", updateDto.Id, nguoiCapNhat);

                // OWASP A01: Broken Access Control - Verify question exists
                var existingQuestion = await _cauhoiRepository.GetWithChoicesAsync(updateDto.Id);
                if (existingQuestion == null)
                {
                    return new BaseResponseDto<CauhoiDto>
                    {
                        Success = false,
                        Message = "Không tìm thấy câu hỏi"
                    };
                }

                // OWASP A03: Injection - Input validation
                if (string.IsNullOrWhiteSpace(updateDto.NoiDung))
                {
                    return new BaseResponseDto<CauhoiDto>
                    {
                        Success = false,
                        Message = "Nội dung câu hỏi không được để trống"
                    };
                }

                // Kiểm tra câu hỏi trùng nội dung cho update (tránh trùng với câu hỏi khác)
                var noiDungChuan = updateDto.NoiDung.Trim().ToLower();
                var duplicate = await _cauhoiRepository.FindDuplicateAsync(noiDungChuan, updateDto.KhoaPhong);
                if (duplicate != null && duplicate.Id != updateDto.Id)
                {
                    return new BaseResponseDto<CauhoiDto>
                    {
                        Success = false,
                        Message = $"Câu hỏi đã tồn tại trong ngân hàng (Id: {duplicate.Id}): \"{duplicate.NoiDung}\""
                    };
                }

                // OWASP A04: Insecure Design - Transaction integrity
                using var transaction = await _cauhoiRepository.BeginTransactionAsync();
                try
                {
                    // Update question
                    existingQuestion.NoiDung = SanitizeHtmlContent(updateDto.NoiDung);
                    existingQuestion.IdLoaiCauHoi = updateDto.IdLoaiCauHoi ?? existingQuestion.IdLoaiCauHoi ?? 1;
                    existingQuestion.DoKho = MapDifficultyToDb(updateDto.DoKho ?? updateDto.MucDo);
                    existingQuestion.HinhAnh = updateDto.HinhAnh;
                    existingQuestion.KhoaPhong = updateDto.KhoaPhong;
                    existingQuestion.NguoiCapNhat = nguoiCapNhat;
                    existingQuestion.NgayCapNhat = DateTime.Now;

                    await _cauhoiRepository.UpdateAsync(existingQuestion);

                    // Update choices if provided
                    if (updateDto.DanhSachLuaChon != null && updateDto.DanhSachLuaChon.Any())
                    {
                        // Remove existing choices
                        await _luachonRepository.DeleteByQuestionIdAsync(updateDto.Id);

                        // Add new choices
                        foreach (var choiceDto in updateDto.DanhSachLuaChon)
                        {
                            var luachon = new Luachon
                            {
                                IdCauHoi = updateDto.Id,
                                NoiDung = SanitizeHtmlContent(choiceDto.NoiDung),
                                ThuTu = choiceDto.ThuTu,
                                LaDapAnDung = choiceDto.LaDapAnDung
                            };

                            await _luachonRepository.AddAsync(luachon);
                        }
                    }

                    await transaction.CommitAsync();

                    await _securityService.LogSecurityEventAsync("QUESTION_UPDATED", 
                        $"User {nguoiCapNhat} updated question {updateDto.Id}", 
                        nguoiCapNhat, "Info");

                    // Get the updated question
                    var updatedQuestion = await _cauhoiRepository.GetWithChoicesAsync(updateDto.Id);
                    var result = _mapper.Map<CauhoiDto>(updatedQuestion);

                    return new BaseResponseDto<CauhoiDto>
                    {
                        Success = true,
                        Message = "Cập nhật câu hỏi thành công",
                        Data = result
                    };
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating question {QuestionId} by user {UserId}", updateDto.Id, nguoiCapNhat);
                
                await _securityService.LogSecurityEventAsync("QUESTION_UPDATE_ERROR", 
                    $"System error updating question {updateDto.Id} by user {nguoiCapNhat}: {ex.Message}", 
                    nguoiCapNhat, "High");

                return new BaseResponseDto<CauhoiDto>
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi cập nhật câu hỏi",
                    Errors = new List<string> { "Lỗi hệ thống" }
                };
            }
        }

        public async Task<BaseResponseDto> DeleteQuestionAsync(int id, int nguoiCapNhat)
        {
            try
            {
                _logger.LogInformation("Deleting question {QuestionId} by user {UserId}", id, nguoiCapNhat);

                // OWASP A01: Broken Access Control - Verify question exists
                var question = await _cauhoiRepository.GetByIdAsync(id);
                if (question == null)
                {
                    return new BaseResponseDto
                    {
                        Success = false,
                        Message = "Không tìm thấy câu hỏi"
                    };
                }

                // Soft delete - mark as deleted instead of hard delete
                question.DaXoa = true;
                question.NguoiCapNhat = nguoiCapNhat;
                question.NgayCapNhat = DateTime.Now;

                await _cauhoiRepository.UpdateAsync(question);

                await _securityService.LogSecurityEventAsync("QUESTION_DELETED", 
                    $"User {nguoiCapNhat} deleted question {id}", 
                    nguoiCapNhat, "Info");

                return new BaseResponseDto
                {
                    Success = true,
                    Message = "Xóa câu hỏi thành công"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting question {QuestionId} by user {UserId}", id, nguoiCapNhat);
                
                await _securityService.LogSecurityEventAsync("QUESTION_DELETE_ERROR", 
                    $"System error deleting question {id} by user {nguoiCapNhat}: {ex.Message}", 
                    nguoiCapNhat, "High");

                return new BaseResponseDto
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi xóa câu hỏi",
                    Errors = new List<string> { "Lỗi hệ thống" }
                };
            }
        }

        public async Task<BaseResponseDto<List<CauhoiDto>>> ImportQuestionsFromExcelAsync(IFormFile file, int nguoiTao, string khoaPhong, int idLoaiCauHoi, bool isExamImport = false, int? expectedCount = null)
        {
            try
            {
                _logger.LogInformation("Importing questions from Excel for user {UserId}, khoa: {Khoa}, loai: {Loai}", nguoiTao, khoaPhong, idLoaiCauHoi);

                // OWASP A08: Software and Data Integrity Failures - File validation
                if (file == null || file.Length == 0)
                {
                    return new BaseResponseDto<List<CauhoiDto>>
                    {
                        Success = false,
                        Message = "File không hợp lệ"
                    };
                }

                // Validate file type
                var allowedExtensions = new[] { ".xlsx", ".xls" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return new BaseResponseDto<List<CauhoiDto>>
                    {
                        Success = false,
                        Message = "Chỉ hỗ trợ file Excel (.xlsx, .xls)"
                    };
                }

                // Validate file size (max 10MB)
                if (file.Length > 10 * 1024 * 1024)
                {
                    return new BaseResponseDto<List<CauhoiDto>>
                    {
                        Success = false,
                        Message = "File quá lớn (tối đa 10MB)"
                    };
                }

                // Lấy loại câu hỏi từ DB để xác định Strategy
                var loaiCauHoi = await _loaiRepository.GetByIdAsync(idLoaiCauHoi);
                if (loaiCauHoi == null)
                {
                    return new BaseResponseDto<List<CauhoiDto>>
                    {
                        Success = false,
                        Message = "Loại câu hỏi không tồn tại trong hệ thống."
                    };
                }

                IQuestionImportStrategy strategy;
                try
                {
                    // Dùng TenLoai từ DB thay vì hardcode ID — an toàn khi xóa/tạo lại loại câu hỏi
                    strategy = _strategyFactory.GetStrategy(loaiCauHoi.TenLoai ?? "");
                }
                catch (Exception ex)
                {
                    return new BaseResponseDto<List<CauhoiDto>>
                    {
                        Success = false,
                        Message = ex.Message
                    };
                }

                var errors = new List<string>();
                var importedQuestions = new List<CauhoiDto>();

                using (var stream = file.OpenReadStream())
                using (var workbook = new ClosedXML.Excel.XLWorkbook(stream))
                {
                    var worksheet = workbook.Worksheets.FirstOrDefault(ws =>
                                        ws.Name.Equals("IMPORT_CAU_HOI", StringComparison.OrdinalIgnoreCase))
                                    ?? workbook.Worksheets.First(ws =>
                                        !ws.Name.Equals("HUONG_DAN", StringComparison.OrdinalIgnoreCase));

                    // Gọi strategy để phân tích cú pháp
                    var parsedEntities = await strategy.ParseAndValidateAsync(worksheet, nguoiTao, khoaPhong, errors, isExamImport);

                    // Kiểm tra số lượng hợp lệ (yêu cầu từ user: nếu không đúng phải thông báo lỗi ngay)
                    if (expectedCount.HasValue && parsedEntities.Count != expectedCount.Value)
                    {
                        var msg = $"Số lượng câu hỏi trong file Excel ({parsedEntities.Count} câu hợp lệ) không khớp với cấu hình kỳ thi ({expectedCount.Value} câu).";
                        if (errors.Any())
                        {
                            msg += $" Có lỗi tại các dòng: {string.Join("; ", errors.Take(3))}...";
                        }
                        return new BaseResponseDto<List<CauhoiDto>>
                        {
                            Success = false,
                            Message = msg,
                            Data = null,
                            Errors = errors
                        };
                    }

                    // Nếu isExamImport = true và có bất kỳ lỗi nào, CHẶN lưu (strict mode)
                    if (isExamImport && errors.Any())
                    {
                        return new BaseResponseDto<List<CauhoiDto>>
                        {
                            Success = false,
                            Message = "File Excel có chứa dòng lỗi, không thể thêm vào đề thi. Vui lòng sửa lại file.",
                            Data = null,
                            Errors = errors
                        };
                    }

                    if (parsedEntities != null && parsedEntities.Any())
                    {
                        using var transaction = await _cauhoiRepository.BeginTransactionAsync();
                        try
                        {
                            foreach (var entity in parsedEntities)
                            {
                                // Thiết lập loại câu hỏi
                                entity.IdLoaiCauHoi = idLoaiCauHoi;
                                
                                // Clean HTML content
                                entity.NoiDung = SanitizeHtmlContent(entity.NoiDung);
                                if (entity.Luachons != null)
                                {
                                    foreach (var lc in entity.Luachons)
                                    {
                                        lc.NoiDung = SanitizeHtmlContent(lc.NoiDung);
                                    }
                                }
                                
                                // Lưu câu hỏi cùng với các lựa chọn (nếu có) thông qua AddAsync
                                var saved = await _cauhoiRepository.AddAsync(entity);
                                
                                var fullQuestion = await _cauhoiRepository.GetWithChoicesAsync(saved.Id);
                                if (fullQuestion != null)
                                {
                                    importedQuestions.Add(_mapper.Map<CauhoiDto>(fullQuestion));
                                }
                            }

                            await transaction.CommitAsync();
                        }
                        catch (Exception dbEx)
                        {
                            await transaction.RollbackAsync();
                            errors.Add($"Lỗi lưu cơ sở dữ liệu: {dbEx.Message}");
                        }
                    }
                }

                if (importedQuestions.Count == 0)
                {
                    var noDataMsg = errors.Any()
                        ? $"Import thất bại: 0 câu hỏi được thêm, {errors.Count} dòng lỗi."
                        : "Không tìm thấy dữ liệu hợp lệ trong file Excel. Vui lòng kiểm tra lại đúng template và nhập dữ liệu.";

                    await _securityService.LogSecurityEventAsync("QUESTION_IMPORT_EMPTY",
                        $"User {nguoiTao} imported 0 questions (empty or wrong sheet)",
                        nguoiTao, "Medium");

                    return new BaseResponseDto<List<CauhoiDto>>
                    {
                        Success = false,
                        Message = noDataMsg,
                        Data = importedQuestions,
                        Errors = errors
                    };
                }

                await _securityService.LogSecurityEventAsync("QUESTION_IMPORT_SUCCESS",
                    $"User {nguoiTao} imported {importedQuestions.Count} questions from Excel",
                    nguoiTao, "Info");

                return new BaseResponseDto<List<CauhoiDto>>
                {
                    Success = true,
                    Message = $"Import thành công {importedQuestions.Count} câu hỏi" + (errors.Any() ? $", {errors.Count} dòng lỗi" : ""),
                    Data = importedQuestions,
                    Errors = errors
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing questions from Excel for user {UserId}", nguoiTao);
                
                await _securityService.LogSecurityEventAsync("QUESTION_IMPORT_ERROR", 
                    $"System error importing questions for user {nguoiTao}: {ex.Message}", 
                    nguoiTao, "High");

                return new BaseResponseDto<List<CauhoiDto>>
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi import câu hỏi",
                    Errors = new List<string> { ex.Message, ex.InnerException?.Message ?? "" }
                };
            }
        }

        public async Task<BaseResponseDto<byte[]>> DownloadImportTemplateAsync(int idLoaiCauHoi, bool isExamImport = false)
        {
            try
            {
                var loaiCauHoi = await _loaiRepository.GetByIdAsync(idLoaiCauHoi);
                if (loaiCauHoi == null)
                {
                    return new BaseResponseDto<byte[]>
                    {
                        Success = false,
                        Message = "Loại câu hỏi không tồn tại."
                    };
                }

                IQuestionImportStrategy strategy;
                try
                {
                    // Dùng TenLoai từ DB thay vì hardcode ID — an toàn khi xóa/tạo lại loại câu hỏi
                    strategy = _strategyFactory.GetStrategy(loaiCauHoi.TenLoai ?? "");
                }
                catch (Exception ex)
                {
                    return new BaseResponseDto<byte[]>
                    {
                        Success = false,
                        Message = ex.Message
                    };
                }

                using var workbook = new ClosedXML.Excel.XLWorkbook();

                // 1. Sheet hướng dẫn
                var wsGuide = workbook.Worksheets.Add("HUONG_DAN");
                wsGuide.Cell("A1").Value = $"TEMPLATE IMPORT CÂU HỎI — LOẠI: {(loaiCauHoi.TenLoai ?? "").ToUpper()}";
                wsGuide.Cell("A1").Style.Font.Bold = true;
                wsGuide.Cell("A1").Style.Font.FontSize = 14;
                wsGuide.Cell("A1").Style.Font.FontColor = ClosedXML.Excel.XLColor.DarkBlue;

                wsGuide.Cell("A3").Value = "Chú ý:";
                wsGuide.Cell("A3").Style.Font.Bold = true;
                wsGuide.Cell("B3").Value = "Vui lòng nhập dữ liệu bắt đầu từ dòng số 2 của Sheet IMPORT_CAU_HOI.";

                wsGuide.Cell("A4").Value = "Khoa/Phòng:";
                wsGuide.Cell("A4").Style.Font.Bold = true;
                wsGuide.Cell("B4").Value = "Khoa/Phòng và Loại câu hỏi được chọn trực tiếp trên giao diện khi Import.";

                wsGuide.Column(1).Width = 20;
                wsGuide.Column(2).Width = 70;

                // 2. Sheet dữ liệu
                var ws = workbook.Worksheets.Add("IMPORT_CAU_HOI");
                strategy.GenerateTemplate(ws, isExamImport);

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                return new BaseResponseDto<byte[]>
                {
                    Success = true,
                    Message = "Tải template thành công",
                    Data = stream.ToArray()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi tạo file template cho loại câu hỏi ID: {IdLoai}", idLoaiCauHoi);
                return new BaseResponseDto<byte[]>
                {
                    Success = false,
                    Message = $"Lỗi khi tải template: {ex.Message}"
                };
            }
        }

        public async Task<BaseResponseDto<List<CauhoiDto>>> GetRandomQuestionsAsync(int count)
        {
            try
            {
                _logger.LogInformation("Getting {Count} random questions", count);

                // OWASP A04: Insecure Design - Limit random question count
                if (count > 100)
                {
                    count = 100; // Prevent DoS
                }

                if (count <= 0)
                {
                    return new BaseResponseDto<List<CauhoiDto>>
                    {
                        Success = false,
                        Message = "Số lượng câu hỏi phải lớn hơn 0"
                    };
                }

                var questions = await _cauhoiRepository.GetRandomQuestionsAsync(count);
                var result = _mapper.Map<List<CauhoiDto>>(questions);

                return new BaseResponseDto<List<CauhoiDto>>
                {
                    Success = true,
                    Message = "Lấy câu hỏi ngẫu nhiên thành công",
                    Data = result
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting random questions");
                
                await _securityService.LogSecurityEventAsync("RANDOM_QUESTIONS_ERROR", 
                    $"System error getting random questions: {ex.Message}", 
                    null, "Medium");

                return new BaseResponseDto<List<CauhoiDto>>
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi lấy câu hỏi ngẫu nhiên",
                    Errors = new List<string> { "Lỗi hệ thống" }
                };
            }
        }

        public async Task<bool> CheckDuplicateAsync(string noiDung, string? khoaPhong = null, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(noiDung)) return false;
            var noiDungChuan = noiDung.Trim().ToLower();
            var duplicate = await _cauhoiRepository.FindDuplicateAsync(noiDungChuan, khoaPhong);
            if (duplicate == null) return false;
            if (excludeId.HasValue && duplicate.Id == excludeId.Value) return false;
            return true;
        }

        #region Private Helper Methods

        /// <summary>
        /// OWASP A03: Injection - Sanitize HTML content to prevent XSS
        /// </summary>
        private string? SanitizeHtmlContent(string? content)
        {
            if (string.IsNullOrEmpty(content))
                return content;

            // Basic HTML sanitization - in production, use a proper HTML sanitizer like HtmlSanitizer
            return content
                .Replace("<script", "&lt;script")
                .Replace("</script>", "&lt;/script&gt;")
                .Replace("javascript:", "")
                .Replace("vbscript:", "")
                .Replace("onload=", "")
                .Replace("onerror=", "")
                .Replace("onclick=", "")
                .Trim();
        }

        private string? MapDifficultyToDb(string? doKho)
        {
            if (string.IsNullOrEmpty(doKho)) return "1";
            var d = doKho.Trim().ToLower();
            if (d == "3" || d == "k" || d.Contains("khó") || d.Contains("kho")) return "3";
            if (d == "2" || d == "tb" || d.Contains("trung bình") || d.Contains("trung binh") || d.Contains("trungbinh")) return "2";
            return "1";
        }

        #endregion
    }
}