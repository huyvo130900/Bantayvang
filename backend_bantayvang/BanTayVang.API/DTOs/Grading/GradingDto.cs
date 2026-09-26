using System.Text.Json.Serialization;

namespace BanTayVang.API.DTOs.Grading
{
    /// <summary>
    /// Result detail of a single exam submission
    /// </summary>
    public class ExamResultDetailDto
    {
        [JsonPropertyName("examSubmissionId")]
        public int ExamSubmissionId { get; set; }
        public int? UserId { get; set; }
        public string? Username { get; set; }
        public string? FullName { get; set; }
        public string? EmployeeCode { get; set; }
        public string? Department { get; set; }
        public int ExamId { get; set; }
        public int? ExamPaperId { get; set; }
        public string? ExamPaperCode { get; set; }
        public string? ExamPaperName { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? SubmitTime { get; set; }
        public int? DurationMinutes { get; set; }
        public int? DurationSeconds { get; set; }
        public double? TotalScore { get; set; }
        public int? CorrectAnswers { get; set; }
        public int? TotalQuestions { get; set; }
        public string? Status { get; set; }
        public bool Pass { get; set; }
        public int? MinPassQuestions { get; set; }
        public int? WarningCount { get; set; }
        /// <summary>Số câu đã được chấm điểm</summary>
        public int QuestionsGraded { get; set; } = 0;
        /// <summary>Tổng số lần đã thi (kể cả lần đầu)</summary>
        public int AttemptCount { get; set; } = 1;
        /// <summary>Tổng số lần gian lận tích lũy qua các lần thi</summary>
        public int CheatingCount { get; set; } = 0;
        /// <summary>Số lần thi lại (LanThi - 1)</summary>
        public int RetakeCount { get; set; } = 0;
        /// <summary>Đề thi có công bố kết quả hay không</summary>
        public bool IsResultPublished { get; set; } = false;
        /// <summary>Nhận xét của quản lý khoa về thí sinh</summary>
        public string? DepartmentEvaluation { get; set; }
        public int TotalMultipleChoiceQuestions { get; set; } = 0;
        public int MultipleChoiceQuestionsGraded { get; set; } = 0;
        public int TotalEssayQuestions { get; set; } = 0;
        public int EssayQuestionsGraded { get; set; } = 0;
        public List<AnswerDetailDto> Answers { get; set; } = new();
    }

    public class AnswerDetailDto
    {
        public int QuestionId { get; set; }
        public string? QuestionContent { get; set; }
        public string? QuestionCategory { get; set; }
        public int? SelectedOptionId { get; set; }
        public string? AnswerContent { get; set; }
        public string? EssayAnswer { get; set; }
        public string? EssayImageUrl { get; set; }
        public string? SuggestedAnswer { get; set; }
        public string? TeacherComment { get; set; }
        public bool IsCorrect { get; set; }
        public double? ScoreObtained { get; set; }
        public int? CorrectOptionId { get; set; }
        public string? CorrectAnswerContent { get; set; }
        public int? SubmissionDetailId { get; set; }
        // AI Grading fields
        public double? AiScore { get; set; }
        public string? AiComment { get; set; }
        public string? AiGradingStatus { get; set; }
    }

    /// <summary>
    /// Manual grading for essay questions
    /// </summary>
    public class ManualGradingDto
    {
        public int SubmissionDetailId { get; set; }
        /// <summary>Điểm câu tự luận: chỉ nhận 0, 0.5, hoặc 1. null = chưa chấm/chấm lại.</summary>
        public double? Score { get; set; }
        public string? Comment { get; set; }
    }

    /// <summary>
    /// Đánh giá của quản lý khoa về thí sinh
    /// </summary>
    public class DanhGiaThiSinhDto
    {
        public int ExamSubmissionId { get; set; }
        public string Evaluation { get; set; } = string.Empty;
    }

    /// <summary>
    /// Re-grade request
    /// </summary>
    public class RegradeRequestDto
    {
        public int ExamSubmissionId { get; set; }
    }

    /// <summary>
    /// Export result options
    /// </summary>
    public class ExportResultsDto
    {
        public int? ExamId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? Department { get; set; }
    }

    /// <summary>
    /// Represents an item selected in the UI to be exported to Excel
    /// </summary>
    public class SelectedExportItemDto
    {
        public int ExamSubmissionId { get; set; }
        public int AttemptNumber { get; set; }
    }

    /// <summary>
    /// Kích hoạt AI chấm hàng loạt câu tự luận
    /// </summary>
    public class AiGradeBatchDto
    {
        public List<int> SubmissionDetailIds { get; set; } = new();

        /// <summary>
        /// true (default): AI writes ScoreObtained directly - the score counts as final immediately.
        /// false: AI only fills AiScore/AiComment as a suggestion; ScoreObtained stays null until a
        /// grader picks a score by hand in the review UI (result-detail-dialog.tsx), which already
        /// shows the AI's suggestion alongside the 0/0.5/1 buttons.
        /// </summary>
        public bool AutoFinalize { get; set; } = true;
    }
}
