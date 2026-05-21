import apiClient from '@/lib/axios'
import type { ApiResponse } from '@/types'

export interface NotificationDto {
  id: number
  userId: number | null
  title: string
  message: string
  type: string
  isRead: boolean
  createdAt: string
  relatedUrl: string | null
}

export interface CreateNotificationDto {
  userId?: number | null
  title: string
  message: string
  type?: string
  relatedUrl?: string
}

export interface ExamScheduleDto {
  examId: number
  maDeThi: string | null
  tenDeThi: string | null
  thoiGianBatDau: string | null
  thoiGianLamBai: number | null
  thoiGianKetThuc: string | null
  trangThai: string | null
  soCauHoi: number
  isAvailable: boolean
  availabilityMessage: string
}

export const notificationsApi = {
  getMyNotifications: (unreadOnly?: boolean) => {
    const params = unreadOnly !== undefined ? `?unreadOnly=${unreadOnly}` : ''
    return apiClient.get<ApiResponse<NotificationDto[]>>(`/notification${params}`)
  },

  getUnreadCount: () =>
    apiClient.get<ApiResponse<number>>('/notification/unread-count'),

  create: (data: CreateNotificationDto) =>
    apiClient.post<ApiResponse<NotificationDto>>('/notification', data),

  markAsRead: (id: number) =>
    apiClient.post<ApiResponse>(`/notification/${id}/read`),

  markAllAsRead: () =>
    apiClient.post<ApiResponse>('/notification/mark-all-read'),

  delete: (id: number) =>
    apiClient.delete<ApiResponse>(`/notification/${id}`),

  broadcast: (data: CreateNotificationDto) =>
    apiClient.post<ApiResponse>('/notification/broadcast', data),

  getUpcomingExams: () =>
    apiClient.get<ApiResponse<ExamScheduleDto[]>>('/notification/upcoming-exams'),

  getCurrentExams: () =>
    apiClient.get<ApiResponse<ExamScheduleDto[]>>('/notification/current-exams'),
}
