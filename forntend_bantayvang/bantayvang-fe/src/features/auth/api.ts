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
}
