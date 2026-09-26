namespace BanTayVang.API.DTOs.Exam
{
    public class ExamSubmissionDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ExamPaperId { get; set; }
        public int? ExamCampaignId { get; set; }
        public string? Status { get; set; }
        public DateTime? SubmitTime { get; set; }

        // Điểm tự tính: (CorrectAnswers * 10.0 / TotalQuestions)
        public double? TotalScore { get; set; }
        public double? CalculatedScore => TotalQuestions > 0 ? Math.Round((CorrectAnswers ?? 0) * 10.0 / TotalQuestions!.Value, 2) : TotalScore;

        public int? CorrectAnswers { get; set; }
        public int? TotalQuestions { get; set; }
        public int? TotalWarnings { get; set; }

        // Thông tin đề thi
        public string? ExamPaperName { get; set; }
        public string? ExamPaperCode { get; set; }
        public int? DurationMinutes { get; set; }
        public DateTime? StartTime { get; set; }
        public int? RemainingTimeSeconds { get; set; } // giây

        // Công bố kết quả
        public bool IsResultPublished { get; set; } = false;

        // Kết quả đạt/không đạt (backend tính)
        public bool? Pass { get; set; }
    }
}
