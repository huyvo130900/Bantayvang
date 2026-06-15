# 🧪 API Testing Guide - Hệ thống thi BanTayVang

## 🚀 Quick Start

### Bước 1: Chạy migration trong SSMS
File: `BanTayVang.API/Migrations/000-RUN-ALL-MIGRATIONS.sql`

### Bước 2: Restart API
```bash
cd BanTayVang.API
dotnet run
```

### Bước 3: Reset password admin
```http
POST https://localhost:7249/api/Seed/reset-admin-password
```

### Bước 4: Login để lấy token
```json
POST https://localhost:7249/api/Auth/login
{
  "username": "admin",
  "password": "admin123",
  "rememberMe": false
}
```

Copy `accessToken` từ response.

### Bước 5: Authorize Swagger
- Click `Authorize` ở góc phải Swagger UI
- Paste token (KHÔNG có "Bearer ")
- Click `Authorize` → `Close`

---

## 📂 Test Files

| File | Module | Tests |
|------|--------|-------|
| `01-test-categories.http` | Login | Lấy token |
| `02-test-questions.http` | Câu hỏi | CRUD, search, random |
| `03-test-exams.http` | Đề thi | Tạo đề, làm bài, nộp |
| `04-test-categories-types.http` | Danh mục & Loại CH | CRUD danh mục, loại |
| `05-test-users.http` | Quản lý User | CRUD users (admin) |
| `06-test-statistics.http` | Thống kê | Dashboard, ranking |
| `07-test-notifications.http` | Thông báo | CRUD, broadcast, lịch thi |
| `08-test-grading.http` | Chấm điểm | Chi tiết KQ, regrade |

---

## ✅ Module Đầy Đủ

### 🔐 Authentication (đã test)
- ✅ Register / Login / Logout
- ✅ Refresh token / Validate token
- ✅ Change password / Reset password
- ✅ Get current user

### 👥 User Management
- ✅ List users (paging, filter)
- ✅ CRUD users by admin
- ✅ Activate / Deactivate
- ✅ Reset password

### 📚 Question Management
- ✅ CRUD câu hỏi
- ✅ Filter, search, paging
- ✅ Random questions
- ✅ Soft delete

### 🗂️ Category & Type Management
- ✅ CRUD danh mục câu hỏi
- ✅ CRUD loại câu hỏi

### 📝 Exam Management
- ✅ Tạo / Update / Deactivate đề thi
- ✅ Get active exams
- ✅ Get by code

### 🎓 Online Exam
- ✅ Start exam session
- ✅ Get questions
- ✅ Save answers
- ✅ Get progress
- ✅ Submit exam

### 🛡️ Anti-Cheat
- ✅ Log warnings
- ✅ Get warnings count
- ✅ Auto-terminate when threshold reached

### 📊 Grading & Results
- ✅ Auto-grade
- ✅ Manual grade (essay)
- ✅ Re-grade
- ✅ Bảng xếp hạng

### 📈 Statistics & Dashboard
- ✅ Dashboard tổng quan
- ✅ Thống kê đề thi (pass rate, distribution)
- ✅ Lịch sử thi user
- ✅ Top performers

### 🔔 Notifications & Schedule
- ✅ User notifications
- ✅ Broadcast
- ✅ Mark as read
- ✅ Lịch đề thi sắp diễn ra
- ✅ Đề thi đang diễn ra

---

## 🐛 Troubleshooting

### 401 Unauthorized
- Token hết hạn → Login lại
- Forgot to authorize Swagger

### 500 Internal Server Error
- Check Console output Visual Studio
- DB connection lỗi
- Missing column → chạy lại migration

### "Exam has not started yet"
Update đề thi cho `ThoiGianBatDau` về quá khứ:
```sql
UPDATE DETHI 
SET ThoiGianBatDau = DATEADD(HOUR, -1, GETUTCDATE())
WHERE MaDeThi = 'TEST001';
```

---

## 🎯 Default Credentials

- **Username:** `admin`
- **Password:** `admin123`
- **Role:** Admin (IdVaiTro = 1)

⚠️ Đổi password sau khi test xong!