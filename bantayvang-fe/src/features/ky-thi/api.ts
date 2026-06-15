import apiClient from '@/lib/axios'
import type { ApiResponse } from '@/types'
import type { KyThiDto, CreateKyThiDto, UpdateKyThiDto, ExamGenerationConfig, ExamCheckResult } from './types'

export const kyThiApi = {
  getAll: (trangThai?: string) => {
    const params = trangThai ? `?trangThai=${trangThai}` : ''
    return apiClient.get<ApiResponse<KyThiDto[]>>(`/kythi${params}`)
  },

  getById: (id: number) =>
    apiClient.get<ApiResponse<KyThiDto>>(`/kythi/${id}`),

  create: (data: CreateKyThiDto) =>
    apiClient.post<ApiResponse<KyThiDto>>('/kythi', data),

  update: (id: number, data: UpdateKyThiDto) =>
    apiClient.put<ApiResponse<KyThiDto>>(`/kythi/${id}`, data),

  updateStatus: (id: number, trangThai: string) =>
    apiClient.post<ApiResponse>(`/kythi/${id}/status`, JSON.stringify(trangThai), {
      headers: { 'Content-Type': 'application/json' },
    }),

  delete: (id: number) =>
    apiClient.delete<ApiResponse>(`/kythi/${id}`),

  // Sinh đề cho kỳ thi
  checkExamGeneration: (kyThiId: number, config: ExamGenerationConfig) =>
    apiClient.post<ApiResponse<ExamCheckResult>>(`/kythi/${kyThiId}/check-generation`, config),

  generateExams: (kyThiId: number, config: ExamGenerationConfig) =>
    apiClient.post<ApiResponse>(`/kythi/${kyThiId}/generate-exams`, config),
}
