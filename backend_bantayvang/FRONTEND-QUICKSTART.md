# 🚀 FRONTEND QUICKSTART - BanTayVang

## 📌 Đọc trước

1. **`BACKEND-SUMMARY.md`** - Toàn bộ thông tin API
2. File này - Quickstart 5 phút

## 🔥 30 giây tóm tắt

```
Backend URL:    https://localhost:7249
Swagger:        https://localhost:7249/swagger
SignalR:        wss://localhost:7249/hubs/exam-monitor
Static files:   https://localhost:7249/uploads/...

Login:          POST /api/Auth/login → { accessToken, refreshToken, user }
Auth header:    Authorization: Bearer {accessToken}
Refresh:        POST /api/Auth/refresh khi 401
Token expire:   60 phút (access), 30 ngày (refresh)

Roles:          1=Admin, 2=Teacher, 3=Student, 4=Supervisor
Default user:   admin / admin123
```

## 📦 Stack Frontend đề xuất

```bash
# Vite + React + TS
npm create vite@latest bantayvang-fe -- --template react-ts
cd bantayvang-fe

# Core packages
npm install axios react-router-dom @reduxjs/toolkit react-redux
npm install @microsoft/signalr
npm install antd  # hoặc @mui/material
npm install react-hook-form
npm install dayjs
npm install recharts  # cho dashboard

# Dev
npm install -D @types/node
```

## 🎯 4 Module chính cần build

### 1. Auth (Public)
- Login page → POST /api/Auth/login
- Register page → POST /api/Auth/register  
- Forgot password → POST /api/Auth/request-reset
- Reset password → POST /api/Auth/reset-password

### 2. Student
- Dashboard (lịch thi sắp tới, lịch sử)
- My exams (đề được phân) → GET /api/ExamAssignment/my-exams
- Take exam → POST /api/Exam/start, GET questions, POST answer, POST submit
- Result → GET /api/Grading/result/{baiThiId}

### 3. Admin/Teacher
- Question Bank → CRUD /api/Cauhoi
- Exam Builder → POST /api/Exam, choose questions
- Categories → /api/Category/categories|types
- User Management → /api/User
- Statistics → /api/Statistics/dashboard
- Kỳ thi → /api/KyThi
- Phân công → /api/ExamAssignment

### 4. Supervisor
- Real-time monitoring (SignalR)
- Live warnings
- Live progress
- Audit log → /api/AuditLog/recent

## 🗂️ Folder structure đề xuất

```
src/
├── api/
│   ├── client.ts              # Axios instance với interceptors
│   ├── auth.api.ts
│   ├── exam.api.ts
│   ├── question.api.ts
│   ├── user.api.ts
│   └── ...
├── components/
│   ├── common/                # Button, Input, Modal, Loading...
│   ├── layout/                # Header, Sidebar, Footer
│   └── exam/                  # ExamTimer, QuestionCard...
├── pages/
│   ├── auth/
│   │   ├── LoginPage.tsx
│   │   ├── RegisterPage.tsx
│   │   └── ForgotPasswordPage.tsx
│   ├── student/
│   │   ├── DashboardPage.tsx
│   │   ├── MyExamsPage.tsx
│   │   ├── TakeExamPage.tsx
│   │   └── ResultPage.tsx
│   ├── admin/
│   │   ├── UsersPage.tsx
│   │   ├── QuestionsPage.tsx
│   │   ├── ExamsPage.tsx
│   │   ├── KyThiPage.tsx
│   │   └── DashboardPage.tsx
│   └── supervisor/
│       └── MonitoringPage.tsx
├── hooks/
│   ├── useAuth.ts
│   ├── useExam.ts
│   └── useSignalR.ts
├── store/
│   ├── authSlice.ts
│   ├── examSlice.ts
│   └── store.ts
├── types/
│   ├── auth.types.ts
│   ├── exam.types.ts
│   └── api.types.ts
├── utils/
│   ├── auth.utils.ts          # Token storage
│   ├── format.utils.ts        # Date, number format
│   └── validation.utils.ts
├── constants/
│   ├── api.ts                 # Endpoint URLs
│   └── roles.ts
├── router/
│   └── index.tsx              # Route + ProtectedRoute
├── App.tsx
└── main.tsx
```

## 🔐 Auth Flow code mẫu

### 1. Axios client (`src/api/client.ts`)

```typescript
import axios from 'axios';

const API_BASE = import.meta.env.VITE_API_URL || 'https://localhost:7249';

export const api = axios.create({
  baseURL: API_BASE,
  timeout: 30000,
  headers: { 'Content-Type': 'application/json' }
});

// Attach token
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

// Auto-refresh on 401
let isRefreshing = false;
let pendingRequests: Array<(token: string) => void> = [];

api.interceptors.response.use(
  (res) => res,
  async (error) => {
    const original = error.config;
    if (error.response?.status === 401 && !original._retry) {
      if (isRefreshing) {
        return new Promise((resolve) => {
          pendingRequests.push((token) => {
            original.headers.Authorization = `Bearer ${token}`;
            resolve(api(original));
          });
        });
      }
      original._retry = true;
      isRefreshing = true;
      try {
        const refreshToken = localStorage.getItem('refreshToken');
        const { data } = await axios.post(`${API_BASE}/api/Auth/refresh`, { refreshToken });
        const newToken = data.data.accessToken;
        localStorage.setItem('accessToken', newToken);
        localStorage.setItem('refreshToken', data.data.refreshToken);
        pendingRequests.forEach((cb) => cb(newToken));
        pendingRequests = [];
        original.headers.Authorization = `Bearer ${newToken}`;
        return api(original);
      } catch {
        localStorage.clear();
        window.location.href = '/login';
      } finally {
        isRefreshing = false;
      }
    }
    return Promise.reject(error);
  }
);
```

### 2. Auth API (`src/api/auth.api.ts`)

```typescript
import { api } from './client';

export const authApi = {
  login: (username: string, password: string, rememberMe = false) =>
    api.post('/api/Auth/login', { username, password, rememberMe }),

  register: (data: any) => api.post('/api/Auth/register', data),

  logout: (refreshToken: string) =>
    api.post('/api/Auth/logout', { refreshToken, logoutFromAllDevices: false }),

  me: () => api.get('/api/Auth/me'),

  changePassword: (currentPassword: string, newPassword: string, confirmPassword: string) =>
    api.post('/api/Auth/change-password', { currentPassword, newPassword, confirmPassword }),

  requestReset: (email: string) => api.post('/api/Auth/request-reset', JSON.stringify(email), {
    headers: { 'Content-Type': 'application/json' }
  })
};
```

### 3. Protected Route

```typescript
import { Navigate } from 'react-router-dom';

export function ProtectedRoute({ children, allowedRoles }: any) {
  const token = localStorage.getItem('accessToken');
  const user = JSON.parse(localStorage.getItem('user') || '{}');
  
  if (!token) return <Navigate to="/login" replace />;
  if (allowedRoles && !allowedRoles.includes(user.role)) 
    return <Navigate to="/forbidden" replace />;
  
  return children;
}
```

### 4. SignalR Hook (`src/hooks/useSignalR.ts`)

```typescript
import { useEffect, useRef } from 'react';
import * as signalR from '@microsoft/signalr';

export function useExamMonitor(examId: number, callbacks: {
  onWarning?: (data: any) => void;
  onProgress?: (data: any) => void;
}) {
  const connectionRef = useRef<signalR.HubConnection | null>(null);

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl('https://localhost:7249/hubs/exam-monitor', {
        accessTokenFactory: () => localStorage.getItem('accessToken') || ''
      })
      .withAutomaticReconnect()
      .build();

    if (callbacks.onWarning) connection.on('CheatingWarning', callbacks.onWarning);
    if (callbacks.onProgress) connection.on('StudentProgress', callbacks.onProgress);

    connection.start().then(() => {
      connection.invoke('JoinExamMonitoring', examId);
      connectionRef.current = connection;
    });

    return () => {
      connection.stop();
    };
  }, [examId]);

  return connectionRef.current;
}
```

## 📝 Tips quan trọng

### 1. CORS đã enabled cho mọi origin (dev)
Production cần config restrict.

### 2. Self-signed certificate
Browser có thể warn HTTPS. Trust certificate:
```bash
dotnet dev-certs https --trust
```

### 3. Response format
Tất cả response đều là `BaseResponseDto<T>`:
```typescript
interface BaseResponse<T> {
  success: boolean;
  message: string;
  data?: T;
  errors: string[];
}
```

### 4. File upload
Dùng FormData:
```typescript
const formData = new FormData();
formData.append('file', file);
api.post('/api/Upload/image', formData, {
  headers: { 'Content-Type': 'multipart/form-data' }
});
```

### 5. Anti-cheat (Take Exam Page)
```typescript
useEffect(() => {
  const handleVisibility = () => {
    if (document.hidden) {
      // Send warning
      api.post('/api/Exam/warning', {
        idBaiThi,
        loaiCanhBao: 'TAB_SWITCH',
        moTa: 'User chuyển tab'
      });
    }
  };
  
  const handleCopy = (e: ClipboardEvent) => {
    e.preventDefault();
    api.post('/api/Exam/warning', { ... });
  };

  document.addEventListener('visibilitychange', handleVisibility);
  document.addEventListener('copy', handleCopy);
  
  return () => {
    document.removeEventListener('visibilitychange', handleVisibility);
    document.removeEventListener('copy', handleCopy);
  };
}, []);
```

### 6. Timer countdown
```typescript
const [timeLeft, setTimeLeft] = useState(thoiGianConLai); // seconds

useEffect(() => {
  const timer = setInterval(() => {
    setTimeLeft(t => {
      if (t <= 0) {
        // Auto-submit
        submitExam();
        return 0;
      }
      return t - 1;
    });
  }, 1000);
  return () => clearInterval(timer);
}, []);
```

### 7. Auto-save mỗi N giây
```typescript
useEffect(() => {
  const interval = setInterval(() => {
    if (currentAnswer) {
      api.post('/api/Exam/answer', {
        idBaiThi, idCauHoi, idLuaChonDaChon: currentAnswer, daLuu: true
      });
    }
  }, 30000); // every 30s
  return () => clearInterval(interval);
}, [currentAnswer]);
```

## 🎨 UI/UX gợi ý

### Take Exam Page (quan trọng nhất)
```
┌─────────────────────────────────────────────┐
│  [Đề thi: TEST001]  ⏱ 45:23 còn lại    [Nộp]│
├─────────────────────┬───────────────────────┤
│ Câu 1/20:           │  Danh sách câu hỏi:   │
│                     │  [1][2][3][4][5]      │
│  Nội dung câu hỏi?  │  [6][7][8][9][10]     │
│                     │  ...                  │
│  ○ Đáp án A         │                       │
│  ● Đáp án B         │  ✓ Đã trả lời         │
│  ○ Đáp án C         │  ◷ Chưa trả lời       │
│  ○ Đáp án D         │  ⚑ Đánh dấu          │
│                     │                       │
│  [⚑ Đánh dấu]       │                       │
│  [← Trước][Sau →]   │                       │
└─────────────────────┴───────────────────────┘
```

### Admin Dashboard
- Cards: Total users, exams, questions, in-progress
- Chart: Pass rate by exam
- Table: Recent activities
- Top performers

### Supervisor Real-time
- Grid showing each student card
- Status: Online (green), Warning (yellow), Critical (red)
- Live progress bar per student
- Warnings panel sidebar

## ✅ Checklist trước khi start FE

- [ ] Backend đang chạy (`dotnet run`)
- [ ] Database migrations đã run
- [ ] Test login admin được trên Swagger
- [ ] Đọc `BACKEND-SUMMARY.md`
- [ ] Decide stack (React/Vue/Angular)
- [ ] Setup project với Vite
- [ ] Install packages
- [ ] Setup folder structure
- [ ] Implement auth flow trước

## 🔗 Useful Links

- Swagger: https://localhost:7249/swagger
- API base: https://localhost:7249
- Test files: `TestingFeatures/08-API-Testing/`
- Backend code: `BanTayVang.API/`

---

**Khi sang folder mới, mang theo file này + `BACKEND-SUMMARY.md` là đủ thông tin để code FE!**