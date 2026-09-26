using AutoMapper;
using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Question;
using BanTayVang.API.Helpers;
using BanTayVang.API.Models;
using BanTayVang.API.Repositories.Interfaces;
using BanTayVang.API.Services.Interfaces;
using BanTayVang.API.Services.Interfaces.Security;
using BanTayVang.API.Services.Interfaces.Validation;
using BanTayVang.API.Services.Interfaces.Import;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace BanTayVang.API.Services.Impl
{
    /// <summary>
    /// Question service implementation following SOLID principles and OWASP security
    /// </summary>
    public class QuestionService : IQuestionService
    {
        private readonly IQuestionRepository _questionRepository;
        private readonly IQuestionOptionRepository _questionOptionRepository;
        private readonly IExamSecurityService _securityService;
        private readonly IMapper _mapper;
        private readonly ILogger<QuestionService> _logger;
        private readonly IQuestionImportStrategyFactory _strategyFactory;
        private readonly IQuestionCategoryRepository _categoryRepository;
        private readonly IWordQuestionImportService _wordImportService;

        public QuestionService(
            IQuestionRepository questionRepository,
            IQuestionOptionRepository questionOptionRepository,
            IExamSecurityService securityService,
            IMapper mapper,
            ILogger<QuestionService> logger,
            IQuestionImportStrategyFactory strategyFactory,
            IQuestionCategoryRepository loaiRepository,
            IWordQuestionImportService wordImportService)
        {
            _questionRepository = questionRepository ?? throw new ArgumentNullException(nameof(questionRepository));
            _questionOptionRepository = questionOptionRepository ?? throw new ArgumentNullException(nameof(questionOptionRepository));
            _securityService = securityService ?? throw new ArgumentNullException(nameof(securityService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _strategyFactory = strategyFactory ?? throw new ArgumentNullException(nameof(strategyFactory));
            _categoryRepository = loaiRepository ?? throw new ArgumentNullException(nameof(loaiRepository));
            _wordImportService = wordImportService ?? throw new ArgumentNullException(nameof(wordImportService));
        }

        public async Task<BaseResponseDto<PagedResultDto<QuestionDto>>> GetFilteredQuestionsAsync(QuestionFilterDto filter)
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

                var questions = await _questionRepository.GetFilteredAsync(filter);
                var totalCount = await _questionRepository.GetFilteredCountAsync(filter);

                var questionDtos = _mapper.Map<List<QuestionDto>>(questions.Items);

                var result = new PagedResultDto<QuestionDto>
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

                return new BaseResponseDto<PagedResultDto<QuestionDto>>
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

                return new BaseResponseDto<PagedResultDto<QuestionDto>>
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi lấy danh sách câu hỏi",
                    Errors = new List<string> { ex.Message, ex.InnerException?.Message ?? "" }
                };
            }
        }

        public async Task<BaseResponseDto<QuestionDto>> GetQuestionByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Getting question by ID: {QuestionId}", id);

                // OWASP A01: Broken Access Control - Input validation
                if (id <= 0)
                {
                    return new BaseResponseDto<QuestionDto>
                    {
                        Success = false,
                        Message = "ID câu hỏi không hợp lệ"
                    };
                }

                var question = await _questionRepository.GetWithChoicesAsync(id);
                if (question == null)
                {
                    return new BaseResponseDto<QuestionDto>
                    {
                        Success = false,
                        Message = "Không tìm thấy câu hỏi"
                    };
                }

                var result = _mapper.Map<QuestionDto>(question);
                
                return new BaseResponseDto<QuestionDto>
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

                return new BaseResponseDto<QuestionDto>
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi lấy thông tin câu hỏi",
                    Errors = new List<string> { "Lỗi hệ thống" }
                };
            }
        }

        public async Task<BaseResponseDto<QuestionDto>> CreateQuestionAsync(CreateQuestionDto createDto, int createdBy)
        {
            try
            {
                _logger.LogInformation("Creating question for user {UserId}", createdBy);

                // OWASP A03: Injection - Input validation and sanitization
                if (string.IsNullOrWhiteSpace(createDto.Content))
                {
                    return new BaseResponseDto<QuestionDto>
                    {
                        Success = false,
                        Message = "Nội dung câu hỏi không được để trống"
                    };
                }

                var loaiId = createDto.QuestionCategoryId ?? 1;
                bool requiresChoices = true;
                if (loaiId > 0)
                {
                    var loai = await _categoryRepository.GetByIdAsync(loaiId);
                    // BUG FIX: previously only matched CategoryName containing "tự luận"/"tu luan",
                    // which missed the common abbreviated code "TL" used in seed/real data.
                    // Use the centralized EssayQuestionHelper (same list the frontend already
                    // relies on for MC/essay detection) so "TL" and other recognised essay
                    // codes correctly skip the "at least 2 choices" requirement.
                    if (loai != null && EssayQuestionHelper.IsEssayCategory(loai.CategoryName))
                    {
                        requiresChoices = false;
                    }
                }

                if (requiresChoices && (createDto.Options == null || createDto.Options.Count < 2))
                {
                    return new BaseResponseDto<QuestionDto>
                    {
                        Success = false,
                        Message = "Câu hỏi trắc nghiệm phải có ít nhất 2 lựa chọn"
                    };
                }

                // Kiểm tra câu hỏi trùng nội dung (so sánh không phân biệt hoa thường, bỏ khoảng trắng thừa)
                var standardizedContent = createDto.Content.Trim().ToLower();
                var duplicate = await _questionRepository.FindDuplicateAsync(standardizedContent, createDto.Department);
                if (duplicate != null)
                {
                    return new BaseResponseDto<QuestionDto>
                    {
                        Success = false,
                        Message = $"Câu hỏi đã tồn tại trong ngân hàng (Id: {duplicate.Id}): \"{duplicate.Content}\""
                    };
                }

                // OWASP A04: Insecure Design - Transaction integrity
                using var transaction = await _questionRepository.BeginTransactionAsync();
                try
                {
                    var question = new Question
                    {
                        Content = SanitizeHtmlContent(createDto.Content),
                        QuestionCategoryId = loaiId,
                        Difficulty = MapDifficultyToDb(createDto.Difficulty ?? createDto.Level),
                        ImageUrl = createDto.ImageUrl,
                        CreatedBy = createdBy,
                        CreatedAt = DateTime.UtcNow.AddHours(7),
                        IsDeleted = false,
                        Department = createDto.Department,
                        // BUG FIX: the question-bank UI stores the Tự luận "Đáp án chuẩn" text as
                        // Options[0].Content (see question-form-dialog.tsx), but the grading screens
                        // (bulk-essay-grading-page.tsx, result-detail-dialog.tsx) only ever read
                        // Question.SuggestedAnswer to display the model answer to the grader.
                        // Excel/Word import already sets SuggestedAnswer directly, but this
                        // create path never mirrored it, so a Tự luận question created from the
                        // question bank silently had NO visible "Đáp án chuẩn" during grading,
                        // even though the save itself succeeded.
                        SuggestedAnswer = !requiresChoices
                            ? (createDto.Options?.FirstOrDefault()?.Content ?? createDto.SuggestedAnswer)
                            : createDto.SuggestedAnswer
                    };

                    var savedQuestion = await _questionRepository.AddAsync(question);

                    // BUG FIX (confirmed live, corrupted 2 real questions in production data):
                    // the question-bank UI always submits Options[0].Content as the carrier for a
                    // Tự luận question's "Đáp án chuẩn" (see the SuggestedAnswer assignment above
                    // and question-form-dialog.tsx, which binds that textarea directly to
                    // `options.0.content`) - react-hook-form materializes that path into a real
                    // array entry the moment the field is touched, even when left blank. This loop
                    // used to persist THAT entry as a genuine QuestionOption (IsCorrect=true) for
                    // every essay question created through the UI, silently turning a 0-option
                    // essay question into a fake 1-option "multiple choice" answer key the instant
                    // anyone typed a reference answer. Only persist real choices when the question
                    // actually requires them (i.e. is not essay).
                    if (requiresChoices && createDto.Options != null)
                    {
                        foreach (var choiceDto in createDto.Options)
                        {
                            var questionOption = new QuestionOption
                            {
                                QuestionId = savedQuestion.Id,
                                Content = SanitizeHtmlContent(choiceDto.Content),
                                OrderIndex = choiceDto.OrderIndex,
                                IsCorrect = choiceDto.IsCorrect
                            };

                            await _questionOptionRepository.AddAsync(questionOption);
                        }
                    }

                    await transaction.CommitAsync();

                    await _securityService.LogSecurityEventAsync("QUESTION_CREATED", 
                        $"User {createdBy} created question {savedQuestion.Id}", 
                        createdBy, "Info");

                    // Get the complete question with choices
                    var completeQuestion = await _questionRepository.GetWithChoicesAsync(savedQuestion.Id);
                    var result = _mapper.Map<QuestionDto>(completeQuestion);

                    return new BaseResponseDto<QuestionDto>
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
                _logger.LogError(ex, "Error creating question for user {UserId}", createdBy);
                
                await _securityService.LogSecurityEventAsync("QUESTION_CREATE_ERROR", 
                    $"System error creating question for user {createdBy}: {ex.Message}", 
                    createdBy, "High");

                return new BaseResponseDto<QuestionDto>
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi tạo câu hỏi",
                    Errors = new List<string> { "Lỗi hệ thống" }
                };
            }
        }

        public async Task<BaseResponseDto<QuestionDto>> UpdateQuestionAsync(UpdateQuestionDto updateDto, int updatedBy)
        {
            try
            {
                _logger.LogInformation("Updating question {QuestionId} by user {UserId}", updateDto.Id, updatedBy);

                // OWASP A01: Broken Access Control - Verify question exists
                var existingQuestion = await _questionRepository.GetWithChoicesAsync(updateDto.Id);
                if (existingQuestion == null)
                {
                    return new BaseResponseDto<QuestionDto>
                    {
                        Success = false,
                        Message = "Không tìm thấy câu hỏi"
                    };
                }

                // OWASP A03: Injection - Input validation
                if (string.IsNullOrWhiteSpace(updateDto.Content))
                {
                    return new BaseResponseDto<QuestionDto>
                    {
                        Success = false,
                        Message = "Nội dung câu hỏi không được để trống"
                    };
                }

                // Kiểm tra câu hỏi trùng nội dung cho update (tránh trùng với câu hỏi khác)
                var standardizedContent = updateDto.Content.Trim().ToLower();
                var duplicate = await _questionRepository.FindDuplicateAsync(standardizedContent, updateDto.Department);
                if (duplicate != null && duplicate.Id != updateDto.Id)
                {
                    return new BaseResponseDto<QuestionDto>
                    {
                        Success = false,
                        Message = $"Câu hỏi đã tồn tại trong ngân hàng (Id: {duplicate.Id}): \"{duplicate.Content}\""
                    };
                }

                var newCategoryId = updateDto.QuestionCategoryId ?? existingQuestion.QuestionCategoryId ?? 1;
                // BUG FIX: look up the (possibly changed) category so we know whether this
                // is a Tự luận question - needed below to mirror the "Đáp án chuẩn" text
                // into SuggestedAnswer, the same way CreateQuestionAsync now does.
                bool isEssayCategory = false;
                if (newCategoryId > 0)
                {
                    var loai = await _categoryRepository.GetByIdAsync(newCategoryId);
                    isEssayCategory = loai != null && EssayQuestionHelper.IsEssayCategory(loai.CategoryName);
                }

                // BUG FIX: CreateQuestionAsync has always required >=2 options for a non-essay
                // category, but this method never had the equivalent check - changing an existing
                // question's category away from essay without also submitting a fresh Options list
                // (e.g. a direct API call, bypassing the question-bank UI form which auto-fills 4
                // blank options whenever it detects the category flip) would silently leave a
                // "Trắc nghiệm"-categorized question with 0 options. Every scoring code path
                // (EssayQuestionHelper.IsEssay) treats a 0-option question as an essay regardless
                // of its category, so such a question would never appear in the essay-grading queue
                // (which matches by category name only) and would score 0 forever with no way to fix it.
                if (!isEssayCategory)
                {
                    var resultingOptionCount = (updateDto.Options != null && updateDto.Options.Any())
                        ? updateDto.Options.Count
                        : existingQuestion.QuestionOptions?.Count ?? 0;
                    if (resultingOptionCount < 2)
                    {
                        return new BaseResponseDto<QuestionDto>
                        {
                            Success = false,
                            Message = "Câu hỏi trắc nghiệm phải có ít nhất 2 lựa chọn"
                        };
                    }
                }

                // OWASP A04: Insecure Design - Transaction integrity
                using var transaction = await _questionRepository.BeginTransactionAsync();
                try
                {
                    // Update question
                    existingQuestion.Content = SanitizeHtmlContent(updateDto.Content);
                    existingQuestion.QuestionCategoryId = newCategoryId;
                    existingQuestion.Difficulty = MapDifficultyToDb(updateDto.Difficulty ?? updateDto.Level);
                    existingQuestion.ImageUrl = updateDto.ImageUrl;
                    existingQuestion.Department = updateDto.Department;
                    existingQuestion.UpdatedBy = updatedBy;
                    existingQuestion.UpdatedAt = DateTime.UtcNow.AddHours(7);
                    // BUG FIX: see matching comment in CreateQuestionAsync - the question-bank UI
                    // saves the Tự luận "Đáp án chuẩn" into Options[0].Content, but grading only
                    // reads SuggestedAnswer, which this method never updated. This is the concrete
                    // bug behind "sửa câu hỏi tự luận xong nhưng không thấy đáp án khi chấm bài":
                    // the PUT succeeded every time, but the answer never reached the field grading
                    // actually reads.
                    // Guard against blanking out a good SuggestedAnswer (e.g. one set by Excel/Word
                    // import, where Options is empty) when the UI form round-trips an empty
                    // Options[0].Content - only overwrite when a non-empty value is actually supplied.
                    var essayAnswerFromOptions = updateDto.Options?.FirstOrDefault()?.Content;
                    var newSuggestedAnswer = !string.IsNullOrWhiteSpace(essayAnswerFromOptions)
                        ? essayAnswerFromOptions
                        : updateDto.SuggestedAnswer;
                    existingQuestion.SuggestedAnswer = !string.IsNullOrWhiteSpace(newSuggestedAnswer)
                        ? newSuggestedAnswer
                        : (isEssayCategory ? existingQuestion.SuggestedAnswer : newSuggestedAnswer);

                    await _questionRepository.UpdateAsync(existingQuestion);

                    // BUG FIX (confirmed live, corrupted 2 real questions in production data -
                    // #83 and #85): same root cause as CreateQuestionAsync above - the UI always
                    // submits Options[0].Content as the carrier for a Tự luận question's "Đáp án
                    // chuẩn", and this used to persist THAT as a real QuestionOption whenever the
                    // list was non-empty (which it always is for essay, even with blank content -
                    // react-hook-form materializes `options.0.content` into a real array entry the
                    // moment that field exists on the form). Result: opening any essay question's
                    // edit dialog and saving silently created a fake 1-option "trắc nghiệm" answer
                    // key for it. Essay questions must never have real options - always clear
                    // (never insert) instead of following the submitted list.
                    if (isEssayCategory)
                    {
                        await _questionOptionRepository.DeleteByQuestionIdAsync(updateDto.Id);
                    }
                    else if (updateDto.Options != null && updateDto.Options.Any())
                    {
                        // Remove existing choices
                        await _questionOptionRepository.DeleteByQuestionIdAsync(updateDto.Id);

                        // Add new choices
                        foreach (var choiceDto in updateDto.Options)
                        {
                            var questionOption = new QuestionOption
                            {
                                QuestionId = updateDto.Id,
                                Content = SanitizeHtmlContent(choiceDto.Content),
                                OrderIndex = choiceDto.OrderIndex,
                                IsCorrect = choiceDto.IsCorrect
                            };

                            await _questionOptionRepository.AddAsync(questionOption);
                        }
                    }

                    await transaction.CommitAsync();

                    await _securityService.LogSecurityEventAsync("QUESTION_UPDATED", 
                        $"User {updatedBy} updated question {updateDto.Id}", 
                        updatedBy, "Info");

                    // Get the updated question
                    var updatedQuestion = await _questionRepository.GetWithChoicesAsync(updateDto.Id);
                    var result = _mapper.Map<QuestionDto>(updatedQuestion);

                    return new BaseResponseDto<QuestionDto>
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
                _logger.LogError(ex, "Error updating question {QuestionId} by user {UserId}", updateDto.Id, updatedBy);
                
                await _securityService.LogSecurityEventAsync("QUESTION_UPDATE_ERROR", 
                    $"System error updating question {updateDto.Id} by user {updatedBy}: {ex.Message}", 
                    updatedBy, "High");

                return new BaseResponseDto<QuestionDto>
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi cập nhật câu hỏi",
                    Errors = new List<string> { "Lỗi hệ thống" }
                };
            }
        }

        public async Task<BaseResponseDto> DeleteQuestionAsync(int id, int updatedBy)
        {
            try
            {
                _logger.LogInformation("Deleting question {QuestionId} by user {UserId}", id, updatedBy);

                // OWASP A01: Broken Access Control - Verify question exists
                var question = await _questionRepository.GetByIdAsync(id);
                if (question == null)
                {
                    return new BaseResponseDto
                    {
                        Success = false,
                        Message = "Không tìm thấy câu hỏi"
                    };
                }

                // Soft delete - mark as deleted instead of hard delete
                question.IsDeleted = true;
                question.UpdatedBy = updatedBy;
                question.UpdatedAt = DateTime.UtcNow.AddHours(7);

                await _questionRepository.UpdateAsync(question);

                await _securityService.LogSecurityEventAsync("QUESTION_DELETED", 
                    $"User {updatedBy} deleted question {id}", 
                    updatedBy, "Info");

                return new BaseResponseDto
                {
                    Success = true,
                    Message = "Xóa câu hỏi thành công"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting question {QuestionId} by user {UserId}", id, updatedBy);
                
                await _securityService.LogSecurityEventAsync("QUESTION_DELETE_ERROR", 
                    $"System error deleting question {id} by user {updatedBy}: {ex.Message}", 
                    updatedBy, "High");

                return new BaseResponseDto
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi xóa câu hỏi",
                    Errors = new List<string> { "Lỗi hệ thống" }
                };
            }
        }

        public async Task<BaseResponseDto<List<QuestionDto>>> PreviewQuestionsFromExcelAsync(IFormFile file, int createdBy, string department, int questionCategoryId)
        {
            try
            {
                if (file == null || file.Length == 0) return new BaseResponseDto<List<QuestionDto>> { Success = false, Message = "File không hợp lệ" };
                var allowedExtensions = new[] { ".xlsx", ".xls" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension)) return new BaseResponseDto<List<QuestionDto>> { Success = false, Message = "Chỉ hỗ trợ file Excel (.xlsx, .xls)" };

                var questionCategory = await _categoryRepository.GetByIdAsync(questionCategoryId);
                if (questionCategory == null) return new BaseResponseDto<List<QuestionDto>> { Success = false, Message = "Loại câu hỏi không tồn tại." };

                IQuestionImportStrategy strategy;
                try
                {
                    strategy = _strategyFactory.GetStrategy(questionCategory.CategoryName ?? "");
                }
                catch (Exception ex)
                {
                    return new BaseResponseDto<List<QuestionDto>> { Success = false, Message = ex.Message };
                }

                var errors = new List<string>();
                var parsedEntities = new List<Question>();

                using (var stream = file.OpenReadStream())
                using (var workbook = new ClosedXML.Excel.XLWorkbook(stream))
                {
                    var worksheet = workbook.Worksheets.FirstOrDefault(ws =>
                                        ws.Name.Equals("IMPORT_CAU_HOI", StringComparison.OrdinalIgnoreCase))
                                    ?? workbook.Worksheets.First(ws =>
                                        !ws.Name.Equals("HUONG_DAN", StringComparison.OrdinalIgnoreCase));

                    parsedEntities = await strategy.ParseAndValidateAsync(worksheet, createdBy, department, errors, false);
                }

                var dtos = parsedEntities.Select(q => new QuestionDto
                {
                    Id = 0,
                    Content = q.Content,
                    Difficulty = q.Difficulty == "3" ? "Khó" : q.Difficulty == "2" ? "Trung bình" : "Dễ",
                    Department = q.Department,
                    ImageUrl = q.ImageUrl,
                    SuggestedAnswer = q.SuggestedAnswer,
                    QuestionCategoryId = q.QuestionCategoryId,
                    Options = q.QuestionOptions?.Select(o => new QuestionOptionDto
                    {
                        Id = 0, Content = o.Content, IsCorrect = o.IsCorrect ?? false, OrderIndex = o.OrderIndex ?? 0
                    }).ToList() ?? new()
                }).ToList();

                return new BaseResponseDto<List<QuestionDto>>
                {
                    Success = true,
                    Message = errors.Count > 0 ? $"Xem trước xong ({errors.Count} lỗi)" : "Xem trước thành công",
                    Data = dtos,
                    Errors = errors
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi preview câu hỏi từ Excel");
                return new BaseResponseDto<List<QuestionDto>> { Success = false, Message = "Có lỗi xảy ra khi đọc file." };
            }
        }

        public async Task<BaseResponseDto<List<QuestionDto>>> ImportQuestionsFromExcelAsync(IFormFile file, int createdBy, string department, int questionCategoryId, bool isExamImport = false, int? expectedCount = null)
        {
            try
            {
                _logger.LogInformation("Importing questions from Excel for user {UserId}, khoa: {Khoa}, loai: {Loai}", createdBy, department, questionCategoryId);

                // OWASP A08: Software and Data Integrity Failures - File validation
                if (file == null || file.Length == 0)
                {
                    return new BaseResponseDto<List<QuestionDto>>
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
                    return new BaseResponseDto<List<QuestionDto>>
                    {
                        Success = false,
                        Message = "Chỉ hỗ trợ file Excel (.xlsx, .xls)"
                    };
                }

                // Validate file size (max 10MB)
                if (file.Length > 10 * 1024 * 1024)
                {
                    return new BaseResponseDto<List<QuestionDto>>
                    {
                        Success = false,
                        Message = "File quá lớn (tối đa 10MB)"
                    };
                }

                // Lấy loại câu hỏi từ DB để xác định Strategy
                var questionCategory = await _categoryRepository.GetByIdAsync(questionCategoryId);
                if (questionCategory == null)
                {
                    return new BaseResponseDto<List<QuestionDto>>
                    {
                        Success = false,
                        Message = "Loại câu hỏi không tồn tại trong hệ thống."
                    };
                }

                IQuestionImportStrategy strategy;
                try
                {
                    // Dùng CategoryName từ DB thay vì hardcode ID — an toàn khi xóa/tạo lại loại câu hỏi
                    strategy = _strategyFactory.GetStrategy(questionCategory.CategoryName ?? "");
                }
                catch (Exception ex)
                {
                    return new BaseResponseDto<List<QuestionDto>>
                    {
                        Success = false,
                        Message = ex.Message
                    };
                }

                var errors = new List<string>();
                var importedQuestions = new List<QuestionDto>();

                using (var stream = file.OpenReadStream())
                using (var workbook = new ClosedXML.Excel.XLWorkbook(stream))
                {
                    var worksheet = workbook.Worksheets.FirstOrDefault(ws =>
                                        ws.Name.Equals("IMPORT_CAU_HOI", StringComparison.OrdinalIgnoreCase))
                                    ?? workbook.Worksheets.First(ws =>
                                        !ws.Name.Equals("HUONG_DAN", StringComparison.OrdinalIgnoreCase));

                    // Gọi strategy để phân tích cú pháp
                    var parsedEntities = await strategy.ParseAndValidateAsync(worksheet, createdBy, department, errors, isExamImport);

                    // Kiểm tra số lượng hợp lệ (yêu cầu từ user: nếu không đúng phải thông báo lỗi ngay)
                    if (expectedCount.HasValue && parsedEntities.Count != expectedCount.Value)
                    {
                        var msg = $"Số lượng câu hỏi trong file Excel ({parsedEntities.Count} câu hợp lệ) không khớp với cấu hình kỳ thi ({expectedCount.Value} câu).";
                        if (errors.Any())
                        {
                            msg += $" Có lỗi tại các dòng: {string.Join("; ", errors.Take(3))}...";
                        }
                        return new BaseResponseDto<List<QuestionDto>>
                        {
                            Success = false,
                            Message = msg,
                            Data = null,
                            Errors = errors
                        };
                    }

                    // Nếu có bất kỳ lỗi nào, CHẶN lưu (strict mode) để tránh Partial Import
                    if (errors.Any())
                    {
                        return new BaseResponseDto<List<QuestionDto>>
                        {
                            Success = false,
                            Message = $"File Excel có {errors.Count} dòng lỗi, không thể thêm vào ngân hàng. Vui lòng sửa lại file.",
                            Data = null,
                            Errors = errors
                        };
                    }

                    if (parsedEntities != null && parsedEntities.Any())
                    {
                        using var transaction = await _questionRepository.BeginTransactionAsync();
                        try
                        {
                            foreach (var entity in parsedEntities)
                            {
                                // Thiết lập loại câu hỏi
                                entity.QuestionCategoryId = questionCategoryId;
                                
                                // Clean HTML content
                                entity.Content = SanitizeHtmlContent(entity.Content);
                                if (entity.QuestionOptions != null)
                                {
                                    foreach (var lc in entity.QuestionOptions)
                                    {
                                        lc.Content = SanitizeHtmlContent(lc.Content);
                                    }
                                }
                                
                                // Lưu câu hỏi cùng với các lựa chọn (nếu có) thông qua AddAsync
                                var saved = await _questionRepository.AddAsync(entity);
                                
                                var fullQuestion = await _questionRepository.GetWithChoicesAsync(saved.Id);
                                if (fullQuestion != null)
                                {
                                    importedQuestions.Add(_mapper.Map<QuestionDto>(fullQuestion));
                                }
                            }

                            await transaction.CommitAsync();
                        }
                        catch (Exception dbEx)
                        {
                            await transaction.RollbackAsync();
                            errors.Add($"Lỗi lưu cơ sở dữ liệu: {dbEx.Message}");
                            // BUG FIX: the transaction was rolled back, so NONE of the rows added to
                            // importedQuestions during the loop above actually exist in the DB anymore.
                            // Without this, the code below would see a non-empty importedQuestions list
                            // and report a false "Import thành công" even though 0 rows were saved.
                            importedQuestions.Clear();
                        }
                    }
                }

                if (importedQuestions.Count == 0)
                {
                    var noDataMsg = errors.Any()
                        ? $"Import thất bại: 0 câu hỏi được thêm, {errors.Count} dòng lỗi."
                        : "Không tìm thấy dữ liệu hợp lệ trong file Excel. Vui lòng kiểm tra lại đúng template và nhập dữ liệu.";

                    await _securityService.LogSecurityEventAsync("QUESTION_IMPORT_EMPTY",
                        $"User {createdBy} imported 0 questions (empty or wrong sheet)",
                        createdBy, "Medium");

                    return new BaseResponseDto<List<QuestionDto>>
                    {
                        Success = false,
                        Message = noDataMsg,
                        Data = importedQuestions,
                        Errors = errors
                    };
                }

                await _securityService.LogSecurityEventAsync("QUESTION_IMPORT_SUCCESS",
                    $"User {createdBy} imported {importedQuestions.Count} questions from Excel",
                    createdBy, "Info");

                return new BaseResponseDto<List<QuestionDto>>
                {
                    Success = true,
                    Message = $"Import thành công {importedQuestions.Count} câu hỏi" + (errors.Any() ? $", {errors.Count} dòng lỗi" : ""),
                    Data = importedQuestions,
                    Errors = errors
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing questions from Excel for user {UserId}", createdBy);
                
                await _securityService.LogSecurityEventAsync("QUESTION_IMPORT_ERROR", 
                    $"System error importing questions for user {createdBy}: {ex.Message}", 
                    createdBy, "High");

                return new BaseResponseDto<List<QuestionDto>>
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi import câu hỏi",
                    Errors = new List<string> { ex.Message, ex.InnerException?.Message ?? "" }
                };
            }
        }

        public async Task<BaseResponseDto<byte[]>> DownloadImportTemplateAsync(int questionCategoryId, bool isExamImport = false)
        {
            try
            {
                var questionCategory = await _categoryRepository.GetByIdAsync(questionCategoryId);
                if (questionCategory == null)
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
                    // Dùng CategoryName từ DB thay vì hardcode ID — an toàn khi xóa/tạo lại loại câu hỏi
                    strategy = _strategyFactory.GetStrategy(questionCategory.CategoryName ?? "");
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
                wsGuide.Cell("A1").Value = $"TEMPLATE IMPORT CÂU HỎI — LOẠI: {(questionCategory.CategoryName ?? "").ToUpper()}";
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
                _logger.LogError(ex, "Lỗi tạo file template cho loại câu hỏi ID: {IdLoai}", questionCategoryId);
                return new BaseResponseDto<byte[]>
                {
                    Success = false,
                    Message = $"Lỗi khi tải template: {ex.Message}"
                };
            }
        }

        public async Task<BaseResponseDto<List<QuestionDto>>> GetRandomQuestionsAsync(int count, int? categoryId = null)
        {
            try
            {
                _logger.LogInformation("Getting {Count} random questions (categoryId={CategoryId})", count, categoryId);

                // OWASP A04: Insecure Design - Limit random question count
                if (count > 100)
                {
                    count = 100; // Prevent DoS
                }

                if (count <= 0)
                {
                    return new BaseResponseDto<List<QuestionDto>>
                    {
                        Success = false,
                        Message = "Số lượng câu hỏi phải lớn hơn 0"
                    };
                }

                var questions = await _questionRepository.GetRandomQuestionsAsync(count, categoryId);
                var result = _mapper.Map<List<QuestionDto>>(questions);

                return new BaseResponseDto<List<QuestionDto>>
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

                return new BaseResponseDto<List<QuestionDto>>
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi lấy câu hỏi ngẫu nhiên",
                    Errors = new List<string> { "Lỗi hệ thống" }
                };
            }
        }

        public async Task<bool> CheckDuplicateAsync(string content, string? department = null, int? excludeId = null)
        {
            if (string.IsNullOrWhiteSpace(content)) return false;
            var standardizedContent = content.Trim().ToLower();
            var duplicate = await _questionRepository.FindDuplicateAsync(standardizedContent, department);
            if (duplicate == null) return false;
            if (excludeId.HasValue && duplicate.Id == excludeId.Value) return false;
            return true;
        }

        #region Private Helper Methods

        /// <summary>
        /// OWASP A03: Injection - Sanitize HTML content to prevent XSS
        /// </summary>
        // BUG FIX: the previous version used plain, CASE-SENSITIVE string.Replace() calls
        // (e.g. "<script", "onerror=") which <SCRIPT, OnError=, JavaScript: etc. all bypassed
        // completely. This is still not a full HTML sanitizer (a real allowlist-based library
        // like HtmlSanitizer/Ganss.Xss should replace this long-term - see original comment),
        // but it closes the trivial case-bypass and blocks more of the common XSS vectors
        // (iframe/object/embed tags, event-handler attributes, javascript:/vbscript:/data: URIs).
        private static readonly Regex DangerousTagPattern = new(
            @"<\s*(script|iframe|object|embed|svg|link|meta|style)\b[^>]*>",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex DangerousAttrOrProtocolPattern = new(
            @"(on\w+\s*=)|(javascript\s*:)|(vbscript\s*:)|(data\s*:\s*text/html)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private string? SanitizeHtmlContent(string? content)
        {
            if (string.IsNullOrEmpty(content))
                return content;

            // Basic HTML sanitization - in production, use a proper HTML sanitizer like HtmlSanitizer
            var sanitized = DangerousTagPattern.Replace(content, m => System.Net.WebUtility.HtmlEncode(m.Value));
            sanitized = DangerousAttrOrProtocolPattern.Replace(sanitized, "blocked:");
            return sanitized.Trim();
        }

        private string? MapDifficultyToDb(string? difficulty)
        {
            if (string.IsNullOrEmpty(difficulty)) return "1";
            var d = difficulty.Trim().ToLower();
            if (d == "3" || d == "k" || d.Contains("khó") || d.Contains("kho")) return "3";
            if (d == "2" || d == "tb" || d.Contains("trung bình") || d.Contains("trung binh") || d.Contains("trungbinh")) return "2";
            return "1";
        }

        #endregion

        #region Word Import

        public async Task<BaseResponseDto<List<QuestionDto>>> ImportQuestionsFromWordAsync(
            IFormFile file,
            int createdBy,
            string department,
            int questionCategoryId)
        {
            try
            {
                _logger.LogInformation("Importing questions from Word for user {UserId}, khoa: {Khoa}", createdBy, department);

                if (file == null || file.Length == 0)
                    return new BaseResponseDto<List<QuestionDto>> { Success = false, Message = "File không hợp lệ" };

                var ext = Path.GetExtension(file.FileName).ToLower();
                if (ext != ".docx")
                    return new BaseResponseDto<List<QuestionDto>> { Success = false, Message = "Chỉ hỗ trợ file .docx" };

                var errors = new List<string>();
                var questions = await _wordImportService.ParseAndValidateAsync(file, createdBy, department, questionCategoryId, errors);

                if (errors.Count > 0)
                    return new BaseResponseDto<List<QuestionDto>> { Success = false, Message = $"File Word có {errors.Count} lỗi. Không thể thêm câu hỏi vào ngân hàng. Vui lòng sửa lại file.\nChi tiết: {string.Join("; ", errors)}" };

                // BUG FIX: a document whose paragraphs don't match the "Câu N:" heading pattern at
                // all (wrong heading style, wrong numbering format, scanned/image-only content...)
                // parses to an empty question list with zero errors too - this used to fall through
                // to the save loop below (a no-op on an empty list) and return Success=true with
                // "Import thành công 0/0 câu hỏi từ Word.", which reads as a completed import to an
                // admin instead of a parsing failure they need to fix their document for.
                if (questions.Count == 0)
                    return new BaseResponseDto<List<QuestionDto>> { Success = false, Message = "Không tìm thấy câu hỏi nào trong file Word. Kiểm tra lại định dạng tiêu đề câu hỏi (vd: \"Câu 1:\")." };

                // Lưu câu hỏi hợp lệ vào DB với transaction
                var saved = new List<Question>();
                using var transaction = await _questionRepository.BeginTransactionAsync();
                try
                {
                    foreach (var q in questions)
                    {
                        // BUG FIX: the Excel import path (and CreateQuestionAsync/UpdateQuestionAsync)
                        // all sanitize Content/QuestionOptions[].Content through SanitizeHtmlContent
                        // before saving; this Word import path saved the parsed .docx text as-is.
                        // No current frontend renders question content as raw HTML (React escapes
                        // it), so this isn't exploitable through today's UI, but it was a real drift
                        // from the sanitization every other question-creation path already has -
                        // closing it here so Word-imported content gets the same treatment.
                        q.Content = SanitizeHtmlContent(q.Content);
                        if (q.QuestionOptions != null)
                        {
                            foreach (var opt in q.QuestionOptions)
                            {
                                opt.Content = SanitizeHtmlContent(opt.Content);
                            }
                        }

                        var created = await _questionRepository.AddAsync(q);
                        saved.Add(created);
                    }
                    await transaction.CommitAsync();
                }
                catch (Exception dbEx)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(dbEx, "Lỗi khi lưu câu hỏi Word vào DB");
                    string errorMsg = dbEx.InnerException != null ? dbEx.InnerException.Message : dbEx.Message;
                    return new BaseResponseDto<List<QuestionDto>> { Success = false, Message = $"Lỗi lưu CSDL: {errorMsg}" };
                }

                var dtos = saved.Select(q => _mapper.Map<QuestionDto>(q)).ToList();
                var message = $"Import thành công {saved.Count}/{questions.Count} câu hỏi từ Word.";
                if (errors.Count > 0) message += $" Có {errors.Count} lỗi: {string.Join("; ", errors)}";

                return new BaseResponseDto<List<QuestionDto>> { Success = true, Message = message, Data = dtos };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi import câu hỏi từ Word");
                return new BaseResponseDto<List<QuestionDto>> { Success = false, Message = $"Lỗi: {ex.Message}" };
            }
        }

        public Task<BaseResponseDto<byte[]>> DownloadWordTemplateAsync()
        {
            try
            {
                var bytes = _wordImportService.GenerateTemplateDocx();
                if (bytes.Length == 0)
                    return Task.FromResult(new BaseResponseDto<byte[]> { Success = false, Message = "Không tìm thấy file mẫu Word." });

                return Task.FromResult(new BaseResponseDto<byte[]> { Success = true, Data = bytes });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo template Word");
                return Task.FromResult(new BaseResponseDto<byte[]> { Success = false, Message = ex.Message });
            }
        }

        #endregion
    }
}
