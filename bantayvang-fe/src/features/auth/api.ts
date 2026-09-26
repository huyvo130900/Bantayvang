import apiClient from '@/lib/axios'
import type { ApiResponse } from '@/types'
import type { AuthResponse, LoginRequest, User } from './types'

export const authApi = {
  login: (data: LoginRequest) =>
    apiClient.post<ApiResponse<AuthResponse>>('/auth/login', data),

  logout: (refreshToken: string, logoutFromAllDevices = false) =>
    apiClient.post<ApiResponse>('/auth/logout', {
      refreshToken,
      logoutFromAllDevices,
    }),

  refreshToken: (refreshToken: string) =>
    apiClient.post<ApiResponse<AuthResponse>>('/auth/refresh', {
      refreshToken,
    }),

  getMe: () => apiClient.get<ApiResponse<User>>('/auth/me'),

  validateToken: () => apiClient.get<ApiResponse<User>>('/auth/validate'),

  // Xác thực email bằng OTP (dùng khi đăng ký dự thi - thí sinh ngoại)
  sendEmailVerificationCode: (email: string) =>
    apiClient.post<ApiResponse>('/auth/send-verification-code', { email }),

  verifyEmailCode: (email: string, code: string) =>
    apiClient.post<ApiResponse>('/auth/verify-email-code', { email, code }),

  // Quên mật khẩu bằng OTP
  forgotPassword: (email: string) =>
    apiClient.post<ApiResponse>('/auth/request-reset', JSON.stringify(email), {
      headers: { 'Content-Type': 'application/json' },
    }),

  verifyResetCode: (email: string, code: string) =>
    apiClient.post<ApiResponse<string>>('/auth/verify-reset-code', { email, code }),

  resetPassword: (token: string, newPassword: string) =>
    apiClient.post<ApiResponse>('/auth/reset-password', { token, newPassword }),

  // Đổi mật khẩu (yêu cầu đã đăng nhập)
  changePassword: (currentPassword: string, newPassword: string, confirmPassword: string) =>
    apiClient.post<ApiResponse>('/auth/change-password', {
      currentPassword,
      newPassword,
      confirmPassword,
    }),
}
