# 🏥 Hệ Thống Kiểm Tra Nội Bộ — Bàn Tay Vàng

> Hệ thống quản lý thi và kiểm tra nội bộ dành cho các cơ sở y tế, hỗ trợ toàn bộ quy trình từ quản lý câu hỏi, tổ chức kỳ thi, đến chấm điểm và thống kê kết quả.

---

## 📋 Mục Lục

- [Giới thiệu](#-giới-thiệu)
- [Tính năng chính](#-tính-năng-chính)
- [Phân quyền người dùng](#-phân-quyền-người-dùng)
- [Tài khoản mặc định](#-tài-khoản-mặc-định)
- [Cài đặt và chạy](#-cài-đặt-và-chạy)
- [Cấu hình](#-cấu-hình)
- [API Documentation](#-api-documentation)
- [Cơ sở dữ liệu](#-cơ-sở-dữ-liệu)

---

## 🎯 Giới Thiệu

**Bàn Tay Vàng** là hệ thống thi trắc nghiệm nội bộ được xây dựng cho môi trường bệnh viện / cơ sở y tế. Hệ thống cho phép:

- Tổ chức các **kỳ thi** theo 1-n khoa/phòng, hoặc theo danh sách thí sinh được chỉ định riêng (ví dụ: _Kỳ thi Bàn tay vàng Q2/2026_, _Kiểm soát nhiễm khuẩn 2026_)
- Quản lý **ngân hàng câu hỏi** theo danh mục và khoa/phòng
- Tạo và phân công **đề thi** cho nhân viên, sinh đề tự động không trùng lặp
- Cho **thí sinh ngoài bệnh viện** tự đăng ký dự thi, chờ duyệt
- Chế độ **luyện tập** tồn tại vĩnh viễn để học viên tự ôn tập, không tính vào thống kê chính thức
- Thực hiện thi **online thời gian thực** với giám sát phiên và chống gian lận
- **Chấm điểm** (thủ công và hỗ trợ AI cho câu tự luận) và xuất báo cáo Excel

---

## ✨ Tính Năng Chính

### 👨‍💼 Quản trị viên (Admin)
| Tính năng | Mô tả |
|---|---|
| Quản lý người dùng | Tạo, sửa, phân quyền, đặt lại mật khẩu, import Excel |
| Quản lý khoa/phòng | Cấu trúc phòng ban, import Excel |
| Quản lý câu hỏi | Soạn thảo, phân loại, import từ Excel/Word |
| Quản lý đề thi | Tạo đề, xáo trộn câu hỏi, cấu hình thời gian |
| Quản lý kỳ thi | Gán 1-n khoa hoặc danh sách chỉ định, theo dõi trạng thái, chế độ luyện tập |
| Duyệt đăng ký thi | Duyệt/từ chối đơn đăng ký của thí sinh ngoài |
| Phân công thi | Giao đề thi cho người dùng hoặc nhóm, gia hạn giờ làm bài |
| Chấm điểm | Xem kết quả, chấm tự luận (thủ công/AI), xử lý phúc khảo |
| Thống kê | Dashboard tổng hợp, biểu đồ kết quả |
| Nhật ký thao tác | Audit log đầy đủ mọi hành động |
| Thông báo | Gửi thông báo realtime đến người dùng |

### 🏥 Trưởng khoa/phòng (Dept Manager)
| Tính năng | Mô tả |
|---|---|
| Ngân hàng câu hỏi | Quản lý câu hỏi của khoa mình |
| Kỳ thi | Tạo và theo dõi kỳ thi thuộc khoa |
| Duyệt đăng ký thi | Duyệt đơn đăng ký thuộc khoa mình |
| Kết quả thi | Xem kết quả nhân viên trong khoa |
| Chấm điểm | Hỗ trợ chấm và xem lại bài thi |
| Thông báo | Nhận thông báo từ hệ thống |

### 👨‍⚕️ Nhân viên / Thí sinh ngoài (Student / Thí sinh ngoài)
| Tính năng | Mô tả |
|---|---|
| Đăng ký dự thi | Dành cho thí sinh ngoài bệnh viện, chờ duyệt |
| Phòng chờ thi | Xem kỳ thi có thể thi ngay, sắp diễn ra, và khu luyện tập |
| Làm bài thi | Giao diện thi toàn màn hình, tự lưu câu trả lời |
| Xem kết quả | Kết quả sau khi nộp bài (nếu đã được công bố) |
| Thông báo | Nhận thông báo từ quản trị viên |

---

## 👥 Phân Quyền Người Dùng

| Role | Giá trị | Mô tả |
|---|---|---|
| Admin | 1 | Quản trị viên hệ thống — toàn quyền |
| DeptManager | 5 | Trưởng khoa/phòng — quản lý khoa của mình |
| Student | 3 | Nhân viên nội bộ — làm bài thi |
| ThiSinhNgoai | 6 | Thí sinh ngoài bệnh viện — tự đăng ký, làm bài thi |

> **Lưu ý:** Các role Teacher (2) và Supervisor (4) đã bị loại bỏ, không còn sử dụng.

### Phân quyền theo route (frontend)

| URL | Quyền truy cập |
|---|---|
| /login, /dang-ky, /quen-mat-khau | Public |
| /admin/* | Admin |
| /dept-manager/* | DeptManager |
| /exam-waiting, /dashboard (student) | Student, ThiSinhNgoai |
| /exam/:examSubmissionId | Student, ThiSinhNgoai |
| /exam-result/:examSubmissionId | Đã xác thực |

---

## 🔑 Tài Khoản Mặc Định

### Tài khoản Admin được tạo tự động

Khi khởi động backend **lần đầu tiên**, hệ thống sẽ **tự động kiểm tra** và tạo tài khoản Admin mặc định nếu chưa tồn tại. Logic này được thực hiện trong `Program.cs` trước khi ứng dụng bắt đầu nhận request.

| Trường | Giá trị |
|---|---|
| **Tên đăng nhập** | `admin` |
| **Mật khẩu** | `admin123` |
| **Họ tên** | Quản trị viên hệ thống |
| **Role** | Admin (toàn quyền) |

> [!CAUTION]
> **Bắt buộc đổi mật khẩu** ngay sau khi đăng nhập lần đầu trong môi trường production. Mật khẩu mặc định `admin123` chỉ dùng cho mục đích khởi tạo hệ thống.

### Cơ chế hoạt động

Mỗi lần khởi động, hệ thống kiểm tra:
```
Nếu chưa có tài khoản nào có role Admin trong database
    → Tự động tạo tài khoản admin với mật khẩu đã được mã hoá BCrypt
Nếu đã có admin
    → Bỏ qua, không tạo thêm
```

Tài khoản chỉ được tạo **một lần duy nhất** — các lần khởi động sau sẽ không ghi đè lên tài khoản hiện có.

---

## 🚀 Cài Đặt Và Chạy

### Yêu Cầu Hệ Thống

- **Backend:** .NET 8 SDK, SQL Server 2019+
- **Frontend:** Node.js 18+, npm

---

### 1. Cài đặt Database

Kết nối vào SQL Server và chạy lần lượt:

```sql
-- Bước 1: Tạo cấu trúc database
-- Chạy file: database.sql

-- Bước 2 (tuỳ chọn): Seed dữ liệu mẫu
-- Chạy file: clean_and_seed.sql
```

> Schema hiện tại được quản lý qua EF Core Migrations (`backend_bantayvang/BanTayVang.API/Migrations`). Nếu đã có database từ trước, chạy `dotnet ef database update` trong thư mục `BanTayVang.API` để đồng bộ thay vì chạy lại `database.sql`.

### 2. Cài đặt Backend

```bash
# Di chuyển vào thư mục backend
cd backend_bantayvang

# Khôi phục package
dotnet restore

# Áp dụng migration (tạo/cập nhật schema)
dotnet ef database update --project BanTayVang.API --startup-project BanTayVang.API

# Chạy API
dotnet run --project BanTayVang.API
```

API sẽ chạy tại: https://localhost:7xxx (port hiển thị trên console)

### 3. Cài đặt Frontend

```bash
# Di chuyển vào thư mục frontend
cd bantayvang-fe

# Cài đặt dependencies
npm install

# Chạy development server
npm run dev
```

Frontend sẽ chạy tại: http://localhost:5173

---

## ⚙️ Cấu Hình

### Backend — appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=HeThongBanTayVang;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
  },
  "JwtSettings": {
    "SecretKey": "YOUR-SECRET-KEY-AT-LEAST-256-BITS",
    "Issuer": "BanTayVang.API",
    "Audience": "BanTayVang.Client",
    "AccessTokenExpirationMinutes": 60,
    "RefreshTokenExpirationDays": 30,
    "RememberMeExpirationDays": 90,
    "RequireHttps": true
  },
  "EmailSettings": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUsername": "your-email@gmail.com",
    "SmtpPassword": "your-app-password",
    "FromEmail": "noreply@bantayvang.vn",
    "FromName": "He thong Kiem tra noi bo",
    "EnableEmailSending": false
  }
}
```

### Frontend — .env

```env
VITE_API_URL=https://localhost:7xxx
```

---

## 📚 API Documentation

Sau khi chạy backend, truy cập Swagger UI tại:

```
https://localhost:7xxx/swagger
```

### Các nhóm API chính

| Nhóm | Endpoint prefix | Mô tả |
|---|---|---|
| Xác thực | /api/Auth | Đăng nhập, refresh token, đổi mật khẩu |
| Người dùng | /api/User | CRUD người dùng, import Excel |
| Khoa/Phòng | /api/Department | Quản lý cơ cấu tổ chức |
| Câu hỏi | /api/Question | Ngân hàng câu hỏi, import Excel/Word |
| Danh mục câu hỏi | /api/Category | Loại câu hỏi (trắc nghiệm/tự luận...) |
| Đề thi | /api/Exam | Tạo, sửa, bắt đầu/nộp bài thi |
| Kỳ thi | /api/ExamCampaign | Tổ chức kỳ thi, sinh đề tự động, gán khoa/danh sách |
| Đăng ký thi | /api/ExamRegistration | Đăng ký công khai + duyệt cho thí sinh ngoài |
| Phân công | /api/ExamAssignment | Giao đề thi, gia hạn giờ |
| Chấm điểm | /api/Grading | Kết quả và chấm điểm |
| Thống kê | /api/Statistics | Số liệu tổng hợp |
| Thông báo | /api/Notification | Gửi và nhận thông báo |
| Nhật ký | /api/AuditLog | Lịch sử thao tác |
| Upload | /api/Upload | Upload ảnh/file đính kèm |

> **Xác thực API:** Tất cả API (trừ `/api/Auth/login` và `/api/ExamRegistration` công khai) yêu cầu JWT Bearer Token trong header Authorization.

---

## 🗄️ Cơ Sở Dữ Liệu

### Các bảng chính

| Bảng | Mô tả |
|---|---|
| Users | Tài khoản người dùng |
| Roles | Danh sách role |
| UserRoles | Phân quyền người dùng |
| Departments | Khoa/phòng ban |
| Questions | Câu hỏi thi |
| QuestionCategories | Loại câu hỏi |
| QuestionOptions | Đáp án lựa chọn |
| ExamPapers | Đề thi |
| ExamPaperQuestions | Câu hỏi trong đề thi |
| ExamCampaigns | Kỳ thi |
| ExamCampaignDepartments | Khoa được gán cho kỳ thi (1 kỳ thi - n khoa) |
| ExamRegistrations | Đơn đăng ký dự thi (thí sinh ngoài) |
| ExamAssignments | Phân công thi theo danh sách chỉ định |
| ExamSubmissions | Bài thi của thí sinh |
| SubmissionDetails | Chi tiết câu trả lời |
| CheatWarnings | Cảnh báo gian lận |
| AuditLogs | Nhật ký thao tác |
| Notifications | Thông báo |
| RefreshTokens | JWT Refresh Token |
| LoginSessions / UserSessions | Phiên đăng nhập |
| EmailVerificationCodes | Mã OTP xác thực email |

---

## 🔒 Bảo Mật

- **JWT Authentication** với Access Token (60 phút) và Refresh Token (30 ngày)
- **BCrypt** mã hoá mật khẩu
- **Role-based Authorization** kiểm tra quyền truy cập theo từng API
- **Audit Logging** ghi lại toàn bộ thao tác quan trọng
- **Phát hiện gian lận** theo dõi chuyển tab, mất focus khi thi
- **HTTPS** bắt buộc trong môi trường production

---

## 📊 Tính Năng Realtime (SignalR)

Hệ thống sử dụng **SignalR** để cung cấp:
- Thông báo realtime khi có bài thi mới
- Cập nhật trạng thái phòng thi, giám sát thí sinh đang làm bài
- Đồng bộ kết quả ngay sau khi nộp bài

---

## 📄 License

Dự án này được phát triển cho mục đích thực tập và nội bộ. Không phát hành công khai.

---

Được phát triển với yêu cho he thong y te Viet Nam
