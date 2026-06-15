# Test Data cho BanTayVang API

Thư mục này chứa dữ liệu mẫu và file test để kiểm tra các tính năng của hệ thống thi trắc nghiệm.

## 📁 Cấu trúc file

### 1. SQL Scripts (Chạy theo thứ tự)
- `01-SampleData-Categories-Types.sql` - Danh mục câu hỏi, loại câu hỏi, tài khoản
- `02-SampleData-Questions.sql` - Câu hỏi và lựa chọn mẫu
- `03-SampleData-Exams.sql` - Đề thi và bài thi mẫu

### 2. API Test Files
- `04-Test-API-Requests.http` - File test API với Visual Studio Code REST Client

## 🚀 Hướng dẫn sử dụng

### Bước 1: Chạy SQL Scripts
```sql
-- Chạy lần lượt các file SQL trong SQL Server Management Studio hoặc Azure Data Studio
-- 1. 01-SampleData-Categories-Types.sql
-- 2. 02-SampleData-Questions.sql  
-- 3. 03-SampleData-Exams.sql
```

### Bước 2: Test API
1. Mở file `04-Test-API-Requests.http` trong VS Code
2. Cài đặt extension "REST Client" nếu chưa có
3. Chạy từng request để test các tính năng

## 📊 Dữ liệu mẫu được tạo

### Danh mục câu hỏi (5 danh mục)
- Lập trình C#
- Cơ sở dữ liệu  
- ASP.NET Core
- JavaScript
- HTML/CSS

### Loại câu hỏi (4 loại)
- Trắc nghiệm
- Đúng/Sai
- Tự luận
- Điền khuyết

### Tài khoản (4 tài khoản)
- `admin` / `admin123` - Quản trị viên
- `giaovien1` / `gv123` - Giáo viên
- `sinhvien1` / `sv123` - Sinh viên 1
- `sinhvien2` / `sv123` - Sinh viên 2

### Câu hỏi (7 câu hỏi)
- 3 câu về C#
- 2 câu về Database
- 2 câu về ASP.NET Core

### Đề thi (3 đề thi)
- `CSHARP001` - Kiểm tra C# cơ bản (Active, 30 phút)
- `DATABASE001` - Kiểm tra cơ sở dữ liệu (Active, 45 phút)
- `ASPNET001` - Kiểm tra ASP.NET Core (Draft, 60 phút)

### Bài thi mẫu
- Sinh viên 1 đã hoàn thành bài thi CSHARP001 (4/5 điểm)
- Sinh viên 2 đang làm bài thi CSHARP001 (InProgress)

## 🧪 Kịch bản test

### Test Question Management
1. ✅ Lấy danh sách câu hỏi có phân trang
2. ✅ Tìm kiếm câu hỏi theo từ khóa
3. ✅ Lọc câu hỏi theo danh mục, độ khó
4. ✅ Tạo câu hỏi mới
5. ✅ Cập nhật câu hỏi
6. ✅ Xóa mềm câu hỏi
7. ✅ Lấy câu hỏi ngẫu nhiên

### Test Exam System
1. ✅ Lấy danh sách đề thi đang hoạt động
2. ✅ Tạo đề thi mới
3. ✅ Bắt đầu làm bài thi
4. ✅ Lấy danh sách câu hỏi trong đề thi
5. ✅ Lưu câu trả lời
6. ✅ Theo dõi tiến độ làm bài
7. ✅ Nộp bài thi
8. ✅ Ghi nhận cảnh báo gian lận

### Test Anti-Cheat System
1. ✅ Ghi nhận chuyển tab
2. ✅ Ghi nhận copy/paste
3. ✅ Đếm số lượng cảnh báo
4. ✅ Tự động nộp bài khi quá nhiều cảnh báo

## 🔧 Troubleshooting

### Lỗi thường gặp
1. **Connection String**: Kiểm tra chuỗi kết nối database
2. **Foreign Key**: Đảm bảo chạy SQL scripts theo đúng thứ tự
3. **CORS**: Kiểm tra cấu hình CORS trong Program.cs
4. **SSL Certificate**: Chấp nhận certificate khi chạy HTTPS

### Debug Tips
- Sử dụng Swagger UI tại `/swagger` để test API
- Kiểm tra logs trong console khi chạy API
- Sử dụng SQL Profiler để debug database queries
- Kiểm tra Network tab trong browser để debug AJAX calls

## 📈 Performance Testing

### Load Testing với dữ liệu mẫu
- Test với 10 sinh viên cùng làm bài
- Test tạo 100 câu hỏi cùng lúc
- Test import file Excel lớn
- Test auto-submit khi hết thời gian

---

**Happy Testing! 🎯**