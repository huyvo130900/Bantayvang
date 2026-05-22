namespace BanTayVang.API.DTOs.Exam
{
    public class ExamAssignmentDto
    {
        public int Id { get; set; }
        public int ExamId { get; set; }
        public string? MaDeThi { get; set; }
        public string? TenDeThi { get; set; }
        public int UserId { get; set; }
        public string? Username { get; set; }
        public string? FullName { get; set; }
        public DateTime AssignedAt { get; set; }
        public DateTime? CustomStartTime { get; set; }
        public int? ExtraMinutes { get; set; }
        public bool IsActive { get; set; }
        public string? Note { get; set; }
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
        public int BaiThiId { get; set; }
        public int AdditionalMinutes { get; set; }
        public string? Reason { get; set; }
    }
}