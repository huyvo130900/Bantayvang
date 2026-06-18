using System.ComponentModel.DataAnnotations;

namespace BanTayVang.API.DTOs.Exam
{
    public class CreateDethiDto
    {
        [Required]
        [StringLength(50)]
        public string MaDeThi { get; set; } = string.Empty;

        [StringLength(255)]
        public string? TenDeThi { get; set; }

        [Range(1, 1008000)]
        public int? ThoiGianLamBai { get; set; }

        public DateTime? ThoiGianBatDau { get; set; }
        public string? TrangThai { get; set; } = "Active";

        /// <summary>
        /// Khoa/phong lấy câu hỏi từ ngân hàng câu hỏi
        /// </summary>
        public string? KhoaPhong { get; set; }

        /// <summary>
        /// Số câu hỏi random từ ngân hàng (nếu null thì lấy tất cả)
        /// </summary>



        /// <summary>
        /// Tương thích ngược - nếu truyền list câu hỏi cụ thể (legacy)
        /// </summary>
        public List<int> DanhSachIdCauHoi { get; set; } = new();



        /// <summary>
        /// Kỳ thi liên kết
        /// </summary>
        public int? KyThiId { get; set; }

        /// <summary>
        /// Số câu đúng tối thiểu để đạt cho đề thi này (tùy chọn)
        /// </summary>
        public int? SoCauDungToiThieu { get; set; }
    }
}
