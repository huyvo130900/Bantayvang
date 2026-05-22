using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace BanTayVang.API.Models;

public partial class BanTayVangDbContext : DbContext
{
    public BanTayVangDbContext()
    {
    }

    public BanTayVangDbContext(DbContextOptions<BanTayVangDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Baithi> Baithis { get; set; }

    public virtual DbSet<Canhbaogianlan> Canhbaogianlans { get; set; }

    public virtual DbSet<Cauhoi> Cauhois { get; set; }

    public virtual DbSet<Chitietlambai> Chitietlambais { get; set; }

    public virtual DbSet<Danhmucauhoi> Danhmucauhois { get; set; }

    public virtual DbSet<Dethi> Dethis { get; set; }

    public virtual DbSet<DethiCauhoi> DethiCauhois { get; set; }

    public virtual DbSet<Loaicauhoi> Loaicauhois { get; set; }

    public virtual DbSet<Logthaotac> Logthaotacs { get; set; }

    public virtual DbSet<Luachon> Luachons { get; set; }

    public virtual DbSet<Phiendangnhap> Phiendangnhaps { get; set; }

    public virtual DbSet<Taikhoan> Taikhoans { get; set; }

    public virtual DbSet<TaikhoanVaitro> TaikhoanVaitros { get; set; }

    public virtual DbSet<Vaitro> Vaitros { get; set; }

    // JWT Authentication Models
    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<UserSession> UserSessions { get; set; }

    // Notification System
    public virtual DbSet<Notification> Notifications { get; set; }

    // Exam Assignments
    public virtual DbSet<ExamAssignment> ExamAssignments { get; set; }

    // Ky Thi & Ca Thi
    public virtual DbSet<KyThi> KyThis { get; set; }
    public virtual DbSet<CaThi> CaThis { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Baithi>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__BAITHI__3214EC077508193A");

            entity.ToTable("BAITHI");

            entity.Property(e => e.MaDeThi).HasMaxLength(50);
            entity.Property(e => e.ThoiGianNop).HasColumnType("datetime");
            entity.Property(e => e.TrangThai).HasMaxLength(50);

            entity.HasOne(d => d.IdDeThiNavigation).WithMany(p => p.Baithis)
                .HasForeignKey(d => d.IdDeThi)
                .HasConstraintName("FK__BAITHI__IdDeThi__52593CB8");

            entity.HasOne(d => d.IdTaiKhoanNavigation).WithMany(p => p.Baithis)
                .HasForeignKey(d => d.IdTaiKhoan)
                .HasConstraintName("FK__BAITHI__IdTaiKho__534D60F1");
        });

        modelBuilder.Entity<Canhbaogianlan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CANHBAOG__3214EC07EB40AF3B");

            entity.ToTable("CANHBAOGIANLAN");

            entity.Property(e => e.LoaiCanhBao).HasMaxLength(100);
            entity.Property(e => e.ThoiGian).HasColumnType("datetime");

            entity.HasOne(d => d.IdBaiThiNavigation).WithMany(p => p.Canhbaogianlans)
                .HasForeignKey(d => d.IdBaiThi)
                .HasConstraintName("FK__CANHBAOGI__IdBai__5441852A");
        });

        modelBuilder.Entity<Cauhoi>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CAUHOI__3214EC070C9BF858");

            entity.ToTable("CAUHOI");

            entity.Property(e => e.DaXoa).HasDefaultValue(false);
            entity.Property(e => e.DoKho).HasMaxLength(50);
            entity.Property(e => e.KhoaPhong).HasMaxLength(100);
            entity.Property(e => e.NgayCapNhat).HasColumnType("datetime");
            entity.Property(e => e.NgayTao).HasColumnType("datetime");

            entity.HasOne(d => d.IdDanhMucNavigation).WithMany(p => p.Cauhois)
                .HasForeignKey(d => d.IdDanhMuc)
                .HasConstraintName("FK__CAUHOI__IdDanhMu__5535A963");

            entity.HasOne(d => d.IdLoaiCauHoiNavigation).WithMany(p => p.Cauhois)
                .HasForeignKey(d => d.IdLoaiCauHoi)
                .HasConstraintName("FK__CAUHOI__IdLoaiCa__5629CD9C");
        });

        modelBuilder.Entity<Chitietlambai>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CHITIETL__3214EC07199D44E7");

            entity.ToTable("CHITIETLAMBAI");

            entity.Property(e => e.DaLuu).HasDefaultValue(false);
            entity.Property(e => e.ThoiGianTraLoi).HasColumnType("datetime");

            entity.HasOne(d => d.IdBaiThiNavigation).WithMany(p => p.Chitietlambais)
                .HasForeignKey(d => d.IdBaiThi)
                .HasConstraintName("FK__CHITIETLA__IdBai__571DF1D5");

            entity.HasOne(d => d.IdCauHoiNavigation).WithMany(p => p.Chitietlambais)
                .HasForeignKey(d => d.IdCauHoi)
                .HasConstraintName("FK__CHITIETLA__IdCau__5812160E");

            entity.HasOne(d => d.IdLuaChonDaChonNavigation).WithMany(p => p.Chitietlambais)
                .HasForeignKey(d => d.IdLuaChonDaChon)
                .HasConstraintName("FK__CHITIETLA__IdLua__59063A47");
        });

        modelBuilder.Entity<Danhmucauhoi>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__DANHMUCA__3214EC07169BFCA5");

            entity.ToTable("DANHMUCAUHOI");

            entity.Property(e => e.TenDanhMuc).HasMaxLength(255);
        });

        modelBuilder.Entity<Dethi>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__DETHI__3214EC07A61E2A85");

            entity.ToTable("DETHI");

            entity.Property(e => e.MaDeThi).HasMaxLength(50);
            entity.Property(e => e.NgayTao).HasColumnType("datetime");
            entity.Property(e => e.TenDeThi).HasMaxLength(255);
            entity.Property(e => e.ThoiGianBatDau).HasColumnType("datetime");
            entity.Property(e => e.TrangThai).HasMaxLength(50);
        });

        modelBuilder.Entity<DethiCauhoi>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__DETHI_CA__3214EC074EED9C55");

            entity.ToTable("DETHI_CAUHOI");

            entity.HasOne(d => d.IdCauHoiNavigation).WithMany(p => p.DethiCauhois)
                .HasForeignKey(d => d.IdCauHoi)
                .HasConstraintName("FK__DETHI_CAU__IdCau__59FA5E80");

            entity.HasOne(d => d.IdDeThiNavigation).WithMany(p => p.DethiCauhois)
                .HasForeignKey(d => d.IdDeThi)
                .HasConstraintName("FK__DETHI_CAU__IdDeT__5AEE82B9");
        });

        modelBuilder.Entity<Loaicauhoi>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__LOAICAUH__3214EC071DD715C9");

            entity.ToTable("LOAICAUHOI");

            entity.Property(e => e.MoTa).HasMaxLength(255);
            entity.Property(e => e.TenLoai).HasMaxLength(100);
        });

        modelBuilder.Entity<Logthaotac>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__LOGTHAOT__3214EC076B8FE2F7");

            entity.ToTable("LOGTHAOTAC");

            entity.Property(e => e.DiaChiIp)
                .HasMaxLength(50)
                .HasColumnName("DiaChi_IP");
            entity.Property(e => e.LoaiThaoTac).HasMaxLength(100);
            entity.Property(e => e.ThoiGian).HasColumnType("datetime");

            entity.HasOne(d => d.IdBaiThiNavigation).WithMany(p => p.Logthaotacs)
                .HasForeignKey(d => d.IdBaiThi)
                .HasConstraintName("FK__LOGTHAOTA__IdBai__5BE2A6F2");
        });

        modelBuilder.Entity<Luachon>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__LUACHON__3214EC07716560A0");

            entity.ToTable("LUACHON");

            entity.HasOne(d => d.IdCauHoiNavigation).WithMany(p => p.Luachons)
                .HasForeignKey(d => d.IdCauHoi)
                .HasConstraintName("FK__LUACHON__IdCauHo__5CD6CB2B");
        });

        modelBuilder.Entity<Phiendangnhap>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__PHIENDAN__3214EC0786D0DC9F");

            entity.ToTable("PHIENDANGNHAP");

            entity.Property(e => e.Ip)
                .HasMaxLength(50)
                .HasColumnName("IP");
            entity.Property(e => e.ThietBiUserAgent).HasColumnName("ThietBi_UserAgent");
            entity.Property(e => e.ThoiGianHetHan).HasColumnType("datetime");
            entity.Property(e => e.ThoiGianTao).HasColumnType("datetime");

            entity.HasOne(d => d.IdTaiKhoanNavigation).WithMany(p => p.Phiendangnhaps)
                .HasForeignKey(d => d.IdTaiKhoan)
                .HasConstraintName("FK__PHIENDANG__IdTai__5DCAEF64");
        });

        modelBuilder.Entity<Taikhoan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TAIKHOAN__3214EC07D2108DB4");

            entity.ToTable("TAIKHOAN");

            entity.Property(e => e.ChucDanh).HasMaxLength(100);
            entity.Property(e => e.KhoaPhong).HasMaxLength(100);
            entity.Property(e => e.MaNhanVien).HasMaxLength(50);
            entity.Property(e => e.MatKhau).HasMaxLength(255);
            entity.Property(e => e.TenDangNhap).HasMaxLength(100);
        });

        modelBuilder.Entity<TaikhoanVaitro>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TAIKHOAN__3214EC07DCEDD2F9");

            entity.ToTable("TAIKHOAN_VAITRO");

            entity.HasOne(d => d.IdTaiKhoanNavigation).WithMany(p => p.TaikhoanVaitros)
                .HasForeignKey(d => d.IdTaiKhoan)
                .HasConstraintName("FK__TAIKHOAN___IdTai__5EBF139D");

            entity.HasOne(d => d.IdVaiTroNavigation).WithMany(p => p.TaikhoanVaitros)
                .HasForeignKey(d => d.IdVaiTro)
                .HasConstraintName("FK__TAIKHOAN___IdVai__5FB337D6");
        });

        modelBuilder.Entity<Vaitro>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__VAITRO__3214EC07828E2FD1");

            entity.ToTable("VAITRO");

            entity.Property(e => e.MaVaiTro).HasMaxLength(50);
            entity.Property(e => e.MoTa).HasMaxLength(255);
            entity.Property(e => e.TenVaiTro).HasMaxLength(100);
        });

        // JWT Authentication Models Configuration
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("RefreshTokens");

            entity.Property(e => e.Token)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.UserId)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.ExpiresAt)
                .IsRequired();

            entity.Property(e => e.IpAddress)
                .HasMaxLength(45);

            entity.Property(e => e.UserAgent)
                .HasMaxLength(500);

            entity.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.Token)
                .IsUnique();

            entity.HasIndex(e => e.UserId);
        });

        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("UserSessions");

            entity.Property(e => e.SessionId)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.UserId)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.ExpiresAt)
                .IsRequired();

            entity.Property(e => e.IpAddress)
                .HasMaxLength(45);

            entity.Property(e => e.UserAgent)
                .HasMaxLength(500);

            entity.Property(e => e.EndReason)
                .HasMaxLength(50);

            entity.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.SessionId)
                .IsUnique();

            entity.HasIndex(e => e.UserId);
        });

        // Notification configuration
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Notifications");
            entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Type).HasMaxLength(50);
            entity.Property(e => e.RelatedUrl).HasMaxLength(500);
            entity.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.UserId);
        });

        // ExamAssignment configuration
        modelBuilder.Entity<ExamAssignment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("ExamAssignments");
            entity.HasOne(d => d.Exam)
                .WithMany()
                .HasForeignKey(d => d.ExamId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasIndex(e => new { e.ExamId, e.UserId }).IsUnique();
            entity.HasIndex(e => e.UserId);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
