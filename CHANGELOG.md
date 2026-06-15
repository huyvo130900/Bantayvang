# CHANGELOG

## v2.1.0 — 2026-05 — Department Manager & Feature Upgrades

### 🆕 Tính năng mới

#### Role: Quản lý Khoa (DeptManager, ID=5)
- Thêm role mới `DeptManager` (ID=5) vào `UserRole` enum, JWT, và toàn bộ hệ thống phân quyền
- Đánh dấu `Teacher` (ID=2) và `Supervisor` (ID=4) là `[Obsolete]` — giữ dữ liệu cũ nhưng không dùng trên UI mới
- DeptManager chỉ thấy câu hỏi, đề thi, kết quả thi của **khoa mình được gán**

#### Bảng KHOA_PHONG
- Model `KhoaPhong` với `MaKhoa`, `TenKhoa`, `DeptManagerId`
- Seed 8 khoa mẫu (Nội, Ngoại, Sản, Nhi, Cấp cứu, CĐHA, PTTT, Điều dưỡng)
- Admin gán Quản lý Khoa qua `POST /api/Department/{id}/assign-manager`

#### Công bố kết quả (CongBoKetQua)
- Trường `CongBoKetQua` (bool, mặc định `false`) trên bảng `DETHI`
- Admin/DeptManager toggle per đề thi qua `POST /api/Department/exam/{id}/toggle-visibility`
- Thí sinh chỉ thấy điểm khi `CongBoKetQua = true`

#### Kết quả thi phân cấp theo Kỳ thi
- Endpoint `GET /api/Grading/by-kythi/{kyThiId}` — lấy tất cả bài thi thuộc kỳ thi
- Trang `ResultsByKyThiPage` — split view: danh sách Kỳ thi trái, bảng thí sinh phải
- Filter theo xếp loại, tìm kiếm tên, export Excel (placeholder)

#### Sidebar DeptManager riêng
- Layout `DeptManagerLayout` với menu: Tổng quan, Ngân hàng câu hỏi, Đề thi, Kết quả thi
- Hiển thị tên Khoa đang quản lý trong sidebar header
- Route group `/dept-manager/*` — bảo vệ bởi ProtectedRoute với role `DeptManager`

#### LICHSU_THI
- Bảng `LICHSU_THI` với computed columns `DiemSo` (tự tính từ SoCauDung/TongSoCau) và `XepLoai` (Xuất sắc/Giỏi/Khá/Trung bình/Không đạt)
- Thí sinh có thể thi lại không giới hạn (mỗi lần tạo bản ghi mới)

### 🔒 Bảo mật

#### Anti-cheat nâng cấp
- Hook `useAntiCheat` nhận callback `onForceSubmit` — tự động nộp bài khi vi phạm > 3 lần
- Màn hình overlay đỏ "Bài thi bị kết thúc" khi bị terminate cưỡng bức
- Toast cảnh báo cầm lần hiện tại / số lần còn lại (e.g. "Cảnh báo 2/3")
- `MAX_CHEATING_WARNINGS = 3` — dễ dàng thay đổi ở `constants.ts`

#### Department Authorization
- Helper `DepartmentAuthHelper` đọc claims `id_khoa_quan_ly` và `khoa_phong` từ JWT
- `CauhoiController` — DeptManager chỉ GET/POST/PUT câu hỏi của khoa mình
- `ExamController` — DeptManager chỉ thấy đề thi của khoa mình
- Authorization policies: `AdminOnly`, `ManagementOnly` (Admin+DeptManager), `AuthenticatedUser`

### 🐛 Fixes & Improvements

- `UserInfoDto` (AuthResponse) bổ sung `KhoaPhong`, `IdKhoaQuanLy`, `TenKhoaQuanLy`
- JWT claims bổ sung `khoa_phong` và `id_khoa_quan_ly`
- `DethiDto` bổ sung `KhoaPhong`, `CongBoKetQua`, `ThoiGianCongBo`
- `ExamResultDetailDto` bổ sung `MaNhanVien`, `IdDeThi`, `SoLanThiLai`, `CongBoKetQua`
- AutoMapper profile: Dethi → DethiDto map đủ các field mới
- `RoleBadge` UI: DeptManager = badge xanh dương "Quản lý Khoa"
- User filter: bỏ Teacher/Supervisor, thêm "Quản lý Khoa"
- Login redirect: DeptManager → `/dept-manager/dashboard`
- `ProtectedRoute`: redirect đúng theo role
- Exam waiting page: hiện kết quả đã thi với điểm (nếu congBoKetQua) + nút "Xem kết quả"
- Exam result page: gated bởi `congBoKetQua`, hiện banner khi bị force terminate, nút "Thi lại"

### 📊 Database Migrations

| File | Nội dung |
|------|----------|
| `006-Add-Department-Manager.sql` | Bảng KHOA_PHONG, cột IdKhoaQuanLy, role DEPT_MANAGER, 8 khoa seed |
| `008-Add-Exam-Visibility.sql` | Cột CongBoKetQua, NguoiCongBo, ThoiGianCongBo trên DETHI |
| `009-AntiCheating-LichsuThi.sql` | Bảng LICHSU_THI với computed columns DiemSo, XepLoai |

### ⚙️ Cấu hình

```
MAX_CHEATING_WARNINGS = 3   (constants.ts)
DIEM_DAT_DEFAULT = 5.0      (Dethi.DiemDat)
ROLE_IDS.DEPT_MANAGER = 5
```
