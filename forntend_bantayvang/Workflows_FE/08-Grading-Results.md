# Workflow 08: Grading & Results

## Mục tiêu
Chấm điểm, xem kết quả chi tiết, bảng xếp hạng, export Excel.

## Features
- Xem kết quả chi tiết bài thi (từng câu đúng/sai)
- Danh sách kết quả theo đề thi
- Bảng xếp hạng (ranking)
- Chấm lại bài thi (regrade)
- Chấm thủ công câu tự luận
- Auto-grade tất cả bài chưa chấm
- Export kết quả ra Excel
- Export bảng xếp hạng ra Excel

## API Endpoints (từ BE)
```
GET    /api/grading/result/:baiThiId
GET    /api/grading/exam/:examId/results
GET    /api/grading/exam/:examId/ranking?top=50
POST   /api/grading/regrade/:baiThiId
POST   /api/grading/manual-grade     { chiTietLamBaiId, diemDatDuoc, nhanXet }
POST   /api/grading/auto-grade-all
GET    /api/grading/exam/:examId/export          (download xlsx)
GET    /api/grading/exam/:examId/ranking/export  (download xlsx)
```

## Files cần tạo
```
src/features/grading/
├── api.ts
├── types.ts
├── components/
│   ├── exam-results-table.tsx      # Danh sách kết quả
│   ├── result-detail-dialog.tsx    # Chi tiết bài thi (từng câu)
│   ├── ranking-table.tsx           # Bảng xếp hạng
│   ├── manual-grade-dialog.tsx     # Chấm thủ công tự luận
│   └── export-buttons.tsx          # Nút export Excel
└── pages/
    └── grading-page.tsx
```

## Kết quả
- Admin xem được kết quả tất cả bài thi
- Chấm thủ công câu tự luận
- Export Excel cho báo cáo
- Bảng xếp hạng

## Tiếp theo → Workflow 09: KyThi Management
