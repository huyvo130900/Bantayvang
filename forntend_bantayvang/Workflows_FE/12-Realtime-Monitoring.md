# Workflow 12: Real-time Monitoring & Audit Log

## Mục tiêu
Giám sát thi real-time qua SignalR + Audit log cho admin.

## Features

### Real-time Exam Monitoring (SignalR)
- Xem danh sách thí sinh đang thi live
- Nhận cảnh báo gian lận real-time
- Xem tiến độ làm bài (số câu đã trả lời)
- Thông báo khi thí sinh nộp bài
- Heartbeat (student còn online không)

### Audit Log
- Xem log gần nhất (top 200)
- Filter theo user, action type, thời gian
- Log của 1 bài thi cụ thể
- Search logs

## API Endpoints (từ BE)
```
# SignalR Hub: /hubs/exam-monitor
# Events:
#   JoinExamMonitoring(examId)
#   LeaveExamMonitoring(examId)
#   JoinGlobalMonitoring()
#   CheatingWarning → { examId, baithiId, username, warningType, description, timestamp }
#   ExamStarted → { examId, userId, username, timestamp }
#   ExamSubmitted → { examId, baithiId, username, score, timestamp }
#   StudentProgress → { examId, baithiId, answeredCount, totalCount, timestamp }
#   StudentHeartbeat → { baithiId, timestamp }

# Audit Log REST
GET    /api/auditlog/recent?top=200
GET    /api/auditlog/user/:userId?top=100
GET    /api/auditlog/exam-session/:baithiId
GET    /api/auditlog/search?actionType=&from=&to=
```

## Files cần tạo
```
src/features/monitoring/
├── signalr-client.ts             # SignalR connection setup
├── types.ts
├── hooks/
│   └── use-exam-monitor.ts       # Hook quản lý SignalR connection
├── components/
│   ├── live-exam-panel.tsx        # Panel thí sinh đang thi
│   ├── cheating-alerts.tsx        # Danh sách cảnh báo real-time
│   ├── student-progress-card.tsx  # Card tiến độ 1 thí sinh
│   └── monitoring-dashboard.tsx   # Tổng hợp monitoring
└── pages/
    └── monitoring-page.tsx

src/features/audit-log/
├── api.ts
├── types.ts
├── components/
│   ├── audit-log-table.tsx
│   └── audit-log-filter.tsx
└── pages/
    └── audit-log-page.tsx
```

## SignalR Setup
```typescript
// Pseudo-code
import { HubConnectionBuilder } from '@microsoft/signalr'

const connection = new HubConnectionBuilder()
  .withUrl('/hubs/exam-monitor', {
    accessTokenFactory: () => localStorage.getItem('accessToken') || ''
  })
  .withAutomaticReconnect()
  .build()

// Listen events
connection.on('CheatingWarning', (data) => { ... })
connection.on('ExamSubmitted', (data) => { ... })
```

## Kết quả
- Admin giám sát thi real-time
- Cảnh báo gian lận push ngay lập tức
- Audit log đầy đủ cho compliance
- SignalR auto-reconnect

## 🎉 HOÀN THÀNH ROADMAP
Sau 12 workflows, hệ thống FE đầy đủ tính năng.
