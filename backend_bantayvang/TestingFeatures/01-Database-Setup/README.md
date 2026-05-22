# Database Setup & Verification

## 🎯 Mục tiêu
Thiết lập database với dữ liệu mẫu và verify tất cả hoạt động đúng.

## 📋 Checklist Setup

### Bước 1: Chạy SQL Scripts
```sql
-- Chạy theo thứ tự trong SQL Server Management Studio:
-- 1. TestData/01-SampleData-Categories-Types.sql
-- 2. TestData/02-SampleData-Questions.sql  
-- 3. TestData/03-SampleData-Exams.sql
```

### Bước 2: Verify Data
Chạy file `verify-data.sql` để kiểm tra dữ liệu đã được tạo đúng.

### Bước 3: Test Connection
Chạy API server và test connection string.

## 🔧 Troubleshooting

### Lỗi Foreign Key
- Kiểm tra thứ tự chạy SQL scripts
- Verify ID references đúng

### Lỗi Connection String  
- Kiểm tra SQL Server đang chạy
- Verify database name và credentials

### Lỗi Table Names
- Kiểm tra tên bảng trong DbContext
- So sánh với tên bảng thực tế trong database

## ✅ Expected Results

Sau khi setup thành công:
- 5 danh mục câu hỏi
- 4 loại câu hỏi  
- 4 tài khoản (admin, giáo viên, 2 sinh viên)
- 7 câu hỏi với lựa chọn
- 3 đề thi
- 2 bài thi mẫu