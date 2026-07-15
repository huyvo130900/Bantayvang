namespace BanTayVang.API.DTOs.Grading
{
    /// <summary>
    /// Result detail of a single exam submission
    /// </summary>
    public class ExamResultDetailDto
    {
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
        public int? SoCanhBao { get; set; }
        /// <summary>Số câu đã được chấm điểm</summary>
        public int SoCauDaCham { get; set; } = 0;
        /// <summary>Tổng số lần đã thi (kể cả lần đầu)</summary>
        public int SoLanThi { get; set; } = 1;
        /// <summary>Tổng số lần gian lận tích lũy qua các lần thi</summary>
        public int SoLanGianLan { get; set; } = 0;
        /// <summary>Số lần thi lại (LanThi - 1)</summary>
        public int SoLanThiLai { get; set; } = 0;
        /// <summary>Đề thi có công bố kết quả hay không</summary>
        public bool IsResultPublished { get; set; } = false;
        /// <summary>Nhận xét của quản lý khoa về thí sinh</summary>
        public string? DanhGiaKhoa { get; set; }
        public int TongSoCauTracNghiem { get; set; } = 0;
        public int SoCauTracNghiemDaCham { get; set; } = 0;
        public int TongSoCauTuLuan { get; set; } = 0;
        public int SoCauTuLuanDaCham { get; set; } = 0;
        public List<AnswerDetailDto> Answers { get; set; } = new();
    }

    public class AnswerDetailDto
    {
        public int QuestionId { get; set; }
        public string? NoiDungCauHoi { get; set; }
        public string? QuestionCategory { get; set; }
        public int? SelectedOptionId { get; set; }
        public string? NoiDungDapAn { get; set; }
        public string? CauTraLoiTuLuan { get; set; }
        public bool IsCorrect { get; set; }
        public double? ScoreObtained { get; set; }
        public int? IdLuaChonDung { get; set; }
        public string? NoiDungDapAnDung { get; set; }
        public int? ChiTietLamBaiId { get; set; }
    }

    /// <summary>
    /// Manual grading for essay questions
    /// </summary>
    public class ManualGradingDto
    {
        public int ChiTietLamBaiId { get; set; }
        /// <summary>Đánh dấu câu trả lời là Đúng (true) hay Sai (false)</summary>
        public bool? IsCorrect { get; set; }
        public string? NhanXet { get; set; }
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
        public int LanThi { get; set; }
    }
}
