# Frontend Workflows - BanTayVang Exam System

## 📋 Tổng quan
Hệ thống thi trực tuyến "Bàn Tay Vàng" dành cho bệnh viện. Frontend xây dựng với React + TypeScript, tuân thủ SOLID và OWASP Top 10.

## 🛠️ Tech Stack
- **Framework:** React 18 + TypeScript (Vite)
- **State Management:** Redux Toolkit
- **UI:** Tailwind CSS + shadcn/ui
- **Routing:** React Router v6
- **HTTP Client:** Axios (interceptors cho JWT refresh)
- **Form:** React Hook Form + Zod
- **Real-time:** @microsoft/signalr
- **Charts:** Recharts

## 🎯 Phân quyền & Giao diện
- **Admin/Teacher/Supervisor:** Dashboard quản trị đầy đủ
- **Student:** Trang chờ thi → Làm bài → Xem kết quả

## 📁 Folder Structure
```
src/
├── app/                    # App setup (store, router, providers)
├── components/
│   ├── ui/                 # shadcn/ui components (Button, Input, Card...)
│   ├── layout/             # AdminLayout, StudentLayout, Header, Sidebar
│   └── shared/             # Reusable: DataTable, Modal, LoadingSpinner
├── features/
│   ├── auth/               # Login, register, token management
│   ├── users/              # User CRUD (admin)
│   ├── questions/          # Question bank CRUD, import Excel
│   ├── exams/              # Exam management, assignment
│   ├── exam-taking/        # Student exam interface, anti-cheat
│   ├── grading/            # Results, ranking, export
│   ├── ky-thi/             # Kỳ thi & ca thi management
│   ├── notifications/      # Notifications, exam schedule
│   ├── statistics/         # Dashboard charts & stats
│   └── audit-log/          # Activity logs (admin)
├── hooks/                  # Custom hooks (useAuth, useDebounce...)
├── lib/                    # Utilities (axios instance, helpers, constants)
├── types/                  # Global TypeScript types/interfaces
└── styles/                 # Global styles, Tailwind config
```

## 🚀 Roadmap Implementation

### Phase 1: Foundation
| # | Workflow | Mô tả |
|---|---------|--------|
| 01 | Setup Project | Vite + TS + Tailwind + shadcn/ui + Redux + Router + Axios |
| 02 | Auth System | Login, JWT refresh, protected routes, role guard |
| 03 | Layout & Navigation | Admin layout (sidebar, header), Student layout |

### Phase 2: Core Features (Admin)
| # | Workflow | Mô tả |
|---|---------|--------|
| 04 | User Management | CRUD users, filter, activate/deactivate |
| 05 | Question Bank | CRUD câu hỏi, import Excel, upload ảnh |
| 06 | Exam Management | Tạo đề thi, chọn câu hỏi, assign thí sinh |

### Phase 3: Exam System
| # | Workflow | Mô tả |
|---|---------|--------|
| 07 | Exam Taking | Giao diện làm bài, timer, auto-save, anti-cheat |
| 08 | Grading & Results | Chấm điểm, ranking, export Excel |

### Phase 4: Advanced
| # | Workflow | Mô tả |
|---|---------|--------|
| 09 | KyThi Management | Quản lý kỳ thi, ca thi |
| 10 | Notifications | Thông báo, lịch thi sắp tới |
| 11 | Statistics Dashboard | Charts, thống kê tổng quan |
| 12 | Real-time Monitoring | SignalR exam monitoring |

## 🔒 Security (OWASP Top 10)
- **A01 Broken Access Control:** Role-based route guards, API authorization headers
- **A02 Cryptographic Failures:** Tokens stored in memory/httpOnly, no sensitive data in localStorage
- **A03 Injection:** Input sanitization via Zod schemas, DOMPurify for rich content
- **A05 Security Misconfiguration:** Environment variables, no secrets in code
- **A07 Auth Failures:** Auto-refresh tokens, session timeout, logout on 401

## 📝 Conventions
- Code & comments: English
- UI text: Tiếng Việt
- File naming: kebab-case
- Component naming: PascalCase
- Feature-based folder structure (colocation)
