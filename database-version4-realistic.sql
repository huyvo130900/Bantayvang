USE [master]
GO

-- Nếu database cũ đã tồn tại thì xóa để tạo mới sạch hoàn toàn.
-- Lưu ý: chạy đoạn này sẽ xóa toàn bộ dữ liệu cũ trong database HeThongBanTayVang.
IF DB_ID(N'HeThongBanTayVang') IS NOT NULL
BEGIN
    ALTER DATABASE [HeThongBanTayVang] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [HeThongBanTayVang];
END
GO

-- Tạo database bằng đường dẫn mặc định của SQL Server hiện tại.
-- Không hard-code đường dẫn C:\Program Files\... vì mỗi máy có thư mục DATA khác nhau.
CREATE DATABASE [HeThongBanTayVang];
GO

ALTER DATABASE [HeThongBanTayVang] SET COMPATIBILITY_LEVEL = 160
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [HeThongBanTayVang].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [HeThongBanTayVang] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [HeThongBanTayVang] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [HeThongBanTayVang] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [HeThongBanTayVang] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [HeThongBanTayVang] SET ARITHABORT OFF 
GO
ALTER DATABASE [HeThongBanTayVang] SET AUTO_CLOSE OFF 
GO
ALTER DATABASE [HeThongBanTayVang] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [HeThongBanTayVang] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [HeThongBanTayVang] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [HeThongBanTayVang] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [HeThongBanTayVang] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [HeThongBanTayVang] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [HeThongBanTayVang] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [HeThongBanTayVang] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [HeThongBanTayVang] SET  DISABLE_BROKER 
GO
ALTER DATABASE [HeThongBanTayVang] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [HeThongBanTayVang] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [HeThongBanTayVang] SET TRUSTWORTHY OFF 
GO
ALTER DATABASE [HeThongBanTayVang] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO
ALTER DATABASE [HeThongBanTayVang] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [HeThongBanTayVang] SET READ_COMMITTED_SNAPSHOT OFF 
GO
ALTER DATABASE [HeThongBanTayVang] SET HONOR_BROKER_PRIORITY OFF 
GO
ALTER DATABASE [HeThongBanTayVang] SET RECOVERY FULL 
GO
ALTER DATABASE [HeThongBanTayVang] SET  MULTI_USER 
GO
ALTER DATABASE [HeThongBanTayVang] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [HeThongBanTayVang] SET DB_CHAINING OFF 
GO
ALTER DATABASE [HeThongBanTayVang] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO
ALTER DATABASE [HeThongBanTayVang] SET TARGET_RECOVERY_TIME = 60 SECONDS 
GO
ALTER DATABASE [HeThongBanTayVang] SET DELAYED_DURABILITY = DISABLED 
GO
ALTER DATABASE [HeThongBanTayVang] SET ACCELERATED_DATABASE_RECOVERY = OFF  
GO
ALTER DATABASE [HeThongBanTayVang] SET QUERY_STORE = ON
GO
ALTER DATABASE [HeThongBanTayVang] SET QUERY_STORE (OPERATION_MODE = READ_WRITE, CLEANUP_POLICY = (STALE_QUERY_THRESHOLD_DAYS = 30), DATA_FLUSH_INTERVAL_SECONDS = 900, INTERVAL_LENGTH_MINUTES = 60, MAX_STORAGE_SIZE_MB = 1000, QUERY_CAPTURE_MODE = AUTO, SIZE_BASED_CLEANUP_MODE = AUTO, MAX_PLANS_PER_QUERY = 200, WAIT_STATS_CAPTURE_MODE = ON)
GO
USE [HeThongBanTayVang]
GO
-- =============================================================
-- Bộ dữ liệu mẫu đã được làm sạch và chuẩn hóa lại theo ngữ cảnh
-- hệ thống thi Bàn tay vàng điều dưỡng. Dữ liệu bên dưới là dữ liệu
-- giả lập nhưng được thiết kế theo nghiệp vụ thật: khoa/phòng, tài
-- khoản, kỳ thi, đề thi, câu hỏi, bài làm, cảnh báo gian lận, phiên
-- đăng nhập và thông báo đều có quan hệ nhất quán.
-- =============================================================

/****** Object:  Schema [PhucHuy]    Script Date: 01/07/2026 12:25:06 SA ******/
CREATE SCHEMA [PhucHuy]
GO
/****** Object:  Table [dbo].[__EFMigrationsHistory]    Script Date: 01/07/2026 12:25:06 SA ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[__EFMigrationsHistory](
	[MigrationId] [nvarchar](150) NOT NULL,
	[ProductVersion] [nvarchar](32) NOT NULL,
 CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY CLUSTERED 
(
	[MigrationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[BAITHI]    Script Date: 01/07/2026 12:25:06 SA ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[BAITHI](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[IdTaiKhoan] [int] NULL,
	[IdDeThi] [int] NULL,
	[TrangThai] [nvarchar](50) NULL,
	[MaDeThi] [nvarchar](50) NULL,
	[ThoiGianNop] [datetime] NULL,
	[TongDiem] [float] NULL,
	[SoCauDung] [int] NULL,
	[TongSoCau] [int] NULL,
	[TongSoCanhBao] [int] NULL,
	[ThoiGianBatDau] [datetime2](7) NULL,
	[NgayCapNhat] [datetime2](7) NULL,
	[LyDoKetThuc] [nvarchar](255) NULL,
	[DanhGiaKhoa] [nvarchar](max) NULL,
	[CongBoRieng] [bit] NOT NULL,
	[ThoiGianCongBoRieng] [datetime] NULL,
	[NguoiCongBoRieng] [int] NULL,
	[IdKyThi] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[CANHBAOGIANLAN]    Script Date: 01/07/2026 12:25:06 SA ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CANHBAOGIANLAN](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[IdBaiThi] [int] NULL,
	[LoaiCanhBao] [nvarchar](100) NULL,
	[MoTa] [nvarchar](max) NULL,
	[ThoiGian] [datetime] NULL,
	[SoLanViPham] [int] NULL,
	[MucDoNghiemTrong] [nvarchar](50) NULL,
	[CorrelationId] [nvarchar](100) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[CAUHOI]    Script Date: 01/07/2026 12:25:06 SA ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CAUHOI](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[IdLoaiCauHoi] [int] NULL,
	[NoiDung] [nvarchar](max) NULL,
	[NguoiTao] [int] NULL,
	[NgayTao] [datetime] NULL,
	[NgayCapNhat] [datetime] NULL,
	[NguoiCapNhat] [int] NULL,
	[DaXoa] [bit] NULL,
	[DoKho] [nvarchar](50) NULL,
	[KhoaPhong] [nvarchar](100) NULL,
	[HinhAnh] [nvarchar](max) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[CHITIETLAMBAI]    Script Date: 01/07/2026 12:25:06 SA ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[CHITIETLAMBAI](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[IdBaiThi] [int] NULL,
	[IdCauHoi] [int] NULL,
	[IdLuaChonDaChon] [int] NULL,
	[ThoiGianTraLoi] [datetime] NULL,
	[DaLuu] [bit] NULL,
	[CauTraLoiTuLuan] [nvarchar](max) NULL,
	[DiemDatDuoc] [float] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DETHI]    Script Date: 01/07/2026 12:25:06 SA ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DETHI](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[MaDeThi] [nvarchar](50) NULL,
	[TenDeThi] [nvarchar](255) NULL,
	[ThoiGianLamBai] [int] NULL,
	[TongDiem] [float] NULL,
	[ThoiGianBatDau] [datetime] NULL,
	[LinkTruyCap] [nvarchar](max) NULL,
	[TrangThai] [nvarchar](50) NULL,
	[NguoiTao] [int] NULL,
	[NgayTao] [datetime] NULL,
	[ChecksumData] [nvarchar](500) NULL,
	[NguoiCapNhat] [int] NULL,
	[NgayCapNhat] [datetime2](7) NULL,
	[KhoaPhong] [nvarchar](200) NULL,
	[CongBoKetQua] [bit] NOT NULL,
	[NguoiCongBo] [int] NULL,
	[ThoiGianCongBo] [datetime] NULL,
	[KyThiId] [int] NULL,
	[SoCauDungToiThieu] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DETHI_CAUHOI]    Script Date: 01/07/2026 12:25:06 SA ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DETHI_CAUHOI](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[IdDeThi] [int] NULL,
	[IdCauHoi] [int] NULL,
	[TrongSo] [float] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[KHOA_PHONG]    Script Date: 01/07/2026 12:25:06 SA ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[KHOA_PHONG](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[MaKhoa] [nvarchar](50) NOT NULL,
	[TenKhoa] [nvarchar](255) NOT NULL,
	[MoTa] [nvarchar](1000) NULL,
	[TrangThai] [bit] NOT NULL,
	[DeptManagerId] [int] NULL,
	[NguoiTao] [int] NULL,
	[NgayTao] [datetime] NOT NULL,
	[NgayCapNhat] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[KyThi]    Script Date: 01/07/2026 12:25:06 SA ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[KyThi](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[MaKyThi] [nvarchar](50) NOT NULL,
	[TenKyThi] [nvarchar](255) NOT NULL,
	[MoTa] [nvarchar](1000) NULL,
	[TrangThai] [nvarchar](50) NOT NULL,
	[ThoiGianBatDau] [datetime2](7) NULL,
	[ThoiGianKetThuc] [datetime2](7) NULL,
	[NguoiTao] [int] NULL,
	[NgayTao] [datetime] NOT NULL,
	[NgayCapNhat] [datetime2](7) NULL,
	[DonViToChuc] [nvarchar](100) NULL,
	[KhoaPhongId] [int] NULL,
	[SoCauDungToiThieu] [int] NULL,
	[TongSoCauHoi] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[LOAICAUHOI]    Script Date: 01/07/2026 12:25:06 SA ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[LOAICAUHOI](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[TenLoai] [nvarchar](100) NULL,
	[MoTa] [nvarchar](255) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[LOGTHAOTAC]    Script Date: 01/07/2026 12:25:06 SA ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[LOGTHAOTAC](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[IdBaiThi] [int] NULL,
	[LoaiThaoTac] [nvarchar](100) NULL,
	[ChiTiet] [nvarchar](max) NULL,
	[ThoiGian] [datetime] NULL,
	[DiaChi_IP] [nvarchar](50) NULL,
	[UserAgent] [nvarchar](max) NULL,
	[IdTaiKhoan] [int] NULL,
	[TenDangNhap] [nvarchar](100) NULL,
	[PhuongThuc] [nvarchar](10) NULL,
	[DuongDan] [nvarchar](500) NULL,
	[MaHttp] [int] NULL,
	[KhoaPhong] [nvarchar](100) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[LUACHON]    Script Date: 01/07/2026 12:25:06 SA ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[LUACHON](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[IdCauHoi] [int] NULL,
	[NoiDung] [nvarchar](max) NULL,
	[LaDapAnDung] [bit] NULL,
	[ThuTu] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PHANCONG_THI]    Script Date: 01/07/2026 12:25:06 SA ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PHANCONG_THI](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[ExamId] [int] NOT NULL,
	[UserId] [int] NOT NULL,
	[AssignedAt] [datetime2](7) NOT NULL,
	[AssignedBy] [int] NULL,
	[CustomStartTime] [datetime2](7) NULL,
	[ExtraMinutes] [int] NULL,
	[IsActive] [bit] NOT NULL,
	[Note] [nvarchar](500) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PHIEN_NGUOIDUNG]    Script Date: 01/07/2026 12:25:06 SA ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PHIEN_NGUOIDUNG](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[SessionId] [nvarchar](100) NOT NULL,
	[UserId] [int] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[ExpiresAt] [datetime2](7) NOT NULL,
	[LastActivityAt] [datetime2](7) NULL,
	[IpAddress] [nvarchar](45) NULL,
	[UserAgent] [nvarchar](500) NULL,
	[IsActive] [bit] NOT NULL,
	[EndReason] [nvarchar](50) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PHIENDANGNHAP]    Script Date: 01/07/2026 12:25:06 SA ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PHIENDANGNHAP](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[IdTaiKhoan] [int] NULL,
	[Token] [nvarchar](max) NULL,
	[ThoiGianTao] [datetime] NULL,
	[ThoiGianHetHan] [datetime] NULL,
	[IP] [nvarchar](50) NULL,
	[ThietBi_UserAgent] [nvarchar](max) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TAIKHOAN]    Script Date: 01/07/2026 12:25:06 SA ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TAIKHOAN](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[MaNhanVien] [nvarchar](50) NULL,
	[TenDangNhap] [nvarchar](100) NULL,
	[MatKhau] [nvarchar](255) NULL,
	[ChucDanh] [nvarchar](100) NULL,
	[KhoaPhong] [nvarchar](100) NULL,
	[HoTen] [nvarchar](255) NULL,
	[IdVaiTro] [int] NULL,
	[TrangThai] [bit] NULL,
	[NgayTao] [datetime2](7) NULL,
	[NgayCapNhat] [datetime2](7) NULL,
	[LanDangNhapCuoi] [datetime2](7) NULL,
	[IdKhoaQuanLy] [int] NULL,
	[Email] [nvarchar](255) NULL,
	[SoDienThoai] [nvarchar](50) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TAIKHOAN_VAITRO]    Script Date: 01/07/2026 12:25:06 SA ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TAIKHOAN_VAITRO](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[IdTaiKhoan] [int] NULL,
	[IdVaiTro] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[THONGBAO]    Script Date: 01/07/2026 12:25:06 SA ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[THONGBAO](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NULL,
	[Title] [nvarchar](255) NOT NULL,
	[Message] [nvarchar](1000) NOT NULL,
	[Type] [nvarchar](50) NOT NULL,
	[IsRead] [bit] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[ReadAt] [datetime2](7) NULL,
	[RelatedUrl] [nvarchar](500) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TOKEN_LAM_MOI]    Script Date: 01/07/2026 12:25:06 SA ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TOKEN_LAM_MOI](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Token] [nvarchar](500) NOT NULL,
	[UserId] [int] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[ExpiresAt] [datetime2](7) NOT NULL,
	[IsUsed] [bit] NOT NULL,
	[IsRevoked] [bit] NOT NULL,
	[IpAddress] [nvarchar](45) NULL,
	[UserAgent] [nvarchar](500) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[VAITRO]    Script Date: 01/07/2026 12:25:06 SA ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[VAITRO](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[MaVaiTro] [nvarchar](50) NULL,
	[TenVaiTro] [nvarchar](100) NULL,
	[MoTa] [nvarchar](255) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
SET IDENTITY_INSERT [dbo].[BAITHI] ON 

INSERT [dbo].[BAITHI] ([Id], [IdTaiKhoan], [IdDeThi], [TrangThai], [MaDeThi], [ThoiGianNop], [TongDiem], [SoCauDung], [TongSoCau], [TongSoCanhBao], [ThoiGianBatDau], [NgayCapNhat], [LyDoKetThuc], [DanhGiaKhoa], [CongBoRieng], [ThoiGianCongBoRieng], [NguoiCongBoRieng], [IdKyThi]) VALUES (1, 18, 1, N'Completed', N'BTV_2026_BV_DE01', CAST(N'2026-06-20T09:03:31.000' AS DateTime), 18.0, 18, 20, 0, CAST(N'2026-06-20T08:30:42.000000' AS DateTime2), CAST(N'2026-06-20T09:05:31.000000' AS DateTime2), N'Nộp bài đúng hạn', N'Đạt yêu cầu, thao tác ổn định trong suốt quá trình làm bài.', 0, NULL, NULL, 1)
INSERT [dbo].[BAITHI] ([Id], [IdTaiKhoan], [IdDeThi], [TrangThai], [MaDeThi], [ThoiGianNop], [TongDiem], [SoCauDung], [TongSoCau], [TongSoCanhBao], [ThoiGianBatDau], [NgayCapNhat], [LyDoKetThuc], [DanhGiaKhoa], [CongBoRieng], [ThoiGianCongBoRieng], [NguoiCongBoRieng], [IdKyThi]) VALUES (2, 19, 1, N'Completed', N'BTV_2026_BV_DE01', CAST(N'2026-06-20T08:55:24.000' AS DateTime), 15.0, 15, 20, 1, CAST(N'2026-06-20T08:31:05.000000' AS DateTime2), CAST(N'2026-06-20T08:57:24.000000' AS DateTime2), N'Nộp bài đúng hạn', N'Đạt yêu cầu, có một cảnh báo chuyển cửa sổ cần nhắc nhở.', 0, NULL, NULL, 1)
INSERT [dbo].[BAITHI] ([Id], [IdTaiKhoan], [IdDeThi], [TrangThai], [MaDeThi], [ThoiGianNop], [TongDiem], [SoCauDung], [TongSoCau], [TongSoCanhBao], [ThoiGianBatDau], [NgayCapNhat], [LyDoKetThuc], [DanhGiaKhoa], [CongBoRieng], [ThoiGianCongBoRieng], [NguoiCongBoRieng], [IdKyThi]) VALUES (3, 20, 1, N'Completed', N'BTV_2026_BV_DE01', CAST(N'2026-06-20T08:59:56.000' AS DateTime), 13.0, 13, 20, 0, CAST(N'2026-06-20T08:32:10.000000' AS DateTime2), CAST(N'2026-06-20T09:01:56.000000' AS DateTime2), N'Nộp bài đúng hạn', N'Chưa đạt ngưỡng công bố kết quả chung, cần ôn lại nhóm câu hỏi an toàn thuốc.', 1, CAST(N'2026-06-20T11:59:56.000' AS DateTime), 3, 1)
INSERT [dbo].[BAITHI] ([Id], [IdTaiKhoan], [IdDeThi], [TrangThai], [MaDeThi], [ThoiGianNop], [TongDiem], [SoCauDung], [TongSoCau], [TongSoCanhBao], [ThoiGianBatDau], [NgayCapNhat], [LyDoKetThuc], [DanhGiaKhoa], [CongBoRieng], [ThoiGianCongBoRieng], [NguoiCongBoRieng], [IdKyThi]) VALUES (4, 21, 1, N'Completed', N'BTV_2026_BV_DE01', CAST(N'2026-06-20T08:57:23.000' AS DateTime), 17.0, 17, 20, 0, CAST(N'2026-06-20T08:30:58.000000' AS DateTime2), CAST(N'2026-06-20T08:59:23.000000' AS DateTime2), N'Nộp bài đúng hạn', N'Đạt yêu cầu.', 0, NULL, NULL, 1)
INSERT [dbo].[BAITHI] ([Id], [IdTaiKhoan], [IdDeThi], [TrangThai], [MaDeThi], [ThoiGianNop], [TongDiem], [SoCauDung], [TongSoCau], [TongSoCanhBao], [ThoiGianBatDau], [NgayCapNhat], [LyDoKetThuc], [DanhGiaKhoa], [CongBoRieng], [ThoiGianCongBoRieng], [NguoiCongBoRieng], [IdKyThi]) VALUES (5, 22, 1, N'Completed', N'BTV_2026_BV_DE01', CAST(N'2026-06-20T09:18:57.000' AS DateTime), 14.0, 14, 20, 2, CAST(N'2026-06-20T08:33:11.000000' AS DateTime2), CAST(N'2026-06-20T09:20:57.000000' AS DateTime2), N'Hết thời gian làm bài', N'Vừa đạt ngưỡng, cần rút kinh nghiệm về quản lý thời gian.', 0, NULL, NULL, 1)
INSERT [dbo].[BAITHI] ([Id], [IdTaiKhoan], [IdDeThi], [TrangThai], [MaDeThi], [ThoiGianNop], [TongDiem], [SoCauDung], [TongSoCau], [TongSoCanhBao], [ThoiGianBatDau], [NgayCapNhat], [LyDoKetThuc], [DanhGiaKhoa], [CongBoRieng], [ThoiGianCongBoRieng], [NguoiCongBoRieng], [IdKyThi]) VALUES (6, 23, 1, N'Completed', N'BTV_2026_BV_DE01', CAST(N'2026-06-20T09:08:13.000' AS DateTime), 19.0, 19, 20, 0, CAST(N'2026-06-20T08:31:47.000000' AS DateTime2), CAST(N'2026-06-20T09:10:13.000000' AS DateTime2), N'Nộp bài đúng hạn', N'Kết quả tốt, có thể tham gia vòng thực hành.', 0, NULL, NULL, 1)
INSERT [dbo].[BAITHI] ([Id], [IdTaiKhoan], [IdDeThi], [TrangThai], [MaDeThi], [ThoiGianNop], [TongDiem], [SoCauDung], [TongSoCau], [TongSoCanhBao], [ThoiGianBatDau], [NgayCapNhat], [LyDoKetThuc], [DanhGiaKhoa], [CongBoRieng], [ThoiGianCongBoRieng], [NguoiCongBoRieng], [IdKyThi]) VALUES (7, 24, 1, N'Completed', N'BTV_2026_BV_DE01', CAST(N'2026-06-20T09:15:27.000' AS DateTime), 12.0, 12, 20, 1, CAST(N'2026-06-20T08:34:02.000000' AS DateTime2), CAST(N'2026-06-20T09:17:27.000000' AS DateTime2), N'Nộp bài đúng hạn', N'Chưa đạt, cần ôn lại kiến thức kiểm soát nhiễm khuẩn và xử trí cấp cứu.', 1, CAST(N'2026-06-20T12:15:27.000' AS DateTime), 3, 1)
INSERT [dbo].[BAITHI] ([Id], [IdTaiKhoan], [IdDeThi], [TrangThai], [MaDeThi], [ThoiGianNop], [TongDiem], [SoCauDung], [TongSoCau], [TongSoCanhBao], [ThoiGianBatDau], [NgayCapNhat], [LyDoKetThuc], [DanhGiaKhoa], [CongBoRieng], [ThoiGianCongBoRieng], [NguoiCongBoRieng], [IdKyThi]) VALUES (8, 18, 2, N'Completed', N'CC_NHI_2026_DE01', CAST(N'2026-06-24T14:40:40.000' AS DateTime), 16.0, 16, 20, 0, CAST(N'2026-06-24T14:15:22.000000' AS DateTime2), CAST(N'2026-06-24T14:42:40.000000' AS DateTime2), N'Nộp bài đúng hạn', N'Đạt yêu cầu chuyên đề cấp cứu.', 0, NULL, NULL, 2)
INSERT [dbo].[BAITHI] ([Id], [IdTaiKhoan], [IdDeThi], [TrangThai], [MaDeThi], [ThoiGianNop], [TongDiem], [SoCauDung], [TongSoCau], [TongSoCanhBao], [ThoiGianBatDau], [NgayCapNhat], [LyDoKetThuc], [DanhGiaKhoa], [CongBoRieng], [ThoiGianCongBoRieng], [NguoiCongBoRieng], [IdKyThi]) VALUES (9, 19, 2, N'Completed', N'CC_NHI_2026_DE01', CAST(N'2026-06-24T14:51:25.000' AS DateTime), 14.0, 14, 20, 0, CAST(N'2026-06-24T14:16:03.000000' AS DateTime2), CAST(N'2026-06-24T14:53:25.000000' AS DateTime2), N'Nộp bài đúng hạn', N'Đạt yêu cầu.', 0, NULL, NULL, 2)
INSERT [dbo].[BAITHI] ([Id], [IdTaiKhoan], [IdDeThi], [TrangThai], [MaDeThi], [ThoiGianNop], [TongDiem], [SoCauDung], [TongSoCau], [TongSoCanhBao], [ThoiGianBatDau], [NgayCapNhat], [LyDoKetThuc], [DanhGiaKhoa], [CongBoRieng], [ThoiGianCongBoRieng], [NguoiCongBoRieng], [IdKyThi]) VALUES (10, 20, 2, N'Completed', N'CC_NHI_2026_DE01', CAST(N'2026-06-24T14:49:57.000' AS DateTime), 11.0, 11, 20, 2, CAST(N'2026-06-24T14:16:15.000000' AS DateTime2), CAST(N'2026-06-24T14:51:57.000000' AS DateTime2), N'Nộp bài đúng hạn', N'Chưa đạt, cần ôn lại hồi sức tim phổi và phân loại cấp cứu.', 1, CAST(N'2026-06-24T17:49:57.000' AS DateTime), 3, 2)
INSERT [dbo].[BAITHI] ([Id], [IdTaiKhoan], [IdDeThi], [TrangThai], [MaDeThi], [ThoiGianNop], [TongDiem], [SoCauDung], [TongSoCau], [TongSoCanhBao], [ThoiGianBatDau], [NgayCapNhat], [LyDoKetThuc], [DanhGiaKhoa], [CongBoRieng], [ThoiGianCongBoRieng], [NguoiCongBoRieng], [IdKyThi]) VALUES (11, 21, 2, N'Completed', N'CC_NHI_2026_DE01', CAST(N'2026-06-24T14:43:34.000' AS DateTime), 18.0, 18, 20, 0, CAST(N'2026-06-24T14:15:54.000000' AS DateTime2), CAST(N'2026-06-24T14:45:34.000000' AS DateTime2), N'Nộp bài đúng hạn', N'Kết quả tốt.', 0, NULL, NULL, 2)
INSERT [dbo].[BAITHI] ([Id], [IdTaiKhoan], [IdDeThi], [TrangThai], [MaDeThi], [ThoiGianNop], [TongDiem], [SoCauDung], [TongSoCau], [TongSoCanhBao], [ThoiGianBatDau], [NgayCapNhat], [LyDoKetThuc], [DanhGiaKhoa], [CongBoRieng], [ThoiGianCongBoRieng], [NguoiCongBoRieng], [IdKyThi]) VALUES (12, 22, 2, N'Completed', N'CC_NHI_2026_DE01', CAST(N'2026-06-24T14:42:12.000' AS DateTime), 13.0, 13, 20, 1, CAST(N'2026-06-24T14:16:30.000000' AS DateTime2), CAST(N'2026-06-24T14:44:12.000000' AS DateTime2), N'Nộp bài đúng hạn', N'Đạt ngưỡng tối thiểu, cần củng cố phần nhận định ban đầu.', 0, NULL, NULL, 2)
INSERT [dbo].[BAITHI] ([Id], [IdTaiKhoan], [IdDeThi], [TrangThai], [MaDeThi], [ThoiGianNop], [TongDiem], [SoCauDung], [TongSoCau], [TongSoCanhBao], [ThoiGianBatDau], [NgayCapNhat], [LyDoKetThuc], [DanhGiaKhoa], [CongBoRieng], [ThoiGianCongBoRieng], [NguoiCongBoRieng], [IdKyThi]) VALUES (13, 30, 3, N'Completed', N'KSNK_2026_DOT1_DE01', CAST(N'2026-06-30T08:39:44.000' AS DateTime), 17.0, 17, 20, 0, CAST(N'2026-06-30T08:05:18.000000' AS DateTime2), CAST(N'2026-06-30T08:41:44.000000' AS DateTime2), N'Nộp bài đúng hạn', NULL, 0, NULL, NULL, 3)
INSERT [dbo].[BAITHI] ([Id], [IdTaiKhoan], [IdDeThi], [TrangThai], [MaDeThi], [ThoiGianNop], [TongDiem], [SoCauDung], [TongSoCau], [TongSoCanhBao], [ThoiGianBatDau], [NgayCapNhat], [LyDoKetThuc], [DanhGiaKhoa], [CongBoRieng], [ThoiGianCongBoRieng], [NguoiCongBoRieng], [IdKyThi]) VALUES (14, 31, 3, N'Completed', N'KSNK_2026_DOT1_DE01', CAST(N'2026-06-30T08:30:18.000' AS DateTime), 15.0, 15, 20, 1, CAST(N'2026-06-30T08:07:42.000000' AS DateTime2), CAST(N'2026-06-30T08:32:18.000000' AS DateTime2), N'Nộp bài đúng hạn', NULL, 0, NULL, NULL, 3)
INSERT [dbo].[BAITHI] ([Id], [IdTaiKhoan], [IdDeThi], [TrangThai], [MaDeThi], [ThoiGianNop], [TongDiem], [SoCauDung], [TongSoCau], [TongSoCanhBao], [ThoiGianBatDau], [NgayCapNhat], [LyDoKetThuc], [DanhGiaKhoa], [CongBoRieng], [ThoiGianCongBoRieng], [NguoiCongBoRieng], [IdKyThi]) VALUES (15, 32, 3, N'Completed', N'KSNK_2026_DOT1_DE01', CAST(N'2026-06-30T08:27:26.000' AS DateTime), 12.0, 12, 20, 0, CAST(N'2026-06-30T08:06:05.000000' AS DateTime2), CAST(N'2026-06-30T08:29:26.000000' AS DateTime2), N'Nộp bài đúng hạn', NULL, 0, NULL, NULL, 3)
INSERT [dbo].[BAITHI] ([Id], [IdTaiKhoan], [IdDeThi], [TrangThai], [MaDeThi], [ThoiGianNop], [TongDiem], [SoCauDung], [TongSoCau], [TongSoCanhBao], [ThoiGianBatDau], [NgayCapNhat], [LyDoKetThuc], [DanhGiaKhoa], [CongBoRieng], [ThoiGianCongBoRieng], [NguoiCongBoRieng], [IdKyThi]) VALUES (16, 33, 3, N'Completed', N'KSNK_2026_DOT1_DE01', CAST(N'2026-06-30T08:31:38.000' AS DateTime), 16.0, 16, 20, 2, CAST(N'2026-06-30T08:09:10.000000' AS DateTime2), CAST(N'2026-06-30T08:33:38.000000' AS DateTime2), N'Nộp bài đúng hạn', NULL, 0, NULL, NULL, 3)
SET IDENTITY_INSERT [dbo].[BAITHI] OFF
GO
SET IDENTITY_INSERT [dbo].[CANHBAOGIANLAN] ON 

INSERT [dbo].[CANHBAOGIANLAN] ([Id], [IdBaiThi], [LoaiCanhBao], [MoTa], [ThoiGian], [SoLanViPham], [MucDoNghiemTrong], [CorrelationId]) VALUES (1, 2, N'TAB_SWITCH', N'Thí sinh chuyển sang cửa sổ khác trong quá trình làm bài', CAST(N'2026-06-20T08:44:19.000' AS DateTime), 1, N'Nhẹ', N'CB-002-01')
INSERT [dbo].[CANHBAOGIANLAN] ([Id], [IdBaiThi], [LoaiCanhBao], [MoTa], [ThoiGian], [SoLanViPham], [MucDoNghiemTrong], [CorrelationId]) VALUES (2, 5, N'BROWSER_FOCUS_LOST', N'Cửa sổ trình duyệt mất focus khi đang làm bài', CAST(N'2026-06-20T08:46:27.000' AS DateTime), 1, N'Nhẹ', N'CB-005-01')
INSERT [dbo].[CANHBAOGIANLAN] ([Id], [IdBaiThi], [LoaiCanhBao], [MoTa], [ThoiGian], [SoLanViPham], [MucDoNghiemTrong], [CorrelationId]) VALUES (3, 5, N'FULLSCREEN_EXIT', N'Thí sinh thoát chế độ toàn màn hình', CAST(N'2026-06-20T08:52:05.000' AS DateTime), 2, N'Trung bình', N'CB-005-02')
INSERT [dbo].[CANHBAOGIANLAN] ([Id], [IdBaiThi], [LoaiCanhBao], [MoTa], [ThoiGian], [SoLanViPham], [MucDoNghiemTrong], [CorrelationId]) VALUES (4, 7, N'BROWSER_FOCUS_LOST', N'Cửa sổ trình duyệt mất focus khi đang làm bài', CAST(N'2026-06-20T08:47:52.000' AS DateTime), 1, N'Nhẹ', N'CB-007-01')
INSERT [dbo].[CANHBAOGIANLAN] ([Id], [IdBaiThi], [LoaiCanhBao], [MoTa], [ThoiGian], [SoLanViPham], [MucDoNghiemTrong], [CorrelationId]) VALUES (5, 10, N'TAB_SWITCH', N'Thí sinh chuyển tab trong lúc làm bài', CAST(N'2026-06-24T14:29:35.000' AS DateTime), 1, N'Trung bình', N'CB-010-01')
INSERT [dbo].[CANHBAOGIANLAN] ([Id], [IdBaiThi], [LoaiCanhBao], [MoTa], [ThoiGian], [SoLanViPham], [MucDoNghiemTrong], [CorrelationId]) VALUES (6, 10, N'SUSPICIOUS_KEYBOARD', N'Thí sinh sử dụng phím tắt bị hạn chế', CAST(N'2026-06-24T14:35:02.000' AS DateTime), 2, N'Trung bình', N'CB-010-02')
INSERT [dbo].[CANHBAOGIANLAN] ([Id], [IdBaiThi], [LoaiCanhBao], [MoTa], [ThoiGian], [SoLanViPham], [MucDoNghiemTrong], [CorrelationId]) VALUES (7, 12, N'BROWSER_FOCUS_LOST', N'Cửa sổ trình duyệt mất focus khi đang làm bài', CAST(N'2026-06-24T14:29:44.000' AS DateTime), 1, N'Nhẹ', N'CB-012-01')
INSERT [dbo].[CANHBAOGIANLAN] ([Id], [IdBaiThi], [LoaiCanhBao], [MoTa], [ThoiGian], [SoLanViPham], [MucDoNghiemTrong], [CorrelationId]) VALUES (8, 14, N'TAB_SWITCH', N'Thí sinh chuyển tab trong lúc làm bài', CAST(N'2026-06-30T08:20:55.000' AS DateTime), 1, N'Trung bình', N'CB-014-01')
INSERT [dbo].[CANHBAOGIANLAN] ([Id], [IdBaiThi], [LoaiCanhBao], [MoTa], [ThoiGian], [SoLanViPham], [MucDoNghiemTrong], [CorrelationId]) VALUES (9, 16, N'BROWSER_FOCUS_LOST', N'Cửa sổ trình duyệt mất focus khi đang làm bài', CAST(N'2026-06-30T08:22:56.000' AS DateTime), 1, N'Nhẹ', N'CB-016-01')
INSERT [dbo].[CANHBAOGIANLAN] ([Id], [IdBaiThi], [LoaiCanhBao], [MoTa], [ThoiGian], [SoLanViPham], [MucDoNghiemTrong], [CorrelationId]) VALUES (10, 16, N'FULLSCREEN_EXIT', N'Thí sinh thoát chế độ toàn màn hình', CAST(N'2026-06-30T08:27:56.000' AS DateTime), 2, N'Trung bình', N'CB-016-02')
SET IDENTITY_INSERT [dbo].[CANHBAOGIANLAN] OFF
GO
SET IDENTITY_INSERT [dbo].[CAUHOI] ON 

INSERT [dbo].[CAUHOI] ([Id], [IdLoaiCauHoi], [NoiDung], [NguoiTao], [NgayTao], [NgayCapNhat], [NguoiCapNhat], [DaXoa], [DoKho], [KhoaPhong], [HinhAnh]) VALUES (1, 1, N'Khi xác định đúng người bệnh trước khi thực hiện thuốc hoặc thủ thuật, điều dưỡng cần ưu tiên thao tác nào?', 3, CAST(N'2026-06-03T08:39:00.000' AS DateTime), NULL, NULL, 0, N'1', N'Phòng Điều dưỡng', NULL)
INSERT [dbo].[CAUHOI] ([Id], [IdLoaiCauHoi], [NoiDung], [NguoiTao], [NgayTao], [NgayCapNhat], [NguoiCapNhat], [DaXoa], [DoKho], [KhoaPhong], [HinhAnh]) VALUES (2, 1, N'Mục đích chính của bàn giao người bệnh theo cấu trúc SBAR là gì?', 3, CAST(N'2026-06-03T08:48:00.000' AS DateTime), NULL, NULL, 0, N'1', N'Phòng Điều dưỡng', NULL)
INSERT [dbo].[CAUHOI] ([Id], [IdLoaiCauHoi], [NoiDung], [NguoiTao], [NgayTao], [NgayCapNhat], [NguoiCapNhat], [DaXoa], [DoKho], [KhoaPhong], [HinhAnh]) VALUES (3, 1, N'Trước khi cho người bệnh dùng thuốc qua đường uống, điều dưỡng cần kiểm tra nội dung nào?', 3, CAST(N'2026-06-03T08:57:00.000' AS DateTime), NULL, NULL, 0, N'1', N'Phòng Điều dưỡng', NULL)
INSERT [dbo].[CAUHOI] ([Id], [IdLoaiCauHoi], [NoiDung], [NguoiTao], [NgayTao], [NgayCapNhat], [NguoiCapNhat], [DaXoa], [DoKho], [KhoaPhong], [HinhAnh]) VALUES (4, 1, N'Khi phát hiện y lệnh thuốc không rõ liều dùng, điều dưỡng nên xử trí thế nào?', 3, CAST(N'2026-06-03T09:06:00.000' AS DateTime), NULL, NULL, 0, N'2', N'Phòng Điều dưỡng', NULL)
INSERT [dbo].[CAUHOI] ([Id], [IdLoaiCauHoi], [NoiDung], [NguoiTao], [NgayTao], [NgayCapNhat], [NguoiCapNhat], [DaXoa], [DoKho], [KhoaPhong], [HinhAnh]) VALUES (5, 1, N'Dấu hiệu nào gợi ý trẻ cần được đánh giá cấp cứu ngay khi vào khoa?', 4, CAST(N'2026-06-03T09:15:00.000' AS DateTime), NULL, NULL, 0, N'1', N'Khoa Cấp cứu', NULL)
INSERT [dbo].[CAUHOI] ([Id], [IdLoaiCauHoi], [NoiDung], [NguoiTao], [NgayTao], [NgayCapNhat], [NguoiCapNhat], [DaXoa], [DoKho], [KhoaPhong], [HinhAnh]) VALUES (6, 1, N'Khi trẻ có dấu hiệu ngừng thở hoặc không bắt được mạch, điều dưỡng cần ưu tiên hành động nào?', 4, CAST(N'2026-06-03T09:24:00.000' AS DateTime), NULL, NULL, 0, N'2', N'Khoa Cấp cứu', NULL)
INSERT [dbo].[CAUHOI] ([Id], [IdLoaiCauHoi], [NoiDung], [NguoiTao], [NgayTao], [NgayCapNhat], [NguoiCapNhat], [DaXoa], [DoKho], [KhoaPhong], [HinhAnh]) VALUES (7, 1, N'Tần số ép tim ngoài lồng ngực thường được khuyến nghị trong hồi sức tim phổi cơ bản là khoảng bao nhiêu?', 4, CAST(N'2026-06-03T09:33:00.000' AS DateTime), NULL, NULL, 0, N'1', N'Khoa Cấp cứu', NULL)
INSERT [dbo].[CAUHOI] ([Id], [IdLoaiCauHoi], [NoiDung], [NguoiTao], [NgayTao], [NgayCapNhat], [NguoiCapNhat], [DaXoa], [DoKho], [KhoaPhong], [HinhAnh]) VALUES (8, 1, N'Khi xử trí trẻ nghi sốc phản vệ sau tiêm thuốc, thao tác điều dưỡng cần làm ngay là gì?', 4, CAST(N'2026-06-03T09:42:00.000' AS DateTime), NULL, NULL, 0, N'2', N'Khoa Cấp cứu', NULL)
INSERT [dbo].[CAUHOI] ([Id], [IdLoaiCauHoi], [NoiDung], [NguoiTao], [NgayTao], [NgayCapNhat], [NguoiCapNhat], [DaXoa], [DoKho], [KhoaPhong], [HinhAnh]) VALUES (9, 1, N'Trong chăm sóc người bệnh thở máy, việc nâng đầu giường khi không chống chỉ định nhằm mục đích gì?', 5, CAST(N'2026-06-03T09:51:00.000' AS DateTime), NULL, NULL, 0, N'2', N'Khoa Hồi sức tích cực - Chống độc', NULL)
INSERT [dbo].[CAUHOI] ([Id], [IdLoaiCauHoi], [NoiDung], [NguoiTao], [NgayTao], [NgayCapNhat], [NguoiCapNhat], [DaXoa], [DoKho], [KhoaPhong], [HinhAnh]) VALUES (10, 1, N'Khi bơm rửa đường truyền tĩnh mạch trung tâm, điều dưỡng cần chú ý nguyên tắc nào?', 5, CAST(N'2026-06-03T10:00:00.000' AS DateTime), NULL, NULL, 0, N'2', N'Khoa Hồi sức tích cực - Chống độc', NULL)
INSERT [dbo].[CAUHOI] ([Id], [IdLoaiCauHoi], [NoiDung], [NguoiTao], [NgayTao], [NgayCapNhat], [NguoiCapNhat], [DaXoa], [DoKho], [KhoaPhong], [HinhAnh]) VALUES (11, 1, N'Dấu hiệu nào cần báo bác sĩ ngay ở người bệnh đang truyền vận mạch?', 5, CAST(N'2026-06-03T10:09:00.000' AS DateTime), NULL, NULL, 0, N'3', N'Khoa Hồi sức tích cực - Chống độc', NULL)
INSERT [dbo].[CAUHOI] ([Id], [IdLoaiCauHoi], [NoiDung], [NguoiTao], [NgayTao], [NgayCapNhat], [NguoiCapNhat], [DaXoa], [DoKho], [KhoaPhong], [HinhAnh]) VALUES (12, 1, N'Khi hút đờm qua ống nội khí quản, điều dưỡng cần ưu tiên nội dung nào?', 5, CAST(N'2026-06-03T10:18:00.000' AS DateTime), NULL, NULL, 0, N'2', N'Khoa Hồi sức tích cực - Chống độc', NULL)
INSERT [dbo].[CAUHOI] ([Id], [IdLoaiCauHoi], [NoiDung], [NguoiTao], [NgayTao], [NgayCapNhat], [NguoiCapNhat], [DaXoa], [DoKho], [KhoaPhong], [HinhAnh]) VALUES (13, 1, N'Khi chăm sóc trẻ sơ sinh non tháng, mục tiêu giữ ấm nhằm hạn chế nguy cơ nào?', 6, CAST(N'2026-06-03T10:27:00.000' AS DateTime), NULL, NULL, 0, N'1', N'Khoa Sơ sinh', NULL)
INSERT [dbo].[CAUHOI] ([Id], [IdLoaiCauHoi], [NoiDung], [NguoiTao], [NgayTao], [NgayCapNhat], [NguoiCapNhat], [DaXoa], [DoKho], [KhoaPhong], [HinhAnh]) VALUES (14, 1, N'Dấu hiệu nào cần lưu ý sớm ở trẻ sơ sinh có nguy cơ suy hô hấp?', 6, CAST(N'2026-06-03T10:36:00.000' AS DateTime), NULL, NULL, 0, N'2', N'Khoa Sơ sinh', NULL)
INSERT [dbo].[CAUHOI] ([Id], [IdLoaiCauHoi], [NoiDung], [NguoiTao], [NgayTao], [NgayCapNhat], [NguoiCapNhat], [DaXoa], [DoKho], [KhoaPhong], [HinhAnh]) VALUES (15, 1, N'Khi cho trẻ sơ sinh ăn qua sonde, điều dưỡng cần kiểm tra nội dung nào trước khi bơm sữa?', 6, CAST(N'2026-06-03T10:45:00.000' AS DateTime), NULL, NULL, 0, N'1', N'Khoa Sơ sinh', NULL)
INSERT [dbo].[CAUHOI] ([Id], [IdLoaiCauHoi], [NoiDung], [NguoiTao], [NgayTao], [NgayCapNhat], [NguoiCapNhat], [DaXoa], [DoKho], [KhoaPhong], [HinhAnh]) VALUES (16, 1, N'Khi phát hiện trẻ sơ sinh tím tái trong lồng ấp, điều dưỡng nên làm gì trước tiên?', 6, CAST(N'2026-06-03T10:54:00.000' AS DateTime), NULL, NULL, 0, N'2', N'Khoa Sơ sinh', NULL)
INSERT [dbo].[CAUHOI] ([Id], [IdLoaiCauHoi], [NoiDung], [NguoiTao], [NgayTao], [NgayCapNhat], [NguoiCapNhat], [DaXoa], [DoKho], [KhoaPhong], [HinhAnh]) VALUES (17, 1, N'Dấu hiệu rút lõm lồng ngực ở trẻ thường gợi ý tình trạng nào?', 7, CAST(N'2026-06-03T11:03:00.000' AS DateTime), NULL, NULL, 0, N'1', N'Khoa Hô hấp', NULL)
INSERT [dbo].[CAUHOI] ([Id], [IdLoaiCauHoi], [NoiDung], [NguoiTao], [NgayTao], [NgayCapNhat], [NguoiCapNhat], [DaXoa], [DoKho], [KhoaPhong], [HinhAnh]) VALUES (18, 1, N'Khi cho trẻ thở oxy qua cannula mũi, điều dưỡng cần theo dõi thường xuyên chỉ số nào?', 7, CAST(N'2026-06-03T11:12:00.000' AS DateTime), NULL, NULL, 0, N'1', N'Khoa Hô hấp', NULL)
INSERT [dbo].[CAUHOI] ([Id], [IdLoaiCauHoi], [NoiDung], [NguoiTao], [NgayTao], [NgayCapNhat], [NguoiCapNhat], [DaXoa], [DoKho], [KhoaPhong], [HinhAnh]) VALUES (19, 1, N'Sau khí dung, điều dưỡng nên đánh giá nội dung nào?', 7, CAST(N'2026-06-03T11:21:00.000' AS DateTime), NULL, NULL, 0, N'2', N'Khoa Hô hấp', NULL)
INSERT [dbo].[CAUHOI] ([Id], [IdLoaiCauHoi], [NoiDung], [NguoiTao], [NgayTao], [NgayCapNhat], [NguoiCapNhat], [DaXoa], [DoKho], [KhoaPhong], [HinhAnh]) VALUES (20, 1, N'Khi trẻ đang khó thở đột ngột tím tái, điều dưỡng cần ưu tiên?', 7, CAST(N'2026-06-03T11:30:00.000' AS DateTime), NULL, NULL, 0, N'2', N'Khoa Hô hấp', NULL)
INSERT [dbo].[CAUHOI] ([Id], [IdLoaiCauHoi], [NoiDung], [NguoiTao], [NgayTao], [NgayCapNhat], [NguoiCapNhat], [DaXoa], [DoKho], [KhoaPhong], [HinhAnh]) VALUES (21, 1, N'Thời điểm vệ sinh tay theo khuyến cáo trong chăm sóc người bệnh bao gồm nội dung nào?', 8, CAST(N'2026-06-03T11:39:00.000' AS DateTime), NULL, NULL, 0, N'1', N'Khoa Nhiễm', NULL)
INSERT [dbo].[CAUHOI] ([Id], [IdLoaiCauHoi], [NoiDung], [NguoiTao], [NgayTao], [NgayCapNhat], [NguoiCapNhat], [DaXoa], [DoKho], [KhoaPhong], [HinhAnh]) VALUES (22, 1, N'Khi chăm sóc người bệnh cần cách ly giọt bắn, điều dưỡng cần sử dụng phương tiện phòng hộ nào là phù hợp?', 8, CAST(N'2026-06-03T11:48:00.000' AS DateTime), NULL, NULL, 0, N'2', N'Khoa Nhiễm', NULL)
INSERT [dbo].[CAUHOI] ([Id], [IdLoaiCauHoi], [NoiDung], [NguoiTao], [NgayTao], [NgayCapNhat], [NguoiCapNhat], [DaXoa], [DoKho], [KhoaPhong], [HinhAnh]) VALUES (23, 1, N'Chất thải sắc nhọn sau sử dụng cần được xử lý như thế nào?', 8, CAST(N'2026-06-03T11:57:00.000' AS DateTime), NULL, NULL, 0, N'1', N'Khoa Nhiễm', NULL)
INSERT [dbo].[CAUHOI] ([Id], [IdLoaiCauHoi], [NoiDung], [NguoiTao], [NgayTao], [NgayCapNhat], [NguoiCapNhat], [DaXoa], [DoKho], [KhoaPhong], [HinhAnh]) VALUES (24, 1, N'Khi bị kim tiêm đâm trong lúc chăm sóc, điều dưỡng cần làm gì?', 8, CAST(N'2026-06-03T12:06:00.000' AS DateTime), NULL, NULL, 0, N'2', N'Khoa Nhiễm', NULL)
INSERT [dbo].[CAUHOI] ([Id], [IdLoaiCauHoi], [NoiDung], [NguoiTao], [NgayTao], [NgayCapNhat], [NguoiCapNhat], [DaXoa], [DoKho], [KhoaPhong], [HinhAnh]) VALUES (25, 1, N'Sau phẫu thuật, dấu hiệu nào cần báo bác sĩ ngay?', 9, CAST(N'2026-06-03T12:15:00.000' AS DateTime), NULL, NULL, 0, N'1', N'Khoa Ngoại tổng hợp', NULL)
INSERT [dbo].[CAUHOI] ([Id], [IdLoaiCauHoi], [NoiDung], [NguoiTao], [NgayTao], [NgayCapNhat], [NguoiCapNhat], [DaXoa], [DoKho], [KhoaPhong], [HinhAnh]) VALUES (26, 1, N'Khi chăm sóc dẫn lưu sau mổ, điều dưỡng cần ghi nhận nội dung nào?', 9, CAST(N'2026-06-03T12:24:00.000' AS DateTime), NULL, NULL, 0, N'2', N'Khoa Ngoại tổng hợp', NULL)
INSERT [dbo].[CAUHOI] ([Id], [IdLoaiCauHoi], [NoiDung], [NguoiTao], [NgayTao], [NgayCapNhat], [NguoiCapNhat], [DaXoa], [DoKho], [KhoaPhong], [HinhAnh]) VALUES (27, 1, N'Trước khi chuyển người bệnh đi phẫu thuật, điều dưỡng cần kiểm tra?', 9, CAST(N'2026-06-03T12:33:00.000' AS DateTime), NULL, NULL, 0, N'1', N'Khoa Ngoại tổng hợp', NULL)
INSERT [dbo].[CAUHOI] ([Id], [IdLoaiCauHoi], [NoiDung], [NguoiTao], [NgayTao], [NgayCapNhat], [NguoiCapNhat], [DaXoa], [DoKho], [KhoaPhong], [HinhAnh]) VALUES (28, 1, N'Khi thay băng vết mổ, nguyên tắc nào cần tuân thủ?', 9, CAST(N'2026-06-03T12:42:00.000' AS DateTime), NULL, NULL, 0, N'2', N'Khoa Ngoại tổng hợp', NULL)
INSERT [dbo].[CAUHOI] ([Id], [IdLoaiCauHoi], [NoiDung], [NguoiTao], [NgayTao], [NgayCapNhat], [NguoiCapNhat], [DaXoa], [DoKho], [KhoaPhong], [HinhAnh]) VALUES (29, 1, N'Ở trẻ có bệnh tim, dấu hiệu nào có thể gợi ý tình trạng giảm oxy máu?', 10, CAST(N'2026-06-03T12:51:00.000' AS DateTime), NULL, NULL, 0, N'2', N'Khoa Tim mạch', NULL)
INSERT [dbo].[CAUHOI] ([Id], [IdLoaiCauHoi], [NoiDung], [NguoiTao], [NgayTao], [NgayCapNhat], [NguoiCapNhat], [DaXoa], [DoKho], [KhoaPhong], [HinhAnh]) VALUES (30, 1, N'Khi dùng thuốc lợi tiểu theo y lệnh, điều dưỡng cần theo dõi nội dung nào?', 10, CAST(N'2026-06-03T13:00:00.000' AS DateTime), NULL, NULL, 0, N'2', N'Khoa Tim mạch', NULL)
INSERT [dbo].[CAUHOI] ([Id], [IdLoaiCauHoi], [NoiDung], [NguoiTao], [NgayTao], [NgayCapNhat], [NguoiCapNhat], [DaXoa], [DoKho], [KhoaPhong], [HinhAnh]) VALUES (31, 1, N'Dấu hiệu mất nước ở trẻ tiêu chảy cần được theo dõi gồm?', 11, CAST(N'2026-06-03T13:09:00.000' AS DateTime), NULL, NULL, 0, N'1', N'Khoa Tiêu hóa', NULL)
INSERT [dbo].[CAUHOI] ([Id], [IdLoaiCauHoi], [NoiDung], [NguoiTao], [NgayTao], [NgayCapNhat], [NguoiCapNhat], [DaXoa], [DoKho], [KhoaPhong], [HinhAnh]) VALUES (32, 1, N'Khi bù dịch đường uống cho trẻ tiêu chảy, điều dưỡng cần hướng dẫn?', 11, CAST(N'2026-06-03T13:18:00.000' AS DateTime), NULL, NULL, 0, N'2', N'Khoa Tiêu hóa', NULL)
INSERT [dbo].[CAUHOI] ([Id], [IdLoaiCauHoi], [NoiDung], [NguoiTao], [NgayTao], [NgayCapNhat], [NguoiCapNhat], [DaXoa], [DoKho], [KhoaPhong], [HinhAnh]) VALUES (33, 1, N'Sau phẫu thuật thần kinh, dấu hiệu nào cần theo dõi sát?', 12, CAST(N'2026-06-03T13:27:00.000' AS DateTime), NULL, NULL, 0, N'2', N'Khoa Ngoại thần kinh', NULL)
INSERT [dbo].[CAUHOI] ([Id], [IdLoaiCauHoi], [NoiDung], [NguoiTao], [NgayTao], [NgayCapNhat], [NguoiCapNhat], [DaXoa], [DoKho], [KhoaPhong], [HinhAnh]) VALUES (34, 1, N'Khi người bệnh co giật, điều dưỡng cần làm gì?', 12, CAST(N'2026-06-03T13:36:00.000' AS DateTime), NULL, NULL, 0, N'2', N'Khoa Ngoại thần kinh', NULL)
INSERT [dbo].[CAUHOI] ([Id], [IdLoaiCauHoi], [NoiDung], [NguoiTao], [NgayTao], [NgayCapNhat], [NguoiCapNhat], [DaXoa], [DoKho], [KhoaPhong], [HinhAnh]) VALUES (35, 1, N'Khi chăm sóc trẻ đang giảm bạch cầu, điều dưỡng cần ưu tiên?', 13, CAST(N'2026-06-03T13:45:00.000' AS DateTime), NULL, NULL, 0, N'2', N'Khoa Ung bướu huyết học', NULL)
INSERT [dbo].[CAUHOI] ([Id], [IdLoaiCauHoi], [NoiDung], [NguoiTao], [NgayTao], [NgayCapNhat], [NguoiCapNhat], [DaXoa], [DoKho], [KhoaPhong], [HinhAnh]) VALUES (36, 1, N'Trước truyền máu, điều dưỡng cần thực hiện nội dung nào?', 13, CAST(N'2026-06-03T13:54:00.000' AS DateTime), NULL, NULL, 0, N'2', N'Khoa Ung bướu huyết học', NULL)
INSERT [dbo].[CAUHOI] ([Id], [IdLoaiCauHoi], [NoiDung], [NguoiTao], [NgayTao], [NgayCapNhat], [NguoiCapNhat], [DaXoa], [DoKho], [KhoaPhong], [HinhAnh]) VALUES (37, 1, N'Khi đánh giá tri giác trẻ, điều dưỡng cần ghi nhận?', 14, CAST(N'2026-06-03T14:03:00.000' AS DateTime), NULL, NULL, 0, N'1', N'Khoa Thần kinh', NULL)
INSERT [dbo].[CAUHOI] ([Id], [IdLoaiCauHoi], [NoiDung], [NguoiTao], [NgayTao], [NgayCapNhat], [NguoiCapNhat], [DaXoa], [DoKho], [KhoaPhong], [HinhAnh]) VALUES (38, 1, N'Khi trẻ có nguy cơ té ngã, biện pháp nào phù hợp?', 14, CAST(N'2026-06-03T14:12:00.000' AS DateTime), NULL, NULL, 0, N'2', N'Khoa Thần kinh', NULL)
INSERT [dbo].[CAUHOI] ([Id], [IdLoaiCauHoi], [NoiDung], [NguoiTao], [NgayTao], [NgayCapNhat], [NguoiCapNhat], [DaXoa], [DoKho], [KhoaPhong], [HinhAnh]) VALUES (39, 1, N'Trước khi đưa trẻ đi chụp có thuốc cản quang, điều dưỡng cần kiểm tra?', 15, CAST(N'2026-06-03T14:21:00.000' AS DateTime), NULL, NULL, 0, N'1', N'Khoa Chẩn đoán hình ảnh', NULL)
INSERT [dbo].[CAUHOI] ([Id], [IdLoaiCauHoi], [NoiDung], [NguoiTao], [NgayTao], [NgayCapNhat], [NguoiCapNhat], [DaXoa], [DoKho], [KhoaPhong], [HinhAnh]) VALUES (40, 1, N'Sau thủ thuật có dùng thuốc an thần, điều dưỡng cần theo dõi?', 15, CAST(N'2026-06-03T14:30:00.000' AS DateTime), NULL, NULL, 0, N'2', N'Khoa Chẩn đoán hình ảnh', NULL)
SET IDENTITY_INSERT [dbo].[CAUHOI] OFF
GO
SET IDENTITY_INSERT [dbo].[CHITIETLAMBAI] ON 

INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (1, 1, 1, 2, CAST(N'2026-06-20T08:32:39.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (2, 1, 2, 8, CAST(N'2026-06-20T08:33:26.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (3, 1, 3, 9, CAST(N'2026-06-20T08:34:53.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (4, 1, 4, 14, CAST(N'2026-06-20T08:35:44.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (5, 1, 5, 18, CAST(N'2026-06-20T08:36:31.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (6, 1, 6, 21, CAST(N'2026-06-20T08:37:38.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (7, 1, 7, 26, CAST(N'2026-06-20T08:39:28.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (8, 1, 8, 30, CAST(N'2026-06-20T08:40:12.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (9, 1, 9, 33, CAST(N'2026-06-20T08:41:56.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (10, 1, 10, 38, CAST(N'2026-06-20T08:43:42.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (11, 1, 11, 42, CAST(N'2026-06-20T08:45:07.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (12, 1, 12, 46, CAST(N'2026-06-20T08:45:53.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (13, 1, 13, 49, CAST(N'2026-06-20T08:47:07.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (14, 1, 14, 53, CAST(N'2026-06-20T08:48:26.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (15, 1, 15, 57, CAST(N'2026-06-20T08:49:22.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (16, 1, 16, 62, CAST(N'2026-06-20T08:50:48.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (17, 1, 17, 65, CAST(N'2026-06-20T08:51:27.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (18, 1, 18, 69, CAST(N'2026-06-20T08:52:21.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (19, 1, 19, 73, CAST(N'2026-06-20T08:53:08.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (20, 1, 20, 77, CAST(N'2026-06-20T08:54:10.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (21, 2, 1, 2, CAST(N'2026-06-20T08:33:03.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (22, 2, 2, 5, CAST(N'2026-06-20T08:34:50.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (23, 2, 3, 9, CAST(N'2026-06-20T08:36:36.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (24, 2, 4, 14, CAST(N'2026-06-20T08:37:27.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (25, 2, 5, 20, CAST(N'2026-06-20T08:38:22.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (26, 2, 6, 21, CAST(N'2026-06-20T08:39:36.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (27, 2, 7, 27, CAST(N'2026-06-20T08:41:10.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (28, 2, 8, 32, CAST(N'2026-06-20T08:42:20.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (29, 2, 9, 33, CAST(N'2026-06-20T08:43:05.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (30, 2, 10, 38, CAST(N'2026-06-20T08:44:47.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (31, 2, 11, 42, CAST(N'2026-06-20T08:46:36.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (32, 2, 12, 46, CAST(N'2026-06-20T08:48:07.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (33, 2, 13, 49, CAST(N'2026-06-20T08:49:19.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (34, 2, 14, 56, CAST(N'2026-06-20T08:50:03.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (35, 2, 15, 57, CAST(N'2026-06-20T08:50:50.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (36, 2, 16, 62, CAST(N'2026-06-20T08:52:19.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (37, 2, 17, 68, CAST(N'2026-06-20T08:53:05.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (38, 2, 18, 69, CAST(N'2026-06-20T08:53:48.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (39, 2, 19, 73, CAST(N'2026-06-20T08:55:13.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (40, 2, 20, 79, CAST(N'2026-06-20T08:56:07.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (41, 3, 1, 2, CAST(N'2026-06-20T08:35:19.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (42, 3, 2, 5, CAST(N'2026-06-20T08:36:58.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (43, 3, 3, 9, CAST(N'2026-06-20T08:37:37.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (44, 3, 4, 14, CAST(N'2026-06-20T08:38:41.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (45, 3, 5, 18, CAST(N'2026-06-20T08:40:24.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (46, 3, 6, 21, CAST(N'2026-06-20T08:42:08.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (47, 3, 7, 27, CAST(N'2026-06-20T08:42:50.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (48, 3, 8, 31, CAST(N'2026-06-20T08:43:54.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (49, 3, 9, 36, CAST(N'2026-06-20T08:45:40.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (50, 3, 10, 38, CAST(N'2026-06-20T08:47:25.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (51, 3, 11, 42, CAST(N'2026-06-20T08:48:00.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (52, 3, 12, 45, CAST(N'2026-06-20T08:49:25.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (53, 3, 13, 49, CAST(N'2026-06-20T08:50:43.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (54, 3, 14, 53, CAST(N'2026-06-20T08:51:22.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (55, 3, 15, 60, CAST(N'2026-06-20T08:52:27.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (56, 3, 16, 64, CAST(N'2026-06-20T08:53:51.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (57, 3, 17, 66, CAST(N'2026-06-20T08:55:00.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (58, 3, 18, 71, CAST(N'2026-06-20T08:55:51.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (59, 3, 19, 73, CAST(N'2026-06-20T08:56:58.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (60, 3, 20, 77, CAST(N'2026-06-20T08:58:21.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (61, 4, 1, 2, CAST(N'2026-06-20T08:33:09.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (62, 4, 2, 5, CAST(N'2026-06-20T08:34:03.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (63, 4, 3, 9, CAST(N'2026-06-20T08:34:49.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (64, 4, 4, 16, CAST(N'2026-06-20T08:36:05.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (65, 4, 5, 18, CAST(N'2026-06-20T08:37:35.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (66, 4, 6, 21, CAST(N'2026-06-20T08:38:48.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (67, 4, 7, 27, CAST(N'2026-06-20T08:39:26.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (68, 4, 8, 30, CAST(N'2026-06-20T08:40:16.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (69, 4, 9, 33, CAST(N'2026-06-20T08:41:45.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (70, 4, 10, 38, CAST(N'2026-06-20T08:42:35.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (71, 4, 11, 42, CAST(N'2026-06-20T08:44:11.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (72, 4, 12, 46, CAST(N'2026-06-20T08:44:52.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (73, 4, 13, 49, CAST(N'2026-06-20T08:46:04.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (74, 4, 14, 56, CAST(N'2026-06-20T08:46:42.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (75, 4, 15, 57, CAST(N'2026-06-20T08:47:48.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (76, 4, 16, 62, CAST(N'2026-06-20T08:48:25.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (77, 4, 17, 65, CAST(N'2026-06-20T08:49:49.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (78, 4, 18, 71, CAST(N'2026-06-20T08:51:20.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (79, 4, 19, 73, CAST(N'2026-06-20T08:52:02.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (80, 4, 20, 77, CAST(N'2026-06-20T08:53:00.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (81, 5, 1, 2, CAST(N'2026-06-20T08:35:49.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (82, 5, 2, 5, CAST(N'2026-06-20T08:37:26.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (83, 5, 3, 9, CAST(N'2026-06-20T08:38:13.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (84, 5, 4, 14, CAST(N'2026-06-20T08:39:21.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (85, 5, 5, 18, CAST(N'2026-06-20T08:40:25.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (86, 5, 6, 22, CAST(N'2026-06-20T08:41:37.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (87, 5, 7, 27, CAST(N'2026-06-20T08:42:27.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (88, 5, 8, 30, CAST(N'2026-06-20T08:44:02.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (89, 5, 9, 33, CAST(N'2026-06-20T08:45:03.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (90, 5, 10, 40, CAST(N'2026-06-20T08:46:28.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (91, 5, 11, 42, CAST(N'2026-06-20T08:47:48.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (92, 5, 12, 46, CAST(N'2026-06-20T08:49:32.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (93, 5, 13, 50, CAST(N'2026-06-20T08:50:41.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (94, 5, 14, 56, CAST(N'2026-06-20T08:51:55.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (95, 5, 15, 57, CAST(N'2026-06-20T08:53:05.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (96, 5, 16, 63, CAST(N'2026-06-20T08:54:40.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (97, 5, 17, 65, CAST(N'2026-06-20T08:56:13.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (98, 5, 18, 71, CAST(N'2026-06-20T08:57:30.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (99, 5, 19, 73, CAST(N'2026-06-20T08:59:10.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (100, 5, 20, 77, CAST(N'2026-06-20T09:00:21.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (101, 6, 1, 2, CAST(N'2026-06-20T08:34:21.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (102, 6, 2, 5, CAST(N'2026-06-20T08:34:56.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (103, 6, 3, 9, CAST(N'2026-06-20T08:36:05.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (104, 6, 4, 14, CAST(N'2026-06-20T08:36:46.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (105, 6, 5, 18, CAST(N'2026-06-20T08:37:59.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (106, 6, 6, 21, CAST(N'2026-06-20T08:38:35.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (107, 6, 7, 27, CAST(N'2026-06-20T08:39:15.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (108, 6, 8, 30, CAST(N'2026-06-20T08:40:23.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (109, 6, 9, 33, CAST(N'2026-06-20T08:41:34.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (110, 6, 10, 38, CAST(N'2026-06-20T08:42:20.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (111, 6, 11, 42, CAST(N'2026-06-20T08:43:06.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (112, 6, 12, 48, CAST(N'2026-06-20T08:44:30.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (113, 6, 13, 49, CAST(N'2026-06-20T08:45:06.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (114, 6, 14, 53, CAST(N'2026-06-20T08:46:38.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (115, 6, 15, 57, CAST(N'2026-06-20T08:48:01.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (116, 6, 16, 62, CAST(N'2026-06-20T08:49:38.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (117, 6, 17, 65, CAST(N'2026-06-20T08:50:58.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (118, 6, 18, 69, CAST(N'2026-06-20T08:52:14.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (119, 6, 19, 73, CAST(N'2026-06-20T08:53:40.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (120, 6, 20, 77, CAST(N'2026-06-20T08:54:28.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (121, 7, 1, 1, CAST(N'2026-06-20T08:36:06.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (122, 7, 2, 6, CAST(N'2026-06-20T08:36:48.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (123, 7, 3, 9, CAST(N'2026-06-20T08:37:58.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (124, 7, 4, 14, CAST(N'2026-06-20T08:39:27.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (125, 7, 5, 18, CAST(N'2026-06-20T08:40:39.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (126, 7, 6, 23, CAST(N'2026-06-20T08:41:43.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (127, 7, 7, 27, CAST(N'2026-06-20T08:42:21.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (128, 7, 8, 31, CAST(N'2026-06-20T08:44:09.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (129, 7, 9, 35, CAST(N'2026-06-20T08:44:55.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (130, 7, 10, 37, CAST(N'2026-06-20T08:45:31.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (131, 7, 11, 41, CAST(N'2026-06-20T08:47:21.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (132, 7, 12, 46, CAST(N'2026-06-20T08:48:52.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (133, 7, 13, 49, CAST(N'2026-06-20T08:49:42.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (134, 7, 14, 53, CAST(N'2026-06-20T08:51:15.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (135, 7, 15, 57, CAST(N'2026-06-20T08:52:40.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (136, 7, 16, 62, CAST(N'2026-06-20T08:53:51.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (137, 7, 17, 65, CAST(N'2026-06-20T08:55:20.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (138, 7, 18, 69, CAST(N'2026-06-20T08:56:29.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (139, 7, 19, 74, CAST(N'2026-06-20T08:57:52.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (140, 7, 20, 77, CAST(N'2026-06-20T08:59:01.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (141, 8, 5, 18, CAST(N'2026-06-24T14:18:19.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (142, 8, 6, 21, CAST(N'2026-06-24T14:19:12.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (143, 8, 7, 28, CAST(N'2026-06-24T14:19:52.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (144, 8, 8, 30, CAST(N'2026-06-24T14:20:54.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (145, 8, 17, 65, CAST(N'2026-06-24T14:21:47.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (146, 8, 18, 69, CAST(N'2026-06-24T14:22:35.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (147, 8, 19, 73, CAST(N'2026-06-24T14:23:39.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (148, 8, 20, 77, CAST(N'2026-06-24T14:25:02.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (149, 8, 1, 2, CAST(N'2026-06-24T14:26:21.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (150, 8, 2, 5, CAST(N'2026-06-24T14:27:09.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (151, 8, 3, 10, CAST(N'2026-06-24T14:28:02.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (152, 8, 4, 14, CAST(N'2026-06-24T14:29:05.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (153, 8, 21, 81, CAST(N'2026-06-24T14:30:27.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (154, 8, 22, 85, CAST(N'2026-06-24T14:31:33.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (155, 8, 23, 90, CAST(N'2026-06-24T14:32:21.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (156, 8, 24, 93, CAST(N'2026-06-24T14:33:52.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (157, 8, 25, 100, CAST(N'2026-06-24T14:35:24.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (158, 8, 26, 101, CAST(N'2026-06-24T14:36:37.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (159, 8, 27, 105, CAST(N'2026-06-24T14:37:55.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (160, 8, 28, 109, CAST(N'2026-06-24T14:38:46.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (161, 9, 5, 17, CAST(N'2026-06-24T14:19:03.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (162, 9, 6, 21, CAST(N'2026-06-24T14:19:50.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (163, 9, 7, 28, CAST(N'2026-06-24T14:21:08.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (164, 9, 8, 30, CAST(N'2026-06-24T14:22:42.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (165, 9, 17, 65, CAST(N'2026-06-24T14:23:18.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (166, 9, 18, 72, CAST(N'2026-06-24T14:24:10.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (167, 9, 19, 73, CAST(N'2026-06-24T14:25:30.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (168, 9, 20, 77, CAST(N'2026-06-24T14:26:22.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (169, 9, 1, 2, CAST(N'2026-06-24T14:27:14.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (170, 9, 2, 5, CAST(N'2026-06-24T14:28:47.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (171, 9, 3, 9, CAST(N'2026-06-24T14:29:49.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (172, 9, 4, 14, CAST(N'2026-06-24T14:31:18.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (173, 9, 21, 81, CAST(N'2026-06-24T14:31:55.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (174, 9, 22, 85, CAST(N'2026-06-24T14:32:51.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (175, 9, 23, 91, CAST(N'2026-06-24T14:34:24.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (176, 9, 24, 95, CAST(N'2026-06-24T14:35:37.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (177, 9, 25, 98, CAST(N'2026-06-24T14:36:46.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (178, 9, 26, 102, CAST(N'2026-06-24T14:38:13.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (179, 9, 27, 105, CAST(N'2026-06-24T14:39:35.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (180, 9, 28, 109, CAST(N'2026-06-24T14:40:53.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (181, 10, 5, 18, CAST(N'2026-06-24T14:19:16.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (182, 10, 6, 21, CAST(N'2026-06-24T14:20:00.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (183, 10, 7, 25, CAST(N'2026-06-24T14:21:42.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (184, 10, 8, 30, CAST(N'2026-06-24T14:23:23.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (185, 10, 17, 65, CAST(N'2026-06-24T14:25:08.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (186, 10, 18, 69, CAST(N'2026-06-24T14:26:19.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (187, 10, 19, 76, CAST(N'2026-06-24T14:27:12.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (188, 10, 20, 78, CAST(N'2026-06-24T14:28:51.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (189, 10, 1, 2, CAST(N'2026-06-24T14:29:48.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (190, 10, 2, 5, CAST(N'2026-06-24T14:30:26.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (191, 10, 3, 9, CAST(N'2026-06-24T14:31:25.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (192, 10, 4, 14, CAST(N'2026-06-24T14:32:55.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (193, 10, 21, 82, CAST(N'2026-06-24T14:33:32.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (194, 10, 22, 88, CAST(N'2026-06-24T14:34:10.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (195, 10, 23, 90, CAST(N'2026-06-24T14:35:54.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (196, 10, 24, 93, CAST(N'2026-06-24T14:36:35.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (197, 10, 25, 100, CAST(N'2026-06-24T14:38:22.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (198, 10, 26, 104, CAST(N'2026-06-24T14:39:03.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (199, 10, 27, 105, CAST(N'2026-06-24T14:40:14.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (200, 10, 28, 110, CAST(N'2026-06-24T14:41:18.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (201, 11, 5, 18, CAST(N'2026-06-24T14:18:33.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (202, 11, 6, 21, CAST(N'2026-06-24T14:19:35.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (203, 11, 7, 27, CAST(N'2026-06-24T14:20:20.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (204, 11, 8, 30, CAST(N'2026-06-24T14:21:50.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (205, 11, 17, 65, CAST(N'2026-06-24T14:23:37.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (206, 11, 18, 69, CAST(N'2026-06-24T14:24:56.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (207, 11, 19, 73, CAST(N'2026-06-24T14:26:32.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (208, 11, 20, 77, CAST(N'2026-06-24T14:27:09.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (209, 11, 1, 3, CAST(N'2026-06-24T14:28:04.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (210, 11, 2, 5, CAST(N'2026-06-24T14:29:47.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (211, 11, 3, 9, CAST(N'2026-06-24T14:30:22.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (212, 11, 4, 16, CAST(N'2026-06-24T14:31:31.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (213, 11, 21, 81, CAST(N'2026-06-24T14:33:13.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (214, 11, 22, 85, CAST(N'2026-06-24T14:34:57.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (215, 11, 23, 89, CAST(N'2026-06-24T14:35:53.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (216, 11, 24, 93, CAST(N'2026-06-24T14:36:40.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (217, 11, 25, 98, CAST(N'2026-06-24T14:37:48.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (218, 11, 26, 101, CAST(N'2026-06-24T14:39:20.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (219, 11, 27, 105, CAST(N'2026-06-24T14:40:24.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (220, 11, 28, 109, CAST(N'2026-06-24T14:41:31.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (221, 12, 5, 19, CAST(N'2026-06-24T14:18:38.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (222, 12, 6, 21, CAST(N'2026-06-24T14:20:14.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (223, 12, 7, 27, CAST(N'2026-06-24T14:21:17.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (224, 12, 8, 30, CAST(N'2026-06-24T14:22:41.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (225, 12, 17, 67, CAST(N'2026-06-24T14:24:02.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (226, 12, 18, 70, CAST(N'2026-06-24T14:25:03.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (227, 12, 19, 76, CAST(N'2026-06-24T14:26:27.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (228, 12, 20, 77, CAST(N'2026-06-24T14:28:13.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (229, 12, 1, 2, CAST(N'2026-06-24T14:29:04.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (230, 12, 2, 7, CAST(N'2026-06-24T14:30:48.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (231, 12, 3, 11, CAST(N'2026-06-24T14:31:51.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (232, 12, 4, 14, CAST(N'2026-06-24T14:32:39.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (233, 12, 21, 84, CAST(N'2026-06-24T14:33:36.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (234, 12, 22, 85, CAST(N'2026-06-24T14:34:11.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (235, 12, 23, 89, CAST(N'2026-06-24T14:35:20.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (236, 12, 24, 93, CAST(N'2026-06-24T14:36:04.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (237, 12, 25, 98, CAST(N'2026-06-24T14:37:44.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (238, 12, 26, 101, CAST(N'2026-06-24T14:38:25.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (239, 12, 27, 105, CAST(N'2026-06-24T14:39:28.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (240, 12, 28, 109, CAST(N'2026-06-24T14:41:07.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (241, 13, 21, 81, CAST(N'2026-06-30T08:07:50.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (242, 13, 22, 85, CAST(N'2026-06-30T08:08:45.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (243, 13, 23, 89, CAST(N'2026-06-30T08:10:10.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (244, 13, 24, 93, CAST(N'2026-06-30T08:11:30.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (245, 13, 1, 2, CAST(N'2026-06-30T08:12:46.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (246, 13, 2, 8, CAST(N'2026-06-30T08:13:46.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (247, 13, 3, 9, CAST(N'2026-06-30T08:14:36.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (248, 13, 4, 14, CAST(N'2026-06-30T08:15:29.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (249, 13, 17, 67, CAST(N'2026-06-30T08:16:17.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (250, 13, 18, 69, CAST(N'2026-06-30T08:17:44.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (251, 13, 19, 73, CAST(N'2026-06-30T08:18:34.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (252, 13, 20, 77, CAST(N'2026-06-30T08:19:57.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (253, 13, 31, 121, CAST(N'2026-06-30T08:21:33.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (254, 13, 32, 125, CAST(N'2026-06-30T08:23:12.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (255, 13, 35, 137, CAST(N'2026-06-30T08:24:10.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (256, 13, 36, 142, CAST(N'2026-06-30T08:25:22.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (257, 13, 37, 145, CAST(N'2026-06-30T08:26:37.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (258, 13, 38, 149, CAST(N'2026-06-30T08:27:48.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (259, 13, 39, 153, CAST(N'2026-06-30T08:28:48.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (260, 13, 40, 157, CAST(N'2026-06-30T08:29:26.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (261, 14, 21, 82, CAST(N'2026-06-30T08:10:49.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (262, 14, 22, 88, CAST(N'2026-06-30T08:11:42.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (263, 14, 23, 89, CAST(N'2026-06-30T08:12:48.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (264, 14, 24, 93, CAST(N'2026-06-30T08:14:29.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (265, 14, 1, 2, CAST(N'2026-06-30T08:15:22.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (266, 14, 2, 5, CAST(N'2026-06-30T08:16:44.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (267, 14, 3, 9, CAST(N'2026-06-30T08:18:30.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (268, 14, 4, 15, CAST(N'2026-06-30T08:20:12.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (269, 14, 17, 68, CAST(N'2026-06-30T08:21:18.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (270, 14, 18, 69, CAST(N'2026-06-30T08:22:16.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (271, 14, 19, 73, CAST(N'2026-06-30T08:23:22.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (272, 14, 20, 77, CAST(N'2026-06-30T08:24:04.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (273, 14, 31, 121, CAST(N'2026-06-30T08:25:14.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (274, 14, 32, 125, CAST(N'2026-06-30T08:26:48.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (275, 14, 35, 137, CAST(N'2026-06-30T08:28:25.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (276, 14, 36, 141, CAST(N'2026-06-30T08:29:45.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (277, 14, 37, 148, CAST(N'2026-06-30T08:31:14.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (278, 14, 38, 149, CAST(N'2026-06-30T08:32:00.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (279, 14, 39, 153, CAST(N'2026-06-30T08:32:39.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (280, 14, 40, 157, CAST(N'2026-06-30T08:33:56.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (281, 15, 21, 81, CAST(N'2026-06-30T08:08:29.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (282, 15, 22, 85, CAST(N'2026-06-30T08:09:33.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (283, 15, 23, 89, CAST(N'2026-06-30T08:10:46.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (284, 15, 24, 94, CAST(N'2026-06-30T08:11:50.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (285, 15, 1, 4, CAST(N'2026-06-30T08:13:13.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (286, 15, 2, 5, CAST(N'2026-06-30T08:15:03.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (287, 15, 3, 10, CAST(N'2026-06-30T08:15:46.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (288, 15, 4, 14, CAST(N'2026-06-30T08:16:53.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (289, 15, 17, 65, CAST(N'2026-06-30T08:18:22.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (290, 15, 18, 71, CAST(N'2026-06-30T08:20:12.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (291, 15, 19, 73, CAST(N'2026-06-30T08:21:43.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (292, 15, 20, 77, CAST(N'2026-06-30T08:23:28.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (293, 15, 31, 124, CAST(N'2026-06-30T08:24:35.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (294, 15, 32, 125, CAST(N'2026-06-30T08:25:53.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (295, 15, 35, 137, CAST(N'2026-06-30T08:27:40.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (296, 15, 36, 142, CAST(N'2026-06-30T08:29:08.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (297, 15, 37, 146, CAST(N'2026-06-30T08:29:56.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (298, 15, 38, 149, CAST(N'2026-06-30T08:30:31.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (299, 15, 39, 154, CAST(N'2026-06-30T08:32:13.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (300, 15, 40, 157, CAST(N'2026-06-30T08:33:55.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (301, 16, 21, 81, CAST(N'2026-06-30T08:12:18.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (302, 16, 22, 85, CAST(N'2026-06-30T08:13:56.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (303, 16, 23, 89, CAST(N'2026-06-30T08:14:54.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (304, 16, 24, 93, CAST(N'2026-06-30T08:15:42.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (305, 16, 1, 2, CAST(N'2026-06-30T08:17:32.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (306, 16, 2, 5, CAST(N'2026-06-30T08:18:41.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (307, 16, 3, 9, CAST(N'2026-06-30T08:19:57.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (308, 16, 4, 14, CAST(N'2026-06-30T08:21:15.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (309, 16, 17, 65, CAST(N'2026-06-30T08:22:36.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (310, 16, 18, 69, CAST(N'2026-06-30T08:23:57.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (311, 16, 19, 73, CAST(N'2026-06-30T08:25:14.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (312, 16, 20, 79, CAST(N'2026-06-30T08:26:26.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (313, 16, 31, 122, CAST(N'2026-06-30T08:27:45.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (314, 16, 32, 125, CAST(N'2026-06-30T08:28:55.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (315, 16, 35, 137, CAST(N'2026-06-30T08:29:33.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (316, 16, 36, 141, CAST(N'2026-06-30T08:31:06.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (317, 16, 37, 145, CAST(N'2026-06-30T08:32:04.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (318, 16, 38, 150, CAST(N'2026-06-30T08:33:08.000' AS DateTime), 1, NULL, 0.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (319, 16, 39, 153, CAST(N'2026-06-30T08:34:16.000' AS DateTime), 1, NULL, 1.0)
INSERT [dbo].[CHITIETLAMBAI] ([Id], [IdBaiThi], [IdCauHoi], [IdLuaChonDaChon], [ThoiGianTraLoi], [DaLuu], [CauTraLoiTuLuan], [DiemDatDuoc]) VALUES (320, 16, 40, 160, CAST(N'2026-06-30T08:35:34.000' AS DateTime), 1, NULL, 0.0)
SET IDENTITY_INSERT [dbo].[CHITIETLAMBAI] OFF
GO
SET IDENTITY_INSERT [dbo].[DETHI] ON 

INSERT [dbo].[DETHI] ([Id], [MaDeThi], [TenDeThi], [ThoiGianLamBai], [TongDiem], [ThoiGianBatDau], [LinkTruyCap], [TrangThai], [NguoiTao], [NgayTao], [ChecksumData], [NguoiCapNhat], [NgayCapNhat], [KhoaPhong], [CongBoKetQua], [NguoiCongBo], [ThoiGianCongBo], [KyThiId], [SoCauDungToiThieu]) VALUES (1, N'BTV_2026_BV_DE01', N'Bàn tay vàng điều dưỡng cấp bệnh viện - Đề 01', 45, 20, CAST(N'2026-06-20T08:30:00.000' AS DateTime), N'/exam/BTV_2026_BV_DE01', N'Closed', 3, CAST(N'2026-06-05T09:20:00.000' AS DateTime), N'sha256:9b0f6b4f1df4d1c2c4f8c5c1b9a5f381', 3, CAST(N'2026-06-18T16:35:00.0000000' AS DateTime2), N'Phòng Điều dưỡng', 1, 3, CAST(N'2026-06-20T12:00:00.000' AS DateTime), 1, 14)
INSERT [dbo].[DETHI] ([Id], [MaDeThi], [TenDeThi], [ThoiGianLamBai], [TongDiem], [ThoiGianBatDau], [LinkTruyCap], [TrangThai], [NguoiTao], [NgayTao], [ChecksumData], [NguoiCapNhat], [NgayCapNhat], [KhoaPhong], [CongBoKetQua], [NguoiCongBo], [ThoiGianCongBo], [KyThiId], [SoCauDungToiThieu]) VALUES (2, N'CC_NHI_2026_DE01', N'Chuyên đề cấp cứu nhi khoa - Đề 01', 35, 20, CAST(N'2026-06-24T14:15:00.000' AS DateTime), N'/exam/CC_NHI_2026_DE01', N'Closed', 4, CAST(N'2026-06-12T10:15:00.000' AS DateTime), N'sha256:1d7d4d2f8f9b61f3017b54bcb0a08d11', 4, CAST(N'2026-06-22T09:00:00.0000000' AS DateTime2), N'Khoa Cấp cứu', 1, 4, CAST(N'2026-06-24T16:10:00.000' AS DateTime), 2, 13)
INSERT [dbo].[DETHI] ([Id], [MaDeThi], [TenDeThi], [ThoiGianLamBai], [TongDiem], [ThoiGianBatDau], [LinkTruyCap], [TrangThai], [NguoiTao], [NgayTao], [ChecksumData], [NguoiCapNhat], [NgayCapNhat], [KhoaPhong], [CongBoKetQua], [NguoiCongBo], [ThoiGianCongBo], [KyThiId], [SoCauDungToiThieu]) VALUES (3, N'KSNK_2026_DOT1_DE01', N'Kiểm soát nhiễm khuẩn đợt 1 - Đề 01', 40, 20, CAST(N'2026-06-30T08:00:00.000' AS DateTime), N'/exam/KSNK_2026_DOT1_DE01', N'Active', 8, CAST(N'2026-06-20T08:30:00.000' AS DateTime), N'sha256:bd69a5d7e1cb203ddf7d1f92aa41a640', 8, CAST(N'2026-06-29T15:25:00.0000000' AS DateTime2), N'Khoa Nhiễm', 0, NULL, NULL, 3, 14)
INSERT [dbo].[DETHI] ([Id], [MaDeThi], [TenDeThi], [ThoiGianLamBai], [TongDiem], [ThoiGianBatDau], [LinkTruyCap], [TrangThai], [NguoiTao], [NgayTao], [ChecksumData], [NguoiCapNhat], [NgayCapNhat], [KhoaPhong], [CongBoKetQua], [NguoiCongBo], [ThoiGianCongBo], [KyThiId], [SoCauDungToiThieu]) VALUES (4, N'HSTC_ONDINH_202607_DE01', N'Ôn tập điều dưỡng hồi sức nhi - Đề 01', 45, 20, CAST(N'2026-07-08T08:30:00.000' AS DateTime), N'/exam/HSTC_ONDINH_202607_DE01', N'Active', 5, CAST(N'2026-06-26T13:45:00.000' AS DateTime), N'sha256:6f34ed3bb1f42cb9ed3f37e6e4d5942c', 5, NULL, N'Khoa Hồi sức tích cực - Chống độc', 0, NULL, NULL, 4, 14)
SET IDENTITY_INSERT [dbo].[DETHI] OFF
GO
SET IDENTITY_INSERT [dbo].[DETHI_CAUHOI] ON 

INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (1, 1, 1, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (2, 1, 2, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (3, 1, 3, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (4, 1, 4, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (5, 1, 5, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (6, 1, 6, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (7, 1, 7, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (8, 1, 8, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (9, 1, 9, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (10, 1, 10, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (11, 1, 11, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (12, 1, 12, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (13, 1, 13, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (14, 1, 14, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (15, 1, 15, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (16, 1, 16, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (17, 1, 17, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (18, 1, 18, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (19, 1, 19, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (20, 1, 20, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (21, 2, 5, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (22, 2, 6, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (23, 2, 7, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (24, 2, 8, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (25, 2, 17, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (26, 2, 18, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (27, 2, 19, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (28, 2, 20, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (29, 2, 1, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (30, 2, 2, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (31, 2, 3, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (32, 2, 4, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (33, 2, 21, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (34, 2, 22, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (35, 2, 23, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (36, 2, 24, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (37, 2, 25, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (38, 2, 26, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (39, 2, 27, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (40, 2, 28, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (41, 3, 21, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (42, 3, 22, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (43, 3, 23, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (44, 3, 24, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (45, 3, 1, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (46, 3, 2, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (47, 3, 3, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (48, 3, 4, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (49, 3, 17, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (50, 3, 18, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (51, 3, 19, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (52, 3, 20, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (53, 3, 31, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (54, 3, 32, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (55, 3, 35, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (56, 3, 36, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (57, 3, 37, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (58, 3, 38, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (59, 3, 39, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (60, 3, 40, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (61, 4, 9, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (62, 4, 10, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (63, 4, 11, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (64, 4, 12, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (65, 4, 5, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (66, 4, 6, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (67, 4, 7, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (68, 4, 8, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (69, 4, 13, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (70, 4, 14, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (71, 4, 15, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (72, 4, 16, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (73, 4, 33, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (74, 4, 34, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (75, 4, 35, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (76, 4, 36, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (77, 4, 37, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (78, 4, 38, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (79, 4, 29, 1.0)
INSERT [dbo].[DETHI_CAUHOI] ([Id], [IdDeThi], [IdCauHoi], [TrongSo]) VALUES (80, 4, 30, 1.0)
SET IDENTITY_INSERT [dbo].[DETHI_CAUHOI] OFF
GO
SET IDENTITY_INSERT [dbo].[PHANCONG_THI] ON 

INSERT [dbo].[PHANCONG_THI] ([Id], [ExamId], [UserId], [AssignedAt], [AssignedBy], [CustomStartTime], [ExtraMinutes], [IsActive], [Note]) VALUES (1, 1, 18, CAST(N'2026-06-18T09:00:00.0000000' AS DateTime2), 3, NULL, 0, 1, N'Phân công tham gia kỳ thi cấp bệnh viện')
INSERT [dbo].[PHANCONG_THI] ([Id], [ExamId], [UserId], [AssignedAt], [AssignedBy], [CustomStartTime], [ExtraMinutes], [IsActive], [Note]) VALUES (2, 1, 19, CAST(N'2026-06-18T09:00:00.0000000' AS DateTime2), 3, NULL, 0, 1, N'Phân công tham gia kỳ thi cấp bệnh viện')
INSERT [dbo].[PHANCONG_THI] ([Id], [ExamId], [UserId], [AssignedAt], [AssignedBy], [CustomStartTime], [ExtraMinutes], [IsActive], [Note]) VALUES (3, 1, 20, CAST(N'2026-06-18T09:00:00.0000000' AS DateTime2), 3, NULL, 0, 1, N'Phân công tham gia kỳ thi cấp bệnh viện')
INSERT [dbo].[PHANCONG_THI] ([Id], [ExamId], [UserId], [AssignedAt], [AssignedBy], [CustomStartTime], [ExtraMinutes], [IsActive], [Note]) VALUES (4, 1, 21, CAST(N'2026-06-18T09:00:00.0000000' AS DateTime2), 3, NULL, 0, 1, N'Phân công tham gia kỳ thi cấp bệnh viện')
INSERT [dbo].[PHANCONG_THI] ([Id], [ExamId], [UserId], [AssignedAt], [AssignedBy], [CustomStartTime], [ExtraMinutes], [IsActive], [Note]) VALUES (5, 1, 22, CAST(N'2026-06-18T09:00:00.0000000' AS DateTime2), 3, NULL, 0, 1, N'Phân công tham gia kỳ thi cấp bệnh viện')
INSERT [dbo].[PHANCONG_THI] ([Id], [ExamId], [UserId], [AssignedAt], [AssignedBy], [CustomStartTime], [ExtraMinutes], [IsActive], [Note]) VALUES (6, 1, 23, CAST(N'2026-06-18T09:00:00.0000000' AS DateTime2), 3, NULL, 0, 1, N'Phân công tham gia kỳ thi cấp bệnh viện')
INSERT [dbo].[PHANCONG_THI] ([Id], [ExamId], [UserId], [AssignedAt], [AssignedBy], [CustomStartTime], [ExtraMinutes], [IsActive], [Note]) VALUES (7, 1, 24, CAST(N'2026-06-18T09:00:00.0000000' AS DateTime2), 3, NULL, 0, 1, N'Phân công tham gia kỳ thi cấp bệnh viện')
INSERT [dbo].[PHANCONG_THI] ([Id], [ExamId], [UserId], [AssignedAt], [AssignedBy], [CustomStartTime], [ExtraMinutes], [IsActive], [Note]) VALUES (8, 1, 25, CAST(N'2026-06-18T09:00:00.0000000' AS DateTime2), 3, NULL, 0, 1, N'Phân công tham gia kỳ thi cấp bệnh viện')
INSERT [dbo].[PHANCONG_THI] ([Id], [ExamId], [UserId], [AssignedAt], [AssignedBy], [CustomStartTime], [ExtraMinutes], [IsActive], [Note]) VALUES (9, 1, 26, CAST(N'2026-06-18T09:00:00.0000000' AS DateTime2), 3, NULL, 0, 1, N'Phân công tham gia kỳ thi cấp bệnh viện')
INSERT [dbo].[PHANCONG_THI] ([Id], [ExamId], [UserId], [AssignedAt], [AssignedBy], [CustomStartTime], [ExtraMinutes], [IsActive], [Note]) VALUES (10, 1, 27, CAST(N'2026-06-18T09:00:00.0000000' AS DateTime2), 3, NULL, 0, 1, N'Phân công tham gia kỳ thi cấp bệnh viện')
INSERT [dbo].[PHANCONG_THI] ([Id], [ExamId], [UserId], [AssignedAt], [AssignedBy], [CustomStartTime], [ExtraMinutes], [IsActive], [Note]) VALUES (11, 1, 28, CAST(N'2026-06-18T09:00:00.0000000' AS DateTime2), 3, NULL, 0, 1, N'Phân công tham gia kỳ thi cấp bệnh viện')
INSERT [dbo].[PHANCONG_THI] ([Id], [ExamId], [UserId], [AssignedAt], [AssignedBy], [CustomStartTime], [ExtraMinutes], [IsActive], [Note]) VALUES (12, 1, 29, CAST(N'2026-06-18T09:00:00.0000000' AS DateTime2), 3, NULL, 0, 1, N'Phân công tham gia kỳ thi cấp bệnh viện')
INSERT [dbo].[PHANCONG_THI] ([Id], [ExamId], [UserId], [AssignedAt], [AssignedBy], [CustomStartTime], [ExtraMinutes], [IsActive], [Note]) VALUES (13, 2, 18, CAST(N'2026-06-22T08:30:00.0000000' AS DateTime2), 4, NULL, 0, 1, N'Phân công kiểm tra chuyên đề cấp cứu')
INSERT [dbo].[PHANCONG_THI] ([Id], [ExamId], [UserId], [AssignedAt], [AssignedBy], [CustomStartTime], [ExtraMinutes], [IsActive], [Note]) VALUES (14, 2, 19, CAST(N'2026-06-22T08:30:00.0000000' AS DateTime2), 4, NULL, 0, 1, N'Phân công kiểm tra chuyên đề cấp cứu')
INSERT [dbo].[PHANCONG_THI] ([Id], [ExamId], [UserId], [AssignedAt], [AssignedBy], [CustomStartTime], [ExtraMinutes], [IsActive], [Note]) VALUES (15, 2, 20, CAST(N'2026-06-22T08:30:00.0000000' AS DateTime2), 4, NULL, 0, 1, N'Phân công kiểm tra chuyên đề cấp cứu')
INSERT [dbo].[PHANCONG_THI] ([Id], [ExamId], [UserId], [AssignedAt], [AssignedBy], [CustomStartTime], [ExtraMinutes], [IsActive], [Note]) VALUES (16, 2, 21, CAST(N'2026-06-22T08:30:00.0000000' AS DateTime2), 4, NULL, 0, 1, N'Phân công kiểm tra chuyên đề cấp cứu')
INSERT [dbo].[PHANCONG_THI] ([Id], [ExamId], [UserId], [AssignedAt], [AssignedBy], [CustomStartTime], [ExtraMinutes], [IsActive], [Note]) VALUES (17, 2, 22, CAST(N'2026-06-22T08:30:00.0000000' AS DateTime2), 4, NULL, 0, 1, N'Phân công kiểm tra chuyên đề cấp cứu')
INSERT [dbo].[PHANCONG_THI] ([Id], [ExamId], [UserId], [AssignedAt], [AssignedBy], [CustomStartTime], [ExtraMinutes], [IsActive], [Note]) VALUES (18, 3, 30, CAST(N'2026-06-29T16:00:00.0000000' AS DateTime2), 8, NULL, 0, 1, N'Phân công đánh giá kiểm soát nhiễm khuẩn đợt 1')
INSERT [dbo].[PHANCONG_THI] ([Id], [ExamId], [UserId], [AssignedAt], [AssignedBy], [CustomStartTime], [ExtraMinutes], [IsActive], [Note]) VALUES (19, 3, 31, CAST(N'2026-06-29T16:00:00.0000000' AS DateTime2), 8, NULL, 0, 1, N'Phân công đánh giá kiểm soát nhiễm khuẩn đợt 1')
INSERT [dbo].[PHANCONG_THI] ([Id], [ExamId], [UserId], [AssignedAt], [AssignedBy], [CustomStartTime], [ExtraMinutes], [IsActive], [Note]) VALUES (20, 3, 32, CAST(N'2026-06-29T16:00:00.0000000' AS DateTime2), 8, NULL, 0, 1, N'Phân công đánh giá kiểm soát nhiễm khuẩn đợt 1')
INSERT [dbo].[PHANCONG_THI] ([Id], [ExamId], [UserId], [AssignedAt], [AssignedBy], [CustomStartTime], [ExtraMinutes], [IsActive], [Note]) VALUES (21, 3, 33, CAST(N'2026-06-29T16:00:00.0000000' AS DateTime2), 8, NULL, 0, 1, N'Phân công đánh giá kiểm soát nhiễm khuẩn đợt 1')
INSERT [dbo].[PHANCONG_THI] ([Id], [ExamId], [UserId], [AssignedAt], [AssignedBy], [CustomStartTime], [ExtraMinutes], [IsActive], [Note]) VALUES (22, 3, 34, CAST(N'2026-06-29T16:00:00.0000000' AS DateTime2), 8, NULL, 0, 1, N'Phân công đánh giá kiểm soát nhiễm khuẩn đợt 1')
INSERT [dbo].[PHANCONG_THI] ([Id], [ExamId], [UserId], [AssignedAt], [AssignedBy], [CustomStartTime], [ExtraMinutes], [IsActive], [Note]) VALUES (23, 3, 35, CAST(N'2026-06-29T16:00:00.0000000' AS DateTime2), 8, NULL, 0, 1, N'Phân công đánh giá kiểm soát nhiễm khuẩn đợt 1')
INSERT [dbo].[PHANCONG_THI] ([Id], [ExamId], [UserId], [AssignedAt], [AssignedBy], [CustomStartTime], [ExtraMinutes], [IsActive], [Note]) VALUES (24, 3, 36, CAST(N'2026-06-29T16:00:00.0000000' AS DateTime2), 8, NULL, 0, 1, N'Phân công đánh giá kiểm soát nhiễm khuẩn đợt 1')
INSERT [dbo].[PHANCONG_THI] ([Id], [ExamId], [UserId], [AssignedAt], [AssignedBy], [CustomStartTime], [ExtraMinutes], [IsActive], [Note]) VALUES (25, 3, 37, CAST(N'2026-06-29T16:00:00.0000000' AS DateTime2), 8, NULL, 0, 1, N'Phân công đánh giá kiểm soát nhiễm khuẩn đợt 1')
INSERT [dbo].[PHANCONG_THI] ([Id], [ExamId], [UserId], [AssignedAt], [AssignedBy], [CustomStartTime], [ExtraMinutes], [IsActive], [Note]) VALUES (26, 4, 21, CAST(N'2026-07-01T08:30:00.0000000' AS DateTime2), 5, CAST(N'2026-07-08T08:30:00.0000000' AS DateTime2), 10, 1, N'Phân công ôn tập trước kỳ thi điều dưỡng hồi sức nhi')
INSERT [dbo].[PHANCONG_THI] ([Id], [ExamId], [UserId], [AssignedAt], [AssignedBy], [CustomStartTime], [ExtraMinutes], [IsActive], [Note]) VALUES (27, 4, 22, CAST(N'2026-07-01T08:30:00.0000000' AS DateTime2), 5, CAST(N'2026-07-08T08:30:00.0000000' AS DateTime2), 10, 1, N'Phân công ôn tập trước kỳ thi điều dưỡng hồi sức nhi')
INSERT [dbo].[PHANCONG_THI] ([Id], [ExamId], [UserId], [AssignedAt], [AssignedBy], [CustomStartTime], [ExtraMinutes], [IsActive], [Note]) VALUES (28, 4, 23, CAST(N'2026-07-01T08:30:00.0000000' AS DateTime2), 5, CAST(N'2026-07-08T08:30:00.0000000' AS DateTime2), 10, 1, N'Phân công ôn tập trước kỳ thi điều dưỡng hồi sức nhi')
INSERT [dbo].[PHANCONG_THI] ([Id], [ExamId], [UserId], [AssignedAt], [AssignedBy], [CustomStartTime], [ExtraMinutes], [IsActive], [Note]) VALUES (29, 4, 24, CAST(N'2026-07-01T08:30:00.0000000' AS DateTime2), 5, CAST(N'2026-07-08T08:30:00.0000000' AS DateTime2), 10, 1, N'Phân công ôn tập trước kỳ thi điều dưỡng hồi sức nhi')
INSERT [dbo].[PHANCONG_THI] ([Id], [ExamId], [UserId], [AssignedAt], [AssignedBy], [CustomStartTime], [ExtraMinutes], [IsActive], [Note]) VALUES (30, 4, 25, CAST(N'2026-07-01T08:30:00.0000000' AS DateTime2), 5, CAST(N'2026-07-08T08:30:00.0000000' AS DateTime2), 10, 1, N'Phân công ôn tập trước kỳ thi điều dưỡng hồi sức nhi')
SET IDENTITY_INSERT [dbo].[PHANCONG_THI] OFF
GO
SET IDENTITY_INSERT [dbo].[KHOA_PHONG] ON 

INSERT [dbo].[KHOA_PHONG] ([Id], [MaKhoa], [TenKhoa], [MoTa], [TrangThai], [DeptManagerId], [NguoiTao], [NgayTao], [NgayCapNhat]) VALUES (1, N'BAN_GIAM_DOC', N'Ban Giám đốc', N'Đơn vị điều hành chung, theo dõi kết quả thi cấp bệnh viện.', 1, 2, 1, CAST(N'2026-05-20T08:00:00.000' AS DateTime), NULL)
INSERT [dbo].[KHOA_PHONG] ([Id], [MaKhoa], [TenKhoa], [MoTa], [TrangThai], [DeptManagerId], [NguoiTao], [NgayTao], [NgayCapNhat]) VALUES (2, N'PHONG_DIEU_DUONG', N'Phòng Điều dưỡng', N'Phụ trách tổ chức kỳ thi tay nghề, quản lý ngân hàng câu hỏi và tổng hợp kết quả.', 1, 3, 1, CAST(N'2026-05-20T08:02:00.000' AS DateTime), NULL)
INSERT [dbo].[KHOA_PHONG] ([Id], [MaKhoa], [TenKhoa], [MoTa], [TrangThai], [DeptManagerId], [NguoiTao], [NgayTao], [NgayCapNhat]) VALUES (3, N'KHOA_CAP_CUU', N'Khoa Cấp cứu', N'Tiếp nhận, phân loại và xử trí cấp cứu ban đầu cho bệnh nhi.', 1, 4, 1, CAST(N'2026-05-20T08:04:00.000' AS DateTime), NULL)
INSERT [dbo].[KHOA_PHONG] ([Id], [MaKhoa], [TenKhoa], [MoTa], [TrangThai], [DeptManagerId], [NguoiTao], [NgayTao], [NgayCapNhat]) VALUES (4, N'KHOA_HSTC_CHONG_DOC', N'Khoa Hồi sức tích cực - Chống độc', N'Chăm sóc người bệnh nặng, theo dõi hô hấp tuần hoàn và xử trí tình huống khẩn cấp.', 1, 5, 1, CAST(N'2026-05-20T08:06:00.000' AS DateTime), NULL)
INSERT [dbo].[KHOA_PHONG] ([Id], [MaKhoa], [TenKhoa], [MoTa], [TrangThai], [DeptManagerId], [NguoiTao], [NgayTao], [NgayCapNhat]) VALUES (5, N'KHOA_SO_SINH', N'Khoa Sơ sinh', N'Chăm sóc trẻ sơ sinh non tháng, sơ sinh bệnh lý và theo dõi dấu hiệu sinh tồn.', 1, 6, 1, CAST(N'2026-05-20T08:08:00.000' AS DateTime), NULL)
INSERT [dbo].[KHOA_PHONG] ([Id], [MaKhoa], [TenKhoa], [MoTa], [TrangThai], [DeptManagerId], [NguoiTao], [NgayTao], [NgayCapNhat]) VALUES (6, N'KHOA_HO_HAP', N'Khoa Hô hấp', N'Điều trị bệnh lý hô hấp, quản lý thở oxy, khí dung và theo dõi suy hô hấp.', 1, 7, 1, CAST(N'2026-05-20T08:10:00.000' AS DateTime), NULL)
INSERT [dbo].[KHOA_PHONG] ([Id], [MaKhoa], [TenKhoa], [MoTa], [TrangThai], [DeptManagerId], [NguoiTao], [NgayTao], [NgayCapNhat]) VALUES (7, N'KHOA_NHIEM', N'Khoa Nhiễm', N'Quản lý bệnh truyền nhiễm, cách ly và kiểm soát lây nhiễm trong chăm sóc.', 1, 8, 1, CAST(N'2026-05-20T08:12:00.000' AS DateTime), NULL)
INSERT [dbo].[KHOA_PHONG] ([Id], [MaKhoa], [TenKhoa], [MoTa], [TrangThai], [DeptManagerId], [NguoiTao], [NgayTao], [NgayCapNhat]) VALUES (8, N'KHOA_NGOAI_TONG_HOP', N'Khoa Ngoại tổng hợp', N'Chăm sóc người bệnh trước và sau phẫu thuật, theo dõi vết mổ và dẫn lưu.', 1, 9, 1, CAST(N'2026-05-20T08:14:00.000' AS DateTime), NULL)
INSERT [dbo].[KHOA_PHONG] ([Id], [MaKhoa], [TenKhoa], [MoTa], [TrangThai], [DeptManagerId], [NguoiTao], [NgayTao], [NgayCapNhat]) VALUES (9, N'KHOA_TIM_MACH', N'Khoa Tim mạch', N'Theo dõi bệnh lý tim mạch nhi khoa, dấu hiệu sinh tồn và chăm sóc dùng thuốc.', 1, 10, 1, CAST(N'2026-05-20T08:16:00.000' AS DateTime), NULL)
INSERT [dbo].[KHOA_PHONG] ([Id], [MaKhoa], [TenKhoa], [MoTa], [TrangThai], [DeptManagerId], [NguoiTao], [NgayTao], [NgayCapNhat]) VALUES (10, N'KHOA_TIEU_HOA', N'Khoa Tiêu hóa', N'Chăm sóc bệnh nhi tiêu hóa, dinh dưỡng, bù dịch và theo dõi mất nước.', 1, 11, 1, CAST(N'2026-05-20T08:18:00.000' AS DateTime), NULL)
INSERT [dbo].[KHOA_PHONG] ([Id], [MaKhoa], [TenKhoa], [MoTa], [TrangThai], [DeptManagerId], [NguoiTao], [NgayTao], [NgayCapNhat]) VALUES (11, N'KHOA_NGOAI_THAN_KINH', N'Khoa Ngoại thần kinh', N'Chăm sóc hậu phẫu thần kinh, theo dõi tri giác và dấu hiệu thần kinh.', 1, 12, 1, CAST(N'2026-05-20T08:20:00.000' AS DateTime), NULL)
INSERT [dbo].[KHOA_PHONG] ([Id], [MaKhoa], [TenKhoa], [MoTa], [TrangThai], [DeptManagerId], [NguoiTao], [NgayTao], [NgayCapNhat]) VALUES (12, N'KHOA_UNG_BUOU_HUYET_HOC', N'Khoa Ung bướu huyết học', N'Theo dõi người bệnh hóa trị, truyền máu và phòng ngừa nhiễm khuẩn.', 1, 13, 1, CAST(N'2026-05-20T08:22:00.000' AS DateTime), NULL)
INSERT [dbo].[KHOA_PHONG] ([Id], [MaKhoa], [TenKhoa], [MoTa], [TrangThai], [DeptManagerId], [NguoiTao], [NgayTao], [NgayCapNhat]) VALUES (13, N'KHOA_THAN_KINH', N'Khoa Thần kinh', N'Chăm sóc bệnh nhi co giật, rối loạn tri giác và phục hồi chức năng cơ bản.', 1, 14, 1, CAST(N'2026-05-20T08:24:00.000' AS DateTime), NULL)
INSERT [dbo].[KHOA_PHONG] ([Id], [MaKhoa], [TenKhoa], [MoTa], [TrangThai], [DeptManagerId], [NguoiTao], [NgayTao], [NgayCapNhat]) VALUES (14, N'KHOA_CHAN_DOAN_HINH_ANH', N'Khoa Chẩn đoán hình ảnh', N'Phối hợp chuẩn bị người bệnh trước thăm dò hình ảnh và theo dõi sau thủ thuật.', 1, 15, 1, CAST(N'2026-05-20T08:26:00.000' AS DateTime), NULL)
INSERT [dbo].[KHOA_PHONG] ([Id], [MaKhoa], [TenKhoa], [MoTa], [TrangThai], [DeptManagerId], [NguoiTao], [NgayTao], [NgayCapNhat]) VALUES (15, N'PHONG_CONG_NGHE_THONG_TIN', N'Phòng Công nghệ thông tin', N'Quản trị hạ tầng, tài khoản và hỗ trợ kỹ thuật trong quá trình tổ chức thi.', 1, 16, 1, CAST(N'2026-05-20T08:28:00.000' AS DateTime), NULL)
INSERT [dbo].[KHOA_PHONG] ([Id], [MaKhoa], [TenKhoa], [MoTa], [TrangThai], [DeptManagerId], [NguoiTao], [NgayTao], [NgayCapNhat]) VALUES (16, N'PHONG_TO_CHUC_CAN_BO', N'Phòng Tổ chức cán bộ', N'Quản lý danh sách nhân sự tham gia thi và phối hợp phân công theo khoa/phòng.', 1, 17, 1, CAST(N'2026-05-20T08:30:00.000' AS DateTime), NULL)
SET IDENTITY_INSERT [dbo].[KHOA_PHONG] OFF
GO
SET IDENTITY_INSERT [dbo].[KyThi] ON 

INSERT [dbo].[KyThi] ([Id], [MaKyThi], [TenKyThi], [MoTa], [TrangThai], [ThoiGianBatDau], [ThoiGianKetThuc], [NguoiTao], [NgayTao], [NgayCapNhat], [DonViToChuc], [KhoaPhongId], [SoCauDungToiThieu], [TongSoCauHoi]) VALUES (1, N'KT_BTV_2026_CAPBV', N'Kỳ thi Bàn tay vàng điều dưỡng cấp bệnh viện năm 2026', N'Đánh giá kiến thức nền tảng về an toàn người bệnh, kiểm soát nhiễm khuẩn và xử trí tình huống thường gặp trong chăm sóc nhi khoa.', N'DaKetThuc', CAST(N'2026-06-20T08:00:00.0000000' AS DateTime2), CAST(N'2026-06-20T11:30:00.0000000' AS DateTime2), 3, CAST(N'2026-06-01T09:15:00.000' AS DateTime), CAST(N'2026-06-20T12:05:00.0000000' AS DateTime2), N'Phòng Điều dưỡng', 2, 14, 20)
INSERT [dbo].[KyThi] ([Id], [MaKyThi], [TenKyThi], [MoTa], [TrangThai], [ThoiGianBatDau], [ThoiGianKetThuc], [NguoiTao], [NgayTao], [NgayCapNhat], [DonViToChuc], [KhoaPhongId], [SoCauDungToiThieu], [TongSoCauHoi]) VALUES (2, N'KT_CC_2026_CK', N'Kiểm tra chuyên đề cấp cứu nhi khoa tháng 06/2026', N'Kiểm tra kiến thức nhận định ban đầu, báo động đỏ nội viện và xử trí cấp cứu cơ bản.', N'DaKetThuc', CAST(N'2026-06-24T14:00:00.0000000' AS DateTime2), CAST(N'2026-06-24T16:00:00.0000000' AS DateTime2), 4, CAST(N'2026-06-10T10:20:00.000' AS DateTime), CAST(N'2026-06-24T16:15:00.0000000' AS DateTime2), N'Khoa Cấp cứu', 3, 13, 20)
INSERT [dbo].[KyThi] ([Id], [MaKyThi], [TenKyThi], [MoTa], [TrangThai], [ThoiGianBatDau], [ThoiGianKetThuc], [NguoiTao], [NgayTao], [NgayCapNhat], [DonViToChuc], [KhoaPhongId], [SoCauDungToiThieu], [TongSoCauHoi]) VALUES (3, N'KT_KSNK_2026_DOT1', N'Đánh giá kiểm soát nhiễm khuẩn đợt 1 năm 2026', N'Đánh giá tuân thủ vệ sinh tay, phòng ngừa phơi nhiễm và phân loại chất thải y tế.', N'DangDienRa', CAST(N'2026-06-30T08:00:00.0000000' AS DateTime2), CAST(N'2026-07-03T17:00:00.0000000' AS DateTime2), 8, CAST(N'2026-06-18T08:45:00.000' AS DateTime), CAST(N'2026-06-29T15:30:00.0000000' AS DateTime2), N'Khoa Nhiễm', 7, 14, 20)
INSERT [dbo].[KyThi] ([Id], [MaKyThi], [TenKyThi], [MoTa], [TrangThai], [ThoiGianBatDau], [ThoiGianKetThuc], [NguoiTao], [NgayTao], [NgayCapNhat], [DonViToChuc], [KhoaPhongId], [SoCauDungToiThieu], [TongSoCauHoi]) VALUES (4, N'KT_HSTC_2026_ONDINH', N'Ôn tập đánh giá điều dưỡng hồi sức nhi tháng 07/2026', N'Bộ đề ôn tập dùng cho nhân sự mới và nhân sự chuyển khoa trước khi tham gia kiểm tra chính thức.', N'DangChuanBi', CAST(N'2026-07-08T08:00:00.0000000' AS DateTime2), CAST(N'2026-07-08T11:00:00.0000000' AS DateTime2), 5, CAST(N'2026-06-25T13:30:00.000' AS DateTime), NULL, N'Khoa Hồi sức tích cực - Chống độc', 4, 14, 20)
SET IDENTITY_INSERT [dbo].[KyThi] OFF
GO
SET IDENTITY_INSERT [dbo].[LOAICAUHOI] ON 

INSERT [dbo].[LOAICAUHOI] ([Id], [TenLoai], [MoTa]) VALUES (1, N'Trắc nghiệm', N'Câu hỏi có nhiều lựa chọn, hệ thống tự động chấm điểm')
INSERT [dbo].[LOAICAUHOI] ([Id], [TenLoai], [MoTa]) VALUES (2, N'Tự luận', N'Câu hỏi tự luận dùng cho đánh giá bổ sung khi cần chấm thủ công')
SET IDENTITY_INSERT [dbo].[LOAICAUHOI] OFF
GO
SET IDENTITY_INSERT [dbo].[LOGTHAOTAC] ON 

INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (1, NULL, N'CREATE_EXAM', N'Tạo kỳ thi Bàn tay vàng điều dưỡng cấp bệnh viện năm 2026', CAST(N'2026-06-01T09:15:00.000' AS DateTime), N'10.20.1.12', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 3, N'ql002', N'POST', N'/api/exams', 200, N'Phòng Điều dưỡng')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (2, NULL, N'IMPORT_CANDIDATES', N'Import danh sách thí sinh từ Phòng Tổ chức cán bộ', CAST(N'2026-06-02T14:10:00.000' AS DateTime), N'10.20.1.12', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 3, N'ql002', N'POST', N'/api/users/import', 200, N'Phòng Điều dưỡng')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (3, NULL, N'CREATE_QUESTION', N'Tạo ngân hàng câu hỏi cấp bệnh viện', CAST(N'2026-06-03T08:30:00.000' AS DateTime), N'10.20.1.18', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 3, N'ql002', N'POST', N'/api/questions', 200, N'Phòng Điều dưỡng')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (4, NULL, N'CREATE_EXAM', N'Tạo kỳ thi chuyên đề cấp cứu nhi khoa tháng 06/2026', CAST(N'2026-06-10T10:20:00.000' AS DateTime), N'10.20.5.22', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 4, N'ql003', N'POST', N'/api/exams', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (5, NULL, N'CREATE_EXAM', N'Tạo kỳ thi kiểm soát nhiễm khuẩn đợt 1 năm 2026', CAST(N'2026-06-18T08:45:00.000' AS DateTime), N'10.20.2.21', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 8, N'ql007', N'POST', N'/api/exams', 200, N'Khoa Nhiễm')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (6, NULL, N'CREATE_EXAM', N'Tạo đề ôn tập điều dưỡng hồi sức nhi tháng 07/2026', CAST(N'2026-06-25T13:30:00.000' AS DateTime), N'10.20.4.08', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 5, N'ql004', N'POST', N'/api/exams', 200, N'Khoa Hồi sức tích cực - Chống độc')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (7, NULL, N'PUBLISH_RESULT', N'Công bố kết quả kỳ thi cấp bệnh viện cho các thí sinh đạt điều kiện', CAST(N'2026-06-20T12:00:00.000' AS DateTime), N'10.20.4.19', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 3, N'ql002', N'POST', N'/api/exams/1/publish', 200, N'Phòng Điều dưỡng')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (8, NULL, N'PUBLISH_RESULT', N'Công bố kết quả kiểm tra chuyên đề cấp cứu nhi khoa', CAST(N'2026-06-24T16:10:00.000' AS DateTime), N'10.20.2.35', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 4, N'ql003', N'POST', N'/api/exams/2/publish', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (9, 1, N'LOGIN', N'Thí sinh đăng nhập hệ thống thi', CAST(N'2026-06-20T08:22:42.000' AS DateTime), N'10.20.1.18', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 18, N'dd001', N'POST', N'/api/auth/login', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (10, 1, N'OPEN_EXAM', N'Mở đề thi BTV_2026_BV_DE01', CAST(N'2026-06-20T08:30:42.000' AS DateTime), N'10.20.1.18', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 18, N'dd001', N'GET', N'/exam/BTV_2026_BV_DE01', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (11, 1, N'START_EXAM', N'Bắt đầu làm bài', CAST(N'2026-06-20T08:30:58.000' AS DateTime), N'10.20.1.12', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 18, N'dd001', N'POST', N'/api/exams/1/start', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (12, 1, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 1-5', CAST(N'2026-06-20T08:37:05.000' AS DateTime), N'10.20.1.12', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 18, N'dd001', N'POST', N'/api/attempts/1/save', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (13, 1, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 6-10', CAST(N'2026-06-20T08:44:21.000' AS DateTime), N'10.20.4.19', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 18, N'dd001', N'POST', N'/api/attempts/1/save', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (14, 1, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 11-15', CAST(N'2026-06-20T08:51:04.000' AS DateTime), N'10.20.4.19', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 18, N'dd001', N'POST', N'/api/attempts/1/save', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (15, 1, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 16-20', CAST(N'2026-06-20T08:57:53.000' AS DateTime), N'10.20.1.12', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 18, N'dd001', N'POST', N'/api/attempts/1/save', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (16, 1, N'SUBMIT_EXAM', N'Nộp bài đúng hạn', CAST(N'2026-06-20T09:03:31.000' AS DateTime), N'10.20.1.12', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 18, N'dd001', N'POST', N'/api/attempts/1/submit', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (17, 2, N'LOGIN', N'Thí sinh đăng nhập hệ thống thi', CAST(N'2026-06-20T08:26:05.000' AS DateTime), N'10.20.5.22', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 19, N'dd002', N'POST', N'/api/auth/login', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (18, 2, N'OPEN_EXAM', N'Mở đề thi BTV_2026_BV_DE01', CAST(N'2026-06-20T08:31:05.000' AS DateTime), N'10.20.3.14', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 19, N'dd002', N'GET', N'/exam/BTV_2026_BV_DE01', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (19, 2, N'START_EXAM', N'Bắt đầu làm bài', CAST(N'2026-06-20T08:31:20.000' AS DateTime), N'10.20.4.08', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 19, N'dd002', N'POST', N'/api/exams/1/start', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (20, 2, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 1-5', CAST(N'2026-06-20T08:37:29.000' AS DateTime), N'10.20.5.22', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 19, N'dd002', N'POST', N'/api/attempts/2/save', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (21, 2, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 6-10', CAST(N'2026-06-20T08:44:35.000' AS DateTime), N'10.20.2.35', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 19, N'dd002', N'POST', N'/api/attempts/2/save', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (22, 2, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 11-15', CAST(N'2026-06-20T08:51:51.000' AS DateTime), N'10.20.5.22', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 19, N'dd002', N'POST', N'/api/attempts/2/save', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (23, 2, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 16-20', CAST(N'2026-06-20T08:58:20.000' AS DateTime), N'10.20.2.35', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 19, N'dd002', N'POST', N'/api/attempts/2/save', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (24, 2, N'TAB_SWITCH', N'Thí sinh chuyển sang cửa sổ khác trong quá trình làm bài', CAST(N'2026-06-20T08:44:19.000' AS DateTime), N'10.20.2.35', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 19, N'dd002', N'POST', N'/api/attempts/2/warnings', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (25, 2, N'SUBMIT_EXAM', N'Nộp bài đúng hạn', CAST(N'2026-06-20T08:55:24.000' AS DateTime), N'10.20.3.14', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 19, N'dd002', N'POST', N'/api/attempts/2/submit', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (26, 3, N'LOGIN', N'Thí sinh đăng nhập hệ thống thi', CAST(N'2026-06-20T08:28:10.000' AS DateTime), N'10.20.5.22', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 20, N'dd003', N'POST', N'/api/auth/login', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (27, 3, N'OPEN_EXAM', N'Mở đề thi BTV_2026_BV_DE01', CAST(N'2026-06-20T08:32:10.000' AS DateTime), N'10.20.2.21', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 20, N'dd003', N'GET', N'/exam/BTV_2026_BV_DE01', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (28, 3, N'START_EXAM', N'Bắt đầu làm bài', CAST(N'2026-06-20T08:32:33.000' AS DateTime), N'10.20.2.21', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 20, N'dd003', N'POST', N'/api/exams/1/start', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (29, 3, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 1-5', CAST(N'2026-06-20T08:38:56.000' AS DateTime), N'10.20.2.21', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 20, N'dd003', N'POST', N'/api/attempts/3/save', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (30, 3, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 6-10', CAST(N'2026-06-20T08:45:28.000' AS DateTime), N'10.20.4.08', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 20, N'dd003', N'POST', N'/api/attempts/3/save', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (31, 3, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 11-15', CAST(N'2026-06-20T08:52:17.000' AS DateTime), N'10.20.5.06', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 20, N'dd003', N'POST', N'/api/attempts/3/save', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (32, 3, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 16-20', CAST(N'2026-06-20T08:59:51.000' AS DateTime), N'10.20.5.22', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 20, N'dd003', N'POST', N'/api/attempts/3/save', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (33, 3, N'SUBMIT_EXAM', N'Nộp bài đúng hạn', CAST(N'2026-06-20T08:59:56.000' AS DateTime), N'10.20.1.12', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 20, N'dd003', N'POST', N'/api/attempts/3/submit', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (34, 4, N'LOGIN', N'Thí sinh đăng nhập hệ thống thi', CAST(N'2026-06-20T08:24:58.000' AS DateTime), N'10.20.3.14', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 21, N'dd004', N'POST', N'/api/auth/login', 200, N'Khoa Hồi sức tích cực - Chống độc')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (35, 4, N'OPEN_EXAM', N'Mở đề thi BTV_2026_BV_DE01', CAST(N'2026-06-20T08:30:58.000' AS DateTime), N'10.20.5.22', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 21, N'dd004', N'GET', N'/exam/BTV_2026_BV_DE01', 200, N'Khoa Hồi sức tích cực - Chống độc')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (36, 4, N'START_EXAM', N'Bắt đầu làm bài', CAST(N'2026-06-20T08:31:15.000' AS DateTime), N'10.20.2.35', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 21, N'dd004', N'POST', N'/api/exams/1/start', 200, N'Khoa Hồi sức tích cực - Chống độc')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (37, 4, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 1-5', CAST(N'2026-06-20T08:37:15.000' AS DateTime), N'10.20.2.21', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 21, N'dd004', N'POST', N'/api/attempts/4/save', 200, N'Khoa Hồi sức tích cực - Chống độc')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (38, 4, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 6-10', CAST(N'2026-06-20T08:44:45.000' AS DateTime), N'10.20.1.12', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 21, N'dd004', N'POST', N'/api/attempts/4/save', 200, N'Khoa Hồi sức tích cực - Chống độc')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (39, 4, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 11-15', CAST(N'2026-06-20T08:51:08.000' AS DateTime), N'10.20.1.12', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 21, N'dd004', N'POST', N'/api/attempts/4/save', 200, N'Khoa Hồi sức tích cực - Chống độc')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (40, 4, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 16-20', CAST(N'2026-06-20T08:58:17.000' AS DateTime), N'10.20.5.06', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 21, N'dd004', N'POST', N'/api/attempts/4/save', 200, N'Khoa Hồi sức tích cực - Chống độc')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (41, 4, N'SUBMIT_EXAM', N'Nộp bài đúng hạn', CAST(N'2026-06-20T08:57:23.000' AS DateTime), N'10.20.5.22', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 21, N'dd004', N'POST', N'/api/attempts/4/submit', 200, N'Khoa Hồi sức tích cực - Chống độc')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (42, 5, N'LOGIN', N'Thí sinh đăng nhập hệ thống thi', CAST(N'2026-06-20T08:30:11.000' AS DateTime), N'10.20.2.21', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 22, N'dd005', N'POST', N'/api/auth/login', 200, N'Khoa Hồi sức tích cực - Chống độc')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (43, 5, N'OPEN_EXAM', N'Mở đề thi BTV_2026_BV_DE01', CAST(N'2026-06-20T08:33:11.000' AS DateTime), N'10.20.4.08', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 22, N'dd005', N'GET', N'/exam/BTV_2026_BV_DE01', 200, N'Khoa Hồi sức tích cực - Chống độc')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (44, 5, N'START_EXAM', N'Bắt đầu làm bài', CAST(N'2026-06-20T08:33:37.000' AS DateTime), N'10.20.4.08', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 22, N'dd005', N'POST', N'/api/exams/1/start', 200, N'Khoa Hồi sức tích cực - Chống độc')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (45, 5, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 1-5', CAST(N'2026-06-20T08:39:17.000' AS DateTime), N'10.20.5.22', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 22, N'dd005', N'POST', N'/api/attempts/5/save', 200, N'Khoa Hồi sức tích cực - Chống độc')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (46, 5, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 6-10', CAST(N'2026-06-20T08:46:36.000' AS DateTime), N'10.20.2.35', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 22, N'dd005', N'POST', N'/api/attempts/5/save', 200, N'Khoa Hồi sức tích cực - Chống độc')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (47, 5, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 11-15', CAST(N'2026-06-20T08:53:56.000' AS DateTime), N'10.20.4.19', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 22, N'dd005', N'POST', N'/api/attempts/5/save', 200, N'Khoa Hồi sức tích cực - Chống độc')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (48, 5, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 16-20', CAST(N'2026-06-20T09:00:22.000' AS DateTime), N'10.20.5.22', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 22, N'dd005', N'POST', N'/api/attempts/5/save', 200, N'Khoa Hồi sức tích cực - Chống độc')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (49, 5, N'BROWSER_FOCUS_LOST', N'Cửa sổ trình duyệt mất focus khi đang làm bài', CAST(N'2026-06-20T08:46:27.000' AS DateTime), N'10.20.4.19', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 22, N'dd005', N'POST', N'/api/attempts/5/warnings', 200, N'Khoa Hồi sức tích cực - Chống độc')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (50, 5, N'FULLSCREEN_EXIT', N'Thí sinh thoát chế độ toàn màn hình', CAST(N'2026-06-20T08:52:05.000' AS DateTime), N'10.20.5.06', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 22, N'dd005', N'POST', N'/api/attempts/5/warnings', 200, N'Khoa Hồi sức tích cực - Chống độc')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (51, 5, N'SUBMIT_EXAM', N'Hết thời gian làm bài', CAST(N'2026-06-20T09:18:57.000' AS DateTime), N'10.20.1.12', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 22, N'dd005', N'POST', N'/api/attempts/5/submit', 200, N'Khoa Hồi sức tích cực - Chống độc')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (52, 6, N'LOGIN', N'Thí sinh đăng nhập hệ thống thi', CAST(N'2026-06-20T08:24:47.000' AS DateTime), N'10.20.1.12', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 23, N'dd006', N'POST', N'/api/auth/login', 200, N'Khoa Hồi sức tích cực - Chống độc')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (53, 6, N'OPEN_EXAM', N'Mở đề thi BTV_2026_BV_DE01', CAST(N'2026-06-20T08:31:47.000' AS DateTime), N'10.20.5.06', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 23, N'dd006', N'GET', N'/exam/BTV_2026_BV_DE01', 200, N'Khoa Hồi sức tích cực - Chống độc')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (54, 6, N'START_EXAM', N'Bắt đầu làm bài', CAST(N'2026-06-20T08:32:12.000' AS DateTime), N'10.20.1.12', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 23, N'dd006', N'POST', N'/api/exams/1/start', 200, N'Khoa Hồi sức tích cực - Chống độc')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (55, 6, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 1-5', CAST(N'2026-06-20T08:38:17.000' AS DateTime), N'10.20.5.22', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 23, N'dd006', N'POST', N'/api/attempts/6/save', 200, N'Khoa Hồi sức tích cực - Chống độc')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (56, 6, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 6-10', CAST(N'2026-06-20T08:45:36.000' AS DateTime), N'10.20.2.35', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 23, N'dd006', N'POST', N'/api/attempts/6/save', 200, N'Khoa Hồi sức tích cực - Chống độc')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (57, 6, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 11-15', CAST(N'2026-06-20T08:52:34.000' AS DateTime), N'10.20.1.18', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 23, N'dd006', N'POST', N'/api/attempts/6/save', 200, N'Khoa Hồi sức tích cực - Chống độc')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (58, 6, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 16-20', CAST(N'2026-06-20T08:59:29.000' AS DateTime), N'10.20.5.22', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 23, N'dd006', N'POST', N'/api/attempts/6/save', 200, N'Khoa Hồi sức tích cực - Chống độc')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (59, 6, N'SUBMIT_EXAM', N'Nộp bài đúng hạn', CAST(N'2026-06-20T09:08:13.000' AS DateTime), N'10.20.2.21', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 23, N'dd006', N'POST', N'/api/attempts/6/submit', 200, N'Khoa Hồi sức tích cực - Chống độc')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (60, 7, N'LOGIN', N'Thí sinh đăng nhập hệ thống thi', CAST(N'2026-06-20T08:30:02.000' AS DateTime), N'10.20.1.18', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 24, N'dd007', N'POST', N'/api/auth/login', 200, N'Khoa Sơ sinh')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (61, 7, N'OPEN_EXAM', N'Mở đề thi BTV_2026_BV_DE01', CAST(N'2026-06-20T08:34:02.000' AS DateTime), N'10.20.4.19', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 24, N'dd007', N'GET', N'/exam/BTV_2026_BV_DE01', 200, N'Khoa Sơ sinh')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (62, 7, N'START_EXAM', N'Bắt đầu làm bài', CAST(N'2026-06-20T08:34:28.000' AS DateTime), N'10.20.3.14', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 24, N'dd007', N'POST', N'/api/exams/1/start', 200, N'Khoa Sơ sinh')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (63, 7, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 1-5', CAST(N'2026-06-20T08:40:43.000' AS DateTime), N'10.20.4.08', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 24, N'dd007', N'POST', N'/api/attempts/7/save', 200, N'Khoa Sơ sinh')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (64, 7, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 6-10', CAST(N'2026-06-20T08:47:28.000' AS DateTime), N'10.20.1.12', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 24, N'dd007', N'POST', N'/api/attempts/7/save', 200, N'Khoa Sơ sinh')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (65, 7, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 11-15', CAST(N'2026-06-20T08:54:14.000' AS DateTime), N'10.20.1.12', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 24, N'dd007', N'POST', N'/api/attempts/7/save', 200, N'Khoa Sơ sinh')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (66, 7, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 16-20', CAST(N'2026-06-20T09:01:36.000' AS DateTime), N'10.20.2.35', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 24, N'dd007', N'POST', N'/api/attempts/7/save', 200, N'Khoa Sơ sinh')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (67, 7, N'BROWSER_FOCUS_LOST', N'Cửa sổ trình duyệt mất focus khi đang làm bài', CAST(N'2026-06-20T08:47:52.000' AS DateTime), N'10.20.1.12', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 24, N'dd007', N'POST', N'/api/attempts/7/warnings', 200, N'Khoa Sơ sinh')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (68, 7, N'SUBMIT_EXAM', N'Nộp bài đúng hạn', CAST(N'2026-06-20T09:15:27.000' AS DateTime), N'10.20.1.18', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 24, N'dd007', N'POST', N'/api/attempts/7/submit', 200, N'Khoa Sơ sinh')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (69, 8, N'LOGIN', N'Thí sinh đăng nhập hệ thống thi', CAST(N'2026-06-24T14:12:22.000' AS DateTime), N'10.20.5.22', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 18, N'dd001', N'POST', N'/api/auth/login', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (70, 8, N'OPEN_EXAM', N'Mở đề thi CC_NHI_2026_DE01', CAST(N'2026-06-24T14:15:22.000' AS DateTime), N'10.20.2.35', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 18, N'dd001', N'GET', N'/exam/CC_NHI_2026_DE01', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (71, 8, N'START_EXAM', N'Bắt đầu làm bài', CAST(N'2026-06-24T14:15:49.000' AS DateTime), N'10.20.3.14', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 18, N'dd001', N'POST', N'/api/exams/2/start', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (72, 8, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 1-5', CAST(N'2026-06-24T14:22:09.000' AS DateTime), N'10.20.3.14', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 18, N'dd001', N'POST', N'/api/attempts/8/save', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (73, 8, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 6-10', CAST(N'2026-06-24T14:29:07.000' AS DateTime), N'10.20.3.14', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 18, N'dd001', N'POST', N'/api/attempts/8/save', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (74, 8, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 11-15', CAST(N'2026-06-24T14:35:38.000' AS DateTime), N'10.20.2.21', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 18, N'dd001', N'POST', N'/api/attempts/8/save', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (75, 8, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 16-20', CAST(N'2026-06-24T14:42:34.000' AS DateTime), N'10.20.1.12', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 18, N'dd001', N'POST', N'/api/attempts/8/save', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (76, 8, N'SUBMIT_EXAM', N'Nộp bài đúng hạn', CAST(N'2026-06-24T14:40:40.000' AS DateTime), N'10.20.1.12', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 18, N'dd001', N'POST', N'/api/attempts/8/submit', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (77, 9, N'LOGIN', N'Thí sinh đăng nhập hệ thống thi', CAST(N'2026-06-24T14:11:03.000' AS DateTime), N'10.20.4.19', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 19, N'dd002', N'POST', N'/api/auth/login', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (78, 9, N'OPEN_EXAM', N'Mở đề thi CC_NHI_2026_DE01', CAST(N'2026-06-24T14:16:03.000' AS DateTime), N'10.20.2.21', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 19, N'dd002', N'GET', N'/exam/CC_NHI_2026_DE01', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (79, 9, N'START_EXAM', N'Bắt đầu làm bài', CAST(N'2026-06-24T14:16:24.000' AS DateTime), N'10.20.2.35', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 19, N'dd002', N'POST', N'/api/exams/2/start', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (80, 9, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 1-5', CAST(N'2026-06-24T14:22:39.000' AS DateTime), N'10.20.5.06', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 19, N'dd002', N'POST', N'/api/attempts/9/save', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (81, 9, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 6-10', CAST(N'2026-06-24T14:29:22.000' AS DateTime), N'10.20.2.35', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 19, N'dd002', N'POST', N'/api/attempts/9/save', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (82, 9, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 11-15', CAST(N'2026-06-24T14:36:19.000' AS DateTime), N'10.20.2.35', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 19, N'dd002', N'POST', N'/api/attempts/9/save', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (83, 9, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 16-20', CAST(N'2026-06-24T14:43:09.000' AS DateTime), N'10.20.4.08', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 19, N'dd002', N'POST', N'/api/attempts/9/save', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (84, 9, N'SUBMIT_EXAM', N'Nộp bài đúng hạn', CAST(N'2026-06-24T14:51:25.000' AS DateTime), N'10.20.4.19', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 19, N'dd002', N'POST', N'/api/attempts/9/submit', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (85, 10, N'LOGIN', N'Thí sinh đăng nhập hệ thống thi', CAST(N'2026-06-24T14:10:15.000' AS DateTime), N'10.20.2.35', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 20, N'dd003', N'POST', N'/api/auth/login', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (86, 10, N'OPEN_EXAM', N'Mở đề thi CC_NHI_2026_DE01', CAST(N'2026-06-24T14:16:15.000' AS DateTime), N'10.20.4.08', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 20, N'dd003', N'GET', N'/exam/CC_NHI_2026_DE01', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (87, 10, N'START_EXAM', N'Bắt đầu làm bài', CAST(N'2026-06-24T14:16:38.000' AS DateTime), N'10.20.5.06', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 20, N'dd003', N'POST', N'/api/exams/2/start', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (88, 10, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 1-5', CAST(N'2026-06-24T14:22:27.000' AS DateTime), N'10.20.1.18', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 20, N'dd003', N'POST', N'/api/attempts/10/save', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (89, 10, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 6-10', CAST(N'2026-06-24T14:29:42.000' AS DateTime), N'10.20.3.14', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 20, N'dd003', N'POST', N'/api/attempts/10/save', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (90, 10, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 11-15', CAST(N'2026-06-24T14:36:38.000' AS DateTime), N'10.20.4.08', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 20, N'dd003', N'POST', N'/api/attempts/10/save', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (91, 10, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 16-20', CAST(N'2026-06-24T14:43:41.000' AS DateTime), N'10.20.1.18', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 20, N'dd003', N'POST', N'/api/attempts/10/save', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (92, 10, N'TAB_SWITCH', N'Thí sinh chuyển tab trong lúc làm bài', CAST(N'2026-06-24T14:29:35.000' AS DateTime), N'10.20.2.35', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 20, N'dd003', N'POST', N'/api/attempts/10/warnings', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (93, 10, N'SUSPICIOUS_KEYBOARD', N'Thí sinh sử dụng phím tắt bị hạn chế', CAST(N'2026-06-24T14:35:02.000' AS DateTime), N'10.20.1.12', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 20, N'dd003', N'POST', N'/api/attempts/10/warnings', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (94, 10, N'SUBMIT_EXAM', N'Nộp bài đúng hạn', CAST(N'2026-06-24T14:49:57.000' AS DateTime), N'10.20.1.18', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 20, N'dd003', N'POST', N'/api/attempts/10/submit', 200, N'Khoa Cấp cứu')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (95, 11, N'LOGIN', N'Thí sinh đăng nhập hệ thống thi', CAST(N'2026-06-24T14:11:54.000' AS DateTime), N'10.20.2.21', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 21, N'dd004', N'POST', N'/api/auth/login', 200, N'Khoa Hồi sức tích cực - Chống độc')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (96, 11, N'OPEN_EXAM', N'Mở đề thi CC_NHI_2026_DE01', CAST(N'2026-06-24T14:15:54.000' AS DateTime), N'10.20.3.14', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 21, N'dd004', N'GET', N'/exam/CC_NHI_2026_DE01', 200, N'Khoa Hồi sức tích cực - Chống độc')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (97, 11, N'START_EXAM', N'Bắt đầu làm bài', CAST(N'2026-06-24T14:16:17.000' AS DateTime), N'10.20.1.18', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 21, N'dd004', N'POST', N'/api/exams/2/start', 200, N'Khoa Hồi sức tích cực - Chống độc')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (98, 11, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 1-5', CAST(N'2026-06-24T14:22:24.000' AS DateTime), N'10.20.5.22', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 21, N'dd004', N'POST', N'/api/attempts/11/save', 200, N'Khoa Hồi sức tích cực - Chống độc')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (99, 11, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 6-10', CAST(N'2026-06-24T14:29:11.000' AS DateTime), N'10.20.5.22', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 21, N'dd004', N'POST', N'/api/attempts/11/save', 200, N'Khoa Hồi sức tích cực - Chống độc')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (100, 11, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 11-15', CAST(N'2026-06-24T14:36:07.000' AS DateTime), N'10.20.2.21', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 21, N'dd004', N'POST', N'/api/attempts/11/save', 200, N'Khoa Hồi sức tích cực - Chống độc')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (101, 11, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 16-20', CAST(N'2026-06-24T14:42:59.000' AS DateTime), N'10.20.3.14', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 21, N'dd004', N'POST', N'/api/attempts/11/save', 200, N'Khoa Hồi sức tích cực - Chống độc')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (102, 11, N'SUBMIT_EXAM', N'Nộp bài đúng hạn', CAST(N'2026-06-24T14:43:34.000' AS DateTime), N'10.20.2.35', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 21, N'dd004', N'POST', N'/api/attempts/11/submit', 200, N'Khoa Hồi sức tích cực - Chống độc')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (103, 12, N'LOGIN', N'Thí sinh đăng nhập hệ thống thi', CAST(N'2026-06-24T14:13:30.000' AS DateTime), N'10.20.4.08', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 22, N'dd005', N'POST', N'/api/auth/login', 200, N'Khoa Hồi sức tích cực - Chống độc')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (104, 12, N'OPEN_EXAM', N'Mở đề thi CC_NHI_2026_DE01', CAST(N'2026-06-24T14:16:30.000' AS DateTime), N'10.20.4.08', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 22, N'dd005', N'GET', N'/exam/CC_NHI_2026_DE01', 200, N'Khoa Hồi sức tích cực - Chống độc')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (105, 12, N'START_EXAM', N'Bắt đầu làm bài', CAST(N'2026-06-24T14:16:58.000' AS DateTime), N'10.20.3.14', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 22, N'dd005', N'POST', N'/api/exams/2/start', 200, N'Khoa Hồi sức tích cực - Chống độc')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (106, 12, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 1-5', CAST(N'2026-06-24T14:23:16.000' AS DateTime), N'10.20.5.22', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 22, N'dd005', N'POST', N'/api/attempts/12/save', 200, N'Khoa Hồi sức tích cực - Chống độc')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (107, 12, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 6-10', CAST(N'2026-06-24T14:29:46.000' AS DateTime), N'10.20.4.08', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 22, N'dd005', N'POST', N'/api/attempts/12/save', 200, N'Khoa Hồi sức tích cực - Chống độc')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (108, 12, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 11-15', CAST(N'2026-06-24T14:36:40.000' AS DateTime), N'10.20.3.14', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 22, N'dd005', N'POST', N'/api/attempts/12/save', 200, N'Khoa Hồi sức tích cực - Chống độc')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (109, 12, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 16-20', CAST(N'2026-06-24T14:43:38.000' AS DateTime), N'10.20.2.21', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 22, N'dd005', N'POST', N'/api/attempts/12/save', 200, N'Khoa Hồi sức tích cực - Chống độc')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (110, 12, N'BROWSER_FOCUS_LOST', N'Cửa sổ trình duyệt mất focus khi đang làm bài', CAST(N'2026-06-24T14:29:44.000' AS DateTime), N'10.20.2.35', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 22, N'dd005', N'POST', N'/api/attempts/12/warnings', 200, N'Khoa Hồi sức tích cực - Chống độc')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (111, 12, N'SUBMIT_EXAM', N'Nộp bài đúng hạn', CAST(N'2026-06-24T14:42:12.000' AS DateTime), N'10.20.4.08', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 22, N'dd005', N'POST', N'/api/attempts/12/submit', 200, N'Khoa Hồi sức tích cực - Chống độc')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (112, 13, N'LOGIN', N'Thí sinh đăng nhập hệ thống thi', CAST(N'2026-06-30T08:01:18.000' AS DateTime), N'10.20.2.35', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 30, N'dd013', N'POST', N'/api/auth/login', 200, N'Khoa Nhiễm')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (113, 13, N'OPEN_EXAM', N'Mở đề thi KSNK_2026_DOT1_DE01', CAST(N'2026-06-30T08:05:18.000' AS DateTime), N'10.20.3.14', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 30, N'dd013', N'GET', N'/exam/KSNK_2026_DOT1_DE01', 200, N'Khoa Nhiễm')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (114, 13, N'START_EXAM', N'Bắt đầu làm bài', CAST(N'2026-06-30T08:05:38.000' AS DateTime), N'10.20.1.12', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 30, N'dd013', N'POST', N'/api/exams/3/start', 200, N'Khoa Nhiễm')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (115, 13, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 1-5', CAST(N'2026-06-30T08:11:44.000' AS DateTime), N'10.20.2.35', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 30, N'dd013', N'POST', N'/api/attempts/13/save', 200, N'Khoa Nhiễm')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (116, 13, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 6-10', CAST(N'2026-06-30T08:18:54.000' AS DateTime), N'10.20.2.35', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 30, N'dd013', N'POST', N'/api/attempts/13/save', 200, N'Khoa Nhiễm')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (117, 13, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 11-15', CAST(N'2026-06-30T08:25:53.000' AS DateTime), N'10.20.1.12', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 30, N'dd013', N'POST', N'/api/attempts/13/save', 200, N'Khoa Nhiễm')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (118, 13, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 16-20', CAST(N'2026-06-30T08:32:52.000' AS DateTime), N'10.20.5.22', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 30, N'dd013', N'POST', N'/api/attempts/13/save', 200, N'Khoa Nhiễm')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (119, 13, N'SUBMIT_EXAM', N'Nộp bài đúng hạn', CAST(N'2026-06-30T08:39:44.000' AS DateTime), N'10.20.4.19', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 30, N'dd013', N'POST', N'/api/attempts/13/submit', 200, N'Khoa Nhiễm')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (120, 14, N'LOGIN', N'Thí sinh đăng nhập hệ thống thi', CAST(N'2026-06-30T08:00:42.000' AS DateTime), N'10.20.3.14', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 31, N'dd014', N'POST', N'/api/auth/login', 200, N'Khoa Nhiễm')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (121, 14, N'OPEN_EXAM', N'Mở đề thi KSNK_2026_DOT1_DE01', CAST(N'2026-06-30T08:07:42.000' AS DateTime), N'10.20.4.08', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 31, N'dd014', N'GET', N'/exam/KSNK_2026_DOT1_DE01', 200, N'Khoa Nhiễm')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (122, 14, N'START_EXAM', N'Bắt đầu làm bài', CAST(N'2026-06-30T08:08:09.000' AS DateTime), N'10.20.1.12', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 31, N'dd014', N'POST', N'/api/exams/3/start', 200, N'Khoa Nhiễm')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (123, 14, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 1-5', CAST(N'2026-06-30T08:14:30.000' AS DateTime), N'10.20.2.21', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 31, N'dd014', N'POST', N'/api/attempts/14/save', 200, N'Khoa Nhiễm')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (124, 14, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 6-10', CAST(N'2026-06-30T08:21:32.000' AS DateTime), N'10.20.4.08', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 31, N'dd014', N'POST', N'/api/attempts/14/save', 200, N'Khoa Nhiễm')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (125, 14, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 11-15', CAST(N'2026-06-30T08:28:06.000' AS DateTime), N'10.20.4.19', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 31, N'dd014', N'POST', N'/api/attempts/14/save', 200, N'Khoa Nhiễm')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (126, 14, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 16-20', CAST(N'2026-06-30T08:35:26.000' AS DateTime), N'10.20.3.14', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 31, N'dd014', N'POST', N'/api/attempts/14/save', 200, N'Khoa Nhiễm')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (127, 14, N'TAB_SWITCH', N'Thí sinh chuyển tab trong lúc làm bài', CAST(N'2026-06-30T08:20:55.000' AS DateTime), N'10.20.1.18', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 31, N'dd014', N'POST', N'/api/attempts/14/warnings', 200, N'Khoa Nhiễm')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (128, 14, N'SUBMIT_EXAM', N'Nộp bài đúng hạn', CAST(N'2026-06-30T08:30:18.000' AS DateTime), N'10.20.2.21', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 31, N'dd014', N'POST', N'/api/attempts/14/submit', 200, N'Khoa Nhiễm')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (129, 15, N'LOGIN', N'Thí sinh đăng nhập hệ thống thi', CAST(N'2026-06-30T08:03:05.000' AS DateTime), N'10.20.1.18', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 32, N'dd015', N'POST', N'/api/auth/login', 200, N'Khoa Nhiễm')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (130, 15, N'OPEN_EXAM', N'Mở đề thi KSNK_2026_DOT1_DE01', CAST(N'2026-06-30T08:06:05.000' AS DateTime), N'10.20.1.18', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 32, N'dd015', N'GET', N'/exam/KSNK_2026_DOT1_DE01', 200, N'Khoa Nhiễm')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (131, 15, N'START_EXAM', N'Bắt đầu làm bài', CAST(N'2026-06-30T08:06:35.000' AS DateTime), N'10.20.5.22', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 32, N'dd015', N'POST', N'/api/exams/3/start', 200, N'Khoa Nhiễm')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (132, 15, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 1-5', CAST(N'2026-06-30T08:12:25.000' AS DateTime), N'10.20.2.35', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 32, N'dd015', N'POST', N'/api/attempts/15/save', 200, N'Khoa Nhiễm')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (133, 15, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 6-10', CAST(N'2026-06-30T08:19:25.000' AS DateTime), N'10.20.4.08', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 32, N'dd015', N'POST', N'/api/attempts/15/save', 200, N'Khoa Nhiễm')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (134, 15, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 11-15', CAST(N'2026-06-30T08:26:50.000' AS DateTime), N'10.20.3.14', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 32, N'dd015', N'POST', N'/api/attempts/15/save', 200, N'Khoa Nhiễm')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (135, 15, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 16-20', CAST(N'2026-06-30T08:33:30.000' AS DateTime), N'10.20.2.35', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 32, N'dd015', N'POST', N'/api/attempts/15/save', 200, N'Khoa Nhiễm')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (136, 15, N'SUBMIT_EXAM', N'Nộp bài đúng hạn', CAST(N'2026-06-30T08:27:26.000' AS DateTime), N'10.20.5.22', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 32, N'dd015', N'POST', N'/api/attempts/15/submit', 200, N'Khoa Nhiễm')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (137, 16, N'LOGIN', N'Thí sinh đăng nhập hệ thống thi', CAST(N'2026-06-30T08:03:10.000' AS DateTime), N'10.20.2.21', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 33, N'dd016', N'POST', N'/api/auth/login', 200, N'Khoa Ngoại tổng hợp')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (138, 16, N'OPEN_EXAM', N'Mở đề thi KSNK_2026_DOT1_DE01', CAST(N'2026-06-30T08:09:10.000' AS DateTime), N'10.20.4.08', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 33, N'dd016', N'GET', N'/exam/KSNK_2026_DOT1_DE01', 200, N'Khoa Ngoại tổng hợp')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (139, 16, N'START_EXAM', N'Bắt đầu làm bài', CAST(N'2026-06-30T08:09:26.000' AS DateTime), N'10.20.1.18', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 33, N'dd016', N'POST', N'/api/exams/3/start', 200, N'Khoa Ngoại tổng hợp')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (140, 16, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 1-5', CAST(N'2026-06-30T08:15:17.000' AS DateTime), N'10.20.3.14', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 33, N'dd016', N'POST', N'/api/attempts/16/save', 200, N'Khoa Ngoại tổng hợp')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (141, 16, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 6-10', CAST(N'2026-06-30T08:22:45.000' AS DateTime), N'10.20.3.14', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 33, N'dd016', N'POST', N'/api/attempts/16/save', 200, N'Khoa Ngoại tổng hợp')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (142, 16, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 11-15', CAST(N'2026-06-30T08:29:50.000' AS DateTime), N'10.20.2.35', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 33, N'dd016', N'POST', N'/api/attempts/16/save', 200, N'Khoa Ngoại tổng hợp')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (143, 16, N'SAVE_PROGRESS', N'Tự động lưu tiến độ làm bài, nhóm câu 16-20', CAST(N'2026-06-30T08:36:17.000' AS DateTime), N'10.20.3.14', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 33, N'dd016', N'POST', N'/api/attempts/16/save', 200, N'Khoa Ngoại tổng hợp')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (144, 16, N'BROWSER_FOCUS_LOST', N'Cửa sổ trình duyệt mất focus khi đang làm bài', CAST(N'2026-06-30T08:22:56.000' AS DateTime), N'10.20.1.12', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 33, N'dd016', N'POST', N'/api/attempts/16/warnings', 200, N'Khoa Ngoại tổng hợp')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (145, 16, N'FULLSCREEN_EXIT', N'Thí sinh thoát chế độ toàn màn hình', CAST(N'2026-06-30T08:27:56.000' AS DateTime), N'10.20.2.35', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 33, N'dd016', N'POST', N'/api/attempts/16/warnings', 200, N'Khoa Ngoại tổng hợp')
INSERT [dbo].[LOGTHAOTAC] ([Id], [IdBaiThi], [LoaiThaoTac], [ChiTiet], [ThoiGian], [DiaChi_IP], [UserAgent], [IdTaiKhoan], [TenDangNhap], [PhuongThuc], [DuongDan], [MaHttp], [KhoaPhong]) VALUES (146, 16, N'SUBMIT_EXAM', N'Nộp bài đúng hạn', CAST(N'2026-06-30T08:31:38.000' AS DateTime), N'10.20.2.35', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 33, N'dd016', N'POST', N'/api/attempts/16/submit', 200, N'Khoa Ngoại tổng hợp')
SET IDENTITY_INSERT [dbo].[LOGTHAOTAC] OFF
GO
SET IDENTITY_INSERT [dbo].[LUACHON] ON 

INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (1, 1, N'Hỏi người nhà xác nhận tên người bệnh', 0, 1)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (2, 1, N'Đối chiếu ít nhất hai thông tin định danh trên vòng tay/hồ sơ', 1, 2)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (3, 1, N'Gọi tên người bệnh một lần trước khi thực hiện', 0, 3)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (4, 1, N'Dựa vào số giường đang điều trị', 0, 4)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (5, 2, N'Giúp thông tin ngắn gọn, có trình tự và hạn chế bỏ sót dữ kiện quan trọng', 1, 1)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (6, 2, N'Rút ngắn hoàn toàn thời gian bàn giao giữa hai ca trực', 0, 2)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (7, 2, N'Thay thế toàn bộ ghi chép hồ sơ chăm sóc', 0, 3)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (8, 2, N'Chỉ dùng để báo cáo sự cố y khoa nghiêm trọng', 0, 4)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (9, 3, N'Tên thuốc, liều dùng, người bệnh, đường dùng, thời điểm dùng và hạn dùng', 1, 1)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (10, 3, N'Chỉ cần kiểm tra tên thuốc trên vỉ thuốc', 0, 2)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (11, 3, N'Chỉ cần hỏi người bệnh đã ăn chưa', 0, 3)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (12, 3, N'Chỉ cần đối chiếu màu sắc viên thuốc', 0, 4)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (13, 4, N'Tự ước lượng liều theo kinh nghiệm', 0, 1)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (14, 4, N'Tạm ngưng thực hiện và liên hệ bác sĩ để xác nhận lại y lệnh', 1, 2)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (15, 4, N'Nhờ đồng nghiệp ký xác nhận rồi thực hiện', 0, 3)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (16, 4, N'Ghi chú vào cuối ca và thực hiện như bình thường', 0, 4)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (17, 5, N'Trẻ chơi bình thường và nói chuyện rõ', 0, 1)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (18, 5, N'Trẻ tím tái, thở rút lõm ngực hoặc lơ mơ', 1, 2)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (19, 5, N'Trẻ ho nhẹ nhưng bú tốt', 0, 3)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (20, 5, N'Trẻ sốt nhẹ dưới 38°C', 0, 4)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (21, 6, N'Gọi hỗ trợ, kích hoạt cấp cứu và bắt đầu hồi sức tim phổi theo quy trình', 1, 1)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (22, 6, N'Đi tìm hồ sơ bệnh án trước', 0, 2)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (23, 6, N'Chờ bác sĩ đến mới bắt đầu can thiệp', 0, 3)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (24, 6, N'Cho trẻ uống nước để đánh giá đáp ứng', 0, 4)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (25, 7, N'60–80 lần/phút', 0, 1)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (26, 7, N'80–90 lần/phút', 0, 2)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (27, 7, N'100–120 lần/phút', 1, 3)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (28, 7, N'Trên 160 lần/phút', 0, 4)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (29, 8, N'Cho trẻ tiếp tục nằm theo dõi thêm 30 phút', 0, 1)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (30, 8, N'Ngừng tác nhân nghi ngờ, báo động cấp cứu và hỗ trợ xử trí theo phác đồ', 1, 2)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (31, 8, N'Tự ý cho trẻ xuất viện nếu đã bớt ngứa', 0, 3)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (32, 8, N'Chỉ ghi nhận vào hồ sơ, không cần báo bác sĩ', 0, 4)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (33, 9, N'Giảm nguy cơ hít sặc và viêm phổi liên quan thở máy', 1, 1)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (34, 9, N'Làm tăng áp lực nội sọ ở mọi trường hợp', 0, 2)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (35, 9, N'Thay thế hoàn toàn vệ sinh răng miệng', 0, 3)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (36, 9, N'Giúp máy thở tự điều chỉnh thuốc an thần', 0, 4)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (37, 10, N'Bỏ qua sát khuẩn đầu nối nếu đang vội', 0, 1)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (38, 10, N'Giữ vô khuẩn, sát khuẩn đầu nối và theo dõi dấu hiệu bất thường', 1, 2)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (39, 10, N'Dùng chung bơm tiêm cho nhiều đường truyền nếu cùng người bệnh', 0, 3)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (40, 10, N'Chỉ rửa đường truyền khi đã tắc hoàn toàn', 0, 4)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (41, 11, N'Huyết áp ổn định hơn sau điều chỉnh liều', 0, 1)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (42, 11, N'Đầu chi lạnh, tím tái hoặc vị trí truyền sưng đau', 1, 2)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (43, 11, N'Người bệnh ngủ được sau chăm sóc', 0, 3)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (44, 11, N'Mạch và huyết áp được ghi nhận đúng giờ', 0, 4)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (45, 12, N'Hút càng lâu càng tốt để sạch hoàn toàn', 0, 1)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (46, 12, N'Theo dõi SpO2, thực hiện vô khuẩn và giới hạn thời gian hút mỗi lần', 1, 2)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (47, 12, N'Không cần giải thích nếu người bệnh còn tỉnh', 0, 3)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (48, 12, N'Luôn hút đờm định kỳ mỗi 5 phút', 0, 4)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (49, 13, N'Hạ thân nhiệt và tăng tiêu hao năng lượng', 1, 1)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (50, 13, N'Tăng cân quá nhanh', 0, 2)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (51, 13, N'Giảm nhu cầu theo dõi dấu hiệu sinh tồn', 0, 3)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (52, 13, N'Làm trẻ ngủ sâu liên tục', 0, 4)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (53, 14, N'Thở rên, phập phồng cánh mũi hoặc rút lõm lồng ngực', 1, 1)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (54, 14, N'Da hồng, bú tốt và ngủ yên', 0, 2)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (55, 14, N'Trẻ hắt hơi một vài lần', 0, 3)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (56, 14, N'Trẻ đi tiêu phân su', 0, 4)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (57, 15, N'Vị trí sonde, lượng dịch tồn lưu theo quy định và tình trạng bụng trẻ', 1, 1)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (58, 15, N'Chỉ cần kiểm tra nhiệt độ phòng', 0, 2)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (59, 15, N'Chỉ cần hỏi người nhà đã pha sữa chưa', 0, 3)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (60, 15, N'Không cần kiểm tra nếu sonde mới đặt trong ngày', 0, 4)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (61, 16, N'Mở cửa lồng ấp cho thoáng khí rồi đi gọi người nhà', 0, 1)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (62, 16, N'Đánh giá nhanh hô hấp tuần hoàn, báo hỗ trợ và xử trí theo quy trình cấp cứu sơ sinh', 1, 2)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (63, 16, N'Chờ đến giờ thăm khám tiếp theo', 0, 3)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (64, 16, N'Ghi nhận vào bảng theo dõi rồi tiếp tục công việc khác', 0, 4)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (65, 17, N'Suy hô hấp hoặc tăng công thở', 1, 1)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (66, 17, N'Trẻ đang ngủ sâu bình thường', 0, 2)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (67, 17, N'Trẻ ăn quá no sau bữa bú', 0, 3)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (68, 17, N'Tác dụng phụ thường gặp của đo nhiệt độ', 0, 4)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (69, 18, N'SpO2 và tình trạng hô hấp', 1, 1)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (70, 18, N'Màu áo của người nhà', 0, 2)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (71, 18, N'Số lượng đồ chơi của trẻ', 0, 3)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (72, 18, N'Số lần thay khăn trải giường', 0, 4)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (73, 19, N'Nhịp thở, co kéo, SpO2 và khả năng đáp ứng thuốc', 1, 1)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (74, 19, N'Chỉ đánh giá trẻ có buồn ngủ không', 0, 2)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (75, 19, N'Chỉ ghi giờ thực hiện, không cần theo dõi', 0, 3)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (76, 19, N'Đợi đến cuối ngày mới đánh giá', 0, 4)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (77, 20, N'Đánh giá đường thở, gọi hỗ trợ và chuẩn bị dụng cụ cấp cứu', 1, 1)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (78, 20, N'Cho trẻ ăn để tăng sức', 0, 2)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (79, 20, N'Tắt chuông báo monitor để tránh ồn', 0, 3)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (80, 20, N'Đưa người nhà ra ngoài rồi chờ bác sĩ', 0, 4)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (81, 21, N'Trước khi tiếp xúc người bệnh và sau khi tiếp xúc dịch tiết có nguy cơ', 1, 1)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (82, 21, N'Chỉ cần rửa tay đầu ca trực', 0, 2)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (83, 21, N'Chỉ rửa tay khi nhìn thấy bẩn rõ', 0, 3)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (84, 21, N'Không cần vệ sinh tay nếu đã mang găng', 0, 4)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (85, 22, N'Khẩu trang y tế đúng cách và phương tiện phòng hộ theo nguy cơ tiếp xúc', 1, 1)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (86, 22, N'Chỉ cần găng tay, không cần khẩu trang', 0, 2)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (87, 22, N'Chỉ cần áo choàng khi ra khỏi buồng bệnh', 0, 3)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (88, 22, N'Không cần phòng hộ nếu thời gian tiếp xúc ngắn', 0, 4)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (89, 23, N'Bỏ vào hộp an toàn chuyên dụng, không đậy lại kim bằng hai tay', 1, 1)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (90, 23, N'Bỏ chung vào túi rác sinh hoạt', 0, 2)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (91, 23, N'Để trên xe tiêm đến cuối ca mới xử lý', 0, 3)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (92, 23, N'Bẻ cong kim trước khi bỏ vào thùng', 0, 4)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (93, 24, N'Xử trí vết thương ban đầu, báo cáo phơi nhiễm và thực hiện quy trình theo dõi', 1, 1)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (94, 24, N'Che lại bằng băng cá nhân và tiếp tục làm việc, không cần báo', 0, 2)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (95, 24, N'Tự mua thuốc uống nếu lo lắng', 0, 3)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (96, 24, N'Chỉ báo cáo khi có triệu chứng sốt', 0, 4)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (97, 25, N'Vết mổ khô, người bệnh tỉnh và sinh hiệu ổn', 0, 1)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (98, 25, N'Chảy máu vết mổ nhiều, đau tăng hoặc sinh hiệu bất thường', 1, 2)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (99, 25, N'Người bệnh ngủ sau dùng thuốc giảm đau', 0, 3)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (100, 25, N'Dịch dẫn lưu giảm dần theo thời gian', 0, 4)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (101, 26, N'Số lượng, màu sắc, tính chất dịch và tình trạng chân dẫn lưu', 1, 1)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (102, 26, N'Chỉ ghi nhận khi bình dẫn lưu đầy', 0, 2)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (103, 26, N'Chỉ quan tâm đến tên phẫu thuật', 0, 3)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (104, 26, N'Không cần theo dõi nếu người bệnh không đau', 0, 4)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (105, 27, N'Hồ sơ, định danh, vị trí phẫu thuật, nhịn ăn và các chuẩn bị theo bảng kiểm', 1, 1)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (106, 27, N'Chỉ cần hỏi người nhà đã ký giấy chưa', 0, 2)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (107, 27, N'Chỉ cần kiểm tra giường bệnh', 0, 3)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (108, 27, N'Không cần kiểm tra nếu đã có lịch mổ', 0, 4)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (109, 28, N'Vô khuẩn, quan sát vết thương và ghi nhận bất thường', 1, 1)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (110, 28, N'Dùng lại gạc sạch nếu chưa bẩn nhiều', 0, 2)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (111, 28, N'Mở băng càng lâu càng tốt để vết thương khô', 0, 3)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (112, 28, N'Không cần rửa tay nếu đã mang găng', 0, 4)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (113, 29, N'Tím môi, khó thở hoặc SpO2 giảm so với nền', 1, 1)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (114, 29, N'Trẻ cười nói bình thường', 0, 2)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (115, 29, N'Trẻ ngủ sau ăn', 0, 3)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (116, 29, N'Trẻ tăng cân đúng kế hoạch', 0, 4)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (117, 30, N'Lượng nước tiểu, cân nặng, điện giải theo chỉ định và dấu hiệu mất nước', 1, 1)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (118, 30, N'Chỉ theo dõi màu sắc viên thuốc', 0, 2)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (119, 30, N'Không cần theo dõi nếu trẻ đi tiểu nhiều', 0, 3)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (120, 30, N'Chỉ ghi nhận khi người nhà hỏi', 0, 4)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (121, 31, N'Mắt trũng, khát nước, tiểu ít và dấu hiệu lừ đừ', 1, 1)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (122, 31, N'Trẻ ngủ đúng giờ', 0, 2)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (123, 31, N'Trẻ thích chơi đồ chơi mới', 0, 3)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (124, 31, N'Da hồng và bú tốt', 0, 4)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (125, 32, N'Cho uống từng ngụm nhỏ, theo dõi nôn ói và số lần tiêu', 1, 1)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (126, 32, N'Cho uống thật nhanh để đủ lượng', 0, 2)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (127, 32, N'Ngưng uống hoàn toàn nếu còn tiêu chảy', 0, 3)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (128, 32, N'Chỉ uống nước ngọt thay dung dịch bù nước', 0, 4)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (129, 33, N'Tri giác, đồng tử, dấu hiệu sinh tồn và vận động chi', 1, 1)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (130, 33, N'Chỉ theo dõi nhiệt độ phòng', 0, 2)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (131, 33, N'Chỉ hỏi trẻ có đói không', 0, 3)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (132, 33, N'Không cần đánh giá nếu trẻ đang ngủ', 0, 4)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (133, 34, N'Đảm bảo an toàn, đặt người bệnh nghiêng nếu phù hợp, không cố nhét vật vào miệng', 1, 1)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (134, 34, N'Giữ chặt tay chân bằng mọi cách', 0, 2)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (135, 34, N'Cho uống nước ngay trong cơn co giật', 0, 3)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (136, 34, N'Rời khỏi phòng để gọi người nhà trước', 0, 4)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (137, 35, N'Phòng ngừa nhiễm khuẩn, theo dõi sốt và hướng dẫn hạn chế nguồn lây', 1, 1)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (138, 35, N'Khuyến khích tiếp xúc đông người để trẻ vui hơn', 0, 2)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (139, 35, N'Không cần đeo khẩu trang khi làm thủ thuật', 0, 3)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (140, 35, N'Chỉ theo dõi khi trẻ than đau', 0, 4)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (141, 36, N'Đối chiếu thông tin người bệnh, chế phẩm máu, nhóm máu và theo dõi phản ứng truyền', 1, 1)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (142, 36, N'Chỉ kiểm tra màu sắc túi máu', 0, 2)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (143, 36, N'Treo máu trước rồi kiểm tra sau', 0, 3)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (144, 36, N'Không cần đo sinh hiệu ban đầu', 0, 4)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (145, 37, N'Mức độ tỉnh táo, đáp ứng lời gọi/đau và thay đổi so với trước', 1, 1)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (146, 37, N'Chỉ ghi nhận trẻ đang ngủ', 0, 2)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (147, 37, N'Chỉ hỏi người nhà trẻ có ngoan không', 0, 3)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (148, 37, N'Không cần đánh giá nếu chưa có y lệnh', 0, 4)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (149, 38, N'Nâng thanh chắn giường, đặt chuông gọi gần người bệnh và hướng dẫn người chăm sóc', 1, 1)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (150, 38, N'Để trẻ tự đi lại để nhanh hồi phục', 0, 2)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (151, 38, N'Không cần đánh giá nếu trẻ còn nhỏ', 0, 3)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (152, 38, N'Tắt đèn hoàn toàn để trẻ dễ ngủ', 0, 4)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (153, 39, N'Tiền sử dị ứng, y lệnh, đường truyền và hướng dẫn chuẩn bị theo quy định', 1, 1)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (154, 39, N'Chỉ cần hỏi trẻ có sợ không', 0, 2)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (155, 39, N'Không cần kiểm tra nếu đã có lịch hẹn', 0, 3)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (156, 39, N'Chỉ cần mang theo giấy ra viện', 0, 4)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (157, 40, N'Tri giác, hô hấp, SpO2 và dấu hiệu bất thường cho đến khi ổn định', 1, 1)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (158, 40, N'Chỉ theo dõi khi trẻ khóc', 0, 2)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (159, 40, N'Cho trẻ ăn uống ngay không cần đánh giá', 0, 3)
INSERT [dbo].[LUACHON] ([Id], [IdCauHoi], [NoiDung], [LaDapAnDung], [ThuTu]) VALUES (160, 40, N'Không cần ghi nhận nếu người nhà nói trẻ bình thường', 0, 4)
SET IDENTITY_INSERT [dbo].[LUACHON] OFF
GO
SET IDENTITY_INSERT [dbo].[PHIEN_NGUOIDUNG] ON 

INSERT [dbo].[PHIEN_NGUOIDUNG] ([Id], [SessionId], [UserId], [CreatedAt], [ExpiresAt], [LastActivityAt], [IpAddress], [UserAgent], [IsActive], [EndReason]) VALUES (1, N'SES-20260630-001-0001', 1, CAST(N'2026-06-30T07:39:00.000000' AS DateTime2), CAST(N'2026-06-30T15:39:00.000000' AS DateTime2), CAST(N'2026-06-30T10:35:00.000000' AS DateTime2), N'10.20.1.18', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 1, NULL)
INSERT [dbo].[PHIEN_NGUOIDUNG] ([Id], [SessionId], [UserId], [CreatedAt], [ExpiresAt], [LastActivityAt], [IpAddress], [UserAgent], [IsActive], [EndReason]) VALUES (2, N'SES-20260630-002-0002', 2, CAST(N'2026-06-30T07:48:00.000000' AS DateTime2), CAST(N'2026-06-30T15:48:00.000000' AS DateTime2), CAST(N'2026-06-30T09:45:00.000000' AS DateTime2), N'10.20.5.22', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 1, NULL)
INSERT [dbo].[PHIEN_NGUOIDUNG] ([Id], [SessionId], [UserId], [CreatedAt], [ExpiresAt], [LastActivityAt], [IpAddress], [UserAgent], [IsActive], [EndReason]) VALUES (3, N'SES-20260630-003-0003', 3, CAST(N'2026-06-30T07:57:00.000000' AS DateTime2), CAST(N'2026-06-30T15:57:00.000000' AS DateTime2), CAST(N'2026-06-30T09:28:00.000000' AS DateTime2), N'10.20.5.22', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 1, NULL)
INSERT [dbo].[PHIEN_NGUOIDUNG] ([Id], [SessionId], [UserId], [CreatedAt], [ExpiresAt], [LastActivityAt], [IpAddress], [UserAgent], [IsActive], [EndReason]) VALUES (4, N'SES-20260630-004-0004', 4, CAST(N'2026-06-30T08:06:00.000000' AS DateTime2), CAST(N'2026-06-30T16:06:00.000000' AS DateTime2), CAST(N'2026-06-30T08:55:00.000000' AS DateTime2), N'10.20.5.06', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 0, N'UserLogout')
INSERT [dbo].[PHIEN_NGUOIDUNG] ([Id], [SessionId], [UserId], [CreatedAt], [ExpiresAt], [LastActivityAt], [IpAddress], [UserAgent], [IsActive], [EndReason]) VALUES (5, N'SES-20260630-005-0005', 5, CAST(N'2026-06-30T08:15:00.000000' AS DateTime2), CAST(N'2026-06-30T16:15:00.000000' AS DateTime2), CAST(N'2026-06-30T09:07:00.000000' AS DateTime2), N'10.20.4.19', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 1, NULL)
INSERT [dbo].[PHIEN_NGUOIDUNG] ([Id], [SessionId], [UserId], [CreatedAt], [ExpiresAt], [LastActivityAt], [IpAddress], [UserAgent], [IsActive], [EndReason]) VALUES (6, N'SES-20260630-008-0006', 8, CAST(N'2026-06-30T08:24:00.000000' AS DateTime2), CAST(N'2026-06-30T16:24:00.000000' AS DateTime2), CAST(N'2026-06-30T10:44:00.000000' AS DateTime2), N'10.20.3.14', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 1, NULL)
INSERT [dbo].[PHIEN_NGUOIDUNG] ([Id], [SessionId], [UserId], [CreatedAt], [ExpiresAt], [LastActivityAt], [IpAddress], [UserAgent], [IsActive], [EndReason]) VALUES (7, N'SES-20260630-018-0007', 18, CAST(N'2026-06-30T08:33:00.000000' AS DateTime2), CAST(N'2026-06-30T16:33:00.000000' AS DateTime2), CAST(N'2026-06-30T11:39:00.000000' AS DateTime2), N'10.20.1.12', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 1, NULL)
INSERT [dbo].[PHIEN_NGUOIDUNG] ([Id], [SessionId], [UserId], [CreatedAt], [ExpiresAt], [LastActivityAt], [IpAddress], [UserAgent], [IsActive], [EndReason]) VALUES (8, N'SES-20260630-019-0008', 19, CAST(N'2026-06-30T08:42:00.000000' AS DateTime2), CAST(N'2026-06-30T16:42:00.000000' AS DateTime2), CAST(N'2026-06-30T10:23:00.000000' AS DateTime2), N'10.20.5.06', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 0, N'UserLogout')
INSERT [dbo].[PHIEN_NGUOIDUNG] ([Id], [SessionId], [UserId], [CreatedAt], [ExpiresAt], [LastActivityAt], [IpAddress], [UserAgent], [IsActive], [EndReason]) VALUES (9, N'SES-20260630-020-0009', 20, CAST(N'2026-06-30T08:51:00.000000' AS DateTime2), CAST(N'2026-06-30T16:51:00.000000' AS DateTime2), CAST(N'2026-06-30T09:14:00.000000' AS DateTime2), N'10.20.1.18', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 1, NULL)
INSERT [dbo].[PHIEN_NGUOIDUNG] ([Id], [SessionId], [UserId], [CreatedAt], [ExpiresAt], [LastActivityAt], [IpAddress], [UserAgent], [IsActive], [EndReason]) VALUES (10, N'SES-20260630-021-0010', 21, CAST(N'2026-06-30T09:00:00.000000' AS DateTime2), CAST(N'2026-06-30T17:00:00.000000' AS DateTime2), CAST(N'2026-06-30T10:29:00.000000' AS DateTime2), N'10.20.1.18', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 1, NULL)
INSERT [dbo].[PHIEN_NGUOIDUNG] ([Id], [SessionId], [UserId], [CreatedAt], [ExpiresAt], [LastActivityAt], [IpAddress], [UserAgent], [IsActive], [EndReason]) VALUES (11, N'SES-20260630-022-0011', 22, CAST(N'2026-06-30T09:09:00.000000' AS DateTime2), CAST(N'2026-06-30T17:09:00.000000' AS DateTime2), CAST(N'2026-06-30T10:51:00.000000' AS DateTime2), N'10.20.1.12', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36', 1, NULL)
INSERT [dbo].[PHIEN_NGUOIDUNG] ([Id], [SessionId], [UserId], [CreatedAt], [ExpiresAt], [LastActivityAt], [IpAddress], [UserAgent], [IsActive], [EndReason]) VALUES (12, N'SES-20260630-023-0012', 23, CAST(N'2026-06-30T09:18:00.000000' AS DateTime2), CAST(N'2026-06-30T17:18:00.000000' AS DateTime2), CAST(N'2026-06-30T12:06:00.000000' AS DateTime2), N'10.20.2.35', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 0, N'UserLogout')
INSERT [dbo].[PHIEN_NGUOIDUNG] ([Id], [SessionId], [UserId], [CreatedAt], [ExpiresAt], [LastActivityAt], [IpAddress], [UserAgent], [IsActive], [EndReason]) VALUES (13, N'SES-20260630-024-0013', 24, CAST(N'2026-06-30T09:27:00.000000' AS DateTime2), CAST(N'2026-06-30T17:27:00.000000' AS DateTime2), CAST(N'2026-06-30T11:16:00.000000' AS DateTime2), N'10.20.3.14', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 1, NULL)
INSERT [dbo].[PHIEN_NGUOIDUNG] ([Id], [SessionId], [UserId], [CreatedAt], [ExpiresAt], [LastActivityAt], [IpAddress], [UserAgent], [IsActive], [EndReason]) VALUES (14, N'SES-20260630-030-0014', 30, CAST(N'2026-06-30T09:36:00.000000' AS DateTime2), CAST(N'2026-06-30T17:36:00.000000' AS DateTime2), CAST(N'2026-06-30T10:37:00.000000' AS DateTime2), N'10.20.5.22', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 1, NULL)
INSERT [dbo].[PHIEN_NGUOIDUNG] ([Id], [SessionId], [UserId], [CreatedAt], [ExpiresAt], [LastActivityAt], [IpAddress], [UserAgent], [IsActive], [EndReason]) VALUES (15, N'SES-20260630-031-0015', 31, CAST(N'2026-06-30T09:45:00.000000' AS DateTime2), CAST(N'2026-06-30T17:45:00.000000' AS DateTime2), CAST(N'2026-06-30T10:56:00.000000' AS DateTime2), N'10.20.1.12', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 1, NULL)
INSERT [dbo].[PHIEN_NGUOIDUNG] ([Id], [SessionId], [UserId], [CreatedAt], [ExpiresAt], [LastActivityAt], [IpAddress], [UserAgent], [IsActive], [EndReason]) VALUES (16, N'SES-20260630-032-0016', 32, CAST(N'2026-06-30T09:54:00.000000' AS DateTime2), CAST(N'2026-06-30T17:54:00.000000' AS DateTime2), CAST(N'2026-06-30T10:41:00.000000' AS DateTime2), N'10.20.1.12', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 0, N'UserLogout')
INSERT [dbo].[PHIEN_NGUOIDUNG] ([Id], [SessionId], [UserId], [CreatedAt], [ExpiresAt], [LastActivityAt], [IpAddress], [UserAgent], [IsActive], [EndReason]) VALUES (17, N'SES-20260630-033-0017', 33, CAST(N'2026-06-30T10:03:00.000000' AS DateTime2), CAST(N'2026-06-30T18:03:00.000000' AS DateTime2), CAST(N'2026-06-30T11:28:00.000000' AS DateTime2), N'10.20.5.22', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36', 1, NULL)
SET IDENTITY_INSERT [dbo].[PHIEN_NGUOIDUNG] OFF
GO
SET IDENTITY_INSERT [dbo].[TAIKHOAN] ON 

INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (1, N'QT0001', N'admin', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Quản trị hệ thống', NULL, N'Quản trị viên hệ thống', 1, 1, CAST(N'2026-05-20T07:45:00.0000000' AS DateTime2), NULL, CAST(N'2026-06-30T17:24:18.1150000' AS DateTime2), NULL, N'admin@nd2.example.vn', NULL)
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (2, N'QL001', N'ql001', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Phó giám đốc phụ trách chuyên môn', N'Ban Giám đốc', N'Nguyễn Minh Hạnh', 5, 1, CAST(N'2026-05-20T08:06:00.000000' AS DateTime2), NULL, CAST(N'2026-06-30T08:56:00.000000' AS DateTime2), 1, N'ql001@nd2.example.vn', N'0920027458')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (3, N'QL002', N'ql002', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Trưởng phòng Điều dưỡng', N'Phòng Điều dưỡng', N'Trần Quốc Bảo', 5, 1, CAST(N'2026-05-20T08:09:00.000000' AS DateTime2), NULL, CAST(N'2026-06-30T09:19:00.000000' AS DateTime2), 2, N'ql002@nd2.example.vn', N'0920041187')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (4, N'QL003', N'ql003', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Điều dưỡng trưởng', N'Khoa Cấp cứu', N'Phạm Thị Thanh Vân', 5, 1, CAST(N'2026-05-20T08:12:00.000000' AS DateTime2), NULL, CAST(N'2026-06-30T09:42:00.000000' AS DateTime2), 3, N'ql003@nd2.example.vn', N'0920054916')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (5, N'QL004', N'ql004', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Điều dưỡng trưởng', N'Khoa Hồi sức tích cực - Chống độc', N'Lê Hoàng Dũng', 5, 1, CAST(N'2026-05-20T08:15:00.000000' AS DateTime2), NULL, CAST(N'2026-06-30T10:05:00.000000' AS DateTime2), 4, N'ql004@nd2.example.vn', N'0920068645')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (6, N'QL005', N'ql005', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Điều dưỡng trưởng', N'Khoa Sơ sinh', N'Võ Thị Mỹ Linh', 5, 1, CAST(N'2026-05-20T08:18:00.000000' AS DateTime2), NULL, CAST(N'2026-06-30T10:28:00.000000' AS DateTime2), 5, N'ql005@nd2.example.vn', N'0920082374')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (7, N'QL006', N'ql006', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Điều dưỡng trưởng', N'Khoa Hô hấp', N'Đặng Gia Huy', 5, 1, CAST(N'2026-05-20T08:21:00.000000' AS DateTime2), NULL, CAST(N'2026-06-30T10:51:00.000000' AS DateTime2), 6, N'ql006@nd2.example.vn', N'0920096103')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (8, N'QL007', N'ql007', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Điều dưỡng trưởng', N'Khoa Nhiễm', N'Bùi Thị Thu Hà', 5, 1, CAST(N'2026-05-20T08:24:00.000000' AS DateTime2), NULL, CAST(N'2026-06-30T11:14:00.000000' AS DateTime2), 7, N'ql007@nd2.example.vn', N'0920109832')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (9, N'QL008', N'ql008', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Điều dưỡng trưởng', N'Khoa Ngoại tổng hợp', N'Huỳnh Minh Quân', 5, 1, CAST(N'2026-05-20T08:27:00.000000' AS DateTime2), NULL, CAST(N'2026-06-30T11:37:00.000000' AS DateTime2), 8, N'ql008@nd2.example.vn', N'0920123561')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (10, N'QL009', N'ql009', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Điều dưỡng trưởng', N'Khoa Tim mạch', N'Nguyễn Thị Mai Anh', 5, 1, CAST(N'2026-05-20T08:30:00.000000' AS DateTime2), NULL, CAST(N'2026-06-30T12:00:00.000000' AS DateTime2), 9, N'ql009@nd2.example.vn', N'0920137290')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (11, N'QL010', N'ql010', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Điều dưỡng trưởng', N'Khoa Tiêu hóa', N'Trần Thanh Phúc', 5, 1, CAST(N'2026-05-20T08:33:00.000000' AS DateTime2), NULL, CAST(N'2026-06-30T12:23:00.000000' AS DateTime2), 10, N'ql010@nd2.example.vn', N'0920151019')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (12, N'QL011', N'ql011', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Điều dưỡng trưởng', N'Khoa Ngoại thần kinh', N'Phan Ngọc Thảo', 5, 1, CAST(N'2026-05-20T08:36:00.000000' AS DateTime2), NULL, CAST(N'2026-06-30T12:46:00.000000' AS DateTime2), 11, N'ql011@nd2.example.vn', N'0920164748')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (13, N'QL012', N'ql012', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Điều dưỡng trưởng', N'Khoa Ung bướu huyết học', N'Lâm Đức Kiên', 5, 1, CAST(N'2026-05-20T08:39:00.000000' AS DateTime2), NULL, CAST(N'2026-06-30T13:09:00.000000' AS DateTime2), 12, N'ql012@nd2.example.vn', N'0920178477')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (14, N'QL013', N'ql013', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Điều dưỡng trưởng', N'Khoa Thần kinh', N'Đỗ Thị Kim Ngân', 5, 1, CAST(N'2026-05-20T08:42:00.000000' AS DateTime2), NULL, CAST(N'2026-06-30T13:32:00.000000' AS DateTime2), 13, N'ql013@nd2.example.vn', N'0920192206')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (15, N'QL014', N'ql014', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Điều dưỡng trưởng', N'Khoa Chẩn đoán hình ảnh', N'Mai Hoàng Nam', 5, 1, CAST(N'2026-05-20T08:45:00.000000' AS DateTime2), NULL, CAST(N'2026-06-30T13:55:00.000000' AS DateTime2), 14, N'ql014@nd2.example.vn', N'0920205935')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (16, N'QL015', N'ql015', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Trưởng phòng Công nghệ thông tin', N'Phòng Công nghệ thông tin', N'Cao Minh Tâm', 5, 1, CAST(N'2026-05-20T08:48:00.000000' AS DateTime2), NULL, CAST(N'2026-06-30T14:18:00.000000' AS DateTime2), 15, N'ql015@nd2.example.vn', N'0920219664')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (17, N'QL016', N'ql016', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Chuyên viên tổ chức cán bộ', N'Phòng Tổ chức cán bộ', N'Hoàng Thị Bích Ngọc', 5, 1, CAST(N'2026-05-20T08:51:00.000000' AS DateTime2), NULL, CAST(N'2026-06-30T14:41:00.000000' AS DateTime2), 16, N'ql016@nd2.example.vn', N'0920233393')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (18, N'ĐD001', N'dd001', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Điều dưỡng', N'Khoa Cấp cứu', N'Nguyễn Thị Lan', 3, 1, CAST(N'2026-05-25T08:00:00.000000' AS DateTime2), NULL, CAST(N'2026-06-28T07:50:00.000000' AS DateTime2), 3, N'dd001@nd2.example.vn', N'0920247122')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (19, N'ĐD002', N'dd002', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Điều dưỡng', N'Khoa Cấp cứu', N'Trần Văn Minh', 3, 1, CAST(N'2026-05-25T08:11:00.000000' AS DateTime2), NULL, CAST(N'2026-06-28T08:03:00.000000' AS DateTime2), 3, N'dd002@nd2.example.vn', N'0920260851')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (20, N'ĐD003', N'dd003', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Điều dưỡng', N'Khoa Cấp cứu', N'Lê Thị Thu Trang', 3, 1, CAST(N'2026-05-25T08:22:00.000000' AS DateTime2), NULL, NULL, 3, N'dd003@nd2.example.vn', N'0920274580')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (21, N'ĐD004', N'dd004', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Điều dưỡng', N'Khoa Hồi sức tích cực - Chống độc', N'Phạm Quốc Huy', 3, 1, CAST(N'2026-05-25T08:33:00.000000' AS DateTime2), NULL, CAST(N'2026-06-28T08:29:00.000000' AS DateTime2), 4, N'dd004@nd2.example.vn', N'0920288309')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (22, N'ĐD005', N'dd005', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Điều dưỡng', N'Khoa Hồi sức tích cực - Chống độc', N'Vũ Thị Thanh Tâm', 3, 1, CAST(N'2026-05-25T08:44:00.000000' AS DateTime2), NULL, CAST(N'2026-06-28T08:42:00.000000' AS DateTime2), 4, N'dd005@nd2.example.vn', N'0920302038')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (23, N'ĐD006', N'dd006', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Điều dưỡng', N'Khoa Hồi sức tích cực - Chống độc', N'Ngô Minh Khôi', 3, 1, CAST(N'2026-05-25T08:55:00.000000' AS DateTime2), NULL, CAST(N'2026-06-28T08:55:00.000000' AS DateTime2), 4, N'dd006@nd2.example.vn', N'0920315767')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (24, N'ĐD007', N'dd007', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Điều dưỡng', N'Khoa Sơ sinh', N'Đỗ Thị Hồng Nhung', 3, 1, CAST(N'2026-05-25T09:06:00.000000' AS DateTime2), NULL, CAST(N'2026-06-28T09:08:00.000000' AS DateTime2), 5, N'dd007@nd2.example.vn', N'0920329496')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (25, N'ĐD008', N'dd008', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Điều dưỡng', N'Khoa Sơ sinh', N'Huỳnh Gia Bảo', 3, 1, CAST(N'2026-05-25T09:17:00.000000' AS DateTime2), NULL, NULL, 5, N'dd008@nd2.example.vn', N'0920343225')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (26, N'ĐD009', N'dd009', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Điều dưỡng', N'Khoa Sơ sinh', N'Cao Thị Kim Chi', 3, 1, CAST(N'2026-05-25T09:28:00.000000' AS DateTime2), NULL, CAST(N'2026-06-28T09:34:00.000000' AS DateTime2), 5, N'dd009@nd2.example.vn', N'0920356954')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (27, N'ĐD010', N'dd010', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Điều dưỡng', N'Khoa Hô hấp', N'Nguyễn Hoàng Phúc', 3, 1, CAST(N'2026-05-25T09:39:00.000000' AS DateTime2), NULL, CAST(N'2026-06-28T09:47:00.000000' AS DateTime2), 6, N'dd010@nd2.example.vn', N'0920370683')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (28, N'ĐD011', N'dd011', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Điều dưỡng', N'Khoa Hô hấp', N'Trương Thị Ngọc Ánh', 3, 1, CAST(N'2026-05-25T09:50:00.000000' AS DateTime2), NULL, CAST(N'2026-06-28T10:00:00.000000' AS DateTime2), 6, N'dd011@nd2.example.vn', N'0920384412')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (29, N'ĐD012', N'dd012', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Điều dưỡng', N'Khoa Hô hấp', N'Lê Minh Nhật', 3, 1, CAST(N'2026-05-25T10:01:00.000000' AS DateTime2), NULL, CAST(N'2026-06-28T10:13:00.000000' AS DateTime2), 6, N'dd012@nd2.example.vn', N'0920398141')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (30, N'ĐD013', N'dd013', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Điều dưỡng', N'Khoa Nhiễm', N'Phạm Thị Mỹ Duyên', 3, 1, CAST(N'2026-05-25T10:12:00.000000' AS DateTime2), NULL, NULL, 7, N'dd013@nd2.example.vn', N'0920411870')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (31, N'ĐD014', N'dd014', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Điều dưỡng', N'Khoa Nhiễm', N'Bùi Quốc Thắng', 3, 1, CAST(N'2026-05-25T10:23:00.000000' AS DateTime2), NULL, CAST(N'2026-06-28T10:39:00.000000' AS DateTime2), 7, N'dd014@nd2.example.vn', N'0920425599')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (32, N'ĐD015', N'dd015', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Điều dưỡng', N'Khoa Nhiễm', N'Đặng Thị Ngọc Hân', 3, 1, CAST(N'2026-05-25T10:34:00.000000' AS DateTime2), NULL, CAST(N'2026-06-28T10:52:00.000000' AS DateTime2), 7, N'dd015@nd2.example.vn', N'0920439328')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (33, N'ĐD016', N'dd016', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Điều dưỡng', N'Khoa Ngoại tổng hợp', N'Nguyễn Văn Tài', 3, 1, CAST(N'2026-05-25T10:45:00.000000' AS DateTime2), NULL, CAST(N'2026-06-28T11:05:00.000000' AS DateTime2), 8, N'dd016@nd2.example.vn', N'0920453057')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (34, N'ĐD017', N'dd017', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Điều dưỡng', N'Khoa Ngoại tổng hợp', N'Trần Thị Yến Nhi', 3, 1, CAST(N'2026-05-25T10:56:00.000000' AS DateTime2), NULL, CAST(N'2026-06-28T11:18:00.000000' AS DateTime2), 8, N'dd017@nd2.example.vn', N'0920466786')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (35, N'ĐD018', N'dd018', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Điều dưỡng', N'Khoa Ngoại tổng hợp', N'Mai Thanh Bình', 3, 1, CAST(N'2026-05-25T11:07:00.000000' AS DateTime2), NULL, NULL, 8, N'dd018@nd2.example.vn', N'0920480515')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (36, N'ĐD019', N'dd019', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Điều dưỡng', N'Khoa Tim mạch', N'Lâm Thị Hồng Ngọc', 3, 1, CAST(N'2026-05-25T11:18:00.000000' AS DateTime2), NULL, CAST(N'2026-06-28T11:44:00.000000' AS DateTime2), 9, N'dd019@nd2.example.vn', N'0920494244')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (37, N'ĐD020', N'dd020', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Điều dưỡng', N'Khoa Tim mạch', N'Võ Minh Khang', 3, 1, CAST(N'2026-05-25T11:29:00.000000' AS DateTime2), NULL, CAST(N'2026-06-28T11:57:00.000000' AS DateTime2), 9, N'dd020@nd2.example.vn', N'0920507973')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (38, N'ĐD021', N'dd021', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Điều dưỡng', N'Khoa Tim mạch', N'Hồ Thị Thuỳ Linh', 3, 1, CAST(N'2026-05-25T11:40:00.000000' AS DateTime2), NULL, CAST(N'2026-06-28T12:10:00.000000' AS DateTime2), 9, N'dd021@nd2.example.vn', N'0920521702')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (39, N'ĐD022', N'dd022', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Điều dưỡng', N'Khoa Tiêu hóa', N'Phan Quốc Việt', 3, 1, CAST(N'2026-05-25T11:51:00.000000' AS DateTime2), NULL, CAST(N'2026-06-28T12:23:00.000000' AS DateTime2), 10, N'dd022@nd2.example.vn', N'0920535431')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (40, N'ĐD023', N'dd023', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Điều dưỡng', N'Khoa Tiêu hóa', N'Nguyễn Thị Bích Trâm', 3, 1, CAST(N'2026-05-25T12:02:00.000000' AS DateTime2), NULL, NULL, 10, N'dd023@nd2.example.vn', N'0920549160')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (41, N'ĐD024', N'dd024', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Điều dưỡng', N'Khoa Tiêu hóa', N'Trần Minh Đức', 3, 1, CAST(N'2026-05-25T12:13:00.000000' AS DateTime2), NULL, CAST(N'2026-06-28T12:49:00.000000' AS DateTime2), 10, N'dd024@nd2.example.vn', N'0920562889')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (42, N'ĐD025', N'dd025', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Điều dưỡng', N'Khoa Ngoại thần kinh', N'Lê Thị Thanh Vy', 3, 1, CAST(N'2026-05-25T12:24:00.000000' AS DateTime2), NULL, CAST(N'2026-06-28T13:02:00.000000' AS DateTime2), 11, N'dd025@nd2.example.vn', N'0920576618')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (43, N'ĐD026', N'dd026', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Điều dưỡng', N'Khoa Ngoại thần kinh', N'Đinh Hoàng Long', 3, 1, CAST(N'2026-05-25T12:35:00.000000' AS DateTime2), NULL, CAST(N'2026-06-28T13:15:00.000000' AS DateTime2), 11, N'dd026@nd2.example.vn', N'0920590347')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (44, N'ĐD027', N'dd027', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Điều dưỡng', N'Khoa Ung bướu huyết học', N'Nguyễn Thị Cẩm Tú', 3, 1, CAST(N'2026-05-25T12:46:00.000000' AS DateTime2), NULL, CAST(N'2026-06-28T13:28:00.000000' AS DateTime2), 12, N'dd027@nd2.example.vn', N'0920604076')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (45, N'ĐD028', N'dd028', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Điều dưỡng', N'Khoa Ung bướu huyết học', N'Phạm Minh Châu', 3, 1, CAST(N'2026-05-25T12:57:00.000000' AS DateTime2), NULL, NULL, 12, N'dd028@nd2.example.vn', N'0920617805')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (46, N'ĐD029', N'dd029', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Điều dưỡng', N'Khoa Thần kinh', N'Huỳnh Thị Mai Phương', 3, 1, CAST(N'2026-05-25T13:08:00.000000' AS DateTime2), NULL, CAST(N'2026-06-28T13:54:00.000000' AS DateTime2), 13, N'dd029@nd2.example.vn', N'0920631534')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (47, N'ĐD030', N'dd030', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Điều dưỡng', N'Khoa Thần kinh', N'Nguyễn Đức Anh', 3, 1, CAST(N'2026-05-25T13:19:00.000000' AS DateTime2), NULL, CAST(N'2026-06-28T14:07:00.000000' AS DateTime2), 13, N'dd030@nd2.example.vn', N'0920645263')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (48, N'ĐD031', N'dd031', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Kỹ thuật viên điều dưỡng', N'Khoa Chẩn đoán hình ảnh', N'Trần Thị Kim Oanh', 3, 1, CAST(N'2026-05-25T13:30:00.000000' AS DateTime2), NULL, CAST(N'2026-06-28T14:20:00.000000' AS DateTime2), 14, N'dd031@nd2.example.vn', N'0920658992')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (49, N'ĐD032', N'dd032', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Kỹ thuật viên điều dưỡng', N'Khoa Chẩn đoán hình ảnh', N'Lê Quang Hòa', 3, 1, CAST(N'2026-05-25T13:41:00.000000' AS DateTime2), NULL, CAST(N'2026-06-28T14:33:00.000000' AS DateTime2), 14, N'dd032@nd2.example.vn', N'0920672721')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (50, N'ĐD033', N'dd033', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Điều dưỡng hành chính', N'Phòng Điều dưỡng', N'Phạm Thị Ngân Hà', 3, 1, CAST(N'2026-05-25T13:52:00.000000' AS DateTime2), NULL, NULL, 2, N'dd033@nd2.example.vn', N'0920686450')
INSERT [dbo].[TAIKHOAN] ([Id], [MaNhanVien], [TenDangNhap], [MatKhau], [ChucDanh], [KhoaPhong], [HoTen], [IdVaiTro], [TrangThai], [NgayTao], [NgayCapNhat], [LanDangNhapCuoi], [IdKhoaQuanLy], [Email], [SoDienThoai]) VALUES (51, N'ĐD034', N'dd034', N'$2a$12$kXL3AB25B4qVOb4F74qgVuytGFWWlMLve098mc/bZ62cUCJhZ4gzS', N'Chuyên viên hỗ trợ thi', N'Phòng Công nghệ thông tin', N'Ngô Anh Tuấn', 3, 1, CAST(N'2026-05-25T14:03:00.000000' AS DateTime2), NULL, CAST(N'2026-06-28T14:59:00.000000' AS DateTime2), 15, N'dd034@nd2.example.vn', N'0920700179')
SET IDENTITY_INSERT [dbo].[TAIKHOAN] OFF
GO
SET IDENTITY_INSERT [dbo].[TAIKHOAN_VAITRO] ON 

INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (1, 1, 1)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (2, 2, 5)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (3, 3, 5)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (4, 4, 5)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (5, 5, 5)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (6, 6, 5)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (7, 7, 5)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (8, 8, 5)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (9, 9, 5)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (10, 10, 5)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (11, 11, 5)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (12, 12, 5)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (13, 13, 5)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (14, 14, 5)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (15, 15, 5)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (16, 16, 5)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (17, 17, 5)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (18, 18, 3)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (19, 19, 3)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (20, 20, 3)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (21, 21, 3)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (22, 22, 3)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (23, 23, 3)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (24, 24, 3)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (25, 25, 3)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (26, 26, 3)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (27, 27, 3)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (28, 28, 3)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (29, 29, 3)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (30, 30, 3)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (31, 31, 3)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (32, 32, 3)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (33, 33, 3)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (34, 34, 3)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (35, 35, 3)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (36, 36, 3)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (37, 37, 3)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (38, 38, 3)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (39, 39, 3)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (40, 40, 3)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (41, 41, 3)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (42, 42, 3)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (43, 43, 3)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (44, 44, 3)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (45, 45, 3)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (46, 46, 3)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (47, 47, 3)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (48, 48, 3)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (49, 49, 3)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (50, 50, 3)
INSERT [dbo].[TAIKHOAN_VAITRO] ([Id], [IdTaiKhoan], [IdVaiTro]) VALUES (51, 51, 3)
SET IDENTITY_INSERT [dbo].[TAIKHOAN_VAITRO] OFF
GO
SET IDENTITY_INSERT [dbo].[THONGBAO] ON 

INSERT [dbo].[THONGBAO] ([Id], [UserId], [Title], [Message], [Type], [IsRead], [CreatedAt], [ReadAt], [RelatedUrl]) VALUES (1, 18, N'Bạn được phân công tham gia kỳ thi', N'Kỳ thi Bàn tay vàng điều dưỡng cấp bệnh viện năm 2026 sẽ diễn ra lúc 08:30 ngày 20/06/2026. Vui lòng đăng nhập trước giờ thi 15 phút.', N'ExamAssignment', 0, CAST(N'2026-06-18T09:00:00.000000' AS DateTime2), NULL, N'/exams/1')
INSERT [dbo].[THONGBAO] ([Id], [UserId], [Title], [Message], [Type], [IsRead], [CreatedAt], [ReadAt], [RelatedUrl]) VALUES (2, 19, N'Bạn được phân công tham gia kỳ thi', N'Kỳ thi Bàn tay vàng điều dưỡng cấp bệnh viện năm 2026 sẽ diễn ra lúc 08:30 ngày 20/06/2026. Vui lòng đăng nhập trước giờ thi 15 phút.', N'ExamAssignment', 1, CAST(N'2026-06-18T09:02:00.000000' AS DateTime2), CAST(N'2026-06-19T01:02:00.000000' AS DateTime2), N'/exams/1')
INSERT [dbo].[THONGBAO] ([Id], [UserId], [Title], [Message], [Type], [IsRead], [CreatedAt], [ReadAt], [RelatedUrl]) VALUES (3, 20, N'Bạn được phân công tham gia kỳ thi', N'Kỳ thi Bàn tay vàng điều dưỡng cấp bệnh viện năm 2026 sẽ diễn ra lúc 08:30 ngày 20/06/2026. Vui lòng đăng nhập trước giờ thi 15 phút.', N'ExamAssignment', 1, CAST(N'2026-06-18T09:04:00.000000' AS DateTime2), CAST(N'2026-06-18T16:04:00.000000' AS DateTime2), N'/exams/1')
INSERT [dbo].[THONGBAO] ([Id], [UserId], [Title], [Message], [Type], [IsRead], [CreatedAt], [ReadAt], [RelatedUrl]) VALUES (4, 21, N'Bạn được phân công tham gia kỳ thi', N'Kỳ thi Bàn tay vàng điều dưỡng cấp bệnh viện năm 2026 sẽ diễn ra lúc 08:30 ngày 20/06/2026. Vui lòng đăng nhập trước giờ thi 15 phút.', N'ExamAssignment', 0, CAST(N'2026-06-18T09:06:00.000000' AS DateTime2), NULL, N'/exams/1')
INSERT [dbo].[THONGBAO] ([Id], [UserId], [Title], [Message], [Type], [IsRead], [CreatedAt], [ReadAt], [RelatedUrl]) VALUES (5, 22, N'Bạn được phân công tham gia kỳ thi', N'Kỳ thi Bàn tay vàng điều dưỡng cấp bệnh viện năm 2026 sẽ diễn ra lúc 08:30 ngày 20/06/2026. Vui lòng đăng nhập trước giờ thi 15 phút.', N'ExamAssignment', 1, CAST(N'2026-06-18T09:08:00.000000' AS DateTime2), CAST(N'2026-06-18T21:08:00.000000' AS DateTime2), N'/exams/1')
INSERT [dbo].[THONGBAO] ([Id], [UserId], [Title], [Message], [Type], [IsRead], [CreatedAt], [ReadAt], [RelatedUrl]) VALUES (6, 23, N'Bạn được phân công tham gia kỳ thi', N'Kỳ thi Bàn tay vàng điều dưỡng cấp bệnh viện năm 2026 sẽ diễn ra lúc 08:30 ngày 20/06/2026. Vui lòng đăng nhập trước giờ thi 15 phút.', N'ExamAssignment', 1, CAST(N'2026-06-18T09:10:00.000000' AS DateTime2), CAST(N'2026-06-18T19:10:00.000000' AS DateTime2), N'/exams/1')
INSERT [dbo].[THONGBAO] ([Id], [UserId], [Title], [Message], [Type], [IsRead], [CreatedAt], [ReadAt], [RelatedUrl]) VALUES (7, 24, N'Bạn được phân công tham gia kỳ thi', N'Kỳ thi Bàn tay vàng điều dưỡng cấp bệnh viện năm 2026 sẽ diễn ra lúc 08:30 ngày 20/06/2026. Vui lòng đăng nhập trước giờ thi 15 phút.', N'ExamAssignment', 0, CAST(N'2026-06-18T09:12:00.000000' AS DateTime2), NULL, N'/exams/1')
INSERT [dbo].[THONGBAO] ([Id], [UserId], [Title], [Message], [Type], [IsRead], [CreatedAt], [ReadAt], [RelatedUrl]) VALUES (8, 25, N'Bạn được phân công tham gia kỳ thi', N'Kỳ thi Bàn tay vàng điều dưỡng cấp bệnh viện năm 2026 sẽ diễn ra lúc 08:30 ngày 20/06/2026. Vui lòng đăng nhập trước giờ thi 15 phút.', N'ExamAssignment', 1, CAST(N'2026-06-18T09:14:00.000000' AS DateTime2), CAST(N'2026-06-19T00:14:00.000000' AS DateTime2), N'/exams/1')
INSERT [dbo].[THONGBAO] ([Id], [UserId], [Title], [Message], [Type], [IsRead], [CreatedAt], [ReadAt], [RelatedUrl]) VALUES (9, 26, N'Bạn được phân công tham gia kỳ thi', N'Kỳ thi Bàn tay vàng điều dưỡng cấp bệnh viện năm 2026 sẽ diễn ra lúc 08:30 ngày 20/06/2026. Vui lòng đăng nhập trước giờ thi 15 phút.', N'ExamAssignment', 1, CAST(N'2026-06-18T09:16:00.000000' AS DateTime2), CAST(N'2026-06-18T21:16:00.000000' AS DateTime2), N'/exams/1')
INSERT [dbo].[THONGBAO] ([Id], [UserId], [Title], [Message], [Type], [IsRead], [CreatedAt], [ReadAt], [RelatedUrl]) VALUES (10, 27, N'Bạn được phân công tham gia kỳ thi', N'Kỳ thi Bàn tay vàng điều dưỡng cấp bệnh viện năm 2026 sẽ diễn ra lúc 08:30 ngày 20/06/2026. Vui lòng đăng nhập trước giờ thi 15 phút.', N'ExamAssignment', 0, CAST(N'2026-06-18T09:18:00.000000' AS DateTime2), NULL, N'/exams/1')
INSERT [dbo].[THONGBAO] ([Id], [UserId], [Title], [Message], [Type], [IsRead], [CreatedAt], [ReadAt], [RelatedUrl]) VALUES (11, 28, N'Bạn được phân công tham gia kỳ thi', N'Kỳ thi Bàn tay vàng điều dưỡng cấp bệnh viện năm 2026 sẽ diễn ra lúc 08:30 ngày 20/06/2026. Vui lòng đăng nhập trước giờ thi 15 phút.', N'ExamAssignment', 1, CAST(N'2026-06-18T09:20:00.000000' AS DateTime2), CAST(N'2026-06-18T20:20:00.000000' AS DateTime2), N'/exams/1')
INSERT [dbo].[THONGBAO] ([Id], [UserId], [Title], [Message], [Type], [IsRead], [CreatedAt], [ReadAt], [RelatedUrl]) VALUES (12, 29, N'Bạn được phân công tham gia kỳ thi', N'Kỳ thi Bàn tay vàng điều dưỡng cấp bệnh viện năm 2026 sẽ diễn ra lúc 08:30 ngày 20/06/2026. Vui lòng đăng nhập trước giờ thi 15 phút.', N'ExamAssignment', 1, CAST(N'2026-06-18T09:22:00.000000' AS DateTime2), CAST(N'2026-06-18T17:22:00.000000' AS DateTime2), N'/exams/1')
INSERT [dbo].[THONGBAO] ([Id], [UserId], [Title], [Message], [Type], [IsRead], [CreatedAt], [ReadAt], [RelatedUrl]) VALUES (13, 18, N'Kết quả kỳ thi đã được công bố', N'Kết quả kỳ thi cấp bệnh viện đã được công bố. Vui lòng vào mục Kết quả để xem chi tiết.', N'ResultPublished', 1, CAST(N'2026-06-20T12:05:00.000000' AS DateTime2), CAST(N'2026-06-20T13:05:00.000000' AS DateTime2), N'/results/1')
INSERT [dbo].[THONGBAO] ([Id], [UserId], [Title], [Message], [Type], [IsRead], [CreatedAt], [ReadAt], [RelatedUrl]) VALUES (14, 19, N'Kết quả kỳ thi đã được công bố', N'Kết quả kỳ thi cấp bệnh viện đã được công bố. Vui lòng vào mục Kết quả để xem chi tiết.', N'ResultPublished', 1, CAST(N'2026-06-20T12:06:00.000000' AS DateTime2), CAST(N'2026-06-20T13:06:00.000000' AS DateTime2), N'/results/1')
INSERT [dbo].[THONGBAO] ([Id], [UserId], [Title], [Message], [Type], [IsRead], [CreatedAt], [ReadAt], [RelatedUrl]) VALUES (15, 20, N'Kết quả kỳ thi đã được công bố', N'Kết quả kỳ thi cấp bệnh viện đã được công bố. Vui lòng vào mục Kết quả để xem chi tiết.', N'ResultPublished', 1, CAST(N'2026-06-20T12:07:00.000000' AS DateTime2), CAST(N'2026-06-20T13:07:00.000000' AS DateTime2), N'/results/1')
INSERT [dbo].[THONGBAO] ([Id], [UserId], [Title], [Message], [Type], [IsRead], [CreatedAt], [ReadAt], [RelatedUrl]) VALUES (16, 21, N'Kết quả kỳ thi đã được công bố', N'Kết quả kỳ thi cấp bệnh viện đã được công bố. Vui lòng vào mục Kết quả để xem chi tiết.', N'ResultPublished', 1, CAST(N'2026-06-20T12:08:00.000000' AS DateTime2), CAST(N'2026-06-20T13:08:00.000000' AS DateTime2), N'/results/1')
INSERT [dbo].[THONGBAO] ([Id], [UserId], [Title], [Message], [Type], [IsRead], [CreatedAt], [ReadAt], [RelatedUrl]) VALUES (17, 22, N'Kết quả kỳ thi đã được công bố', N'Kết quả kỳ thi cấp bệnh viện đã được công bố. Vui lòng vào mục Kết quả để xem chi tiết.', N'ResultPublished', 1, CAST(N'2026-06-20T12:09:00.000000' AS DateTime2), CAST(N'2026-06-20T13:09:00.000000' AS DateTime2), N'/results/1')
INSERT [dbo].[THONGBAO] ([Id], [UserId], [Title], [Message], [Type], [IsRead], [CreatedAt], [ReadAt], [RelatedUrl]) VALUES (18, 30, N'Thông báo kiểm tra kiểm soát nhiễm khuẩn', N'Bạn được phân công tham gia bài đánh giá kiểm soát nhiễm khuẩn đợt 1. Bài thi mở từ ngày 30/06/2026 đến 03/07/2026.', N'ExamAssignment', 0, CAST(N'2026-06-29T16:00:00.000000' AS DateTime2), NULL, N'/exams/3')
INSERT [dbo].[THONGBAO] ([Id], [UserId], [Title], [Message], [Type], [IsRead], [CreatedAt], [ReadAt], [RelatedUrl]) VALUES (19, 31, N'Thông báo kiểm tra kiểm soát nhiễm khuẩn', N'Bạn được phân công tham gia bài đánh giá kiểm soát nhiễm khuẩn đợt 1. Bài thi mở từ ngày 30/06/2026 đến 03/07/2026.', N'ExamAssignment', 0, CAST(N'2026-06-29T16:03:00.000000' AS DateTime2), NULL, N'/exams/3')
INSERT [dbo].[THONGBAO] ([Id], [UserId], [Title], [Message], [Type], [IsRead], [CreatedAt], [ReadAt], [RelatedUrl]) VALUES (20, 32, N'Thông báo kiểm tra kiểm soát nhiễm khuẩn', N'Bạn được phân công tham gia bài đánh giá kiểm soát nhiễm khuẩn đợt 1. Bài thi mở từ ngày 30/06/2026 đến 03/07/2026.', N'ExamAssignment', 0, CAST(N'2026-06-29T16:06:00.000000' AS DateTime2), NULL, N'/exams/3')
INSERT [dbo].[THONGBAO] ([Id], [UserId], [Title], [Message], [Type], [IsRead], [CreatedAt], [ReadAt], [RelatedUrl]) VALUES (21, 33, N'Thông báo kiểm tra kiểm soát nhiễm khuẩn', N'Bạn được phân công tham gia bài đánh giá kiểm soát nhiễm khuẩn đợt 1. Bài thi mở từ ngày 30/06/2026 đến 03/07/2026.', N'ExamAssignment', 0, CAST(N'2026-06-29T16:09:00.000000' AS DateTime2), NULL, N'/exams/3')
INSERT [dbo].[THONGBAO] ([Id], [UserId], [Title], [Message], [Type], [IsRead], [CreatedAt], [ReadAt], [RelatedUrl]) VALUES (22, 34, N'Thông báo kiểm tra kiểm soát nhiễm khuẩn', N'Bạn được phân công tham gia bài đánh giá kiểm soát nhiễm khuẩn đợt 1. Bài thi mở từ ngày 30/06/2026 đến 03/07/2026.', N'ExamAssignment', 0, CAST(N'2026-06-29T16:12:00.000000' AS DateTime2), NULL, N'/exams/3')
INSERT [dbo].[THONGBAO] ([Id], [UserId], [Title], [Message], [Type], [IsRead], [CreatedAt], [ReadAt], [RelatedUrl]) VALUES (23, 35, N'Thông báo kiểm tra kiểm soát nhiễm khuẩn', N'Bạn được phân công tham gia bài đánh giá kiểm soát nhiễm khuẩn đợt 1. Bài thi mở từ ngày 30/06/2026 đến 03/07/2026.', N'ExamAssignment', 0, CAST(N'2026-06-29T16:15:00.000000' AS DateTime2), NULL, N'/exams/3')
INSERT [dbo].[THONGBAO] ([Id], [UserId], [Title], [Message], [Type], [IsRead], [CreatedAt], [ReadAt], [RelatedUrl]) VALUES (24, 36, N'Thông báo kiểm tra kiểm soát nhiễm khuẩn', N'Bạn được phân công tham gia bài đánh giá kiểm soát nhiễm khuẩn đợt 1. Bài thi mở từ ngày 30/06/2026 đến 03/07/2026.', N'ExamAssignment', 0, CAST(N'2026-06-29T16:18:00.000000' AS DateTime2), NULL, N'/exams/3')
INSERT [dbo].[THONGBAO] ([Id], [UserId], [Title], [Message], [Type], [IsRead], [CreatedAt], [ReadAt], [RelatedUrl]) VALUES (25, 37, N'Thông báo kiểm tra kiểm soát nhiễm khuẩn', N'Bạn được phân công tham gia bài đánh giá kiểm soát nhiễm khuẩn đợt 1. Bài thi mở từ ngày 30/06/2026 đến 03/07/2026.', N'ExamAssignment', 0, CAST(N'2026-06-29T16:21:00.000000' AS DateTime2), NULL, N'/exams/3')
INSERT [dbo].[THONGBAO] ([Id], [UserId], [Title], [Message], [Type], [IsRead], [CreatedAt], [ReadAt], [RelatedUrl]) VALUES (26, 3, N'Báo cáo tiến độ kỳ thi', N'Hệ thống đã ghi nhận kết quả làm bài mới. Vui lòng kiểm tra bảng tổng hợp theo khoa/phòng phụ trách.', N'Report', 0, CAST(N'2026-06-30T17:03:00.000000' AS DateTime2), NULL, N'/admin/reports')
INSERT [dbo].[THONGBAO] ([Id], [UserId], [Title], [Message], [Type], [IsRead], [CreatedAt], [ReadAt], [RelatedUrl]) VALUES (27, 4, N'Báo cáo tiến độ kỳ thi', N'Hệ thống đã ghi nhận kết quả làm bài mới. Vui lòng kiểm tra bảng tổng hợp theo khoa/phòng phụ trách.', N'Report', 0, CAST(N'2026-06-30T17:04:00.000000' AS DateTime2), NULL, N'/admin/reports')
INSERT [dbo].[THONGBAO] ([Id], [UserId], [Title], [Message], [Type], [IsRead], [CreatedAt], [ReadAt], [RelatedUrl]) VALUES (28, 5, N'Báo cáo tiến độ kỳ thi', N'Hệ thống đã ghi nhận kết quả làm bài mới. Vui lòng kiểm tra bảng tổng hợp theo khoa/phòng phụ trách.', N'Report', 0, CAST(N'2026-06-30T17:05:00.000000' AS DateTime2), NULL, N'/admin/reports')
INSERT [dbo].[THONGBAO] ([Id], [UserId], [Title], [Message], [Type], [IsRead], [CreatedAt], [ReadAt], [RelatedUrl]) VALUES (29, 8, N'Báo cáo tiến độ kỳ thi', N'Hệ thống đã ghi nhận kết quả làm bài mới. Vui lòng kiểm tra bảng tổng hợp theo khoa/phòng phụ trách.', N'Report', 0, CAST(N'2026-06-30T17:08:00.000000' AS DateTime2), NULL, N'/admin/reports')
SET IDENTITY_INSERT [dbo].[THONGBAO] OFF
GO
SET IDENTITY_INSERT [dbo].[TOKEN_LAM_MOI] ON 

INSERT [dbo].[TOKEN_LAM_MOI] ([Id], [Token], [UserId], [CreatedAt], [ExpiresAt], [IsUsed], [IsRevoked], [IpAddress], [UserAgent]) VALUES (1, N'b3c86c48cdacff26a1389693b08296ee37ff31c65536e0c7ac9c1d69d390d8bc', 1, CAST(N'2026-06-30T07:39:00.000000' AS DateTime2), CAST(N'2026-07-07T07:39:00.000000' AS DateTime2), 0, 0, N'10.20.2.35', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36')
INSERT [dbo].[TOKEN_LAM_MOI] ([Id], [Token], [UserId], [CreatedAt], [ExpiresAt], [IsUsed], [IsRevoked], [IpAddress], [UserAgent]) VALUES (2, N'c4c57b6adcb7aab119110f97a36263dac0a0c3c36848af8123a8a18875d2036f', 2, CAST(N'2026-06-30T07:48:00.000000' AS DateTime2), CAST(N'2026-07-07T07:48:00.000000' AS DateTime2), 0, 0, N'10.20.4.19', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36')
INSERT [dbo].[TOKEN_LAM_MOI] ([Id], [Token], [UserId], [CreatedAt], [ExpiresAt], [IsUsed], [IsRevoked], [IpAddress], [UserAgent]) VALUES (3, N'bec340f1a99f6dd946a7a0619046fead438357ae9bdfc5da79c0f25045597a19', 3, CAST(N'2026-06-30T07:57:00.000000' AS DateTime2), CAST(N'2026-07-07T07:57:00.000000' AS DateTime2), 0, 0, N'10.20.2.35', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36')
INSERT [dbo].[TOKEN_LAM_MOI] ([Id], [Token], [UserId], [CreatedAt], [ExpiresAt], [IsUsed], [IsRevoked], [IpAddress], [UserAgent]) VALUES (4, N'b782ed8053785f532774834b6677088aafaa6106d93398b550f91fe4a4679364', 4, CAST(N'2026-06-30T08:06:00.000000' AS DateTime2), CAST(N'2026-07-07T08:06:00.000000' AS DateTime2), 0, 1, N'10.20.4.19', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36')
INSERT [dbo].[TOKEN_LAM_MOI] ([Id], [Token], [UserId], [CreatedAt], [ExpiresAt], [IsUsed], [IsRevoked], [IpAddress], [UserAgent]) VALUES (5, N'fb96ba94bbd1d3015154125db62c6619696e844b95bd636b85ee4f662d8411bb', 5, CAST(N'2026-06-30T08:15:00.000000' AS DateTime2), CAST(N'2026-07-07T08:15:00.000000' AS DateTime2), 0, 0, N'10.20.5.22', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36')
INSERT [dbo].[TOKEN_LAM_MOI] ([Id], [Token], [UserId], [CreatedAt], [ExpiresAt], [IsUsed], [IsRevoked], [IpAddress], [UserAgent]) VALUES (6, N'a51ffe4c6d0e9a01c6aca9cabe4c494f75f95cab7c7085d12d9e8c65f5be47ad', 8, CAST(N'2026-06-30T08:24:00.000000' AS DateTime2), CAST(N'2026-07-07T08:24:00.000000' AS DateTime2), 0, 0, N'10.20.2.35', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36')
INSERT [dbo].[TOKEN_LAM_MOI] ([Id], [Token], [UserId], [CreatedAt], [ExpiresAt], [IsUsed], [IsRevoked], [IpAddress], [UserAgent]) VALUES (7, N'80e8a6cc833bac86534ed2783cf4e91b2d9a940c84c049973a83304ad67e19b1', 18, CAST(N'2026-06-30T08:33:00.000000' AS DateTime2), CAST(N'2026-07-07T08:33:00.000000' AS DateTime2), 0, 0, N'10.20.4.19', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36')
INSERT [dbo].[TOKEN_LAM_MOI] ([Id], [Token], [UserId], [CreatedAt], [ExpiresAt], [IsUsed], [IsRevoked], [IpAddress], [UserAgent]) VALUES (8, N'048f1f7a562c4e9c1c70e5b25b6760d6f4be13f28d1ca9a107a2d57521d9d5be', 19, CAST(N'2026-06-30T08:42:00.000000' AS DateTime2), CAST(N'2026-07-07T08:42:00.000000' AS DateTime2), 0, 1, N'10.20.5.22', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36')
INSERT [dbo].[TOKEN_LAM_MOI] ([Id], [Token], [UserId], [CreatedAt], [ExpiresAt], [IsUsed], [IsRevoked], [IpAddress], [UserAgent]) VALUES (9, N'd4d07652cae125c1ee11922252bf683ab84fed7fbf1ab6933ce5a588411504c9', 20, CAST(N'2026-06-30T08:51:00.000000' AS DateTime2), CAST(N'2026-07-07T08:51:00.000000' AS DateTime2), 0, 0, N'10.20.4.19', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36')
INSERT [dbo].[TOKEN_LAM_MOI] ([Id], [Token], [UserId], [CreatedAt], [ExpiresAt], [IsUsed], [IsRevoked], [IpAddress], [UserAgent]) VALUES (10, N'8d95bbfab5475a988eaf230fabb400edc14f34442841775e63db8c2ac9fc7d69', 21, CAST(N'2026-06-30T09:00:00.000000' AS DateTime2), CAST(N'2026-07-07T09:00:00.000000' AS DateTime2), 0, 0, N'10.20.5.06', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36')
INSERT [dbo].[TOKEN_LAM_MOI] ([Id], [Token], [UserId], [CreatedAt], [ExpiresAt], [IsUsed], [IsRevoked], [IpAddress], [UserAgent]) VALUES (11, N'645235aa0a58d81d6abcfb89926c35b1ea58138280e05d0eb9cf44bd076797d8', 22, CAST(N'2026-06-30T09:09:00.000000' AS DateTime2), CAST(N'2026-07-07T09:09:00.000000' AS DateTime2), 0, 0, N'10.20.1.12', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36')
INSERT [dbo].[TOKEN_LAM_MOI] ([Id], [Token], [UserId], [CreatedAt], [ExpiresAt], [IsUsed], [IsRevoked], [IpAddress], [UserAgent]) VALUES (12, N'c433a83c1a6a3d5effbe376853561776b81aa08eeb337a80ca416340ae40f08c', 23, CAST(N'2026-06-30T09:18:00.000000' AS DateTime2), CAST(N'2026-07-07T09:18:00.000000' AS DateTime2), 0, 1, N'10.20.2.21', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36')
INSERT [dbo].[TOKEN_LAM_MOI] ([Id], [Token], [UserId], [CreatedAt], [ExpiresAt], [IsUsed], [IsRevoked], [IpAddress], [UserAgent]) VALUES (13, N'243f40951968d59e2d02fa5fa608c93735bb87c49bd16808735a056eba5cddc3', 24, CAST(N'2026-06-30T09:27:00.000000' AS DateTime2), CAST(N'2026-07-07T09:27:00.000000' AS DateTime2), 0, 0, N'10.20.4.19', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36')
INSERT [dbo].[TOKEN_LAM_MOI] ([Id], [Token], [UserId], [CreatedAt], [ExpiresAt], [IsUsed], [IsRevoked], [IpAddress], [UserAgent]) VALUES (14, N'7c380d16278ed1783467fe23f67c1cf1ebaa5d8748e8676b33abfe49c9d9d4f7', 30, CAST(N'2026-06-30T09:36:00.000000' AS DateTime2), CAST(N'2026-07-07T09:36:00.000000' AS DateTime2), 0, 0, N'10.20.3.14', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36')
INSERT [dbo].[TOKEN_LAM_MOI] ([Id], [Token], [UserId], [CreatedAt], [ExpiresAt], [IsUsed], [IsRevoked], [IpAddress], [UserAgent]) VALUES (15, N'30b1060721a5687ea5abc326206d22d8bccb229a3443e34967837bb94527c5b0', 31, CAST(N'2026-06-30T09:45:00.000000' AS DateTime2), CAST(N'2026-07-07T09:45:00.000000' AS DateTime2), 0, 0, N'10.20.1.12', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/126.0 Safari/537.36')
INSERT [dbo].[TOKEN_LAM_MOI] ([Id], [Token], [UserId], [CreatedAt], [ExpiresAt], [IsUsed], [IsRevoked], [IpAddress], [UserAgent]) VALUES (16, N'7380674416690e57568ff232bcadbd7b58f1f3ce62b31df7d9d210af4a9d73c6', 32, CAST(N'2026-06-30T09:54:00.000000' AS DateTime2), CAST(N'2026-07-07T09:54:00.000000' AS DateTime2), 0, 1, N'10.20.5.06', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36')
INSERT [dbo].[TOKEN_LAM_MOI] ([Id], [Token], [UserId], [CreatedAt], [ExpiresAt], [IsUsed], [IsRevoked], [IpAddress], [UserAgent]) VALUES (17, N'7700b4948e8020271adee22539d3532b3f410b3de8cb013088ebc391b6212e1c', 33, CAST(N'2026-06-30T10:03:00.000000' AS DateTime2), CAST(N'2026-07-07T10:03:00.000000' AS DateTime2), 0, 0, N'10.20.2.35', N'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Edg/126.0 Safari/537.36')
SET IDENTITY_INSERT [dbo].[TOKEN_LAM_MOI] OFF
GO
SET IDENTITY_INSERT [dbo].[VAITRO] ON 

INSERT [dbo].[VAITRO] ([Id], [MaVaiTro], [TenVaiTro], [MoTa]) VALUES (1, N'ADMIN', N'Quản trị viên', N'Toàn quyền cấu hình hệ thống, tài khoản, vai trò và dữ liệu toàn bệnh viện')
INSERT [dbo].[VAITRO] ([Id], [MaVaiTro], [TenVaiTro], [MoTa]) VALUES (3, N'STUDENT', N'Thí sinh', N'Điều dưỡng hoặc nhân viên y tế được phân công tham gia kỳ thi')
INSERT [dbo].[VAITRO] ([Id], [MaVaiTro], [TenVaiTro], [MoTa]) VALUES (5, N'DEPT_MANAGER', N'Quản lý khoa/phòng', N'Tạo kỳ thi, quản lý đề thi, theo dõi kết quả trong phạm vi khoa/phòng phụ trách')
SET IDENTITY_INSERT [dbo].[VAITRO] OFF
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ__KHOA_PHO__653904040EB7D0A8]    Script Date: 01/07/2026 12:25:06 SA ******/
ALTER TABLE [dbo].[KHOA_PHONG] ADD UNIQUE NONCLUSTERED 
(
	[MaKhoa] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [UQ_ExamAssignments_ExamUser]    Script Date: 01/07/2026 12:25:06 SA ******/
ALTER TABLE [dbo].[PHANCONG_THI] ADD  CONSTRAINT [UQ_ExamAssignments_ExamUser] UNIQUE NONCLUSTERED 
(
	[ExamId] ASC,
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ__PHIEN_NG__C9F49291B24639A9]    Script Date: 01/07/2026 12:25:06 SA ******/
ALTER TABLE [dbo].[PHIEN_NGUOIDUNG] ADD UNIQUE NONCLUSTERED 
(
	[SessionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [UQ__TOKEN_LA__1EB4F817ED1F192E]    Script Date: 01/07/2026 12:25:06 SA ******/
ALTER TABLE [dbo].[TOKEN_LAM_MOI] ADD UNIQUE NONCLUSTERED 
(
	[Token] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[BAITHI] ADD  DEFAULT ((0)) FOR [CongBoRieng]
GO
ALTER TABLE [dbo].[CAUHOI] ADD  DEFAULT ((0)) FOR [DaXoa]
GO
ALTER TABLE [dbo].[CAUHOI] ADD  CONSTRAINT [DF_CAUHOI_DoKho]  DEFAULT ('1') FOR [DoKho]
GO
ALTER TABLE [dbo].[CHITIETLAMBAI] ADD  DEFAULT ((0)) FOR [DaLuu]
GO
ALTER TABLE [dbo].[DETHI] ADD  DEFAULT ((0)) FOR [CongBoKetQua]
GO
ALTER TABLE [dbo].[KHOA_PHONG] ADD  DEFAULT ((1)) FOR [TrangThai]
GO
ALTER TABLE [dbo].[KHOA_PHONG] ADD  DEFAULT (getdate()) FOR [NgayTao]
GO
ALTER TABLE [dbo].[KyThi] ADD  DEFAULT ('DangChuanBi') FOR [TrangThai]
GO
ALTER TABLE [dbo].[KyThi] ADD  DEFAULT (getdate()) FOR [NgayTao]
GO
ALTER TABLE [dbo].[PHANCONG_THI] ADD  DEFAULT (getutcdate()) FOR [AssignedAt]
GO
ALTER TABLE [dbo].[PHANCONG_THI] ADD  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[PHIEN_NGUOIDUNG] ADD  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[TAIKHOAN] ADD  DEFAULT ((1)) FOR [TrangThai]
GO
ALTER TABLE [dbo].[TAIKHOAN] ADD  DEFAULT (getdate()) FOR [NgayTao]
GO
ALTER TABLE [dbo].[THONGBAO] ADD  DEFAULT ('Info') FOR [Type]
GO
ALTER TABLE [dbo].[THONGBAO] ADD  DEFAULT ((0)) FOR [IsRead]
GO
ALTER TABLE [dbo].[THONGBAO] ADD  DEFAULT (getutcdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[TOKEN_LAM_MOI] ADD  DEFAULT ((0)) FOR [IsUsed]
GO
ALTER TABLE [dbo].[TOKEN_LAM_MOI] ADD  DEFAULT ((0)) FOR [IsRevoked]
GO
ALTER TABLE [dbo].[BAITHI]  WITH CHECK ADD FOREIGN KEY([IdDeThi])
REFERENCES [dbo].[DETHI] ([Id])
GO
ALTER TABLE [dbo].[BAITHI]  WITH CHECK ADD FOREIGN KEY([IdTaiKhoan])
REFERENCES [dbo].[TAIKHOAN] ([Id])
GO
ALTER TABLE [dbo].[BAITHI]  WITH CHECK ADD  CONSTRAINT [FK_BAITHI_KyThi] FOREIGN KEY([IdKyThi])
REFERENCES [dbo].[KyThi] ([Id])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[BAITHI] CHECK CONSTRAINT [FK_BAITHI_KyThi]
GO
ALTER TABLE [dbo].[CANHBAOGIANLAN]  WITH CHECK ADD FOREIGN KEY([IdBaiThi])
REFERENCES [dbo].[BAITHI] ([Id])
GO
ALTER TABLE [dbo].[CAUHOI]  WITH CHECK ADD FOREIGN KEY([IdLoaiCauHoi])
REFERENCES [dbo].[LOAICAUHOI] ([Id])
GO
ALTER TABLE [dbo].[CHITIETLAMBAI]  WITH CHECK ADD FOREIGN KEY([IdBaiThi])
REFERENCES [dbo].[BAITHI] ([Id])
GO
ALTER TABLE [dbo].[CHITIETLAMBAI]  WITH CHECK ADD FOREIGN KEY([IdCauHoi])
REFERENCES [dbo].[CAUHOI] ([Id])
GO
ALTER TABLE [dbo].[CHITIETLAMBAI]  WITH CHECK ADD FOREIGN KEY([IdLuaChonDaChon])
REFERENCES [dbo].[LUACHON] ([Id])
GO
ALTER TABLE [dbo].[DETHI]  WITH CHECK ADD  CONSTRAINT [FK_DETHI_KyThi] FOREIGN KEY([KyThiId])
REFERENCES [dbo].[KyThi] ([Id])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[DETHI] CHECK CONSTRAINT [FK_DETHI_KyThi]
GO
ALTER TABLE [dbo].[DETHI_CAUHOI]  WITH CHECK ADD FOREIGN KEY([IdCauHoi])
REFERENCES [dbo].[CAUHOI] ([Id])
GO
ALTER TABLE [dbo].[DETHI_CAUHOI]  WITH CHECK ADD FOREIGN KEY([IdDeThi])
REFERENCES [dbo].[DETHI] ([Id])
GO
ALTER TABLE [dbo].[KHOA_PHONG]  WITH CHECK ADD  CONSTRAINT [FK_KHOA_PHONG_DeptManager] FOREIGN KEY([DeptManagerId])
REFERENCES [dbo].[TAIKHOAN] ([Id])
GO
ALTER TABLE [dbo].[KHOA_PHONG] CHECK CONSTRAINT [FK_KHOA_PHONG_DeptManager]
GO
ALTER TABLE [dbo].[KyThi]  WITH CHECK ADD  CONSTRAINT [FK_KyThi_KhoaPhong] FOREIGN KEY([KhoaPhongId])
REFERENCES [dbo].[KHOA_PHONG] ([Id])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[KyThi] CHECK CONSTRAINT [FK_KyThi_KhoaPhong]
GO
ALTER TABLE [dbo].[LOGTHAOTAC]  WITH CHECK ADD FOREIGN KEY([IdBaiThi])
REFERENCES [dbo].[BAITHI] ([Id])
GO
ALTER TABLE [dbo].[LOGTHAOTAC]  WITH CHECK ADD  CONSTRAINT [FK_LOGTHAOTAC_TaiKhoan] FOREIGN KEY([IdTaiKhoan])
REFERENCES [dbo].[TAIKHOAN] ([Id])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[LOGTHAOTAC] CHECK CONSTRAINT [FK_LOGTHAOTAC_TaiKhoan]
GO
ALTER TABLE [dbo].[LUACHON]  WITH CHECK ADD FOREIGN KEY([IdCauHoi])
REFERENCES [dbo].[CAUHOI] ([Id])
GO
ALTER TABLE [dbo].[PHANCONG_THI]  WITH CHECK ADD  CONSTRAINT [FK_ExamAssignments_Exam] FOREIGN KEY([ExamId])
REFERENCES [dbo].[DETHI] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[PHANCONG_THI] CHECK CONSTRAINT [FK_ExamAssignments_Exam]
GO
ALTER TABLE [dbo].[PHANCONG_THI]  WITH CHECK ADD  CONSTRAINT [FK_ExamAssignments_User] FOREIGN KEY([UserId])
REFERENCES [dbo].[TAIKHOAN] ([Id])
GO
ALTER TABLE [dbo].[PHANCONG_THI] CHECK CONSTRAINT [FK_ExamAssignments_User]
GO
ALTER TABLE [dbo].[PHIEN_NGUOIDUNG]  WITH CHECK ADD  CONSTRAINT [FK_UserSessions_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[TAIKHOAN] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[PHIEN_NGUOIDUNG] CHECK CONSTRAINT [FK_UserSessions_UserId]
GO
ALTER TABLE [dbo].[PHIENDANGNHAP]  WITH CHECK ADD FOREIGN KEY([IdTaiKhoan])
REFERENCES [dbo].[TAIKHOAN] ([Id])
GO
ALTER TABLE [dbo].[TAIKHOAN]  WITH CHECK ADD  CONSTRAINT [FK_TAIKHOAN_KhoaQuanLy] FOREIGN KEY([IdKhoaQuanLy])
REFERENCES [dbo].[KHOA_PHONG] ([Id])
GO
ALTER TABLE [dbo].[TAIKHOAN] CHECK CONSTRAINT [FK_TAIKHOAN_KhoaQuanLy]
GO
ALTER TABLE [dbo].[TAIKHOAN_VAITRO]  WITH CHECK ADD FOREIGN KEY([IdTaiKhoan])
REFERENCES [dbo].[TAIKHOAN] ([Id])
GO
ALTER TABLE [dbo].[TAIKHOAN_VAITRO]  WITH CHECK ADD FOREIGN KEY([IdVaiTro])
REFERENCES [dbo].[VAITRO] ([Id])
GO
ALTER TABLE [dbo].[THONGBAO]  WITH CHECK ADD  CONSTRAINT [FK_Notifications_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[TAIKHOAN] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[THONGBAO] CHECK CONSTRAINT [FK_Notifications_UserId]
GO
ALTER TABLE [dbo].[TOKEN_LAM_MOI]  WITH CHECK ADD  CONSTRAINT [FK_RefreshTokens_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[TAIKHOAN] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[TOKEN_LAM_MOI] CHECK CONSTRAINT [FK_RefreshTokens_UserId]
GO
/****** Object:  StoredProcedure [dbo].[CleanupExpiredTokens]    Script Date: 01/07/2026 12:25:06 SA ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER OFF
GO

CREATE PROCEDURE [dbo].[CleanupExpiredTokens]
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @DeletedTokens int = 0;
    DECLARE @DeletedSessions int = 0;
    
    -- Delete expired refresh tokens
    DELETE FROM [dbo].[TOKEN_LAM_MOI] 
    WHERE ExpiresAt <= GETDATE() OR IsUsed = 1 OR IsRevoked = 1;
    SET @DeletedTokens = @@ROWCOUNT;
    
    -- Mark expired sessions as inactive
    UPDATE [dbo].[PHIEN_NGUOIDUNG] 
    SET IsActive = 0, EndReason = 'Expired', LastActivityAt = GETDATE()
    WHERE ExpiresAt <= GETDATE() AND IsActive = 1;
    SET @DeletedSessions = @@ROWCOUNT;
    
    PRINT 'Cleanup completed: ' + CAST(@DeletedTokens AS varchar) + ' tokens deleted, ' + CAST(@DeletedSessions AS varchar) + ' sessions expired';
END

GO
USE [master]
GO
ALTER DATABASE [HeThongBanTayVang] SET  READ_WRITE 
GO