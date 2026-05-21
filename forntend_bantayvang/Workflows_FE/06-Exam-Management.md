# Workflow 06: Exam Management

## Mục tiêu
Tạo và quản lý đề thi: tạo đề, chọn câu hỏi, assign thí sinh, gia hạn thời gian.

## Features
- Danh sách đề thi (active, draft, inactive)
- Tạo đề thi mới (chọn câu hỏi từ ngân hàng)
- Sửa đề thi
- Activate / Deactivate đề thi
- Phân công thí sinh cho đề thi
- Gia hạn thời gian cho thí sinh đang thi
- Xem danh sách thí sinh đã được phân công

## API Endpoints (từ BE)
```
# Đề thi
GET    /api/exam/active
GET    /api/exam/code/:maDeThi
POST   /api/exam

# Phân công
GET    /api/examassignment/exam/:examId
GET    /api/examassignment/user/:userId
POST   /api/examassignment/assign
DELETE /api/examassignment/:assignmentId
GET    /api/examassignment/check/:examId/:userId
POST   /api/examassignment/extend-time
```

## Files cần tạo
```
src/features/exams/
├── api.ts
├── types.ts
├── schemas.ts
├── components/
│   ├── exam-table.tsx            # Danh sách đề thi
│   ├── exam-form-dialog.tsx      # Tạo/sửa đề thi
│   ├── question-picker.tsx       # Chọn câu hỏi cho đề thi (modal + search)
│   ├── assignment-table.tsx      # Danh sách thí sinh được phân công
│   ├── assign-users-dialog.tsx   # Dialog phân công thí sinh
│   └── extend-time-dialog.tsx    # Dialog gia hạn thời gian
└── pages/
    ├── exams-page.tsx            # Trang quản lý đề thi
    └── exam-detail-page.tsx      # Chi tiết đề thi + assignments
```

## Exam Form Structure
```
- Mã đề thi (auto-generate hoặc nhập)
- Tên đề thi
- Thời gian làm bài (phút)
- Thời gian bắt đầu (datetime picker)
- Trạng thái (Draft / Active)
- Chọn câu hỏi:
  - [Search/Filter câu hỏi]
  - [Danh sách câu hỏi đã chọn]
  - [Random N câu từ danh mục]
```

## Kết quả
- Quản lý đề thi đầy đủ
- Chọn câu hỏi linh hoạt (manual + random)
- Phân công thí sinh
- Gia hạn thời gian real-time

## Tiếp theo → Workflow 07: Exam Taking
