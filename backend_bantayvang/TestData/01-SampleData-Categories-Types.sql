-- Dữ liệu mẫu cho Danh mục câu hỏi và Loại câu hỏi

-- Insert Danh mục câu hỏi
INSERT INTO DANHMUCAUHOI (TenDanhMuc, Mota) VALUES
(N'Lập trình C#', N'Câu hỏi về ngôn ngữ lập trình C#'),
(N'Cơ sở dữ liệu', N'Câu hỏi về SQL Server, MySQL'),
(N'ASP.NET Core', N'Câu hỏi về framework ASP.NET Core'),
(N'JavaScript', N'Câu hỏi về ngôn ngữ JavaScript'),
(N'HTML/CSS', N'Câu hỏi về HTML và CSS');

-- Insert Loại câu hỏi
INSERT INTO LOAICAUHOI (TenLoai, MoTa) VALUES
(N'Trắc nghiệm', N'Câu hỏi trắc nghiệm 4 đáp án'),
(N'Đúng/Sai', N'Câu hỏi đúng hoặc sai'),
(N'Tự luận', N'Câu hỏi tự luận ngắn'),
(N'Điền khuyết', N'Câu hỏi điền vào chỗ trống');

-- Insert Tài khoản mẫu
INSERT INTO TAIKHOAN (TenDangNhap, MatKhau, MaNhanVien, ChucDanh, KhoaPhong) VALUES
('admin', 'admin123', 'NV001', N'Quản trị viên', 'CNTT'),
('giaovien1', 'gv123', 'GV001', N'Giáo viên', 'CNTT'),
('sinhvien1', 'sv123', 'SV001', N'Sinh viên', 'CNTT'),
('sinhvien2', 'sv123', 'SV002', N'Sinh viên', 'CNTT');

-- Insert Vai trò
INSERT INTO VAITRO (MaVaiTro, TenVaiTro, MoTa) VALUES
('ADMIN', N'Admin', N'Quản trị viên hệ thống'),
('GIAOVIEN', N'GiaoVien', N'Giáo viên tạo đề thi'),
('SINHVIEN', N'SinhVien', N'Sinh viên làm bài thi');

-- Insert Tài khoản - Vai trò
-- Lấy ID thực tế từ bảng TAIKHOAN và VAITRO
DECLARE @AdminId INT = (SELECT Id FROM TAIKHOAN WHERE TenDangNhap = 'admin');
DECLARE @GiaoVienId INT = (SELECT Id FROM TAIKHOAN WHERE TenDangNhap = 'giaovien1');
DECLARE @SinhVien1Id INT = (SELECT Id FROM TAIKHOAN WHERE TenDangNhap = 'sinhvien1');
DECLARE @SinhVien2Id INT = (SELECT Id FROM TAIKHOAN WHERE TenDangNhap = 'sinhvien2');

DECLARE @AdminRoleId INT = (SELECT Id FROM VAITRO WHERE MaVaiTro = 'ADMIN');
DECLARE @GiaoVienRoleId INT = (SELECT Id FROM VAITRO WHERE MaVaiTro = 'GIAOVIEN');
DECLARE @SinhVienRoleId INT = (SELECT Id FROM VAITRO WHERE MaVaiTro = 'SINHVIEN');

INSERT INTO TAIKHOAN_VAITRO (IdTaiKhoan, IdVaiTro) VALUES
(@AdminId, @AdminRoleId), -- admin có vai trò Admin
(@GiaoVienId, @GiaoVienRoleId), -- giaovien1 có vai trò GiaoVien
(@SinhVien1Id, @SinhVienRoleId), -- sinhvien1 có vai trò SinhVien
(@SinhVien2Id, @SinhVienRoleId); -- sinhvien2 có vai trò SinhVien