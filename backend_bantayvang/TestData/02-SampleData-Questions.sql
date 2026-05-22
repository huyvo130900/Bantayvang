-- Dữ liệu mẫu cho Câu hỏi và Lựa chọn

-- Lấy ID của giáo viên để làm người tạo câu hỏi
DECLARE @GiaoVienId INT = (SELECT Id FROM TAIKHOAN WHERE TenDangNhap = 'giaovien1');

-- Lấy ID của các danh mục và loại câu hỏi
DECLARE @DanhMucCSharp INT = (SELECT Id FROM DANHMUCAUHOI WHERE TenDanhMuc = 'Lập trình C#');
DECLARE @DanhMucDatabase INT = (SELECT Id FROM DANHMUCAUHOI WHERE TenDanhMuc = 'Cơ sở dữ liệu');
DECLARE @DanhMucASPNET INT = (SELECT Id FROM DANHMUCAUHOI WHERE TenDanhMuc = 'ASP.NET Core');
DECLARE @LoaiTracNghiem INT = (SELECT Id FROM LOAICAUHOI WHERE TenLoai = 'Trắc nghiệm');

-- Câu hỏi C#
INSERT INTO CAUHOI (IdDanhMuc, IdLoaiCauHoi, NoiDung, Diem, DoKho, KhoaPhong, NguoiTao, NgayTao, DaXoa) VALUES
(@DanhMucCSharp, @LoaiTracNghiem, 'Từ khóa nào được sử dụng để khai báo một lớp trong C#?', 1.0, 'Dễ', 'CNTT', @GiaoVienId, GETDATE(), 0),
(@DanhMucCSharp, @LoaiTracNghiem, 'Phương thức nào được gọi đầu tiên khi một đối tượng được tạo ra?', 1.0, 'Trung bình', 'CNTT', @GiaoVienId, GETDATE(), 0),
(@DanhMucCSharp, @LoaiTracNghiem, 'Từ khóa nào được sử dụng để kế thừa từ một lớp khác?', 1.0, 'Dễ', 'CNTT', @GiaoVienId, GETDATE(), 0);

-- Câu hỏi Database
INSERT INTO CAUHOI (IdDanhMuc, IdLoaiCauHoi, NoiDung, Diem, DoKho, KhoaPhong, NguoiTao, NgayTao, DaXoa) VALUES
(@DanhMucDatabase, @LoaiTracNghiem, 'Lệnh SQL nào được sử dụng để lấy dữ liệu từ bảng?', 1.0, 'Dễ', 'CNTT', @GiaoVienId, GETDATE(), 0),
(@DanhMucDatabase, @LoaiTracNghiem, 'Khóa chính (Primary Key) có thể có giá trị NULL không?', 1.0, 'Dễ', 'CNTT', @GiaoVienId, GETDATE(), 0);

-- Câu hỏi ASP.NET Core
INSERT INTO CAUHOI (IdDanhMuc, IdLoaiCauHoi, NoiDung, Diem, DoKho, KhoaPhong, NguoiTao, NgayTao, DaXoa) VALUES
(@DanhMucASPNET, @LoaiTracNghiem, 'File nào chứa cấu hình chính của ứng dụng ASP.NET Core?', 1.0, 'Trung bình', 'CNTT', @GiaoVienId, GETDATE(), 0),
(@DanhMucASPNET, @LoaiTracNghiem, 'Attribute nào được sử dụng để định nghĩa route cho Controller?', 1.0, 'Trung bình', 'CNTT', @GiaoVienId, GETDATE(), 0);

-- Lấy ID của các câu hỏi vừa tạo
DECLARE @CauHoi1Id INT = (SELECT Id FROM CAUHOI WHERE NoiDung LIKE '%khai báo một lớp trong C#%');
DECLARE @CauHoi2Id INT = (SELECT Id FROM CAUHOI WHERE NoiDung LIKE '%được gọi đầu tiên khi một đối tượng%');
DECLARE @CauHoi3Id INT = (SELECT Id FROM CAUHOI WHERE NoiDung LIKE '%kế thừa từ một lớp khác%');
DECLARE @CauHoi4Id INT = (SELECT Id FROM CAUHOI WHERE NoiDung LIKE '%lấy dữ liệu từ bảng%');
DECLARE @CauHoi5Id INT = (SELECT Id FROM CAUHOI WHERE NoiDung LIKE '%Primary Key%');
DECLARE @CauHoi6Id INT = (SELECT Id FROM CAUHOI WHERE NoiDung LIKE '%cấu hình chính của ứng dụng ASP.NET Core%');
DECLARE @CauHoi7Id INT = (SELECT Id FROM CAUHOI WHERE NoiDung LIKE '%định nghĩa route cho Controller%');

-- Lựa chọn cho câu hỏi 1: "Từ khóa nào được sử dụng để khai báo một lớp trong C#?"
INSERT INTO LUACHON (IdCauHoi, NoiDung, LaDapAnDung, ThuTu) VALUES
(@CauHoi1Id, 'class', 1, 1),
(@CauHoi1Id, 'struct', 0, 2),
(@CauHoi1Id, 'interface', 0, 3),
(@CauHoi1Id, 'enum', 0, 4);

-- Lựa chọn cho câu hỏi 2: "Phương thức nào được gọi đầu tiên khi một đối tượng được tạo ra?"
INSERT INTO LUACHON (IdCauHoi, NoiDung, LaDapAnDung, ThuTu) VALUES
(@CauHoi2Id, 'Constructor', 1, 1),
(@CauHoi2Id, 'Destructor', 0, 2),
(@CauHoi2Id, 'Main', 0, 3),
(@CauHoi2Id, 'Initialize', 0, 4);

-- Lựa chọn cho câu hỏi 3: "Từ khóa nào được sử dụng để kế thừa từ một lớp khác?"
INSERT INTO LUACHON (IdCauHoi, NoiDung, LaDapAnDung, ThuTu) VALUES
(@CauHoi3Id, 'extends', 0, 1),
(@CauHoi3Id, 'inherits', 0, 2),
(@CauHoi3Id, ':', 1, 3),
(@CauHoi3Id, 'implements', 0, 4);

-- Lựa chọn cho câu hỏi 4: "Lệnh SQL nào được sử dụng để lấy dữ liệu từ bảng?"
INSERT INTO LUACHON (IdCauHoi, NoiDung, LaDapAnDung, ThuTu) VALUES
(@CauHoi4Id, 'SELECT', 1, 1),
(@CauHoi4Id, 'GET', 0, 2),
(@CauHoi4Id, 'FETCH', 0, 3),
(@CauHoi4Id, 'RETRIEVE', 0, 4);

-- Lựa chọn cho câu hỏi 5: "Khóa chính (Primary Key) có thể có giá trị NULL không?"
INSERT INTO LUACHON (IdCauHoi, NoiDung, LaDapAnDung, ThuTu) VALUES
(@CauHoi5Id, 'Có', 0, 1),
(@CauHoi5Id, 'Không', 1, 2),
(@CauHoi5Id, 'Tùy thuộc vào DBMS', 0, 3),
(@CauHoi5Id, 'Chỉ trong một số trường hợp', 0, 4);

-- Lựa chọn cho câu hỏi 6: "File nào chứa cấu hình chính của ứng dụng ASP.NET Core?"
INSERT INTO LUACHON (IdCauHoi, NoiDung, LaDapAnDung, ThuTu) VALUES
(@CauHoi6Id, 'web.config', 0, 1),
(@CauHoi6Id, 'appsettings.json', 1, 2),
(@CauHoi6Id, 'config.json', 0, 3),
(@CauHoi6Id, 'settings.xml', 0, 4);

-- Lựa chọn cho câu hỏi 7: "Attribute nào được sử dụng để định nghĩa route cho Controller?"
INSERT INTO LUACHON (IdCauHoi, NoiDung, LaDapAnDung, ThuTu) VALUES
(@CauHoi7Id, '[Route]', 1, 1),
(@CauHoi7Id, '[Path]', 0, 2),
(@CauHoi7Id, '[Url]', 0, 3),
(@CauHoi7Id, '[Mapping]', 0, 4);