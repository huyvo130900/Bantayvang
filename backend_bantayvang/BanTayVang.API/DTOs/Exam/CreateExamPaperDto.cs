using System.ComponentModel.DataAnnotations;

namespace BanTayVang.API.DTOs.Exam
{
    public class CreateExamPaperDto
    {
        [Required]
        [StringLength(50)]
        public string ExamPaperCode { get; set; } = string.Empty;

        [StringLength(255)]
        public string? ExamPaperName { get; set; }

        [Range(1, 1008000)]
        public int? DurationMinutes { get; set; }

        public DateTime? StartTime { get; set; }
        public string? Status { get; set; } = "Active";

        /// <summary>
        /// Khoa/phong lấy câu hỏi từ ngân hàng câu hỏi
        /// </summary>
        public string? Department { get; set; }

        /// <summary>
        /// Số câu hỏi random từ ngân hàng (nếu null thì lấy tất cả)
        /// </summary>
        public int? RandomQuestionCount { get; set; }

        /// <summary>
        /// Tương thích ngược - nếu truyền list câu hỏi cụ thể (legacy)
        /// </summary>
        public List<int> QuestionIds { get; set; } = new();

        /// <summary>
        /// Kỳ thi liên kết
        /// </summary>
        public int? ExamCampaignId { get; set; }

        /// <summary>
        /// Số câu đúng tối thiểu để đạt cho đề thi này (tùy chọn)
        /// </summary>
        public int? MinPassQuestions { get; set; }
    }
}
