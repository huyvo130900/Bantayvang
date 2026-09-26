using BanTayVang.API.DTOs.Common;
using BanTayVang.API.DTOs.Grading;
using BanTayVang.API.Helpers;
using BanTayVang.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BanTayVang.API.Services.Impl
{
    public class GradingService : Services.Interfaces.IGradingService
    {
        private readonly BanTayVangDbContext _context;
        private readonly ILogger<GradingService> _logger;
        private const double PassScore = 5.0;

        public GradingService(BanTayVangDbContext context, ILogger<GradingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<BaseResponseDto<ExamResultDetailDto>> GetResultDetailAsync(int examSubmissionId)
        {
            try
            {
                var examSubmission = await _context.ExamSubmissions
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                    .Include(b => b.User)
                    .Include(b => b.ExamPaper)
                    .Include(b => b.ExamCampaign)
                    .Include(b => b.SubmissionDetails)
                        .ThenInclude(c => c.Question)
                            .ThenInclude(ch => ch!.QuestionOptions)
                    .Include(b => b.SubmissionDetails)
                        .ThenInclude(c => c.Question)
                            .ThenInclude(ch => ch!.QuestionCategory)
                    .Include(b => b.SubmissionDetails)
                        .ThenInclude(c => c.SelectedOption)
                    .FirstOrDefaultAsync(b => b.Id == examSubmissionId);

                if (examSubmission == null)
                    return new BaseResponseDto<ExamResultDetailDto> { Success = false, Message = "Không tìm thấy bài thi" };

                var detail = MapToDetailDto(examSubmission);

                if (examSubmission.UserId != null && examSubmission.ExamPaperId != null)
                {
                    var attempts = await _context.ExamSubmissions
                        .AsNoTracking()
                        .Where(b => b.UserId == examSubmission.UserId && b.ExamPaperId == examSubmission.ExamPaperId && (b.Status == "Completed" || b.Id == examSubmission.Id))
                        .ToListAsync();

                    detail.AttemptCount = attempts.Count;
                    detail.CheatingCount = attempts.Sum(b => b.WarningCount ?? 0);
                    detail.RetakeCount = Math.Max(0, attempts.Count - 1);
                }

                return new BaseResponseDto<ExamResultDetailDto>
                {
                    Success = true,
                    Message = "Thành công",
                    Data = detail
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting result detail");
                return new BaseResponseDto<ExamResultDetailDto>
                {
                    Success = false,
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<BaseResponseDto<List<ExamResultDetailDto>>> GetResultsByExamAsync(int examId)
        {
            try
            {
                // Lấy TẤT CẢ bài thi của đề này (kể cả thi lại) để tính số lần thi
                var allBaithis = await _context.ExamSubmissions
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                    .Where(b => b.ExamPaperId == examId && b.Status == "Completed")
                    .Include(b => b.User)
                    .Include(b => b.ExamPaper)
                    .Include(b => b.ExamCampaign)
                    .OrderByDescending(b => b.SubmitTime)
                    .ToListAsync();

                // Group theo user, lấy bài thi gần nhất (điểm mới nhất)
                var latestPerUser = allBaithis
                    .GroupBy(b => b.UserId)
                    .Select(g => g.First())
                    .ToList();

                // Đếm số lần thi và tổng gian lận mỗi user
                var countPerUser = allBaithis
                    .GroupBy(b => b.UserId)
                    .ToDictionary(
                        g => g.Key ?? 0,
                        g => new {
                            AttemptCount = g.Count(),
                            CheatingCount = g.Sum(b => b.WarningCount ?? 0)
                        });

                var latestPerUserIds = latestPerUser.Select(b => b.Id).ToList();
                var gradedCounts = await _context.SubmissionDetails
                    .AsNoTracking()
                    .Where(c => c.ExamSubmissionId != null && latestPerUserIds.Contains(c.ExamSubmissionId.Value) && c.ScoreObtained != null)
                    .GroupBy(c => c.ExamSubmissionId!.Value)
                    .Select(g => new { ExamSubmissionId = g.Key, GradedCount = g.Count() })
                    .ToDictionaryAsync(x => x.ExamSubmissionId, x => x.GradedCount);

                var statsDict = new Dictionary<int, (int mcqTotal, int mcqGraded, int essayTotal, int essayGraded)>();
                if (latestPerUserIds.Any())
                {
                    var chitietStats = await _context.SubmissionDetails
                        .AsNoTracking()
                        .Where(c => c.ExamSubmissionId != null && latestPerUserIds.Contains(c.ExamSubmissionId.Value))
                        .Select(c => new {
                            c.ExamSubmissionId,
                            IsGraded = c.ScoreObtained != null,
                            IsEssay = c.Question != null 
                                && c.Question.QuestionCategory != null 
                                && EssayQuestionHelper.EssayCategoryNamesArray.Contains(c.Question.QuestionCategory.CategoryName)
                        })
                        .ToListAsync();

                    statsDict = chitietStats
                        .GroupBy(c => c.ExamSubmissionId!.Value)
                        .ToDictionary(
                            g => g.Key,
                            g => (
                                mcqTotal: g.Count(c => !c.IsEssay),
                                mcqGraded: g.Count(c => !c.IsEssay && c.IsGraded),
                                essayTotal: g.Count(c => c.IsEssay),
                                essayGraded: g.Count(c => c.IsEssay && c.IsGraded)
                            ));
                }

                var results = latestPerUser.Select(b =>
                {
                    var dto = MapToDetailDtoSummary(b);
                    if (countPerUser.TryGetValue(b.UserId ?? 0, out var counts))
                    {
                        dto.AttemptCount = counts.AttemptCount;
                        dto.CheatingCount = counts.CheatingCount;
                        dto.RetakeCount = counts.AttemptCount - 1;
                    }
                    dto.QuestionsGraded = gradedCounts.TryGetValue(b.Id, out var gc) ? gc : 0;
                    if (statsDict.TryGetValue(b.Id, out var stats))
                    {
                        dto.TotalMultipleChoiceQuestions = stats.mcqTotal;
                        dto.MultipleChoiceQuestionsGraded = stats.mcqGraded;
                        dto.TotalEssayQuestions = stats.essayTotal;
                        dto.EssayQuestionsGraded = stats.essayGraded;
                    }
                    return dto;
                }).ToList();

                return new BaseResponseDto<List<ExamResultDetailDto>>
                {
                    Success = true,
                    Message = "Thành công",
                    Data = results
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting results by exam");
                return new BaseResponseDto<List<ExamResultDetailDto>>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = new List<ExamResultDetailDto>()
                };
            }
        }

        public async Task<BaseResponseDto<ExamResultDetailDto>> RegradeAsync(int examSubmissionId)
        {
            try
            {
                var examSubmission = await _context.ExamSubmissions
                    .Include(b => b.SubmissionDetails)
                        .ThenInclude(c => c.Question)
                            .ThenInclude(ch => ch!.QuestionOptions)
                    .Include(b => b.SubmissionDetails)
                        .ThenInclude(c => c.Question)
                            .ThenInclude(ch => ch!.QuestionCategory)
                    .FirstOrDefaultAsync(b => b.Id == examSubmissionId);

                if (examSubmission == null)
                    return new BaseResponseDto<ExamResultDetailDto> { Success = false, Message = "Không tìm thấy bài thi" };

                int correctCount = 0;
                double totalScore = 0;

                // Group answers by question to support multiple-choice
                var answersByQuestion = examSubmission.SubmissionDetails
                    .Where(c => c.QuestionId.HasValue)
                    .GroupBy(c => c.QuestionId!.Value);

                foreach (var group in answersByQuestion)
                {
                    var question = group.First().Question;
                    if (question == null) continue;

                    var correctChoiceIds = question.QuestionOptions
                        .Where(l => l.IsCorrect == true)
                        .Select(l => l.Id)
                        .ToHashSet();

                    var categoryName = question.QuestionCategory?.CategoryName;
                            var description = question.QuestionCategory?.Description;
                    var isEssay = EssayQuestionHelper.IsEssay(question);

                    if (isEssay)
                    {
                        // For essay, preserve existing ScoreObtained which is manually graded
                        var score = group.FirstOrDefault()?.ScoreObtained ?? 0;
                        if (score > 0)
                        {
                            correctCount++;
                        }
                        totalScore += score;
                    }
                    else
                    {
                        var userChoiceIds = group
                            .Where(c => c.SelectedOptionId.HasValue)
                            .Select(c => c.SelectedOptionId!.Value)
                            .ToHashSet();

                        bool isFullyCorrect = correctChoiceIds.Count > 0 
                            && correctChoiceIds.SetEquals(userChoiceIds);

                        foreach (var ct in group)
                        {
                            if (isFullyCorrect)
                            {
                                ct.ScoreObtained = 1.0 / Math.Max(1, group.Count());
                            }
                            else
                            {
                                ct.ScoreObtained = 0;
                            }
                        }

                        if (isFullyCorrect)
                        {
                            correctCount++;
                            totalScore += 1;
                        }
                    }
                }

                examSubmission.CorrectAnswers = correctCount;
                var tongSoCau = examSubmission.TotalQuestions ?? answersByQuestion.Count();
                examSubmission.TotalScore = BanTayVang.API.Services.Impl.Exams.ExamSubmissionService.CalculateTotalScore(totalScore, tongSoCau);

                await _context.SaveChangesAsync();

                return await GetResultDetailAsync(examSubmissionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error regrading");
                return new BaseResponseDto<ExamResultDetailDto>
                {
                    Success = false,
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        public async Task<BaseResponseDto> ManualGradeAsync(ManualGradingDto dto)
        {
            try
            {
                if (dto.Score.HasValue && dto.Score != 0 && dto.Score != 0.5 && dto.Score != 1)
                    return new BaseResponseDto { Success = false, Message = "Điểm chỉ được là 0, 0.5 hoặc 1" };

                var detail = await _context.SubmissionDetails
                    .Include(c => c.ExamSubmission)
                    .Include(c => c.Question)
                    .FirstOrDefaultAsync(c => c.Id == dto.SubmissionDetailId);

                if (detail == null)
                    return new BaseResponseDto { Success = false, Message = "Không tìm thấy chi tiết bài làm" };

                detail.ScoreObtained = dto.Score;
                detail.TeacherComment = dto.Comment;

                await _context.SaveChangesAsync();

                // Tính lại tổng số câu đúng cho bài thi
                if (detail.ExamSubmissionId.HasValue)
                {
                    var examSubmission = await _context.ExamSubmissions
                        .Include(b => b.SubmissionDetails)
                            .ThenInclude(c => c.Question)
                                .ThenInclude(ch => ch!.QuestionCategory)
                        .Include(b => b.SubmissionDetails)
                            .ThenInclude(c => c.Question)
                                .ThenInclude(ch => ch!.QuestionOptions)
                        .FirstOrDefaultAsync(b => b.Id == detail.ExamSubmissionId.Value);
                    
                    if (examSubmission != null)
                    {
                        double sumScore = 0;
                        int correctAnswers = 0;

                        // Group theo QuestionId để xử lý đúng câu hỏi nhiều đáp án (multi-select)
                        var byQuestion = examSubmission.SubmissionDetails
                            .Where(c => c.QuestionId.HasValue)
                            .GroupBy(c => c.QuestionId!.Value);

                        foreach (var group in byQuestion)
                        {
                            var question = group.First().Question;
                            var categoryName = question?.QuestionCategory?.CategoryName;
                            bool isTuLuan = EssayQuestionHelper.IsEssay(question);

                            if (isTuLuan)
                            {
                                // Câu tự luận: lấy điểm của dòng đầu tiên (chỉ 1 dòng/câu)
                                var score = group.First().ScoreObtained;
                                if (score > 0) correctAnswers++;
                                sumScore += score ?? 0;
                            }
                            else
                            {
                                // Câu trắc nghiệm: mỗi đáp án đúng được chia đều 1.0/group.Count()
                                // Tổng các dòng trong group = 1.0 nếu đúng hết, < 1.0 nếu đúng một phần
                                var groupSum = group.Sum(c => c.ScoreObtained ?? 0);
                                if (groupSum > 0) correctAnswers++;
                                sumScore += groupSum;
                            }
                        }

                        examSubmission.CorrectAnswers = correctAnswers;
                        examSubmission.TotalScore = BanTayVang.API.Services.Impl.Exams.ExamSubmissionService.CalculateTotalScore(sumScore, examSubmission.TotalQuestions ?? 0);
                        await _context.SaveChangesAsync();
                    }
                }

                return new BaseResponseDto { Success = true, Message = "Chấm điểm thành công" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error manual grading");
                return new BaseResponseDto { Success = false, Message = ex.Message };
            }
        }

        public async Task<BaseResponseDto<List<ExamResultDetailDto>>> GetRankingByExamAsync(int examId, int top = 50)
        {
            try
            {
                var examSubmissions = await _context.ExamSubmissions
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                    .Where(b => b.ExamPaperId == examId && b.Status == "Completed")
                    .Include(b => b.User)
                    .Include(b => b.ExamPaper)
                    .Include(b => b.ExamCampaign)
                    .OrderByDescending(b => b.TotalScore)
                    .ThenBy(b => b.SubmitTime) // tie-break by submission time
                    .Take(top)
                    .ToListAsync();

                var results = examSubmissions.Select(b => MapToDetailDtoSummary(b)).ToList();

                return new BaseResponseDto<List<ExamResultDetailDto>>
                {
                    Success = true,
                    Message = "Thành công",
                    Data = results
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ranking");
                return new BaseResponseDto<List<ExamResultDetailDto>>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = new List<ExamResultDetailDto>()
                };
            }
        }

        public async Task<BaseResponseDto<int>> AutoGradeAllAsync(string? restrictToDepartment = null)
        {
            try
            {
                // BUG FIX: this was ManagementOnly (Admin + DeptManager) but had zero department
                // scoping - unlike every other grading action in this controller (Regrade,
                // ManualGrade, AiGradeBatch, GetResultsByExam...), which all restrict a DeptManager
                // to their own department's submissions. A DeptManager could trigger a re-grade of
                // every ungraded exam submission system-wide, not just their own department's.
                var query = _context.ExamSubmissions
                    .AsNoTracking()
                    .Where(b => b.Status == "Completed" && (b.TotalScore == null || b.CorrectAnswers == null));

                if (!string.IsNullOrEmpty(restrictToDepartment))
                {
                    query = query.Where(b =>
                        (b.User != null && b.User.Department == restrictToDepartment) ||
                        (b.ExamPaper != null && b.ExamPaper.Department == restrictToDepartment) ||
                        (b.ExamCampaign != null && b.ExamCampaign.OrganizedBy == restrictToDepartment));
                }

                var ungradedIds = await query
                    .Select(b => b.Id)
                    .ToListAsync();

                int count = 0;
                foreach (var id in ungradedIds)
                {
                    await RegradeAsync(id);
                    count++;
                }

                return new BaseResponseDto<int>
                {
                    Success = true,
                    Message = $"Đã chấm lại {count} bài thi",
                    Data = count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error auto-grading all");
                return new BaseResponseDto<int>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = 0
                };
            }
        }

        #region Private Helpers

        private ExamResultDetailDto MapToDetailDto(ExamSubmission examSubmission)
        {
            int? durationMinutes = null;
            int? durationSeconds = null;
            if (examSubmission.StartTime.HasValue && examSubmission.SubmitTime.HasValue)
            {
                var diff = examSubmission.SubmitTime.Value - examSubmission.StartTime.Value;
                durationMinutes = (int)diff.TotalMinutes;
                durationSeconds = (int)diff.TotalSeconds;
            }

            var detail = new ExamResultDetailDto
            {
                ExamSubmissionId = examSubmission.Id,
                UserId = examSubmission.UserId,
                Username = examSubmission.User?.Username,
                FullName = examSubmission.User?.FullName,
                EmployeeCode = examSubmission.User?.EmployeeCode,
                Department = examSubmission.User?.Department,
                ExamId = examSubmission.ExamPaperId ?? 0,
                ExamPaperId = examSubmission.ExamPaperId,
                ExamPaperCode = examSubmission.ExamPaper?.ExamPaperCode,
                ExamPaperName = examSubmission.ExamPaper?.ExamPaperName,
                StartTime = examSubmission.StartTime,
                SubmitTime = examSubmission.SubmitTime,
                DurationMinutes = durationMinutes,
                DurationSeconds = durationSeconds,
                TotalScore = examSubmission.TotalScore,
                CorrectAnswers = examSubmission.CorrectAnswers,
                TotalQuestions = examSubmission.TotalQuestions,
                Status = examSubmission.Status,
                Pass = PassRuleHelper.ComputePass(examSubmission.CorrectAnswers, examSubmission.ExamCampaign?.MinPassQuestions, examSubmission.ExamPaper?.MinPassQuestions),
                MinPassQuestions = examSubmission.ExamCampaign?.MinPassQuestions ?? examSubmission.ExamPaper?.MinPassQuestions,
                WarningCount = examSubmission.WarningCount,
                IsResultPublished = examSubmission.IsIndividualResultPublished || (examSubmission.ExamPaper?.IsResultPublished ?? false),
                Answers = new List<AnswerDetailDto>()
            };

            int mcqTotal = 0;
            int mcqGraded = 0;
            int essayTotal = 0;
            int essayGraded = 0;

            var answersByQuestion = examSubmission.SubmissionDetails
                .Where(c => c.QuestionId.HasValue)
                .GroupBy(c => c.QuestionId!.Value);

            foreach (var group in answersByQuestion)
            {
                var question = group.First().Question;
                if (question == null) continue;

                var categoryName = question.QuestionCategory?.CategoryName;
                            var description = question.QuestionCategory?.Description;
                bool isEssay = EssayQuestionHelper.IsEssay(question);

                if (isEssay)
                {
                    essayTotal++;
                    if (group.Any(c => c.ScoreObtained != null)) essayGraded++;
                }
                else
                {
                    mcqTotal++;
                    if (group.Any(c => c.ScoreObtained != null)) mcqGraded++;
                }

                // Check correctness (so khớp tập hợp đáp án đúng và lựa chọn của user)
                var correctChoiceIds = question.QuestionOptions
                    .Where(l => l.IsCorrect == true)
                    .Select(l => l.Id)
                    .ToHashSet();

                var userChoiceIds = group
                    .Where(c => c.SelectedOptionId.HasValue)
                    .Select(c => c.SelectedOptionId!.Value)
                    .ToHashSet();

                bool isFullyCorrect = false;
                if (isEssay)
                {
                    var score = group.FirstOrDefault()?.ScoreObtained ?? 0;
                    isFullyCorrect = score > 0;
                }
                else
                {
                    isFullyCorrect = correctChoiceIds.Count > 0 && correctChoiceIds.SetEquals(userChoiceIds);
                }

                // Tạo chuỗi hiển thị cho các lựa chọn của thí sinh
                var userChoiceTexts = group
                    .Where(c => c.SelectedOptionId.HasValue && c.SelectedOption != null)
                    .OrderBy(c => c.SelectedOption!.OrderIndex)
                    .Select(c => c.SelectedOption!.Content)
                    .ToList();
                var userChoiceText = userChoiceTexts.Any() ? string.Join(", ", userChoiceTexts) : "";

                // Tạo chuỗi hiển thị cho các đáp án đúng thực tế
                var correctChoiceTexts = question.QuestionOptions
                    .Where(l => l.IsCorrect == true)
                    .OrderBy(l => l.OrderIndex)
                    .Select(l => l.Content)
                    .ToList();
                var correctChoiceText = correctChoiceTexts.Any() ? string.Join(", ", correctChoiceTexts) : "";

                var firstCt = group.First();
                var firstCorrectChoice = question.QuestionOptions.FirstOrDefault(l => l.IsCorrect == true);

                detail.Answers.Add(new AnswerDetailDto
                {
                    QuestionId = question.Id,
                    QuestionContent = question.Content,
                    QuestionCategory = question.QuestionCategory?.CategoryName,
                    SelectedOptionId = firstCt.SelectedOptionId, 
                    AnswerContent = isEssay ? firstCt.EssayAnswer : userChoiceText,
                    EssayAnswer = firstCt.EssayAnswer,
                    EssayImageUrl = firstCt.EssayImageUrl,
                    SuggestedAnswer = firstCt.Question?.SuggestedAnswer,
                    IsCorrect = isFullyCorrect,
                    ScoreObtained = group.All(c => c.ScoreObtained == null) ? (double?)null : group.Sum(c => c.ScoreObtained ?? 0),
                    CorrectOptionId = firstCorrectChoice?.Id, 
                    CorrectAnswerContent = correctChoiceText,
                    SubmissionDetailId = firstCt.Id,
                    TeacherComment = firstCt.TeacherComment,
                    AiScore = firstCt.AiScore,
                    AiComment = firstCt.AiComment,
                    AiGradingStatus = firstCt.AiGradingStatus
                });
            }

            detail.QuestionsGraded = answersByQuestion.Count(g => g.Any(c => c.ScoreObtained != null));
            detail.TotalMultipleChoiceQuestions = mcqTotal;
            detail.MultipleChoiceQuestionsGraded = mcqGraded;
            detail.TotalEssayQuestions = essayTotal;
            detail.EssayQuestionsGraded = essayGraded;

            return detail;
        }

        private ExamResultDetailDto MapToDetailDtoSummary(ExamSubmission examSubmission)
        {
            int? duration = null;
            if (examSubmission.StartTime.HasValue && examSubmission.SubmitTime.HasValue)
            {
                duration = (int)(examSubmission.SubmitTime.Value - examSubmission.StartTime.Value).TotalMinutes;
            }

            return new ExamResultDetailDto
            {
                ExamSubmissionId = examSubmission.Id,
                UserId = examSubmission.UserId,
                Username = examSubmission.User?.Username,
                FullName = examSubmission.User?.FullName,
                EmployeeCode = examSubmission.User?.EmployeeCode,
                Department = examSubmission.User?.Department,
                ExamId = examSubmission.ExamPaperId ?? 0,
                ExamPaperCode = examSubmission.ExamPaperCode ?? examSubmission.ExamPaper?.ExamPaperCode,
                ExamPaperName = examSubmission.ExamPaper?.ExamPaperName,
                StartTime = examSubmission.StartTime,
                SubmitTime = examSubmission.SubmitTime,
                DurationMinutes = duration,
                TotalScore = examSubmission.TotalScore,
                CorrectAnswers = examSubmission.CorrectAnswers,
                TotalQuestions = examSubmission.TotalQuestions,
                Status = examSubmission.Status,
                Pass = PassRuleHelper.ComputePass(examSubmission.CorrectAnswers, examSubmission.ExamCampaign?.MinPassQuestions, examSubmission.ExamPaper?.MinPassQuestions),
                MinPassQuestions = examSubmission.ExamCampaign?.MinPassQuestions ?? examSubmission.ExamPaper?.MinPassQuestions,
                WarningCount = examSubmission.WarningCount,
                IsResultPublished = examSubmission.IsIndividualResultPublished || (examSubmission.ExamPaper?.IsResultPublished ?? false),
                QuestionsGraded = examSubmission.SubmissionDetails?.Count(d => d.ScoreObtained != null) ?? 0,
                DepartmentEvaluation = examSubmission.DepartmentEvaluation,
            };
        }


        /// <summary>
        /// Quản lý khoa đánh giá / nhận xét bài thi của thí sinh
        /// </summary>
        public async Task<BaseResponseDto> EvaluateCandidateAsync(int examSubmissionId, string evaluation)
        {
            try
            {
                var examSubmission = await _context.ExamSubmissions.FindAsync(examSubmissionId);
                if (examSubmission == null)
                    return new BaseResponseDto { Success = false, Message = "Không tìm thấy bài thi" };

                examSubmission.DepartmentEvaluation = evaluation;
                await _context.SaveChangesAsync();

                return new BaseResponseDto { Success = true, Message = "Đã lưu đánh giá" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving DepartmentEvaluation for examSubmission {ExamSubmissionId}", examSubmissionId);
                return new BaseResponseDto { Success = false, Message = "Lỗi khi lưu đánh giá" };
            }
        }

        #endregion

        /// <summary>
        /// Lấy kết quả thi theo Kỳ thi
        /// </summary>
        public async Task<BaseResponseDto<List<ExamResultDetailDto>>> GetResultsByExamCampaignAsync(int examCampaignId)
        {
            try
            {
                var examSubmissions = await _context.ExamSubmissions
                    .AsNoTracking()
                    .IgnoreQueryFilters()
                    .Include(b => b.User)
                    .Include(b => b.ExamPaper)
                    .Include(b => b.ExamCampaign)
                    .Where(b => b.ExamCampaignId == examCampaignId)
                    .OrderByDescending(b => b.TotalScore)
                    .ToListAsync();

                var userIds = examSubmissions.Select(b => b.UserId).Distinct().ToList();
                var examIds = examSubmissions.Select(b => b.ExamPaperId).Distinct().ToList();
                var ExamSubmissionIds = examSubmissions.Select(b => b.Id).ToList();

                var attemptsDict = new Dictionary<string, (int AttemptCount, int CheatingCount)>();
                var gradedCounts = new Dictionary<int, int>();
                if (userIds.Any() && examIds.Any())
                {
                    var allUserExamAttempts = await _context.ExamSubmissions
                        .AsNoTracking()
                        .Where(b => b.UserId != null && userIds.Contains(b.UserId) 
                                 && b.ExamPaperId != null && examIds.Contains(b.ExamPaperId.Value)
                                 && (b.Status == "Completed" || ExamSubmissionIds.Contains(b.Id)))
                        .ToListAsync();

                    attemptsDict = allUserExamAttempts
                        .GroupBy(b => new { UserId = b.UserId ?? 0, ExamPaperId = b.ExamPaperId ?? 0 })
                        .ToDictionary(
                            g => $"{g.Key.UserId}_{g.Key.ExamPaperId}",
                            g => (AttemptCount: g.Count(), CheatingCount: g.Sum(b => b.WarningCount ?? 0))
                        );
                }

                if (ExamSubmissionIds.Any())
                {
                    gradedCounts = await _context.SubmissionDetails
                        .AsNoTracking()
                        .Where(c => c.ExamSubmissionId != null && ExamSubmissionIds.Contains(c.ExamSubmissionId.Value) && c.ScoreObtained != null)
                        .GroupBy(c => c.ExamSubmissionId!.Value)
                        .Select(g => new { ExamSubmissionId = g.Key, GradedCount = g.Count() })
                        .ToDictionaryAsync(x => x.ExamSubmissionId, x => x.GradedCount);
                }

                var statsDict = new Dictionary<int, (int mcqTotal, int mcqGraded, int essayTotal, int essayGraded)>();
                if (ExamSubmissionIds.Any())
                {
                    var chitietStats = await _context.SubmissionDetails
                        .AsNoTracking()
                        .Where(c => c.ExamSubmissionId != null && ExamSubmissionIds.Contains(c.ExamSubmissionId.Value))
                        .Select(c => new {
                            c.ExamSubmissionId,
                            IsGraded = c.ScoreObtained != null,
                            IsEssay = c.Question != null 
                                && c.Question.QuestionCategory != null 
                                && EssayQuestionHelper.EssayCategoryNamesArray.Contains(c.Question.QuestionCategory.CategoryName)
                        })
                        .ToListAsync();

                    statsDict = chitietStats
                        .GroupBy(c => c.ExamSubmissionId!.Value)
                        .ToDictionary(
                            g => g.Key,
                            g => (
                                mcqTotal: g.Count(c => !c.IsEssay),
                                mcqGraded: g.Count(c => !c.IsEssay && c.IsGraded),
                                essayTotal: g.Count(c => c.IsEssay),
                                essayGraded: g.Count(c => c.IsEssay && c.IsGraded)
                            ));
                }

                var results = examSubmissions.Select(b =>
                {
                    var duration = (b.SubmitTime.HasValue && b.StartTime.HasValue)
                        ? (int)(b.SubmitTime.Value - b.StartTime.Value).TotalMinutes : 0;

                    int attemptCount = 1;
                    int cheatingCount = 0;
                    int retakeCount = 0;

                    var key = $"{b.UserId ?? 0}_{b.ExamPaperId ?? 0}";
                    if (attemptsDict.TryGetValue(key, out var counts))
                    {
                        attemptCount = counts.AttemptCount;
                        cheatingCount = counts.CheatingCount;
                        retakeCount = Math.Max(0, counts.AttemptCount - 1);
                    }

                    int mcqTotal = 0, mcqGraded = 0, essayTotal = 0, essayGraded = 0;
                    if (statsDict.TryGetValue(b.Id, out var stats))
                    {
                        mcqTotal = stats.mcqTotal;
                        mcqGraded = stats.mcqGraded;
                        essayTotal = stats.essayTotal;
                        essayGraded = stats.essayGraded;
                    }

                    return new ExamResultDetailDto
                    {
                        ExamSubmissionId = b.Id,
                        ExamPaperId = b.ExamPaperId,
                        Username = b.User?.Username,
                        FullName = b.User?.FullName,
                        EmployeeCode = b.User?.EmployeeCode,
                        Department = b.User?.Department,
                        ExamId = b.ExamPaperId ?? 0,
                        ExamPaperCode = b.ExamPaperCode,
                        ExamPaperName = b.ExamPaper?.ExamPaperName,
                        StartTime = b.StartTime,
                        SubmitTime = b.SubmitTime,
                        DurationMinutes = duration,
                        TotalScore = b.TotalScore,
                        CorrectAnswers = b.CorrectAnswers,
                        TotalQuestions = b.TotalQuestions,
                        Status = b.Status,
                        Pass = PassRuleHelper.ComputePass(b.CorrectAnswers, b.ExamCampaign?.MinPassQuestions, b.ExamPaper?.MinPassQuestions),
                        MinPassQuestions = b.ExamCampaign?.MinPassQuestions ?? b.ExamPaper?.MinPassQuestions,
                        WarningCount = b.WarningCount,
                        IsResultPublished = b.IsIndividualResultPublished || (b.ExamPaper?.IsResultPublished ?? false),
                        AttemptCount = attemptCount,
                        CheatingCount = cheatingCount,
                        RetakeCount = retakeCount,
                        QuestionsGraded = gradedCounts.TryGetValue(b.Id, out var gc) ? gc : 0,
                        TotalMultipleChoiceQuestions = mcqTotal,
                        MultipleChoiceQuestionsGraded = mcqGraded,
                        TotalEssayQuestions = essayTotal,
                        EssayQuestionsGraded = essayGraded
                    };
                }).ToList();

                return new BaseResponseDto<List<ExamResultDetailDto>>
                {
                    Success = true,
                    Message = $"Lấy {results.Count} kết quả thi kỳ {examCampaignId}",
                    Data = results
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting results by ExamCampaign {ExamCampaignId}", examCampaignId);
                return BaseResponseDto<List<ExamResultDetailDto>>.FailureResult("Lỗi khi lấy kết quả");
            }
        }
    }
}



