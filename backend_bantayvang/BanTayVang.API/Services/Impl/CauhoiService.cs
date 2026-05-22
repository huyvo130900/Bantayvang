using AutoMapper;
using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Question;
using BanTayVang.API.Models;
using BanTayVang.API.Repositories.Interfaces;
using BanTayVang.API.Services.Interfaces;
using BanTayVang.API.Services.Interfaces.Security;
using BanTayVang.API.Services.Interfaces.Validation;
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

        public CauhoiService(
            ICauhoiRepository cauhoiRepository,
            ILuachonRepository luachonRepository,
            IExamSecurityService securityService,
            IMapper mapper,
            ILogger<CauhoiService> logger)
        {
            _cauhoiRepository = cauhoiRepository ?? throw new ArgumentNullException(nameof(cauhoiRepository));
            _luachonRepository = luachonRepository ?? throw new ArgumentNullException(nameof(luachonRepository));
            _securityService = securityService ?? throw new ArgumentNullException(nameof(securityService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

                if (createDto.DanhSachLuaChon == null || createDto.DanhSachLuaChon.Count < 2)
                {
                    return new BaseResponseDto<CauhoiDto>
                    {
                        Success = false,
                        Message = "Câu hỏi phải có ít nhất 2 lựa chọn"
                    };
                }

                // OWASP A04: Insecure Design - Transaction integrity
                using var transaction = await _cauhoiRepository.BeginTransactionAsync();
                try
                {
                    var cauhoi = new Cauhoi
                    {
                        NoiDung = SanitizeHtmlContent(createDto.NoiDung),
                        IdLoaiCauHoi = createDto.IdLoaiCauHoi,
                        DoKho = createDto.DoKho ?? createDto.MucDo,
                        Diem = createDto.Diem,
                        HinhAnh = createDto.HinhAnh,
                        IdDanhMuc = createDto.IdDanhMuc,
                        NguoiTao = nguoiTao,
                        NgayTao = DateTime.Now,
                        DaXoa = false,
                        KhoaPhong = createDto.KhoaPhong
                    };

                    var savedQuestion = await _cauhoiRepository.AddAsync(cauhoi);

                    // Add choices
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

                // OWASP A04: Insecure Design - Transaction integrity
                using var transaction = await _cauhoiRepository.BeginTransactionAsync();
                try
                {
                    // Update question
                    existingQuestion.NoiDung = SanitizeHtmlContent(updateDto.NoiDung);
                    existingQuestion.IdLoaiCauHoi = updateDto.IdLoaiCauHoi;
                    existingQuestion.DoKho = updateDto.DoKho ?? updateDto.MucDo;
                    existingQuestion.Diem = updateDto.Diem;
                    existingQuestion.HinhAnh = updateDto.HinhAnh;
                    existingQuestion.IdDanhMuc = updateDto.IdDanhMuc;
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

        public async Task<BaseResponseDto<List<CauhoiDto>>> ImportQuestionsFromExcelAsync(IFormFile file, int nguoiTao)
        {
            try
            {
                _logger.LogInformation("Importing questions from Excel for user {UserId}", nguoiTao);

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

                var importedQuestions = new List<CauhoiDto>();
                var errors = new List<string>();

                using var stream = file.OpenReadStream();
                using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
                var worksheet = workbook.Worksheets.First();
                var rows = worksheet.RowsUsed().Skip(1); // Skip header

                int rowNumber = 1;
                foreach (var row in rows)
                {
                    rowNumber++;
                    try
                    {
                        var noiDung = row.Cell(1).GetString().Trim();
                        if (string.IsNullOrWhiteSpace(noiDung))
                            continue;

                        var diem = row.Cell(2).GetValue<double?>() ?? 1.0;
                        var doKho = row.Cell(3).GetString().Trim();
                        var idDanhMuc = row.Cell(4).GetValue<int?>() ?? 1;
                        var idLoaiCauHoi = row.Cell(5).GetValue<int?>() ?? 1;

                        // Choices in columns 6-9, correct answer index in column 10 (1-4)
                        var choices = new List<Luachon>();
                        for (int i = 0; i < 4; i++)
                        {
                            var choiceContent = row.Cell(6 + i).GetString().Trim();
                            if (!string.IsNullOrWhiteSpace(choiceContent))
                                choices.Add(new Luachon { NoiDung = choiceContent, ThuTu = i + 1, LaDapAnDung = false });
                        }

                        if (choices.Count < 2)
                        {
                            errors.Add($"Dòng {rowNumber}: Câu hỏi phải có ít nhất 2 lựa chọn");
                            continue;
                        }

                        var correctIndex = row.Cell(10).GetValue<int?>() ?? 1;
                        if (correctIndex < 1 || correctIndex > choices.Count)
                        {
                            errors.Add($"Dòng {rowNumber}: Đáp án đúng không hợp lệ");
                            continue;
                        }
                        choices[correctIndex - 1].LaDapAnDung = true;

                        // Save question
                        var cauhoi = new Cauhoi
                        {
                            NoiDung = SanitizeHtmlContent(noiDung),
                            Diem = diem,
                            DoKho = doKho,
                            IdDanhMuc = idDanhMuc,
                            IdLoaiCauHoi = idLoaiCauHoi,
                            NguoiTao = nguoiTao,
                            NgayTao = DateTime.Now,
                            DaXoa = false,
                            KhoaPhong = "CNTT"
                        };

                        var saved = await _cauhoiRepository.AddAsync(cauhoi);

                        foreach (var choice in choices)
                        {
                            choice.IdCauHoi = saved.Id;
                            await _luachonRepository.AddAsync(choice);
                        }

                        var fullQuestion = await _cauhoiRepository.GetWithChoicesAsync(saved.Id);
                        if (fullQuestion != null)
                            importedQuestions.Add(_mapper.Map<CauhoiDto>(fullQuestion));
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Dòng {rowNumber}: {ex.Message}");
                    }
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

        public async Task<BaseResponseDto<List<CauhoiDto>>> GetRandomQuestionsAsync(int count, int? danhMucId = null)
        {
            try
            {
                _logger.LogInformation("Getting {Count} random questions from category {CategoryId}", count, danhMucId);

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

                var questions = await _cauhoiRepository.GetRandomQuestionsAsync(count, danhMucId);
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

        #endregion
    }
}