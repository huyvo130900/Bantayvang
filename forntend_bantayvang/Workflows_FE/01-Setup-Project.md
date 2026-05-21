# Workflow 01: Setup Project

## Mục tiêu
Khởi tạo React project với Vite + TypeScript, cài đặt toàn bộ dependencies, cấu hình Tailwind, shadcn/ui, Redux Toolkit, React Router, Axios.

## Tech Stack
- Vite + React 18 + TypeScript
- Tailwind CSS + shadcn/ui
- Redux Toolkit
- React Router v6
- Axios
- React Hook Form + Zod

## Bước 1: Tạo Project

```bash
npm create vite@latest bantayvang-fe -- --template react-ts
cd bantayvang-fe
```

## Bước 2: Cài Dependencies

```bash
# Core
npm install react-router-dom @reduxjs/toolkit react-redux axios

# UI
npm install tailwindcss @tailwindcss/vite
npm install class-variance-authority clsx tailwind-merge lucide-react

# Form & Validation
npm install react-hook-form @hookform/resolvers zod

# Utilities
npm install date-fns dompurify
npm install -D @types/dompurify

# Real-time (later)
# npm install @microsoft/signalr
```

## Bước 3: Cấu hình Vite

```typescript
// vite.config.ts
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import path from 'path'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    port: 3000,
    proxy: {
      '/api': {
        target: 'https://localhost:7001',
        changeOrigin: true,
        secure: false,
      },
    },
  },
})
```

## Bước 4: Cấu hình Tailwind

```css
/* src/styles/globals.css */
@import "tailwindcss";

@layer base {
  :root {
    --background: 0 0% 100%;
    --foreground: 222.2 84% 4.9%;
    --card: 0 0% 100%;
    --card-foreground: 222.2 84% 4.9%;
    --primary: 221.2 83.2% 53.3%;
    --primary-foreground: 210 40% 98%;
    --secondary: 210 40% 96.1%;
    --secondary-foreground: 222.2 47.4% 11.2%;
    --muted: 210 40% 96.1%;
    --muted-foreground: 215.4 16.3% 46.9%;
    --accent: 210 40% 96.1%;
    --accent-foreground: 222.2 47.4% 11.2%;
    --destructive: 0 84.2% 60.2%;
    --destructive-foreground: 210 40% 98%;
    --border: 214.3 31.8% 91.4%;
    --input: 214.3 31.8% 91.4%;
    --ring: 221.2 83.2% 53.3%;
    --radius: 0.5rem;
  }
}

@layer base {
  * {
    @apply border-border;
  }
  body {
    @apply bg-background text-foreground;
  }
}
```

## Bước 5: Utility function (cn)

```typescript
// src/lib/utils.ts
import { type ClassValue, clsx } from 'clsx'
import { twMerge } from 'tailwind-merge'

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}
```

## Bước 6: Cấu hình TypeScript paths

```json
// tsconfig.app.json (thêm paths)
{
  "compilerOptions": {
    "baseUrl": ".",
    "paths": {
      "@/*": ["./src/*"]
    }
  }
}
```

## Bước 7: Folder Structure

```
src/
├── app/
│   ├── store.ts              # Redux store
│   ├── hooks.ts              # Typed Redux hooks
│   ├── router.tsx            # React Router config
│   └── providers.tsx         # All providers wrapper
├── components/
│   ├── ui/                   # shadcn/ui base components
│   │   ├── button.tsx
│   │   ├── input.tsx
│   │   ├── card.tsx
│   │   ├── dialog.tsx
│   │   ├── table.tsx
│   │   ├── toast.tsx
│   │   └── ...
│   ├── layout/
│   │   ├── admin-layout.tsx
│   │   ├── student-layout.tsx
│   │   ├── sidebar.tsx
│   │   └── header.tsx
│   └── shared/
│       ├── loading-spinner.tsx
│       ├── error-boundary.tsx
│       ├── data-table.tsx
│       └── confirm-dialog.tsx
├── features/
│   ├── auth/
│   │   ├── api.ts            # Auth API calls
│   │   ├── slice.ts          # Redux slice
│   │   ├── types.ts          # Auth types
│   │   ├── schemas.ts        # Zod schemas
│   │   ├── components/       # LoginForm, RegisterForm
│   │   └── pages/            # LoginPage
│   ├── users/
│   ├── questions/
│   ├── exams/
│   ├── exam-taking/
│   ├── grading/
│   ├── ky-thi/
│   ├── notifications/
│   ├── statistics/
│   └── audit-log/
├── hooks/
│   ├── use-auth.ts
│   ├── use-debounce.ts
│   └── use-pagination.ts
├── lib/
│   ├── axios.ts              # Axios instance + interceptors
│   ├── utils.ts              # cn() and helpers
│   └── constants.ts          # App constants
├── types/
│   └── index.ts              # Global shared types
├── styles/
│   └── globals.css
├── App.tsx
└── main.tsx
```

## Bước 8: Redux Store Setup

```typescript
// src/app/store.ts
import { configureStore } from '@reduxjs/toolkit'
import authReducer from '@/features/auth/slice'

export const store = configureStore({
  reducer: {
    auth: authReducer,
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware({
      serializableCheck: false,
    }),
})

export type RootState = ReturnType<typeof store.getState>
export type AppDispatch = typeof store.dispatch
```

```typescript
// src/app/hooks.ts
import { useDispatch, useSelector } from 'react-redux'
import type { RootState, AppDispatch } from './store'

export const useAppDispatch = useDispatch.withTypes<AppDispatch>()
export const useAppSelector = useSelector.withTypes<RootState>()
```

## Bước 9: Axios Instance với JWT Interceptor

```typescript
// src/lib/axios.ts
import axios from 'axios'

const apiClient = axios.create({
  baseURL: '/api',
  headers: {
    'Content-Type': 'application/json',
  },
})

// Request interceptor: attach access token
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('accessToken')
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  (error) => Promise.reject(error)
)

// Response interceptor: handle 401 + refresh token
apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config

    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true

      try {
        const refreshToken = localStorage.getItem('refreshToken')
        if (!refreshToken) {
          // No refresh token, force logout
          window.dispatchEvent(new Event('auth:logout'))
          return Promise.reject(error)
        }

        const response = await axios.post('/api/auth/refresh', {
          refreshToken,
        })

        if (response.data.success) {
          const { accessToken, refreshToken: newRefreshToken } = response.data.data
          localStorage.setItem('accessToken', accessToken)
          localStorage.setItem('refreshToken', newRefreshToken)

          originalRequest.headers.Authorization = `Bearer ${accessToken}`
          return apiClient(originalRequest)
        }
      } catch (refreshError) {
        // Refresh failed, force logout
        localStorage.removeItem('accessToken')
        localStorage.removeItem('refreshToken')
        window.dispatchEvent(new Event('auth:logout'))
        return Promise.reject(refreshError)
      }
    }

    return Promise.reject(error)
  }
)

export default apiClient
```

## Bước 10: Environment Variables

```env
# .env
VITE_API_BASE_URL=https://localhost:7001
VITE_APP_NAME=BanTayVang
```

```env
# .env.development
VITE_API_BASE_URL=https://localhost:7001
```

## Kết quả
- Project React + TypeScript chạy được trên port 3000
- Tailwind CSS configured với design tokens
- Redux store ready
- Axios instance với JWT refresh logic
- Folder structure rõ ràng, dễ mở rộng
- Path aliases (@/) configured

## Tiếp theo → Workflow 02: Auth System
