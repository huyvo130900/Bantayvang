# Workflow 07: Exam Taking (Student Interface)

## Mục tiêu
Giao diện làm bài thi cho student: timer, auto-save, anti-cheat, submit.

## Features

### Exam Waiting Room
- Hiển thị đề thi được phân công
- Countdown đến giờ thi
- Nút "Bắt đầu thi" khi đến giờ

### Exam Interface
- Timer đếm ngược (hiển thị rõ ràng)
- Danh sách câu hỏi (navigation panel)
- Hiển thị câu hỏi + lựa chọn
- Chọn đáp án (radio cho single, checkbox cho multiple)
- Tự luận (textarea)
- Auto-save mỗi khi chọn đáp án
- Đánh dấu câu đã trả lời / chưa trả lời
- Nút "Nộp bài" với confirm dialog
- Auto-submit khi hết giờ

### Anti-Cheat
- Detect tab switch (visibilitychange)
- Detect window blur/focus
- Block right-click
- Block copy/paste (Ctrl+C, Ctrl+V)
- Log cảnh báo lên server
- Hiển thị số cảnh báo cho student
- Fullscreen mode (optional)

### Results
- Hiển thị kết quả sau khi nộp bài
- Điểm, số câu đúng/sai
- Chi tiết từng câu (nếu admin cho phép)

## API Endpoints (từ BE)
```
# Exam session
POST   /api/exam/start              { maDeThi }
GET    /api/exam/:baithiId/questions
POST   /api/exam/answer             { idBaiThi, idCauHoi, idLuaChonDaChon, cauTraLoiTuLuan, daLuu }
POST   /api/exam/answer-multiple    { idBaiThi, idCauHoi, idLuaChonDaChon: [], cauTraLoiTuLuan, daLuu }
GET    /api/exam/:baithiId/progress
POST   /api/exam/submit             { idBaiThi, danhSachCauTraLoi: [] }

# Anti-cheat
POST   /api/exam/warning            { idBaiThi, loaiCanhBao, moTa }
GET    /api/exam/:baithiId/warnings

# Results
GET    /api/grading/result/:baiThiId
```

## Files cần tạo
```
src/features/exam-taking/
├── api.ts
├── types.ts
├── hooks/
│   ├── use-exam-timer.ts         # Timer countdown logic
│   ├── use-anti-cheat.ts         # Anti-cheat detection
│   └── use-auto-save.ts          # Auto-save answers
├── components/
│   ├── exam-waiting-room.tsx     # Chờ thi
│   ├── exam-timer.tsx            # Timer display
│   ├── question-navigation.tsx   # Panel câu hỏi (đã trả lời / chưa)
│   ├── question-display.tsx      # Hiển thị 1 câu hỏi
│   ├── choice-selector.tsx       # Radio/Checkbox cho lựa chọn
│   ├── essay-input.tsx           # Textarea cho tự luận
│   ├── submit-confirm.tsx        # Confirm dialog nộp bài
│   ├── warning-indicator.tsx     # Hiển thị số cảnh báo
│   └── exam-result.tsx           # Kết quả sau nộp bài
└── pages/
    ├── exam-waiting-page.tsx
    ├── exam-taking-page.tsx
    └── exam-result-page.tsx
```

## Anti-Cheat Hook
```typescript
// Pseudo-code
function useAntiCheat(baithiId: number) {
  // 1. Listen visibilitychange → log TAB_SWITCH
  // 2. Listen blur/focus → log BROWSER_FOCUS_LOST
  // 3. Listen contextmenu → prevent + log RIGHT_CLICK
  // 4. Listen keydown (Ctrl+C, Ctrl+V) → prevent + log COPY_PASTE
  // 5. Count warnings locally
  // 6. POST /api/exam/warning for each violation
}
```

## Kết quả
- Student có thể làm bài thi hoàn chỉnh
- Timer chính xác, auto-submit khi hết giờ
- Anti-cheat detect và log vi phạm
- Auto-save đáp án
- Xem kết quả sau nộp bài

## Tiếp theo → Workflow 08: Grading & Results
