using System;
using System.Collections.Generic;

namespace BanTayVang.API.Models;

public partial class Dethi
{
    public int Id { get; set; }

    public string? MaDeThi { get; set; }

    public string? TenDeThi { get; set; }

    public int? ThoiGianLamBai { get; set; }

    public double? TongDiem { get; set; }

    public DateTime? ThoiGianBatDau { get; set; }

    public string? LinkTruyCap { get; set; }

    public string? TrangThai { get; set; }

    public int? NguoiTao { get; set; }

    public DateTime? NgayTao { get; set; }

    public string? ChecksumData { get; set; }
    public string? KhoaPhong { get; set; }
    public int? NguoiCapNhat { get; set; }
    public DateTime? NgayCapNhat { get; set; }



    /// <summary>
    /// Công bố kết quả cho thí sinh xem hay không. Mặc định FALSE.
    /// Department Manager / Admin bật toggle này per đề thi.
    /// </summary>
    public bool CongBoKetQua { get; set; } = false;

    /// <summary>
    /// ID người bật/tắt công bố kết quả
    /// </summary>
    public int? NguoiCongBo { get; set; }

    /// <summary>
    /// Thời điểm công bố kết quả
    /// </summary>
    public DateTime? ThoiGianCongBo { get; set; }

    public int? KyThiId { get; set; }
    public virtual KyThi? KyThiNavigation { get; set; }

    /// <summary>
    /// Số câu đúng tối thiểu để đạt cho đề thi này (override riêng, không phụ thuộc vào KyThi)
    /// </summary>
    public int? SoCauDungToiThieu { get; set; }

    public virtual ICollection<Baithi> Baithis { get; set; } = new List<Baithi>();

    public virtual ICollection<DethiCauhoi> DethiCauhois { get; set; } = new List<DethiCauhoi>();
}
