# Workflow 04: User Management

## Mục tiêu
CRUD người dùng cho Admin: danh sách, tạo mới, sửa, activate/deactivate, reset password, filter/search.

## Features
- Danh sách users với DataTable (pagination, sort, filter)
- Tạo user mới (form + validation)
- Sửa thông tin user
- Activate / Deactivate account
- Reset password
- Filter theo: role, trạng thái, khoa/phòng, keyword search

## API Endpoints (từ BE)
```
GET    /api/user?pageNumber=1&pageSize=10&idVaiTro=&trangThai=&khoaPhong=&searchKeyword=
GET    /api/user/:id
POST   /api/user
PUT    /api/user/:id
POST   /api/user/:id/activate
POST   /api/user/:id/deactivate
POST   /api/user/:id/reset-password
DELETE /api/user/:id
```

## Files cần tạo
```
src/features/users/
├── api.ts              # API calls
├── slice.ts            # Redux slice (optional, có thể dùng RTK Query)
├── types.ts            # User types
├── schemas.ts          # Zod schemas cho form
├── components/
│   ├── user-table.tsx          # DataTable hiển thị users
│   ├── user-form-dialog.tsx    # Dialog tạo/sửa user
│   ├── user-filter.tsx         # Filter bar
│   └── reset-password-dialog.tsx
└── pages/
    └── users-page.tsx          # Page chính
```

## Zod Schemas
```typescript
// Create user
const createUserSchema = z.object({
  tenDangNhap: z.string().min(3).max(100),
  matKhau: z.string().min(6).max(100),
  email: z.string().email(),
  hoTen: z.string().min(1).max(255),
  maNhanVien: z.string().optional(),
  chucDanh: z.string().optional(),
  khoaPhong: z.string().optional(),
  idVaiTro: z.number().min(1).max(4),
  trangThai: z.boolean(),
})

// Update user (no password)
const updateUserSchema = createUserSchema.omit({ tenDangNhap: true, matKhau: true })
```

## Kết quả
- Trang quản lý users hoàn chỉnh
- CRUD với validation
- Filter/search realtime
- Confirm dialog trước khi deactivate/delete

## Tiếp theo → Workflow 05: Question Bank
