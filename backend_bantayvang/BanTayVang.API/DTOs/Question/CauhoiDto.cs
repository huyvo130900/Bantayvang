namespace BanTayVang.API.DTOs.Question
{
    public class CauhoiDto
    {
        public int Id { get; set; }
        public int? IdLoaiCauHoi { get; set; }
        public string? TenLoaiCauHoi { get; set; }
        public string? NoiDung { get; set; }
        public string? DoKho { get; set; }
        public string? KhoaPhong { get; set; }
        public string? HinhAnh { get; set; }
        public DateTime? NgayTao { get; set; }
        public DateTime? NgayCapNhat { get; set; }
        public List<LuachonDto> DanhSachLuaChon { get; set; } = new();
        public List<string> DanhSachKyThi { get; set; } = new();
        public List<string> DanhSachDeThi { get; set; } = new();
    }
}