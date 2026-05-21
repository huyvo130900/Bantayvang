# Workflow 11: Statistics Dashboard

## Mục tiêu
Dashboard tổng quan hệ thống + thống kê chi tiết đề thi + lịch sử thi user.

## Features

### Dashboard Overview
- Tổng số users, câu hỏi, đề thi, bài thi
- Số đề thi đang active
- Số bài thi đang làm / đã hoàn thành
- Điểm trung bình hệ thống
- Số cảnh báo gian lận
- Hoạt động gần đây (10 bài thi mới nhất)

### Thống kê đề thi
- Số thí sinh tham gia
- Tỷ lệ đạt/rớt (pie chart)
- Phân bố điểm (bar chart)
- Điểm cao nhất / thấp nhất / trung bình
- Số bài đang làm vs đã hoàn thành

### Lịch sử thi user
- Danh sách bài thi đã làm
- Điểm, trạng thái, thời gian

### Top Performers
- Xếp hạng theo điểm trung bình
- Số lần thi, điểm cao nhất

## API Endpoints (từ BE)
```
GET    /api/statistics/dashboard
GET    /api/statistics/exam/:examId
GET    /api/statistics/user/:userId/history
GET    /api/statistics/my-history
GET    /api/statistics/top-performers?top=10
```

## Files cần tạo
```
src/features/statistics/
├── api.ts
├── types.ts
├── components/
│   ├── stats-cards.tsx             # Cards tổng quan (số liệu)
│   ├── recent-activities.tsx       # Hoạt động gần đây
│   ├── exam-stats-detail.tsx       # Thống kê 1 đề thi
│   ├── score-distribution-chart.tsx # Bar chart phân bố điểm
│   ├── pass-rate-chart.tsx         # Pie chart đạt/rớt
│   ├── top-performers-table.tsx    # Bảng xếp hạng
│   └── user-history-table.tsx      # Lịch sử thi
└── pages/
    └── statistics-page.tsx
```

## Charts Library
- Sử dụng **Recharts** (lightweight, React-native)
- Hoặc **@tremor/react** (built on Recharts, đẹp hơn)

## Kết quả
- Dashboard overview cho admin
- Thống kê chi tiết từng đề thi
- Charts trực quan
- Top performers

## Tiếp theo → Workflow 12: Real-time Monitoring
