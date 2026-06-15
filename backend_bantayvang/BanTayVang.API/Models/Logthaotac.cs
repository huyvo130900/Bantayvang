using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace BanTayVang.API.Models;

public partial class Logthaotac
{
    public int Id { get; set; }

    /// <summary>FK → TAIKHOAN - người thực hiện thao tác</summary>
    public int? IdTaiKhoan { get; set; }

    /// <summary>Username cached tại thời điểm log (không join khi đọc)</summary>
    public string? TenDangNhap { get; set; }

    public int? IdBaiThi { get; set; }

    /// <summary>HTTP Method: GET, POST, PUT, DELETE, PATCH</summary>
    public string? PhuongThuc { get; set; }

    /// <summary>API Path: /api/Cauhoi/123</summary>
    public string? DuongDan { get; set; }

    /// <summary>Loại thao tác: POST_CAUHOI, DELETE_USER, etc.</summary>
    public string? LoaiThaoTac { get; set; }

    /// <summary>Chi tiết thao tác (ghi chú)</summary>
    public string? ChiTiet { get; set; }

    public DateTime? ThoiGian { get; set; }

    [Column("DiaChi_IP")]
    public string? DiaChiIp { get; set; }

    public string? UserAgent { get; set; }

    /// <summary>HTTP status code: 200, 400, 500...</summary>
    public int? MaHttp { get; set; }

    /// <summary>Khoa/Phòng của người thực hiện</summary>
    public string? KhoaPhong { get; set; }

    public virtual Baithi? IdBaiThiNavigation { get; set; }
    public virtual Taikhoan? TaiKhoan { get; set; }
}
