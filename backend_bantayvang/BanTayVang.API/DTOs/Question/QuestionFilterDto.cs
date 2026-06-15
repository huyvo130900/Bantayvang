namespace BanTayVang.API.DTOs.Question
{
    public class QuestionFilterDto
    {
        public int? IdLoaiCauHoi { get; set; }
        public string? DoKho { get; set; }
        public string? KhoaPhong { get; set; }
        public string? SearchKeyword { get; set; }
        public bool? ShowDuplicatesOnly { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}