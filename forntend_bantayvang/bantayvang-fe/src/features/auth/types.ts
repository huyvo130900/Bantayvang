export interface User {
  id: number
  username: string
  email: string
  fullName: string
  role: string
  isActive: boolean
  lastLoginAt: string
}

export interface LoginRequest {
  username: string
  password: string
  rememberMe?: boolean
  ipAddress?: string
  userAgent?: string
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
