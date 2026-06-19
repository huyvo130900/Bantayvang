# 🏥 Hệ Thống Kiểm Tra Nội Bộ — Bàn Tay Vàng

> Hệ thống quản lý thi và kiểm tra nội bộ dành cho các cơ sở y tế, hỗ trợ toàn bộ quy trình từ quản lý câu hỏi, tổ chức kỳ thi, đến chấm điểm và thống kê kết quả.

---

## 📋 Mục Lục

- [Giới thiệu](#-giới-thiệu)
- [Tính năng chính](#-tính-năng-chính)
- [Kiến trúc hệ thống](#-kiến-trúc-hệ-thống)
- [Công nghệ sử dụng](#-công-nghệ-sử-dụng)
- [Cấu trúc thư mục](#-cấu-trúc-thư-mục)
- [Phân quyền người dùng](#-phân-quyền-người-dùng)
- [Tài khoản mặc định](#-tài-khoản-mặc-định)
- [Cài đặt và chạy](#-cài-đặt-và-chạy)
- [Cấu hình](#-cấu-hình)
- [API Documentation](#-api-documentation)
- [Cơ sở dữ liệu](#-cơ-sở-dữ-liệu)

---

## 🎯 Giới Thiệu

**Bàn Tay Vàng** là hệ thống thi trắc nghiệm nội bộ được xây dựng cho môi trường bệnh viện / cơ sở y tế. Hệ thống cho phép:

- Tổ chức các **kỳ thi** theo từng khoa/phòng (ví dụ: _Kỳ thi Bàn tay vàng Q2/2026_, _Kiểm soát nhiễm khuẩn 2026_)
- Quản lý **ngân hàng câu hỏi** theo danh mục và khoa/phòng
- Tạo và phân công **đề thi** cho nhân viên
- Thực hiện thi **online thời gian thực** với giám sát phiên
- **Chấm điểm tự động** và xuất báo cáo Excel

---

## ✨ Tính Năng Chính

### 👨‍💼 Quản trị viên (Admin)
| Tính năng | Mô tả |
|---|---|
| Quản lý người dùng | Tạo, sửa, phân quyền, đặt lại mật khẩu |
| Quản lý khoa/phòng | Cấu trúc phòng ban theo cây phân cấp |
| Quản lý câu hỏi | Soạn thảo, phân loại, import từ Excel |
| Quản lý đề thi | Tạo đề, xáo trộn câu hỏi, cấu hình thời gian |
| Quản lý kỳ thi | Tổ chức nhiều ca thi, theo dõi trạng thái |
| Phân công thi | Giao đề thi cho người dùng hoặc nhóm |
| Chấm điểm | Xem kết quả, xử lý phúc khảo |
| Thống kê | Dashboard tổng hợp, biểu đồ kết quả |
| Nhật ký thao tác | Audit log đầy đủ mọi hành động |
| Thông báo | Gửi thông báo realtime đến người dùng |

### 🏥 Trưởng khoa/phòng (Dept Manager)
| Tính năng | Mô tả |
|---|---|
| Ngân hàng câu hỏi | Quản lý câu hỏi của khoa mình |
| Kỳ thi | Xem và theo dõi kỳ thi thuộc khoa |
| Kết quả thi | Xem kết quả nhân viên trong khoa |
| Chấm điểm | Hỗ trợ chấm và xem lại bài thi |
| Thông báo | Nhận thông báo từ hệ thống |

### 👨‍⚕️ Nhân viên (Student)
| Tính năng | Mô tả |
|---|---|
| Phòng chờ thi | Xem các bài thi được phân công |
| Làm bài thi | Giao diện thi trắc nghiệm toàn màn hình |
| Xem kết quả | Kết quả sau khi nộp bài |
| Thông báo | Nhận thông báo từ quản trị viên |

---

## 🏗️ Kiến Trúc Hệ Thống

`
+----------------------------------------------------------+
|                    CLIENT (Browser)                      |
|              React + TypeScript + Vite                   |
|                  Port: 5173 (dev)                        |
+---------------------------+------------------------------+
                            | HTTP / WebSocket (SignalR)
+---------------------------v------------------------------+
|                   BACKEND API                            |
|            ASP.NET Core 8 Web API                        |
|                  Port: 7xxx (HTTPS)                      |
|                                                          |
|  Controllers -> Services -> Repositories -> DB Context   |
|                       SignalR Hubs                       |
+---------------------------+------------------------------+
                            | Entity Framework Core
+---------------------------v------------------------------+
|              SQL Server Database                         |
|           HeThongBanTayVang (DB Name)                    |
+----------------------------------------------------------+
`

---

## 🛠️ Công Nghệ Sử Dụng

### Backend
| Thành phần | Công nghệ |
|---|---|
| Framework | ASP.NET Core 8.0 |
| ORM | Entity Framework Core 8 + SQL Server |
| Xác thực | JWT Bearer + Refresh Token |
| Mã hoá mật khẩu | BCrypt.Net |
| Realtime | SignalR |
| Mapping | AutoMapper 12 |
| Validation | FluentValidation |
| Excel | ClosedXML, ExcelDataReader |
| Email | MailKit |
| Tài liệu API | Swagger / OpenAPI |

### Frontend
| Thành phần | Công nghệ |
|---|---|
| Framework | React 19 + TypeScript |
| Build tool | Vite 8 |
| Styling | TailwindCSS 4 |
| State management | Redux Toolkit |
| Routing | React Router DOM v7 |
| HTTP Client | Axios |
| Form | React Hook Form + Zod |
| Realtime | @microsoft/signalr |
| Icons | Lucide React |
| Testing | Playwright |

---

## 📁 Cấu Trúc Thư Mục

`
Bantayvang/
├── backend_bantayvang/                 # Backend ASP.NET Core
│   └── BanTayVang.API/
│       ├── Controllers/                # API endpoints
│       │   ├── AuthController.cs       # Đăng nhập, token
│       │   ├── UserController.cs       # Quản lý người dùng
│       │   ├── DepartmentController.cs # Khoa/phòng
│       │   ├── CauhoiController.cs     # Câu hỏi
│       │   ├── ExamController.cs       # Đề thi
│       │   ├── KyThiController.cs      # Kỳ thi
│       │   ├── GradingController.cs    # Chấm điểm và kết quả
│       │   ├── ExamAssignmentController.cs # Phân công thi
│       │   ├── StatisticsController.cs # Thống kê
│       │   ├── NotificationController.cs   # Thông báo
│       │   ├── AuditLogController.cs   # Nhật ký thao tác
│       │   └── UploadController.cs     # Upload file
│       ├── Models/                     # Entity models
│       ├── DTOs/                       # Data Transfer Objects
│       ├── Services/                   # Business logic
│       ├── Repositories/               # Data access layer
│       ├── Hubs/                       # SignalR hubs
│       ├── Middleware/                 # Custom middleware
│       ├── Mappings/                   # AutoMapper profiles
│       ├── Configuration/              # Cấu hình JWT, Email
│       ├── BackgroundJobs/             # Background tasks
│       └── Program.cs                  # Entry point
│
├── bantayvang-fe/                      # Frontend React
│   └── src/
│       ├── app/                        # Store, Router, Providers
│       ├── features/                   # Feature-sliced design
│       │   ├── auth/                   # Đăng nhập
│       │   ├── users/                  # Người dùng
│       │   ├── departments/            # Khoa/phòng
│       │   ├── questions/              # Câu hỏi
│       │   ├── exams/                  # Đề thi
│       │   ├── ky-thi/                 # Kỳ thi
│       │   ├── exam-taking/            # Làm bài thi
│       │   ├── grading/                # Chấm điểm
│       │   ├── results-by-kythi/       # Kết quả theo kỳ thi
│       │   ├── statistics/             # Thống kê
│       │   ├── notifications/          # Thông báo
│       │   └── audit-log/              # Nhật ký
│       ├── components/                 # Shared UI components
│       ├── pages/                      # Dashboard, Waiting room
│       ├── hooks/                      # Custom React hooks
│       ├── lib/                        # Constants, utilities
│       └── types/                      # TypeScript types
│
├── database.sql                        # Schema database đầy đủ
├── clean_and_seed.sql                  # Script dọn và seed dữ liệu
└── templates_excel/                    # Template Excel import
`

---

## 👥 Phân Quyền Người Dùng

| Role | Giá trị | Mô tả |
|---|---|---|
| Admin | 1 | Quản trị viên hệ thống — toàn quyền |
| DeptManager | 5 | Trưởng khoa/phòng — quản lý khoa của mình |
| Student | 3 | Nhân viên/thí sinh — làm bài thi |

> **Lưu ý:** Các role Teacher (2) và Supervisor (4) đã bị loại bỏ, không còn sử dụng.

### Phân quyền theo route

| URL | Quyền truy cập |
|---|---|
| /login | Public |
| /admin/* | Admin, DeptManager |
| /dept-manager/* | DeptManager |
| /exam-waiting | Student |
| /exam/:id | Student |
| /exam-result/:id | Đã xác thực |

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

`sql
-- Bước 1: Tạo cấu trúc database
-- Chạy file: database.sql

-- Bước 2 (tuỳ chọn): Seed dữ liệu mẫu
-- Chạy file: clean_and_seed.sql
`

### 2. Cài đặt Backend

`ash
# Di chuyển vào thư mục backend
cd backend_bantayvang

# Khôi phục package
dotnet restore

# Chạy API
dotnet run --project BanTayVang.API
`

API sẽ chạy tại: https://localhost:7xxx (port hiển thị trên console)

### 3. Cài đặt Frontend

`ash
# Di chuyển vào thư mục frontend
cd bantayvang-fe

# Cài đặt dependencies
npm install

# Chạy development server
npm run dev
`

Frontend sẽ chạy tại: http://localhost:5173

---

## ⚙️ Cấu Hình

### Backend — appsettings.json

`json
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
`

### Frontend — .env

`env
VITE_API_URL=https://localhost:7xxx
`

---

## 📚 API Documentation

Sau khi chạy backend, truy cập Swagger UI tại:

`
https://localhost:7xxx/swagger
`

### Các nhóm API chính

| Nhóm | Endpoint prefix | Mô tả |
|---|---|---|
| Xác thực | /api/auth | Đăng nhập, refresh token, đổi mật khẩu |
| Người dùng | /api/user | CRUD người dùng |
| Khoa/Phòng | /api/department | Quản lý cơ cấu tổ chức |
| Câu hỏi | /api/cauhoi | Ngân hàng câu hỏi |
| Đề thi | /api/exam | Tạo và quản lý đề thi |
| Kỳ thi | /api/kythi | Tổ chức kỳ thi |
| Phân công | /api/exam-assignment | Giao đề thi |
| Chấm điểm | /api/grading | Kết quả và chấm điểm |
| Thống kê | /api/statistics | Số liệu tổng hợp |
| Thông báo | /api/notification | Gửi và nhận thông báo |
| Nhật ký | /api/auditlog | Lịch sử thao tác |
| Upload | /api/upload | Import Excel |

> **Xác thực API:** Tất cả API (trừ /api/auth/login) yêu cầu JWT Bearer Token trong header Authorization.

---

## 🗄️ Cơ Sở Dữ Liệu

### Các bảng chính

| Bảng | Mô tả |
|---|---|
| Taikhoan | Tài khoản người dùng |
| Vaitro | Danh sách role |
| TaikhoanVaitro | Phân quyền người dùng |
| KhoaPhong | Khoa/phòng ban |
| Cauhoi | Câu hỏi thi |
| Loaicauhoi | Loại câu hỏi |
| Luachon | Đáp án lựa chọn |
| Dethi | Đề thi |
| DethiCauhoi | Câu hỏi trong đề thi |
| KyThi | Kỳ thi |
| ExamAssignment | Phân công thi |
| Baithi | Bài thi của thí sinh |
| Chitietlambai | Chi tiết câu trả lời |
| Canhbaogianlan | Cảnh báo gian lận |
| Logthaotac | Nhật ký thao tác |
| Notification | Thông báo |
| RefreshToken | JWT Refresh Token |
| Phiendangnhap | Phiên đăng nhập |

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
- Cập nhật trạng thái phòng thi
- Đồng bộ kết quả ngay sau khi nộp bài

---

## 📄 License

Dự án này được phát triển cho mục đích thực tập và nội bộ. Không phát hành công khai.

---

Được phát triển với yêu cho he thong y te Viet Nam
