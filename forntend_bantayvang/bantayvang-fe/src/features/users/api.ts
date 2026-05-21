import apiClient from '@/lib/axios'
import type { ApiResponse } from '@/types'
import type {
  CreateUserDto,
  UpdateUserDto,
  UserDto,
  UserFilterDto,
} from './types'

const buildQueryString = (filter: UserFilterDto): string => {
  const params = new URLSearchParams()
  params.set('pageNumber', String(filter.pageNumber))
  params.set('pageSize', String(filter.pageSize))
  if (filter.idVaiTro !== undefined) params.set('idVaiTro', String(filter.idVaiTro))
  if (filter.trangThai !== undefined) params.set('trangThai', String(filter.trangThai))
  if (filter.khoaPhong) params.set('khoaPhong', filter.khoaPhong)
  if (filter.searchKeyword) params.set('searchKeyword', filter.searchKeyword)
  return params.toString()
}

export const usersApi = {
  list: (filter: UserFilterDto) =>
    apiClient.get<ApiResponse<UserDto[]>>(`/user?${buildQueryString(filter)}`),

  getById: (id: number) => apiClient.get<ApiResponse<UserDto>>(`/user/${id}`),

  create: (data: CreateUserDto) =>
    apiClient.post<ApiResponse<UserDto>>('/user', data),

  update: (id: number, data: UpdateUserDto) =>
    apiClient.put<ApiResponse<UserDto>>(`/user/${id}`, data),

  activate: (id: number) =>
    apiClient.post<ApiResponse>(`/user/${id}/activate`),

  deactivate: (id: number) =>
    apiClient.post<ApiResponse>(`/user/${id}/deactivate`),

  resetPassword: (id: number, newPassword: string) =>
    apiClient.post<ApiResponse>(`/user/${id}/reset-password`, { newPassword }),

  delete: (id: number) => apiClient.delete<ApiResponse>(`/user/${id}`),
}
