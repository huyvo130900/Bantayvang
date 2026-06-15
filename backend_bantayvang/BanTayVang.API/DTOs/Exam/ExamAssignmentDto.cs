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

        // Trạng thái bài thi của học sinh
        public string TrangThai { get; set; } = "Pending"; // Pending | InProgress | Completed | AutoSubmitted

        // Thông tin kết quả nếu đã thi xong
        public int? BaithiId { get; set; }
        public double? DiemSo { get; set; }
        public double? TongDiem { get; set; }
        public int? SoCauDung { get; set; }
        public int? TongSoCau { get; set; }
        public DateTime? NgayHoanThanh { get; set; }
        public bool? DatYeuCau { get; set; }  // >= 50% điểm
        public string? ThoiGianBatDau { get; set; }
        public string? ThoiGianKetThuc { get; set; }
        public int? ThoiGianLamBai { get; set; }
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
