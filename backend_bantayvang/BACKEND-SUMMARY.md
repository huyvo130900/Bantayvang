# 📚 BANTAYVANG BACKEND - TỔNG QUAN ĐẦY ĐỦ

> Tài liệu tham khảo cho việc phát triển Frontend tương thích 100%

## 🎯 1. THÔNG TIN CHUNG

### 1.1 Stack công nghệ
- **Framework:** ASP.NET Core 8.0 Web API
- **Database:** SQL Server (`HeThongBanTayVang`)
- **ORM:** Entity Framework Core 8.0
- **Authentication:** JWT Bearer Token + Refresh Token
- **Password Hashing:** BCrypt (work factor 12)
- **Real-time:** SignalR
- **Email:** MailKit (SMTP với TLS)
- **Excel:** ClosedXML
- **Mapping:** AutoMapper
- **Validation:** FluentValidation + Data Annotations

### 1.2 Endpoints
- **Base URL:** `https://localhost:7249`
- **Swagger:** `https://localhost:7249/swagger`
- **Health Check:** `https://localhost:7249/health`
- **SignalR Hub:** `wss://localhost:7249/hubs/exam-monitor`
- **Static Files:** `https://localhost:7249/uploads/{folder}/{filename}`

### 1.3 Authentication Flow
```
1. POST /api/Auth/register hoặc /api/Auth/login
   → Server trả về { accessToken, refreshToken, user }
2. Client lưu accessToken
3. Mọi request kèm header: Authorization: Bearer {accessToken}
4. Khi accessToken hết hạn (401):
   POST /api/Auth/refresh với { refreshToken }
   → Nhận accessToken mới
5. Logout: POST /api/Auth/logout (revoke tokens)
```

### 1.4 Default Credentials
- **Username:** `admin`
- **Password:** `admin123`
- **Role ID:** 1 (Admin)

---

## 🛡️ 2. BẢO MẬT (OWASP Top 10)

| OWASP | Implementation |
|-------|----------------|
| A01: Broken Access Control | RequireAuth, RequireRole attributes, ownership checks |
| A02: Cryptographic Failures | BCrypt password, JWT HMAC SHA256, TLS for email |
| A03: Injection | Parameterized queries (EF), regex validation, XSS sanitize |
| A04: Insecure Design | Rate limiting, business limits, transaction support |
| A05: Security Misconfiguration | HTTPS only, secret key validation |
| A07: Auth Failures | JWT + Refresh tokens + session management |
| A08: Data Integrity | SHA256 checksum, file signature validation |
| A09: Security Logging | Audit log middleware, security event tracking |

### 2.1 Rate Limiting
- **Global:** 100 req/phút/IP
- **Auth endpoints:** 10 req/phút (chống brute-force)
- Response 429 khi vượt giới hạn

### 2.2 CORS
Hiện tại: `AllowAnyOrigin()` - cho dev. Production cần restrict.

---

## 📊 3. ROLES & PERMISSIONS

```
1 = Admin       - Toàn quyền
2 = Teacher     - Tạo/quản lý câu hỏi, đề thi
3 = Student     - Làm bài thi
4 = Supervisor  - Giám sát thi
```

> **Lưu ý:** Hiện tại đã xóa `[RequireAuth]` và `[RequireRole]` ở controllers để dễ test. Khi production cần re-enable.

---

## 🗂️ 4. CẤU TRÚC RESPONSE

### 4.1 Format chuẩn
```json
{
  "success": true,
  "message": "Operation successful",
  "data": { ... },
  "errors": []
}
```

### 4.2 Paged Result
```json
{
  "success": true,
  "data": {
    "items": [...],
    "pagination": {
      "pageNumber": 1,
      "pageSize": 10,
      "totalRecords": 100,
      "totalPages": 10
    }
  }
}
```

### 4.3 Error Response
```json
{
  "success": false,
  "message": "Có lỗi xảy ra",
  "errors": ["error 1", "error 2"]
}
```

---

## 📡 5. API ENDPOINTS (TỔNG HỢP)

### 5.1 🔐 Authentication (`/api/Auth`)

| Method | Endpoint | Body/Params | Mô tả |
|--------|----------|-------------|-------|
| POST | `/register` | RegisterDto | Đăng ký + auto-login |
| POST | `/login` | LoginDto | Đăng nhập, trả token |
| POST | `/refresh` | RefreshTokenDto | Refresh access token |
| POST | `/logout` | LogoutDto | Đăng xuất |
| POST | `/change-password` | ChangePasswordDto | Đổi mật khẩu |
| POST | `/request-reset` | string email | Yêu cầu reset password (gửi email) |
| POST | `/reset-password` | { token, newPassword } | Reset bằng token |
| GET | `/validate` | - | Validate token hiện tại |
| GET | `/me` | - | Thông tin user hiện tại |
| POST | `/revoke-sessions/{userId}` | - | Admin revoke all sessions |

**LoginDto:**
```json
{ "username": "admin", "password": "admin123", "rememberMe": false }
```

**RegisterDto:**
```json
{
  "username": "user01",
  "password": "Pass@123",
  "confirmPassword": "Pass@123",
  "email": "user@example.com",
  "hoTen": "Nguyễn Văn A",
  "idVaiTro": 3,
  "maNhanVien": "NV001",
  "chucDanh": "Học viên",
  "khoaPhong": "Khoa Nội"
}
```

**Response success:**
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJ...",
    "refreshToken": "abc...",
    "expiresAt": "2026-05-21T13:00:00Z",
    "tokenType": "Bearer",
    "user": {
      "id": 1, "username": "admin", "email": "...",
      "fullName": "...", "role": "Admin", "isActive": true
    }
  }
}
```

### 5.2 👥 User Management (`/api/User`)

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/` (filter, paging) | Danh sách users |
| GET | `/{id}` | Chi tiết user |
| POST | `/` | Tạo user (admin) |
| PUT | `/{id}` | Cập nhật user |
| POST | `/{id}/activate` | Kích hoạt |
| POST | `/{id}/deactivate` | Vô hiệu hóa |
| POST | `/{id}/reset-password` | Reset password |
| DELETE | `/{id}` | Soft delete (deactivate) |

**Query params:** `pageNumber`, `pageSize`, `idVaiTro`, `trangThai`, `khoaPhong`, `searchKeyword`

### 5.3 📚 Question Management (`/api/Cauhoi`)

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/` (filter) | Danh sách câu hỏi |
| GET | `/{id}` | Chi tiết câu hỏi |
| GET | `/random?count=10&danhMucId=1` | Random câu hỏi |
| POST | `/` | Tạo câu hỏi |
| PUT | `/{id}` | Cập nhật |
| DELETE | `/{id}` | Soft delete |
| POST | `/import` (multipart) | Import từ Excel |
| GET | `/import-template` | Download template Excel |

**CreateCauhoiDto:**
```json
{
  "idDanhMuc": 1,
  "idLoaiCauHoi": 1,
  "noiDung": "Câu hỏi?",
  "diem": 1.0,
  "doKho": "Dễ",
  "khoaPhong": "CNTT",
  "danhSachLuaChon": [
    { "noiDung": "A", "thuTu": 1, "laDapAnDung": true },
    { "noiDung": "B", "thuTu": 2, "laDapAnDung": false }
  ]
}
```

### 5.4 🗂️ Categories & Types (`/api/Category`)

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/categories` | Danh sách danh mục |
| GET | `/categories/{id}` | Chi tiết |
| POST | `/categories` | Tạo |
| PUT | `/categories/{id}` | Cập nhật |
| DELETE | `/categories/{id}` | Xóa |
| GET | `/types` | Danh sách loại CH |
| GET | `/types/{id}` | Chi tiết |
| POST | `/types` | Tạo |
| PUT | `/types/{id}` | Cập nhật |
| DELETE | `/types/{id}` | Xóa |

### 5.5 📝 Exam Management (`/api/Exam`)

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/active` | Đề thi đang active |
| GET | `/code/{maDeThi}` | Tìm theo mã |
| POST | `/` | Tạo đề thi |
| POST | `/start` | Bắt đầu làm bài |
| GET | `/{baithiId}/questions` | Lấy câu hỏi (đã shuffle) |
| GET | `/{baithiId}/progress` | Tiến độ làm bài |
| POST | `/answer` | Lưu 1 đáp án |
| POST | `/answer-multiple` | Lưu nhiều đáp án |
| POST | `/submit` | Nộp bài |
| POST | `/warning` | Báo cảnh báo gian lận |
| GET | `/{baithiId}/warnings` | Số cảnh báo |

**StartExamDto:** `{ "maDeThi": "TEST001" }`

**SubmitAnswerDto:**
```json
{
  "idBaiThi": 1,
  "idCauHoi": 1,
  "idLuaChonDaChon": 5,
  "cauTraLoiTuLuan": null,
  "daLuu": true
}
```

**SubmitMultipleAnswerDto:**
```json
{
  "idBaiThi": 1,
  "idCauHoi": 1,
  "idLuaChonDaChon": [1, 3, 4],
  "cauTraLoiTuLuan": null,
  "daLuu": true
}
```

### 5.6 📊 Grading & Reports (`/api/Grading`)

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/result/{baiThiId}` | Chi tiết kết quả (kèm đáp án) |
| GET | `/exam/{examId}/results` | Kết quả 1 đề thi |
| GET | `/exam/{examId}/ranking?top=50` | Bảng xếp hạng |
| POST | `/regrade/{baiThiId}` | Chấm lại |
| POST | `/manual-grade` | Chấm thủ công |
| POST | `/auto-grade-all` | Chấm tất cả |
| GET | `/exam/{examId}/export` | Export Excel kết quả |
| GET | `/exam/{examId}/ranking/export` | Export ranking |

### 5.7 📈 Statistics (`/api/Statistics`)

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/dashboard` | Tổng quan hệ thống |
| GET | `/exam/{examId}` | Thống kê đề thi |
| GET | `/user/{userId}/history` | Lịch sử thi |
| GET | `/my-history` | Lịch sử user hiện tại |
| GET | `/top-performers?top=10` | Top performers |

**DashboardDto fields:** TotalUsers, ActiveUsers, TotalQuestions, TotalExams, AverageScore, RecentActivities[]

### 5.8 🔔 Notifications (`/api/Notification`)

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/?unreadOnly=true` | Thông báo của user |
| GET | `/unread-count` | Số chưa đọc |
| POST | `/` | Tạo thông báo |
| POST | `/{id}/read` | Đánh dấu đã đọc |
| POST | `/mark-all-read` | Đọc tất cả |
| DELETE | `/{id}` | Xóa |
| POST | `/broadcast` | Gửi cho tất cả |
| GET | `/upcoming-exams` | Đề thi sắp diễn ra |
| GET | `/current-exams` | Đề thi đang/đã diễn ra |

### 5.9 📋 Exam Assignment (`/api/ExamAssignment`)

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/exam/{examId}` | Thí sinh được phân |
| GET | `/user/{userId}` | Đề thi user được phân |
| GET | `/my-exams` | Đề thi user hiện tại |
| POST | `/assign` | Phân công thí sinh |
| DELETE | `/{assignmentId}` | Hủy phân công |
| GET | `/check/{examId}/{userId}` | Check assignment |
| POST | `/extend-time` | Gia hạn thời gian |

### 5.10 🎯 Kỳ Thi & Ca Thi (`/api/KyThi`)

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/?trangThai=DangDienRa` | DS kỳ thi |
| GET | `/{id}` | Chi tiết kỳ thi |
| POST | `/` | Tạo kỳ thi |
| PUT | `/{id}` | Cập nhật |
| POST | `/{id}/status` | Đổi trạng thái |
| DELETE | `/{id}` | Xóa |
| GET | `/{kyThiId}/ca-thi` | Ca thi của kỳ |
| POST | `/ca-thi` | Tạo ca thi |
| PUT | `/ca-thi/{id}` | Cập nhật ca |
| DELETE | `/ca-thi/{id}` | Xóa ca |

**Trạng thái kỳ thi:** `DangChuanBi`, `DangDienRa`, `TamDung`, `DaKetThuc`

### 5.11 📤 Upload (`/api/Upload`)

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| POST | `/image?folder=questions` | Upload ảnh (multipart) |
| DELETE | `?fileUrl=...` | Xóa file |

**Limits:** 10MB max, formats: jpg/jpeg/png/gif/webp/bmp

### 5.12 📜 Audit Log (`/api/AuditLog`)

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| GET | `/recent?top=200` | Log gần nhất |
| GET | `/user/{userId}` | Log user |
| GET | `/exam-session/{baithiId}` | Log bài thi |
| GET | `/search?actionType=...&from=...&to=...` | Tìm kiếm |

### 5.13 🌱 Seed (Dev only) (`/api/Seed`)

| Method | Endpoint | Mô tả |
|--------|----------|-------|
| POST | `/reset-admin-password` | Reset admin về admin123 |
| GET | `/generate-hash/{password}` | Generate BCrypt hash |

---

## 🔌 6. SIGNALR REAL-TIME

### 6.1 Connection
```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("https://localhost:7249/hubs/exam-monitor", {
    accessTokenFactory: () => localStorage.getItem("accessToken")
  })
  .build();
```

### 6.2 Methods (Client → Server)
- `JoinExamMonitoring(examId)` - Supervisor join room theo exam
- `LeaveExamMonitoring(examId)`
- `JoinExamSession(baithiId)` - Student join session
- `SendHeartbeat(baithiId)` - Student báo còn online
- `JoinGlobalMonitoring()` - Supervisor xem tất cả

### 6.3 Events (Server → Client)
| Event | Payload | Khi nào |
|-------|---------|---------|
| `CheatingWarning` | `{ examId, baithiId, username, warningType, description, timestamp }` | Phát hiện gian lận |
| `ExamStarted` | `{ examId, userId, username }` | Thí sinh bắt đầu |
| `ExamSubmitted` | `{ examId, baithiId, username, score }` | Nộp bài |
| `ExamStatusChanged` | `{ examId, newStatus }` | Đổi trạng thái đề |
| `StudentProgress` | `{ examId, baithiId, answeredCount, totalCount }` | Tiến độ thí sinh |
| `StudentHeartbeat` | `{ baithiId, timestamp }` | Heartbeat |

---

## 🗄️ 7. DATABASE SCHEMA

### 7.1 Bảng cũ (gốc)
- `TAIKHOAN` - Users (đã thêm Email, HoTen, IdVaiTro, TrangThai...)
- `VAITRO` - Roles
- `TAIKHOAN_VAITRO` - User-Role mapping
- `DANHMUCAUHOI` - Categories
- `LOAICAUHOI` - Question types
- `CAUHOI` - Questions
- `LUACHON` - Choices
- `DETHI` - Exams (đã thêm ChecksumData, NguoiCapNhat, NgayCapNhat)
- `DETHI_CAUHOI` - Exam-Question mapping
- `BAITHI` - Exam sessions (đã thêm ThoiGianBatDau, NgayCapNhat, LyDoKetThuc)
- `CHITIETLAMBAI` - Answer details
- `CANHBAOGIANLAN` - Cheating warnings (đã thêm MucDoNghiemTrong, CorrelationId)
- `LOGTHAOTAC` - Audit log
- `PHIENDANGNHAP` - Old session

### 7.2 Bảng mới
- `RefreshTokens` - JWT refresh tokens
- `UserSessions` - Active sessions
- `Notifications` - User notifications
- `ExamAssignments` - Phân công thí sinh
- `KyThi` - Kỳ thi
- `CaThi` - Ca thi

### 7.3 Migration Files
Trong `BanTayVang.API/Migrations/`:
- `000-RUN-ALL-MIGRATIONS.sql` - Chạy 1 lần là đủ
- `001-Add-JWT-Authentication-Tables.sql`
- `002-Fix-Admin-User.sql`
- `003-Add-Dethi-Columns.sql`
- `004-Add-Notifications-Table.sql`
- `005-Add-ExamAssignments.sql`

---

## 🎨 8. KHUYẾN NGHỊ FRONTEND

### 8.1 Tech Stack đề xuất
- **Framework:** React 18 + TypeScript hoặc Vue 3 + TS
- **State Management:** Redux Toolkit / Zustand / Pinia
- **HTTP Client:** Axios với interceptors
- **Routing:** React Router 6 / Vue Router
- **UI Library:** Ant Design / Material-UI / Tailwind + Headless UI
- **Forms:** React Hook Form / VeeValidate
- **SignalR Client:** `@microsoft/signalr`
- **Charts:** Recharts / Chart.js
- **Date:** dayjs / date-fns
- **Build:** Vite

### 8.2 Folder Structure đề xuất
```
frontend/
├── public/
├── src/
│   ├── api/                    # API client services
│   │   ├── axios.ts            # Axios instance + interceptors
│   │   ├── auth.api.ts
│   │   ├── exam.api.ts
│   │   ├── question.api.ts
│   │   └── ...
│   ├── components/             # Reusable components
│   │   ├── common/
│   │   ├── layout/
│   │   └── forms/
│   ├── pages/                  # Page components
│   │   ├── auth/
│   │   ├── admin/
│   │   ├── teacher/
│   │   ├── student/
│   │   └── supervisor/
│   ├── hooks/                  # Custom hooks
│   ├── store/                  # State management
│   ├── types/                  # TypeScript types
│   ├── utils/                  # Helpers
│   ├── constants/              # API URLs, roles, etc.
│   ├── router/                 # Route config
│   └── App.tsx
└── package.json
```

### 8.3 Pages cần xây dựng

**Public:**
- Login
- Register
- Forgot password / Reset password

**Student:**
- Dashboard (đề thi sắp diễn ra, lịch sử)
- Exam list (đề được phân công)
- Take exam (làm bài + anti-cheat)
- Result detail
- Profile

**Teacher:**
- Question bank (CRUD, import Excel)
- Exam creator
- Results & rankings
- Statistics
- Manual grading

**Admin:**
- User management
- Categories & types
- Kỳ thi & ca thi
- Phân công thí sinh
- Audit logs
- Dashboard tổng

**Supervisor:**
- Real-time monitoring (SignalR)
- Live warnings
- Live progress

### 8.4 Axios Setup mẫu

```typescript
import axios from 'axios';

const API_BASE = 'https://localhost:7249';

const api = axios.create({
  baseURL: API_BASE,
  timeout: 30000,
});

// Request interceptor
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Response interceptor for refresh token
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const original = error.config;
    if (error.response?.status === 401 && !original._retry) {
      original._retry = true;
      try {
        const refreshToken = localStorage.getItem('refreshToken');
        const { data } = await axios.post(`${API_BASE}/api/Auth/refresh`, { refreshToken });
        localStorage.setItem('accessToken', data.data.accessToken);
        localStorage.setItem('refreshToken', data.data.refreshToken);
        original.headers.Authorization = `Bearer ${data.data.accessToken}`;
        return api(original);
      } catch {
        localStorage.clear();
        window.location.href = '/login';
      }
    }
    return Promise.reject(error);
  }
);

export default api;
```

### 8.5 SignalR Setup mẫu

```typescript
import * as signalR from "@microsoft/signalr";

export const createHubConnection = () => {
  const connection = new signalR.HubConnectionBuilder()
    .withUrl("https://localhost:7249/hubs/exam-monitor", {
      accessTokenFactory: () => localStorage.getItem("accessToken") || ""
    })
    .withAutomaticReconnect()
    .build();

  return connection;
};

// Listen events
connection.on("CheatingWarning", (data) => {
  console.warn("Warning:", data);
});

connection.on("StudentProgress", (data) => {
  // Update UI
});

await connection.start();
await connection.invoke("JoinExamMonitoring", examId);
```

---

## 🔑 9. NHỮNG ĐIỀU CẦN NHỚ

### 9.1 Token expiration
- Access token: **60 phút**
- Refresh token: **30 ngày** (90 ngày nếu rememberMe)

### 9.2 Pagination defaults
- pageNumber: 1
- pageSize: 10 (max: 100)

### 9.3 File upload
- Max 10MB per file
- Returns full URL: `https://localhost:7249/uploads/questions/{guid}.jpg`

### 9.4 Time zones
- Server lưu **DateTime.Now** (local) cho NgayTao
- Server lưu **DateTime.UtcNow** cho RefreshTokens, Notifications
- FE nên parse cẩn thận (ISO 8601)

### 9.5 Random shuffle
- Câu hỏi shuffled với seed = `baithiId`
- Đáp án shuffled với seed = `baithiId * 1000 + cauhoiId`
- Cùng session sẽ thấy cùng thứ tự

### 9.6 Anti-cheat warning types
- `TAB_SWITCH` - Chuyển tab
- `EXIT_FULLSCREEN` - Thoát fullscreen
- `COPY_PASTE` - Copy/paste
- `RIGHT_CLICK` - Click chuột phải
- Custom...

### 9.7 Auto-submit
Background job chạy mỗi 1 phút để auto-submit các bài thi quá giờ.

### 9.8 Email
Mặc định `EnableEmailSending: false` (chỉ log). Bật khi production.

---

## 🧪 10. TEST CREDENTIALS

| Role | Username | Password |
|------|----------|----------|
| Admin | admin | admin123 |
| Student (đã đăng ký test) | student01 | Student@123 |
| Teacher | (cần tạo) | - |

## 📦 11. TEST DATA AVAILABLE

- 7 câu hỏi mẫu (id 1-7) về C#, Database, ASP.NET
- 5 danh mục: C#, DB, ASP.NET, JS, HTML/CSS
- 4 loại câu hỏi: Trắc nghiệm, Đúng/Sai, Tự luận, Điền khuyết
- 4 vai trò: Admin, Teacher, Student, Supervisor

## 📁 12. CẤU TRÚC PROJECT BACKEND

```
BanTayVang.API/
├── Attributes/                    # RequireAuth, RequireRole
├── BackgroundJobs/               # AutoSubmitExpiredExamsJob
├── Configuration/                # JwtSettings, EmailSettings
├── Controllers/                  # 13 controllers
├── DTOs/                         # Request/Response DTOs
├── Hubs/                         # SignalR ExamMonitorHub
├── Mappings/                     # AutoMapper profiles
├── Middleware/                   # Global Exception, JWT, Audit Log
├── Migrations/                   # SQL migrations
├── Models/                       # EF entities
├── Repositories/
│   ├── Interfaces/
│   └── Impl/
├── Services/
│   ├── Interfaces/
│   │   ├── Auth/
│   │   ├── Exams/
│   │   ├── Security/
│   │   └── Validation/
│   └── Impl/
├── wwwroot/uploads/              # Uploaded files
├── Program.cs                    # Entry point
├── appsettings.json
└── appsettings.Development.json
```

---

**Tài liệu này đủ để code Frontend tương thích 100% với Backend!**

Khi cần thêm chi tiết về 1 endpoint cụ thể, xem file `.http` trong `TestingFeatures/08-API-Testing/`.