# Workflow 02: Auth System

## Mục tiêu
Xây dựng hệ thống authentication hoàn chỉnh: Login, JWT token management, auto-refresh, protected routes, role-based guards.

## Features
- Login form với React Hook Form + Zod validation
- Redux slice quản lý auth state
- Auto-refresh token khi hết hạn
- Protected routes (redirect nếu chưa login)
- Role guard (Admin vs Student)
- Logout (single device / all devices)
- Persist auth state

## Files cần tạo

### 1. Types

```typescript
// src/features/auth/types.ts
export interface User {
  id: number
  username: string
  email: string
  fullName: string
  role: string // 'Admin' | 'Teacher' | 'Student' | 'Supervisor'
  isActive: boolean
  lastLoginAt: string
}

export interface LoginRequest {
  username: string
  password: string
  rememberMe?: boolean
}

export interface AuthResponse {
  accessToken: string
  refreshToken: string
  expiresAt: string
  tokenType: string
  user: User
}

export interface AuthState {
  user: User | null
  accessToken: string | null
  refreshToken: string | null
  isAuthenticated: boolean
  isLoading: boolean
  error: string | null
}
```

### 2. Zod Schemas

```typescript
// src/features/auth/schemas.ts
import { z } from 'zod'

export const loginSchema = z.object({
  username: z
    .string()
    .min(3, 'Tên đăng nhập tối thiểu 3 ký tự')
    .max(100, 'Tên đăng nhập tối đa 100 ký tự'),
  password: z
    .string()
    .min(6, 'Mật khẩu tối thiểu 6 ký tự')
    .max(100, 'Mật khẩu tối đa 100 ký tự'),
  rememberMe: z.boolean().optional(),
})

export type LoginFormData = z.infer<typeof loginSchema>
```

### 3. Auth API

```typescript
// src/features/auth/api.ts
import apiClient from '@/lib/axios'
import type { LoginRequest, AuthResponse, User } from './types'

export const authApi = {
  login: (data: LoginRequest) =>
    apiClient.post<{ success: boolean; data: AuthResponse; message: string }>(
      '/auth/login',
      data
    ),

  logout: (refreshToken: string, logoutFromAllDevices = false) =>
    apiClient.post('/auth/logout', { refreshToken, logoutFromAllDevices }),

  refreshToken: (refreshToken: string) =>
    apiClient.post<{ success: boolean; data: AuthResponse }>(
      '/auth/refresh',
      { refreshToken }
    ),

  getMe: () =>
    apiClient.get<{ success: boolean; data: User }>('/auth/me'),

  validateToken: () =>
    apiClient.get<{ success: boolean; data: User }>('/auth/validate'),
}
```

### 4. Redux Slice

```typescript
// src/features/auth/slice.ts
import { createSlice, createAsyncThunk } from '@reduxjs/toolkit'
import { authApi } from './api'
import type { AuthState, LoginRequest } from './types'

const initialState: AuthState = {
  user: null,
  accessToken: localStorage.getItem('accessToken'),
  refreshToken: localStorage.getItem('refreshToken'),
  isAuthenticated: !!localStorage.getItem('accessToken'),
  isLoading: false,
  error: null,
}

export const login = createAsyncThunk(
  'auth/login',
  async (credentials: LoginRequest, { rejectWithValue }) => {
    try {
      const response = await authApi.login(credentials)
      if (!response.data.success) {
        return rejectWithValue(response.data.message)
      }
      return response.data.data
    } catch (error: any) {
      return rejectWithValue(
        error.response?.data?.message || 'Đăng nhập thất bại'
      )
    }
  }
)

export const logout = createAsyncThunk(
  'auth/logout',
  async (_, { getState, rejectWithValue }) => {
    try {
      const state = getState() as { auth: AuthState }
      const refreshToken = state.auth.refreshToken
      if (refreshToken) {
        await authApi.logout(refreshToken)
      }
    } catch {
      // Logout locally even if API fails
    }
  }
)

export const fetchCurrentUser = createAsyncThunk(
  'auth/fetchCurrentUser',
  async (_, { rejectWithValue }) => {
    try {
      const response = await authApi.getMe()
      if (!response.data.success) {
        return rejectWithValue('Failed to fetch user')
      }
      return response.data.data
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.message || 'Error')
    }
  }
)

const authSlice = createSlice({
  name: 'auth',
  initialState,
  reducers: {
    clearError: (state) => {
      state.error = null
    },
    forceLogout: (state) => {
      state.user = null
      state.accessToken = null
      state.refreshToken = null
      state.isAuthenticated = false
      localStorage.removeItem('accessToken')
      localStorage.removeItem('refreshToken')
    },
  },
  extraReducers: (builder) => {
    builder
      // Login
      .addCase(login.pending, (state) => {
        state.isLoading = true
        state.error = null
      })
      .addCase(login.fulfilled, (state, action) => {
        state.isLoading = false
        state.isAuthenticated = true
        state.user = action.payload.user
        state.accessToken = action.payload.accessToken
        state.refreshToken = action.payload.refreshToken
        localStorage.setItem('accessToken', action.payload.accessToken)
        localStorage.setItem('refreshToken', action.payload.refreshToken)
      })
      .addCase(login.rejected, (state, action) => {
        state.isLoading = false
        state.error = action.payload as string
      })
      // Logout
      .addCase(logout.fulfilled, (state) => {
        state.user = null
        state.accessToken = null
        state.refreshToken = null
        state.isAuthenticated = false
        localStorage.removeItem('accessToken')
        localStorage.removeItem('refreshToken')
      })
      // Fetch current user
      .addCase(fetchCurrentUser.fulfilled, (state, action) => {
        state.user = action.payload
        state.isAuthenticated = true
      })
      .addCase(fetchCurrentUser.rejected, (state) => {
        state.user = null
        state.isAuthenticated = false
        state.accessToken = null
        state.refreshToken = null
        localStorage.removeItem('accessToken')
        localStorage.removeItem('refreshToken')
      })
  },
})

export const { clearError, forceLogout } = authSlice.actions
export default authSlice.reducer
```

### 5. Login Form Component

```tsx
// src/features/auth/components/login-form.tsx
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { loginSchema, type LoginFormData } from '../schemas'
import { login, clearError } from '../slice'
import { useAppDispatch, useAppSelector } from '@/app/hooks'
import { useNavigate } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'

export function LoginForm() {
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const { isLoading, error } = useAppSelector((state) => state.auth)

  const form = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
    defaultValues: { username: '', password: '', rememberMe: false },
  })

  const onSubmit = async (data: LoginFormData) => {
    dispatch(clearError())
    const result = await dispatch(login(data))
    if (login.fulfilled.match(result)) {
      const user = result.payload.user
      if (user.role === 'Student') {
        navigate('/exam-waiting')
      } else {
        navigate('/admin/dashboard')
      }
    }
  }

  return (
    <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
      {error && (
        <div className="p-3 text-sm text-red-600 bg-red-50 rounded-md">
          {error}
        </div>
      )}

      <div className="space-y-2">
        <label className="text-sm font-medium">Tên đăng nhập</label>
        <Input
          {...form.register('username')}
          placeholder="Nhập tên đăng nhập"
          autoComplete="username"
        />
        {form.formState.errors.username && (
          <p className="text-sm text-red-500">
            {form.formState.errors.username.message}
          </p>
        )}
      </div>

      <div className="space-y-2">
        <label className="text-sm font-medium">Mật khẩu</label>
        <Input
          {...form.register('password')}
          type="password"
          placeholder="Nhập mật khẩu"
          autoComplete="current-password"
        />
        {form.formState.errors.password && (
          <p className="text-sm text-red-500">
            {form.formState.errors.password.message}
          </p>
        )}
      </div>

      <div className="flex items-center space-x-2">
        <input
          {...form.register('rememberMe')}
          type="checkbox"
          id="rememberMe"
          className="rounded border-gray-300"
        />
        <label htmlFor="rememberMe" className="text-sm">
          Ghi nhớ đăng nhập
        </label>
      </div>

      <Button type="submit" className="w-full" disabled={isLoading}>
        {isLoading ? 'Đang đăng nhập...' : 'Đăng nhập'}
      </Button>
    </form>
  )
}
```

### 6. Protected Route & Role Guard

```tsx
// src/components/shared/protected-route.tsx
import { Navigate, useLocation } from 'react-router-dom'
import { useAppSelector } from '@/app/hooks'

interface ProtectedRouteProps {
  children: React.ReactNode
  allowedRoles?: string[]
}

export function ProtectedRoute({ children, allowedRoles }: ProtectedRouteProps) {
  const { isAuthenticated, user } = useAppSelector((state) => state.auth)
  const location = useLocation()

  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />
  }

  if (allowedRoles && user && !allowedRoles.includes(user.role)) {
    return <Navigate to="/unauthorized" replace />
  }

  return <>{children}</>
}
```

### 7. Login Page

```tsx
// src/features/auth/pages/login-page.tsx
import { Navigate } from 'react-router-dom'
import { useAppSelector } from '@/app/hooks'
import { LoginForm } from '../components/login-form'
import { Card } from '@/components/ui/card'

export function LoginPage() {
  const { isAuthenticated, user } = useAppSelector((state) => state.auth)

  if (isAuthenticated && user) {
    return user.role === 'Student'
      ? <Navigate to="/exam-waiting" replace />
      : <Navigate to="/admin/dashboard" replace />
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-blue-600 to-purple-700">
      <Card className="w-full max-w-md p-8">
        <div className="text-center mb-6">
          <h1 className="text-2xl font-bold text-gray-900">
            Hệ Thống Thi Trực Tuyến
          </h1>
          <p className="text-gray-500 mt-1">Bàn Tay Vàng</p>
        </div>
        <LoginForm />
      </Card>
    </div>
  )
}
```

### 8. Auth Event Listener (handle force logout)

```tsx
// src/hooks/use-auth-listener.ts
import { useEffect } from 'react'
import { useAppDispatch } from '@/app/hooks'
import { forceLogout } from '@/features/auth/slice'
import { useNavigate } from 'react-router-dom'

export function useAuthListener() {
  const dispatch = useAppDispatch()
  const navigate = useNavigate()

  useEffect(() => {
    const handleLogout = () => {
      dispatch(forceLogout())
      navigate('/login')
    }

    window.addEventListener('auth:logout', handleLogout)
    return () => window.removeEventListener('auth:logout', handleLogout)
  }, [dispatch, navigate])
}
```

## Security Notes (OWASP)
- Tokens stored in localStorage (acceptable for SPA, consider httpOnly cookies for production)
- Auto-refresh prevents session fixation
- Force logout on 401 prevents stale sessions
- Zod validation prevents injection via form inputs
- No sensitive data exposed in error messages to user

## Kết quả
- Login form hoạt động với validation
- JWT tokens được quản lý tự động
- Auto-refresh khi token hết hạn
- Protected routes redirect về login
- Role-based routing (Admin → dashboard, Student → exam waiting)

## Tiếp theo → Workflow 03: Layout & Navigation
