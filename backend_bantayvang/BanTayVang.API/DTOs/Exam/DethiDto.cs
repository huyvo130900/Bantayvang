using BanTayVang.API.DTOs.Question;

namespace BanTayVang.API.DTOs.Exam
{
    public class DethiDto
    {
        public int Id { get; set; }
        public string? MaDeThi { get; set; }
        public string? TenDeThi { get; set; }
        public int? ThoiGianLamBai { get; set; }
        public double? TongDiem { get; set; }
        public DateTime? ThoiGianBatDau { get; set; }
        public string? LinkTruyCap { get; set; }
        public string? TrangThai { get; set; }
        public string? KhoaPhong { get; set; }
        public DateTime? NgayTao { get; set; }
        public int SoCauHoi { get; set; }

        public bool CongBoKetQua { get; set; } = false;
        public DateTime? ThoiGianCongBo { get; set; }
        public int? KyThiId { get; set; }

        /// <summary>
        /// Số câu đúng tối thiểu để đạt (override từ KyThi nếu đề thi có cấu hình riêng)
        /// </summary>
        public int? SoCauDungToiThieu { get; set; }

        public List<CauhoiDto> DanhSachCauHoi { get; set; } = new();
    }

    public class ExamPreviewDto
    {
        public int Id { get; set; }
        public string? MaDeThi { get; set; }
        public string? TenDeThi { get; set; }
        public int? ThoiGianLamBai { get; set; }
        public string? TrangThai { get; set; }
        public string? KhoaPhong { get; set; }
        public bool CongBoKetQua { get; set; }
        public List<QuestionPreviewDto> CauHois { get; set; } = new();
    }

    public class QuestionPreviewDto
    {
        public int Id { get; set; }
        public string? NoiDung { get; set; }
        public string? ChuDe { get; set; }
        public List<ChoicePreviewDto> Luachons { get; set; } = new();
    }

    public class ChoicePreviewDto
    {
        public int Id { get; set; }
        public string? NoiDung { get; set; }
        public bool? LaDapAnDung { get; set; }
    }
}
