import apiClient from '@/lib/axios'
import type { ApiResponse } from '@/types'

export interface AuditLogEntry {
  id?: number
  userId?: number
  username?: string
  action?: string
  path?: string
  method?: string
  ipAddress?: string
  timestamp?: string
  details?: string
}

export const auditLogApi = {
  getRecent: (top = 200) =>
    apiClient.get<ApiResponse<AuditLogEntry[]>>(`/auditlog/recent?top=${top}`),

  getByUser: (userId: number, top = 100) =>
    apiClient.get<ApiResponse<AuditLogEntry[]>>(`/auditlog/user/${userId}?top=${top}`),

  getByExamSession: (baithiId: number) =>
    apiClient.get<ApiResponse<AuditLogEntry[]>>(`/auditlog/exam-session/${baithiId}`),

  search: (actionType?: string, from?: string, to?: string) => {
    const params = new URLSearchParams()
    if (actionType) params.set('actionType', actionType)
    if (from) params.set('from', from)
    if (to) params.set('to', to)
    return apiClient.get<ApiResponse<AuditLogEntry[]>>(`/auditlog/search?${params}`)
  },
}
