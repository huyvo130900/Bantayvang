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
  if (filter.roleId !== undefined) params.set('roleId', String(filter.roleId))
  if (filter.status !== undefined) params.set('status', String(filter.status))
  if (filter.department) params.set('department', filter.department)
  if (filter.searchKeyword) params.set('searchKeyword', filter.searchKeyword)
  if (filter.includeDeleted) params.set('includeDeleted', 'true')
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

  restore: (id: number) => apiClient.post<ApiResponse>(`/user/${id}/restore`),

  hardDelete: (id: number) => apiClient.delete<ApiResponse>(`/user/${id}/hard`),

  bulkDelete: (ids: number[]) => apiClient.post<ApiResponse>('/user/bulk-delete', ids),
  bulkHardDelete: (ids: number[]) => apiClient.post<ApiResponse>('/user/bulk-hard-delete', ids),
}

// Extended user API methods
export const usersApiExtended = {
  importExcel: (file: File) => {
    const formData = new FormData()
    formData.append('file', file)
    return apiClient.post<ApiResponse<any>>('/user/import', formData, {
      // BUG FIX: literal 'multipart/form-data' (no boundary) makes the browser send that exact
      // Content-Type instead of auto-generating one with a boundary - `undefined` deletes
      // apiClient's default 'application/json' header so the browser computes it correctly.
      headers: { 'Content-Type': undefined },
    })
  },
  downloadTemplate: () =>
    apiClient.get('/user/import-template', { responseType: 'blob' }),
}
