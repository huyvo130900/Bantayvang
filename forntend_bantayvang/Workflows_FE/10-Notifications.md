# Workflow 10: Notifications & Exam Schedule

## Mục tiêu
Hệ thống thông báo và lịch thi.

## Features
- Danh sách thông báo (đã đọc / chưa đọc)
- Badge số thông báo chưa đọc trên header
- Tạo thông báo (admin)
- Broadcast thông báo cho tất cả users
- Đánh dấu đã đọc (1 hoặc tất cả)
- Xóa thông báo
- Lịch thi sắp tới (upcoming exams)
- Lịch thi đang/đã diễn ra (current exams)

## API Endpoints (từ BE)
```
GET    /api/notification?unreadOnly=true
GET    /api/notification/unread-count
POST   /api/notification
POST   /api/notification/:id/read
POST   /api/notification/mark-all-read
DELETE /api/notification/:id
POST   /api/notification/broadcast

GET    /api/notification/upcoming-exams
GET    /api/notification/current-exams
```

## Files cần tạo
```
src/features/notifications/
├── api.ts
├── types.ts
├── components/
│   ├── notification-bell.tsx       # Bell icon + badge (header)
│   ├── notification-dropdown.tsx   # Dropdown danh sách nhanh
│   ├── notification-list.tsx       # Full page list
│   ├── create-notification.tsx     # Form tạo thông báo
│   ├── exam-schedule-list.tsx      # Lịch thi upcoming/current
│   └── broadcast-dialog.tsx        # Dialog broadcast
└── pages/
    └── notifications-page.tsx
```

## Kết quả
- Thông báo real-time trên header
- Admin tạo/broadcast thông báo
- Lịch thi cho student và admin

## Tiếp theo → Workflow 11: Statistics Dashboard
