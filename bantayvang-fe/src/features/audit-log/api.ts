import apiClient from '@/lib/axios'
import type { ApiResponse } from '@/types'

export interface AuditLogEntry {
  id?: number
  userId?: number
  username?: string
  method?: string
  path?: string
  actionType?: string
  statusCode?: number
  description?: string
  ipAddress?: string
  timestamp?: string
  department?: string
  action?: string
}

export interface AuditLogPagedResult {
  success: boolean
  data: AuditLogEntry[]
  total: number
  page: number
  pageSize: number
  totalPages: number
}

export const auditLogApi = {
  getList: (params: {
    actionType?: string
    username?: string
    from?: string
    to?: string
    page?: number
    pageSize?: number
  }) => apiClient.get<AuditLogPagedResult>('/AuditLog', { params }),

  getRecent: (top = 500) =>
    apiClient.get<ApiResponse<AuditLogEntry[]>>(`/AuditLog/recent?top=${top}`),

  getByUser: (userId: number, top = 100) =>
    apiClient.get<ApiResponse<AuditLogEntry[]>>(`/AuditLog/user/${userId}?top=${top}`),

  getByExamSession: (baithiId: number) =>
    apiClient.get<ApiResponse<AuditLogEntry[]>>(`/AuditLog/exam-session/${baithiId}`),

  search: (actionType?: string, username?: string, from?: string, to?: string) =>
    apiClient.get<ApiResponse<AuditLogEntry[]>>('/AuditLog/search', {
      params: { actionType, username, from, to }
    }),
}
