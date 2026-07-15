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

    public virtual DbSet<ExamSubmission> ExamSubmissions { get; set; }

    public virtual DbSet<CheatWarning> CheatWarnings { get; set; }

    public virtual DbSet<Question> Questions { get; set; }

    public virtual DbSet<SubmissionDetail> SubmissionDetails { get; set; }

    public virtual DbSet<ExamPaper> ExamPapers { get; set; }

    public virtual DbSet<ExamPaperQuestion> ExamPaperQuestions { get; set; }

    public virtual DbSet<QuestionCategory> QuestionCategories { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<QuestionOption> QuestionOptions { get; set; }

    public virtual DbSet<Phiendangnhap> Phiendangnhaps { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    // JWT Authentication Models
    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<UserSession> UserSessions { get; set; }

    // Notification System
    public virtual DbSet<Notification> Notifications { get; set; }

    // Exam Assignments
    public virtual DbSet<ExamAssignment> ExamAssignments { get; set; }

    // Ky Thi
    public virtual DbSet<ExamCampaign> ExamCampaigns { get; set; }

    // Đăng Ký Thi
    public virtual DbSet<ExamRegistration> ExamRegistrations { get; set; }

    // Department Management (v2.0)
    public virtual DbSet<Department> Departments { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExamSubmission>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__BAITHI__3214EC077508193A");

            

            entity.Property(e => e.ExamPaperCode).HasMaxLength(50);
            entity.Property(e => e.SubmitTime).HasColumnType("datetime");
            entity.Property(e => e.Status).HasMaxLength(50);

            entity.HasOne(d => d.IdDeThiNavigation).WithMany(p => p.ExamSubmissions)
                .HasForeignKey(d => d.ExamPaperId)
                .HasConstraintName("FK__BAITHI__IdDeThi__52593CB8");

            entity.HasOne(d => d.IdTaiKhoanNavigation).WithMany(p => p.ExamSubmissions)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__BAITHI__IdTaiKho__534D60F1");

            entity.HasOne(d => d.KyThiNavigation).WithMany()
                .HasForeignKey(d => d.ExamCampaignId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        });


        modelBuilder.Entity<CheatWarning>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CANHBAOG__3214EC07EB40AF3B");

            

            entity.Property(e => e.WarningType).HasMaxLength(100);
            entity.Property(e => e.ActionTime).HasColumnType("datetime");

            entity.HasOne(d => d.IdBaiThiNavigation).WithMany(p => p.CheatWarnings)
                .HasForeignKey(d => d.ExamSubmissionId)
                .HasConstraintName("FK__CANHBAOGI__IdBai__5441852A");
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CAUHOI__3214EC070C9BF858");

            

            entity.Property(e => e.DaXoa).HasDefaultValue(false);
            entity.Property(e => e.Difficulty).HasMaxLength(50);
            entity.Property(e => e.Department).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.IdLoaiCauHoiNavigation).WithMany(p => p.Questions)
                .HasForeignKey(d => d.QuestionCategoryId)
                .HasConstraintName("FK__CAUHOI__IdLoaiCa__5629CD9C");
        });

        modelBuilder.Entity<SubmissionDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CHITIETL__3214EC07199D44E7");

            

            entity.Property(e => e.DaLuu).HasDefaultValue(false);
            entity.Property(e => e.ThoiGianTraLoi).HasColumnType("datetime");

            entity.HasOne(d => d.IdBaiThiNavigation).WithMany(p => p.SubmissionDetails)
                .HasForeignKey(d => d.ExamSubmissionId)
                .HasConstraintName("FK__CHITIETLA__IdBai__571DF1D5");

            entity.HasOne(d => d.IdCauHoiNavigation).WithMany(p => p.SubmissionDetails)
                .HasForeignKey(d => d.QuestionId)
                .HasConstraintName("FK__CHITIETLA__IdCau__5812160E");

            entity.HasOne(d => d.IdLuaChonDaChonNavigation).WithMany(p => p.SubmissionDetails)
                .HasForeignKey(d => d.SelectedOptionId)
                .HasConstraintName("FK__CHITIETLA__IdLua__59063A47");
        });

        modelBuilder.Entity<ExamPaper>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__DETHI__3214EC07A61E2A85");

            

            entity.Property(e => e.ExamPaperCode).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.ExamPaperName).HasMaxLength(255);
            entity.Property(e => e.StartTime).HasColumnType("datetime");
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.Department).HasMaxLength(200);
            entity.Property(e => e.ChecksumData).HasMaxLength(500);

            entity.HasOne(d => d.KyThiNavigation).WithMany()
                .HasForeignKey(d => d.ExamCampaignId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ExamPaperQuestion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__DETHI_CA__3214EC074EED9C55");

            

            entity.HasOne(d => d.IdCauHoiNavigation).WithMany(p => p.ExamPaperQuestions)
                .HasForeignKey(d => d.QuestionId)
                .HasConstraintName("FK__DETHI_CAU__IdCau__59FA5E80");

            entity.HasOne(d => d.IdDeThiNavigation).WithMany(p => p.ExamPaperQuestions)
                .HasForeignKey(d => d.ExamPaperId)
                .HasConstraintName("FK__DETHI_CAU__IdDeT__5AEE82B9");
        });

        modelBuilder.Entity<QuestionCategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__LOAICAUH__3214EC071DD715C9");

            

            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.CategoryName).HasMaxLength(100);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__LOGTHAOT__3214EC076B8FE2F7");

            

            entity.Property(e => e.DiaChiIp)
                .HasMaxLength(50)
                .HasColumnName("DiaChi_IP");
            entity.Property(e => e.LoaiThaoTac).HasMaxLength(100);
            entity.Property(e => e.ActionTime).HasColumnType("datetime");
            entity.Property(e => e.Username).HasMaxLength(100);
            entity.Property(e => e.PhuongThuc).HasMaxLength(10);
            entity.Property(e => e.DuongDan).HasMaxLength(500);
            entity.Property(e => e.Department).HasMaxLength(100);
            entity.Property(e => e.UserAgent).HasMaxLength(500);

            entity.HasOne(d => d.IdBaiThiNavigation).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.ExamSubmissionId)
                .HasConstraintName("FK__LOGTHAOTA__IdBai__5BE2A6F2");

            entity.HasOne(d => d.User).WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);
        });


        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.MaKhoa).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DepartmentName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.DeptManager)
                .WithMany()
                .HasForeignKey(d => d.DeptManagerId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);
        });

        // DeptManagerDeptId on User
        modelBuilder.Entity<User>()
            .HasOne(t => t.ManagedDepartment)
            .WithMany()
            .HasForeignKey(t => t.DeptManagerDeptId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        modelBuilder.Entity<QuestionOption>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__LUACHON__3214EC07716560A0");

            

            entity.HasOne(d => d.IdCauHoiNavigation).WithMany(p => p.QuestionOptions)
                .HasForeignKey(d => d.QuestionId)
                .HasConstraintName("FK__LUACHON__IdCauHo__5CD6CB2B");
        });

        modelBuilder.Entity<Phiendangnhap>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__PHIENDAN__3214EC0786D0DC9F");

            

            entity.Property(e => e.Ip)
                .HasMaxLength(50)
                .HasColumnName("IP");
            
            entity.Property(e => e.ThoiGianHetHan).HasColumnType("datetime");
            entity.Property(e => e.ThoiGianTao).HasColumnType("datetime");

            entity.HasOne(d => d.IdTaiKhoanNavigation).WithMany(p => p.Phiendangnhaps)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__PHIENDANG__IdTai__5DCAEF64");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TAIKHOAN__3214EC07D2108DB4");

            
            entity.HasQueryFilter(e => !e.IsDeleted);

            entity.Property(e => e.JobTitle).HasMaxLength(100);
            entity.Property(e => e.Department).HasMaxLength(100);
            entity.Property(e => e.EmployeeCode).HasMaxLength(50);
            entity.Property(e => e.Password).HasMaxLength(255);
            entity.Property(e => e.Username).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.SoDienThoai).HasMaxLength(50);
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TAIKHOAN__3214EC07DCEDD2F9");

            

            entity.HasOne(d => d.IdTaiKhoanNavigation).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__TAIKHOAN___IdTai__5EBF139D");

            entity.HasOne(d => d.IdVaiTroNavigation).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("FK__TAIKHOAN___IdVai__5FB337D6");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__VAITRO__3214EC07828E2FD1");

            

            entity.Property(e => e.MaVaiTro).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.RoleName).HasMaxLength(100);
        });

        // JWT Authentication Models Configuration
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            

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
