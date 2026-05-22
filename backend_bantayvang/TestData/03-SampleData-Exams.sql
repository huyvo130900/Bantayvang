-- Dữ liệu mẫu cho Đề thi và Bài thi

-- Lấy ID của giáo viên để làm người tạo đề thi
DECLARE @GiaoVienId INT = (SELECT Id FROM TAIKHOAN WHERE TenDangNhap = 'giaovien1');

-- Tạo đề thi mẫu
INSERT INTO DETHI (MaDeThi, TenDeThi, ThoiGianLamBai, TongDiem, ThoiGianBatDau, LinkTruyCap, TrangThai, NguoiTao, NgayTao) VALUES
('CSHARP001', 'Kiểm tra C# cơ bản', 30, 5.0, DATEADD(HOUR, -1, GETDATE()), '/exam/CSHARP001', 'Active', @GiaoVienId, GETDATE()),
('DATABASE001', 'Kiểm tra cơ sở dữ liệu', 45, 3.0, DATEADD(HOUR, -2, GETDATE()), '/exam/DATABASE001', 'Active', @GiaoVienId, GETDATE()),
('ASPNET001', 'Kiểm tra ASP.NET Core', 60, 7.0, DATEADD(DAY, 1, GETDATE()), '/exam/ASPNET001', 'Draft', @GiaoVienId, GETDATE());

-- Lấy ID của các đề thi vừa tạo
DECLARE @DeThi1Id INT = (SELECT Id FROM DETHI WHERE MaDeThi = 'CSHARP001');
DECLARE @DeThi2Id INT = (SELECT Id FROM DETHI WHERE MaDeThi = 'DATABASE001');
DECLARE @DeThi3Id INT = (SELECT Id FROM DETHI WHERE MaDeThi = 'ASPNET001');

-- Lấy ID của các câu hỏi
DECLARE @CauHoi1Id INT = (SELECT Id FROM CAUHOI WHERE NoiDung LIKE '%khai báo một lớp trong C#%');
DECLARE @CauHoi2Id INT = (SELECT Id FROM CAUHOI WHERE NoiDung LIKE '%được gọi đầu tiên khi một đối tượng%');
DECLARE @CauHoi3Id INT = (SELECT Id FROM CAUHOI WHERE NoiDung LIKE '%kế thừa từ một lớp khác%');
DECLARE @CauHoi4Id INT = (SELECT Id FROM CAUHOI WHERE NoiDung LIKE '%lấy dữ liệu từ bảng%');
DECLARE @CauHoi5Id INT = (SELECT Id FROM CAUHOI WHERE NoiDung LIKE '%Primary Key%');
DECLARE @CauHoi6Id INT = (SELECT Id FROM CAUHOI WHERE NoiDung LIKE '%cấu hình chính của ứng dụng ASP.NET Core%');
DECLARE @CauHoi7Id INT = (SELECT Id FROM CAUHOI WHERE NoiDung LIKE '%định nghĩa route cho Controller%');

-- Thêm câu hỏi vào đề thi CSHARP001
INSERT INTO DETHI_CAUHOI (IdDeThi, IdCauHoi, TrongSo) VALUES
(@DeThi1Id, @CauHoi1Id, 1.0), -- Câu hỏi về class
(@DeThi1Id, @CauHoi2Id, 1.0), -- Câu hỏi về constructor
(@DeThi1Id, @CauHoi3Id, 1.0), -- Câu hỏi về kế thừa
(@DeThi1Id, @CauHoi6Id, 1.0), -- Câu hỏi về appsettings.json
(@DeThi1Id, @CauHoi7Id, 1.0); -- Câu hỏi về Route attribute

-- Thêm câu hỏi vào đề thi DATABASE001
INSERT INTO DETHI_CAUHOI (IdDeThi, IdCauHoi, TrongSo) VALUES
(@DeThi2Id, @CauHoi4Id, 1.0), -- Câu hỏi về SELECT
(@DeThi2Id, @CauHoi5Id, 1.0), -- Câu hỏi về Primary Key
(@DeThi2Id, @CauHoi1Id, 1.0); -- Câu hỏi về class (để test đa dạng)

-- Thêm câu hỏi vào đề thi ASPNET001
INSERT INTO DETHI_CAUHOI (IdDeThi, IdCauHoi, TrongSo) VALUES
(@DeThi3Id, @CauHoi6Id, 1.0), -- Câu hỏi về appsettings.json
(@DeThi3Id, @CauHoi7Id, 1.0), -- Câu hỏi về Route
(@DeThi3Id, @CauHoi1Id, 1.0), -- Câu hỏi về class
(@DeThi3Id, @CauHoi2Id, 1.0), -- Câu hỏi về constructor
(@DeThi3Id, @CauHoi3Id, 1.0), -- Câu hỏi về kế thừa
(@DeThi3Id, @CauHoi4Id, 1.0), -- Câu hỏi về SELECT
(@DeThi3Id, @CauHoi5Id, 1.0); -- Câu hỏi về Primary Key

-- Lấy ID của sinh viên
DECLARE @SinhVien1Id INT = (SELECT Id FROM TAIKHOAN WHERE TenDangNhap = 'sinhvien1');
DECLARE @SinhVien2Id INT = (SELECT Id FROM TAIKHOAN WHERE TenDangNhap = 'sinhvien2');

-- Tạo bài thi mẫu (sinh viên đã làm)
INSERT INTO BAITHI (IdTaiKhoan, IdDeThi, TrangThai, MaDeThi, ThoiGianNop, TongDiem, SoCauDung, TongSoCau, TongSoCanhBao) VALUES
(@SinhVien1Id, @DeThi1Id, 'Completed', 'CSHARP001', DATEADD(MINUTE, -10, GETDATE()), 4.0, 4, 5, 1),
(@SinhVien2Id, @DeThi1Id, 'InProgress', 'CSHARP001', NULL, NULL, NULL, 5, 0);

-- Lấy ID của bài thi vừa tạo
DECLARE @BaiThi1Id INT = (SELECT Id FROM BAITHI WHERE IdTaiKhoan = @SinhVien1Id AND IdDeThi = @DeThi1Id);
DECLARE @BaiThi2Id INT = (SELECT Id FROM BAITHI WHERE IdTaiKhoan = @SinhVien2Id AND IdDeThi = @DeThi1Id);

-- Lấy ID của các lựa chọn đúng
DECLARE @LuaChon1Dung INT = (SELECT Id FROM LUACHON WHERE IdCauHoi = @CauHoi1Id AND LaDapAnDung = 1);
DECLARE @LuaChon2Dung INT = (SELECT Id FROM LUACHON WHERE IdCauHoi = @CauHoi2Id AND LaDapAnDung = 1);
DECLARE @LuaChon3Dung INT = (SELECT Id FROM LUACHON WHERE IdCauHoi = @CauHoi3Id AND LaDapAnDung = 1);
DECLARE @LuaChon6Dung INT = (SELECT Id FROM LUACHON WHERE IdCauHoi = @CauHoi6Id AND LaDapAnDung = 1);
DECLARE @LuaChon7Sai INT = (SELECT TOP 1 Id FROM LUACHON WHERE IdCauHoi = @CauHoi7Id AND LaDapAnDung = 0);

-- Chi tiết làm bài cho sinh viên 1 (đã hoàn thành)
INSERT INTO CHITIETLAMBAI (IdBaiThi, IdCauHoi, IdLuaChonDaChon, ThoiGianTraLoi, DaLuu, DiemDatDuoc) VALUES
(@BaiThi1Id, @CauHoi1Id, @LuaChon1Dung, DATEADD(MINUTE, -25, GETDATE()), 1, 1.0), -- Đúng: class
(@BaiThi1Id, @CauHoi2Id, @LuaChon2Dung, DATEADD(MINUTE, -20, GETDATE()), 1, 1.0), -- Đúng: Constructor
(@BaiThi1Id, @CauHoi3Id, @LuaChon3Dung, DATEADD(MINUTE, -15, GETDATE()), 1, 1.0), -- Đúng: :
(@BaiThi1Id, @CauHoi6Id, @LuaChon6Dung, DATEADD(MINUTE, -10, GETDATE()), 1, 1.0), -- Đúng: appsettings.json
(@BaiThi1Id, @CauHoi7Id, @LuaChon7Sai, DATEADD(MINUTE, -5, GETDATE()), 1, 0.0);  -- Sai: chọn đáp án sai

-- Chi tiết làm bài cho sinh viên 2 (đang làm)
INSERT INTO CHITIETLAMBAI (IdBaiThi, IdCauHoi, IdLuaChonDaChon, ThoiGianTraLoi, DaLuu, DiemDatDuoc) VALUES
(@BaiThi2Id, @CauHoi1Id, @LuaChon1Dung, DATEADD(MINUTE, -5, GETDATE()), 1, NULL), -- Đã trả lời câu 1
(@BaiThi2Id, @CauHoi2Id, @LuaChon2Dung, DATEADD(MINUTE, -3, GETDATE()), 1, NULL); -- Đã trả lời câu 2

-- Cảnh báo gian lận mẫu
INSERT INTO CANHBAOGIANLAN (IdBaiThi, LoaiCanhBao, MoTa, ThoiGian, SoLanViPham) VALUES
(@BaiThi1Id, 'TAB_SWITCH', 'Thí sinh chuyển tab trong quá trình làm bài', DATEADD(MINUTE, -15, GETDATE()), 1),
(@BaiThi2Id, 'COPY_PASTE', 'Phát hiện hành vi copy/paste', DATEADD(MINUTE, -2, GETDATE()), 1);