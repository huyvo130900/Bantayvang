namespace BanTayVang.API.Models;

public partial class Baithi
{
    public int Id { get; set; }
    public int? IdDeThi { get; set; }
    public int? IdTaiKhoan { get; set; }
    public DateTime? ThoiGianBatDau { get; set; }
    public DateTime? ThoiGianNop { get; set; }
    public double? TongDiem { get; set; }
    public string? TrangThai { get; set; }
    public int? SoCauDung { get; set; }
    public int? TongSoCau { get; set; }
    public string? DanhGiaKhoa { get; set; }

    /// <summary>
    /// Công bố điểm riêng cho thí sinh này (không phụ thuộc CongBoKetQua của đề thi).
    /// Nếu true → thí sinh thấy điểm dù CongBoKetQua của Dethi = false.
    /// </summary>
    public bool CongBoRieng { get; set; } = false;

    /// <summary>Thời điểm công bố riêng</summary>
    public DateTime? ThoiGianCongBoRieng { get; set; }

    /// <summary>Người thực hiện công bố riêng (Admin/DeptManager)</summary>
    public int? NguoiCongBoRieng { get; set; }

    /// <summary>Mã đề thi (cache từ Dethi.MaDeThi)</summary>
    public string? MaDeThi { get; set; }

    /// <summary>FK → KyThi — kỳ thi mà bài thi này thuộc về</summary>
    public int? IdKyThi { get; set; }
    public virtual KyThi? KyThiNavigation { get; set; }

    /// <summary>Tổng số cảnh báo gian lận trong bài thi này</summary>
    public int? TongSoCanhBao { get; set; }

    public virtual Dethi? IdDeThiNavigation { get; set; }
    public virtual Taikhoan? IdTaiKhoanNavigation { get; set; }
    public virtual ICollection<Chitietlambai> Chitietlambais { get; set; } = new List<Chitietlambai>();
    public virtual ICollection<Canhbaogianlan> Canhbaogianlans { get; set; } = new List<Canhbaogianlan>();
    public virtual ICollection<Logthaotac> Logthaotacs { get; set; } = new List<Logthaotac>();
}
