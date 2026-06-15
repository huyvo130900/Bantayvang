-- Fixed version: Xóa data cũ và insert lại bằng SCOPE_IDENTITY để tránh duplicate

USE HeThongBanTayVang;
GO

-- ===== Bước 1: Xóa data cũ =====
DELETE FROM CHITIETLAMBAI;
DELETE FROM CANHBAOGIANLAN;
DELETE FROM LOGTHAOTAC;
DELETE FROM BAITHI;
DELETE FROM DETHI_CAUHOI;
DELETE FROM LUACHON;
DELETE FROM CAUHOI;
DELETE FROM DETHI;
GO

-- Reset identity (optional)
DBCC CHECKIDENT ('CAUHOI', RESEED, 0);
DBCC CHECKIDENT ('LUACHON', RESEED, 0);
DBCC CHECKIDENT ('DETHI', RESEED, 0);
DBCC CHECKIDENT ('BAITHI', RESEED, 0);
GO

-- ===== Bước 2: Lấy reference IDs (dùng TOP 1 để chắc chắn không lỗi) =====
DECLARE @AdminId INT = (SELECT TOP 1 Id FROM TAIKHOAN WHERE TenDangNhap = 'admin');
DECLARE @DanhMucCSharp INT = (SELECT TOP 1 Id FROM DANHMUCAUHOI WHERE TenDanhMuc = N'Lập trình C#');
DECLARE @DanhMucDatabase INT = (SELECT TOP 1 Id FROM DANHMUCAUHOI WHERE TenDanhMuc = N'Cơ sở dữ liệu');
DECLARE @DanhMucASPNET INT = (SELECT TOP 1 Id FROM DANHMUCAUHOI WHERE TenDanhMuc = N'ASP.NET Core');
DECLARE @LoaiTracNghiem INT = (SELECT TOP 1 Id FROM LOAICAUHOI WHERE TenLoai = N'Trắc nghiệm');

PRINT 'Admin ID: ' + CAST(@AdminId AS VARCHAR);
PRINT 'DanhMuc C#: ' + CAST(@DanhMucCSharp AS VARCHAR);
PRINT 'LoaiTracNghiem: ' + CAST(@LoaiTracNghiem AS VARCHAR);

-- ===== Bước 3: Insert câu hỏi C# và lấy ID ngay sau insert =====

-- Câu hỏi 1
INSERT INTO CAUHOI (IdDanhMuc, IdLoaiCauHoi, NoiDung, Diem, DoKho, KhoaPhong, NguoiTao, NgayTao, DaXoa) 
VALUES (@DanhMucCSharp, @LoaiTracNghiem, N'Từ khóa nào được sử dụng để khai báo một lớp trong C#?', 1.0, N'Dễ', 'CNTT', @AdminId, GETDATE(), 0);
DECLARE @CauHoi1Id INT = SCOPE_IDENTITY();

INSERT INTO LUACHON (IdCauHoi, NoiDung, LaDapAnDung, ThuTu) VALUES
(@CauHoi1Id, 'class', 1, 1),
(@CauHoi1Id, 'struct', 0, 2),
(@CauHoi1Id, 'interface', 0, 3),
(@CauHoi1Id, 'enum', 0, 4);

-- Câu hỏi 2
INSERT INTO CAUHOI (IdDanhMuc, IdLoaiCauHoi, NoiDung, Diem, DoKho, KhoaPhong, NguoiTao, NgayTao, DaXoa) 
VALUES (@DanhMucCSharp, @LoaiTracNghiem, N'Phương thức nào được gọi đầu tiên khi một đối tượng được tạo ra?', 1.0, N'Trung bình', 'CNTT', @AdminId, GETDATE(), 0);
DECLARE @CauHoi2Id INT = SCOPE_IDENTITY();

INSERT INTO LUACHON (IdCauHoi, NoiDung, LaDapAnDung, ThuTu) VALUES
(@CauHoi2Id, 'Constructor', 1, 1),
(@CauHoi2Id, 'Destructor', 0, 2),
(@CauHoi2Id, 'Main', 0, 3),
(@CauHoi2Id, 'Initialize', 0, 4);

-- Câu hỏi 3
INSERT INTO CAUHOI (IdDanhMuc, IdLoaiCauHoi, NoiDung, Diem, DoKho, KhoaPhong, NguoiTao, NgayTao, DaXoa) 
VALUES (@DanhMucCSharp, @LoaiTracNghiem, N'Từ khóa nào được sử dụng để kế thừa từ một lớp khác trong C#?', 1.0, N'Dễ', 'CNTT', @AdminId, GETDATE(), 0);
DECLARE @CauHoi3Id INT = SCOPE_IDENTITY();

INSERT INTO LUACHON (IdCauHoi, NoiDung, LaDapAnDung, ThuTu) VALUES
(@CauHoi3Id, 'extends', 0, 1),
(@CauHoi3Id, 'inherits', 0, 2),
(@CauHoi3Id, ':', 1, 3),
(@CauHoi3Id, 'implements', 0, 4);

-- Câu hỏi 4
INSERT INTO CAUHOI (IdDanhMuc, IdLoaiCauHoi, NoiDung, Diem, DoKho, KhoaPhong, NguoiTao, NgayTao, DaXoa) 
VALUES (@DanhMucDatabase, @LoaiTracNghiem, N'Lệnh SQL nào được sử dụng để lấy dữ liệu từ bảng?', 1.0, N'Dễ', 'CNTT', @AdminId, GETDATE(), 0);
DECLARE @CauHoi4Id INT = SCOPE_IDENTITY();

INSERT INTO LUACHON (IdCauHoi, NoiDung, LaDapAnDung, ThuTu) VALUES
(@CauHoi4Id, 'SELECT', 1, 1),
(@CauHoi4Id, 'GET', 0, 2),
(@CauHoi4Id, 'FETCH', 0, 3),
(@CauHoi4Id, 'RETRIEVE', 0, 4);

-- Câu hỏi 5
INSERT INTO CAUHOI (IdDanhMuc, IdLoaiCauHoi, NoiDung, Diem, DoKho, KhoaPhong, NguoiTao, NgayTao, DaXoa) 
VALUES (@DanhMucDatabase, @LoaiTracNghiem, N'Khóa chính (Primary Key) có thể có giá trị NULL không?', 1.0, N'Dễ', 'CNTT', @AdminId, GETDATE(), 0);
DECLARE @CauHoi5Id INT = SCOPE_IDENTITY();

INSERT INTO LUACHON (IdCauHoi, NoiDung, LaDapAnDung, ThuTu) VALUES
(@CauHoi5Id, N'Có', 0, 1),
(@CauHoi5Id, N'Không', 1, 2),
(@CauHoi5Id, N'Tùy thuộc vào DBMS', 0, 3),
(@CauHoi5Id, N'Chỉ trong một số trường hợp', 0, 4);

-- Câu hỏi 6
INSERT INTO CAUHOI (IdDanhMuc, IdLoaiCauHoi, NoiDung, Diem, DoKho, KhoaPhong, NguoiTao, NgayTao, DaXoa) 
VALUES (@DanhMucASPNET, @LoaiTracNghiem, N'File nào chứa cấu hình chính của ứng dụng ASP.NET Core?', 1.0, N'Trung bình', 'CNTT', @AdminId, GETDATE(), 0);
DECLARE @CauHoi6Id INT = SCOPE_IDENTITY();

INSERT INTO LUACHON (IdCauHoi, NoiDung, LaDapAnDung, ThuTu) VALUES
(@CauHoi6Id, 'web.config', 0, 1),
(@CauHoi6Id, 'appsettings.json', 1, 2),
(@CauHoi6Id, 'config.json', 0, 3),
(@CauHoi6Id, 'settings.xml', 0, 4);

-- Câu hỏi 7
INSERT INTO CAUHOI (IdDanhMuc, IdLoaiCauHoi, NoiDung, Diem, DoKho, KhoaPhong, NguoiTao, NgayTao, DaXoa) 
VALUES (@DanhMucASPNET, @LoaiTracNghiem, N'Attribute nào được sử dụng để định nghĩa route cho Controller?', 1.0, N'Trung bình', 'CNTT', @AdminId, GETDATE(), 0);
DECLARE @CauHoi7Id INT = SCOPE_IDENTITY();

INSERT INTO LUACHON (IdCauHoi, NoiDung, LaDapAnDung, ThuTu) VALUES
(@CauHoi7Id, '[Route]', 1, 1),
(@CauHoi7Id, '[Path]', 0, 2),
(@CauHoi7Id, '[Url]', 0, 3),
(@CauHoi7Id, '[Mapping]', 0, 4);

PRINT 'Đã insert 7 câu hỏi và 28 lựa chọn thành công';
GO

-- ===== Bước 4: Verify =====
SELECT COUNT(*) AS TotalQuestions FROM CAUHOI;
SELECT COUNT(*) AS TotalChoices FROM LUACHON;

SELECT TOP 5 c.Id, c.NoiDung, c.Diem, c.DoKho, dm.TenDanhMuc 
FROM CAUHOI c
INNER JOIN DANHMUCAUHOI dm ON c.IdDanhMuc = dm.Id
ORDER BY c.Id;