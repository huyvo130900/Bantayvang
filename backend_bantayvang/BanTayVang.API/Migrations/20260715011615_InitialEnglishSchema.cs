using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BanTayVang.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialEnglishSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuestionCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MoTa = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__LOAICAUH__3214EC071DD715C9", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaVaiTro = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TenVaiTro = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MoTa = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__VAITRO__3214EC07828E2FD1", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Questions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuestionCategoryId = table.Column<int>(type: "int", nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    DaXoa = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    DoKho = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__CAUHOI__3214EC070C9BF858", x => x.Id);
                    table.ForeignKey(
                        name: "FK__CAUHOI__IdLoaiCa__5629CD9C",
                        column: x => x.QuestionCategoryId,
                        principalTable: "QuestionCategories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "QuestionOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuestionId = table.Column<int>(type: "int", nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LaDapAnDung = table.Column<bool>(type: "bit", nullable: true),
                    ThuTu = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__LUACHON__3214EC07716560A0", x => x.Id);
                    table.ForeignKey(
                        name: "FK__LUACHON__IdCauHo__5CD6CB2B",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ExamSubmissionId = table.Column<int>(type: "int", nullable: true),
                    PhuongThuc = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    DuongDan = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LoaiThaoTac = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ChiTiet = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ThoiGian = table.Column<DateTime>(type: "datetime", nullable: true),
                    DiaChi_IP = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MaHttp = table.Column<int>(type: "int", nullable: true),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__LOGTHAOT__3214EC076B8FE2F7", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CheatWarnings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamSubmissionId = table.Column<int>(type: "int", nullable: true),
                    LoaiCanhBao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MoTa = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ThoiGian = table.Column<DateTime>(type: "datetime", nullable: true),
                    SoLanViPham = table.Column<int>(type: "int", nullable: true),
                    MucDoNghiemTrong = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__CANHBAOG__3214EC07EB40AF3B", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaKhoa = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DepartmentName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<bool>(type: "bit", nullable: false),
                    DeptManagerId = table.Column<int>(type: "int", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExamCampaigns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CampaignCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CampaignName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    KhoaPhongId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ThoiGianBatDau = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ThoiGianKetThuc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DonViToChuc = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SoCauDungToiThieu = table.Column<int>(type: "int", nullable: true),
                    TongSoCauHoi = table.Column<int>(type: "int", nullable: true),
                    ThoiGianLamBai = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamCampaigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamCampaigns_Departments_KhoaPhongId",
                        column: x => x.KhoaPhongId,
                        principalTable: "Departments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ExamRegistrations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Cccd = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SoDienThoai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    MatKhauHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DonViCongTac = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ChuyenNganh = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    KhoaPhongId = table.Column<int>(type: "int", nullable: true),
                    MucDichThi = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NgayDangKy = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NguoiDuyetId = table.Column<int>(type: "int", nullable: true),
                    NgayDuyet = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamRegistrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamRegistrations_Departments_KhoaPhongId",
                        column: x => x.KhoaPhongId,
                        principalTable: "Departments",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaNhanVien = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Password = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ChucDanh = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    SoDienThoai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RoleId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<bool>(type: "bit", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LanDangNhapCuoi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IdKhoaQuanLy = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__TAIKHOAN__3214EC07D2108DB4", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Departments_IdKhoaQuanLy",
                        column: x => x.IdKhoaQuanLy,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ExamPapers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamPaperCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ExamPaperName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ThoiGianLamBai = table.Column<int>(type: "int", nullable: true),
                    TotalScore = table.Column<double>(type: "float", nullable: true),
                    ThoiGianBatDau = table.Column<DateTime>(type: "datetime", nullable: true),
                    LinkTruyCap = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true),
                    ChecksumData = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Department = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CongBoKetQua = table.Column<bool>(type: "bit", nullable: false),
                    NguoiCongBo = table.Column<int>(type: "int", nullable: true),
                    ThoiGianCongBo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    KyThiId = table.Column<int>(type: "int", nullable: true),
                    SoCauDungToiThieu = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__DETHI__3214EC07A61E2A85", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamPapers_ExamCampaigns_KyThiId",
                        column: x => x.KyThiId,
                        principalTable: "ExamCampaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RelatedUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Phiendangnhaps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Token = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ThoiGianTao = table.Column<DateTime>(type: "datetime", nullable: true),
                    ThoiGianHetHan = table.Column<DateTime>(type: "datetime", nullable: true),
                    IP = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ThietBiUserAgent = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__PHIENDAN__3214EC0786D0DC9F", x => x.Id);
                    table.ForeignKey(
                        name: "FK__PHIENDANG__IdTai__5DCAEF64",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Token = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    RoleId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__TAIKHOAN__3214EC07DCEDD2F9", x => x.Id);
                    table.ForeignKey(
                        name: "FK__TAIKHOAN___IdTai__5EBF139D",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK__TAIKHOAN___IdVai__5FB337D6",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EndReason = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExamAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssignedBy = table.Column<int>(type: "int", nullable: true),
                    CustomStartTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExtraMinutes = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamAssignments_ExamPapers_ExamId",
                        column: x => x.ExamId,
                        principalTable: "ExamPapers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExamAssignments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ExamPaperQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamPaperId = table.Column<int>(type: "int", nullable: true),
                    QuestionId = table.Column<int>(type: "int", nullable: true),
                    TrongSo = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__DETHI_CA__3214EC074EED9C55", x => x.Id);
                    table.ForeignKey(
                        name: "FK__DETHI_CAU__IdCau__59FA5E80",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK__DETHI_CAU__IdDeT__5AEE82B9",
                        column: x => x.ExamPaperId,
                        principalTable: "ExamPapers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ExamSubmissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamPaperId = table.Column<int>(type: "int", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    ThoiGianBatDau = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SubmitTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    TotalScore = table.Column<double>(type: "float", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CorrectAnswers = table.Column<int>(type: "int", nullable: true),
                    TongSoCau = table.Column<int>(type: "int", nullable: true),
                    DanhGiaKhoa = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CongBoRieng = table.Column<bool>(type: "bit", nullable: false),
                    ThoiGianCongBoRieng = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCongBoRieng = table.Column<int>(type: "int", nullable: true),
                    ExamPaperCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ExamCampaignId = table.Column<int>(type: "int", nullable: true),
                    TongSoCanhBao = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__BAITHI__3214EC077508193A", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamSubmissions_ExamCampaigns_ExamCampaignId",
                        column: x => x.ExamCampaignId,
                        principalTable: "ExamCampaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK__BAITHI__IdDeThi__52593CB8",
                        column: x => x.ExamPaperId,
                        principalTable: "ExamPapers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK__BAITHI__IdTaiKho__534D60F1",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SubmissionDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExamSubmissionId = table.Column<int>(type: "int", nullable: true),
                    QuestionId = table.Column<int>(type: "int", nullable: true),
                    IdLuaChonDaChon = table.Column<int>(type: "int", nullable: true),
                    ThoiGianTraLoi = table.Column<DateTime>(type: "datetime", nullable: true),
                    DaLuu = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    CauTraLoiTuLuan = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScoreObtained = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__CHITIETL__3214EC07199D44E7", x => x.Id);
                    table.ForeignKey(
                        name: "FK__CHITIETLA__IdBai__571DF1D5",
                        column: x => x.ExamSubmissionId,
                        principalTable: "ExamSubmissions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK__CHITIETLA__IdCau__5812160E",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK__CHITIETLA__IdLua__59063A47",
                        column: x => x.IdLuaChonDaChon,
                        principalTable: "QuestionOptions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ExamSubmissionId",
                table: "AuditLogs",
                column: "ExamSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CheatWarnings_ExamSubmissionId",
                table: "CheatWarnings",
                column: "ExamSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_DeptManagerId",
                table: "Departments",
                column: "DeptManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamAssignments_ExamId_UserId",
                table: "ExamAssignments",
                columns: new[] { "ExamId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamAssignments_UserId",
                table: "ExamAssignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamCampaigns_KhoaPhongId",
                table: "ExamCampaigns",
                column: "KhoaPhongId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamPaperQuestions_ExamPaperId",
                table: "ExamPaperQuestions",
                column: "ExamPaperId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamPaperQuestions_QuestionId",
                table: "ExamPaperQuestions",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamPapers_KyThiId",
                table: "ExamPapers",
                column: "KyThiId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamRegistrations_KhoaPhongId",
                table: "ExamRegistrations",
                column: "KhoaPhongId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSubmissions_ExamCampaignId",
                table: "ExamSubmissions",
                column: "ExamCampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSubmissions_ExamPaperId",
                table: "ExamSubmissions",
                column: "ExamPaperId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSubmissions_UserId",
                table: "ExamSubmissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Phiendangnhaps_UserId",
                table: "Phiendangnhaps",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionOptions_QuestionId",
                table: "QuestionOptions",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_Questions_QuestionCategoryId",
                table: "Questions",
                column: "QuestionCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionDetails_ExamSubmissionId",
                table: "SubmissionDetails",
                column: "ExamSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionDetails_IdLuaChonDaChon",
                table: "SubmissionDetails",
                column: "IdLuaChonDaChon");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionDetails_QuestionId",
                table: "SubmissionDetails",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId",
                table: "UserRoles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_IdKhoaQuanLy",
                table: "Users",
                column: "IdKhoaQuanLy");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_SessionId",
                table: "UserSessions",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_UserId",
                table: "UserSessions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_Users_UserId",
                table: "AuditLogs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK__LOGTHAOTA__IdBai__5BE2A6F2",
                table: "AuditLogs",
                column: "ExamSubmissionId",
                principalTable: "ExamSubmissions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK__CANHBAOGI__IdBai__5441852A",
                table: "CheatWarnings",
                column: "ExamSubmissionId",
                principalTable: "ExamSubmissions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Users_DeptManagerId",
                table: "Departments",
                column: "DeptManagerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Departments_Users_DeptManagerId",
                table: "Departments");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "CheatWarnings");

            migrationBuilder.DropTable(
                name: "ExamAssignments");

            migrationBuilder.DropTable(
                name: "ExamPaperQuestions");

            migrationBuilder.DropTable(
                name: "ExamRegistrations");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Phiendangnhaps");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "SubmissionDetails");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "UserSessions");

            migrationBuilder.DropTable(
                name: "ExamSubmissions");

            migrationBuilder.DropTable(
                name: "QuestionOptions");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "ExamPapers");

            migrationBuilder.DropTable(
                name: "Questions");

            migrationBuilder.DropTable(
                name: "ExamCampaigns");

            migrationBuilder.DropTable(
                name: "QuestionCategories");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Departments");
        }
    }
}
