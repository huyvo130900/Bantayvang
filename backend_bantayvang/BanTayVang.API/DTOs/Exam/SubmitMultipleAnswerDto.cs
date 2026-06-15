namespace BanTayVang.API.DTOs.Exam
{
    /// <summary>
    /// DTO for multiple-answer questions
    /// </summary>
    public class SubmitMultipleAnswerDto
    {
        public int IdBaiThi { get; set; }
        public int IdCauHoi { get; set; }
        public List<int> IdLuaChonDaChon { get; set; } = new();
        public string? CauTraLoiTuLuan { get; set; }
        public bool DaLuu { get; set; }
    }
}