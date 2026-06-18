namespace BanTayVang.API.DTOs.Exam
{
    public class ExamQuestionDto
    {
        public int Id { get; set; }
        public string? NoiDung { get; set; }
        public string? HinhAnh { get; set; }
        public int ThuTuCau { get; set; }
        public List<ExamChoiceDto> DanhSachLuaChon { get; set; } = new();
        
        // Câu trả lời của thí sinh (nếu đã trả lời)
        public int? IdLuaChonDaChon { get; set; }
        public List<int> IdLuaChonDaChonList { get; set; } = new();
        public string? CauTraLoiTuLuan { get; set; }
        public bool DaLuu { get; set; }
        public bool ChoPhepChonNhieu { get; set; }
    }
}