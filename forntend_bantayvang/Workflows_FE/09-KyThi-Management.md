# Workflow 09: Kỳ Thi & Ca Thi Management

## Mục tiêu
Quản lý kỳ thi (exam period) và ca thi (exam session/shift).

## Features
- CRUD kỳ thi (tên, loại, thời gian, đơn vị tổ chức)
- Cập nhật trạng thái kỳ thi (DangChuanBi → DangDienRa → DaKetThuc)
- CRUD ca thi trong kỳ thi
- Gán đề thi cho ca thi
- Xem tổng quan kỳ thi (số ca, số thí sinh)

## API Endpoints (từ BE)
```
# Kỳ thi
GET    /api/kythi?trangThai=
GET    /api/kythi/:id
POST   /api/kythi
PUT    /api/kythi/:id
POST   /api/kythi/:id/status    (body: "DangDienRa")
DELETE /api/kythi/:id

# Ca thi
GET    /api/kythi/:kyThiId/ca-thi
POST   /api/kythi/ca-thi
PUT    /api/kythi/ca-thi/:id
DELETE /api/kythi/ca-thi/:id
```

## Files cần tạo
```
src/features/ky-thi/
├── api.ts
├── types.ts
├── schemas.ts
├── components/
│   ├── ky-thi-table.tsx          # Danh sách kỳ thi
│   ├── ky-thi-form-dialog.tsx    # Tạo/sửa kỳ thi
│   ├── ky-thi-status-badge.tsx   # Badge trạng thái
│   ├── ca-thi-list.tsx           # Danh sách ca thi trong 1 kỳ
│   └── ca-thi-form-dialog.tsx    # Tạo/sửa ca thi
└── pages/
    ├── ky-thi-page.tsx
    └── ky-thi-detail-page.tsx
```

## Kết quả
- Quản lý kỳ thi theo workflow trạng thái
- Chia ca thi linh hoạt
- Gán đề thi cho từng ca

## Tiếp theo → Workflow 10: Notifications
