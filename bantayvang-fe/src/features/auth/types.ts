export interface User {
  id: number
  // Backend may return either English or Vietnamese field names
  username?: string
  tenDangNhap?: string
  email?: string
  fullName?: string
  hoTen?: string
  role?: string
  tenVaiTro?: string
  isActive?: boolean
  trangThai?: boolean
  lastLoginAt?: string
  khoaPhong?: string
  maNhanVien?: string
  chucDanh?: string
  idKhoaQuanLy?: number
  tenKhoaQuanLy?: string
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
