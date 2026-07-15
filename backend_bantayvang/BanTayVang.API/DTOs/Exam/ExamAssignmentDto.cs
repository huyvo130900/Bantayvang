namespace BanTayVang.API.DTOs.Exam
{
    public class ExamAssignmentDto
    {
        public int Id { get; set; }
        public int ExamId { get; set; }
        public string? ExamPaperCode { get; set; }
        public string? ExamPaperName { get; set; }
        public int UserId { get; set; }
        public string? Username { get; set; }
        public string? FullName { get; set; }
        public DateTime AssignedAt { get; set; }
        public DateTime? CustomStartTime { get; set; }
        public int? ExtraMinutes { get; set; }
        public bool IsActive { get; set; }
        public string? Note { get; set; }

        // Trạng thái bài thi của học sinh
        public string Status { get; set; } = "Pending"; // Pending | InProgress | Completed | AutoSubmitted

        // Thông tin kết quả nếu đã thi xong
        public int? ExamSubmissionId { get; set; }
        public double? DiemSo { get; set; }
        public double? TotalScore { get; set; }
        public int? CorrectAnswers { get; set; }
        public int? TotalQuestions { get; set; }
        public DateTime? NgayHoanThanh { get; set; }
        public bool? DatYeuCau { get; set; }  // >= 50% điểm
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
        public int? DurationMinutes { get; set; }
    }

    public class CreateExamAssignmentDto
    {
        public int ExamId { get; set; }
        public List<int> UserIds { get; set; } = new();
        public DateTime? CustomStartTime { get; set; }
        public string? Note { get; set; }
    }

    public class ExtendExamTimeDto
    {
        public int ExamSubmissionId { get; set; }
        public int AdditionalMinutes { get; set; }
        public string? Reason { get; set; }
    }
}
