import apiClient from '@/lib/axios'
import type { ApiResponse } from '@/types'
import type { ExamCampaignDto, CreateKyThiDto, UpdateKyThiDto, ExamGenerationConfig, ExamCheckResult } from './types'

export const kyThiApi = {
  getAll: (status?: string) => {
    const params = status ? `?status=${status}` : ''
    return apiClient.get<ApiResponse<ExamCampaignDto[]>>(`/ExamCampaign${params}`)
  },

  getById: (id: number) =>
    apiClient.get<ApiResponse<ExamCampaignDto>>(`/ExamCampaign/${id}`),

  create: (data: CreateKyThiDto) =>
    apiClient.post<ApiResponse<ExamCampaignDto>>('/ExamCampaign', data),

  update: (id: number, data: UpdateKyThiDto) =>
    apiClient.put<ApiResponse<ExamCampaignDto>>(`/ExamCampaign/${id}`, data),

  updateStatus: (id: number, status: string) =>
    apiClient.post<ApiResponse>(`/ExamCampaign/${id}/status`, JSON.stringify(status), {
      headers: { 'Content-Type': 'application/json' },
    }),

  delete: (id: number) =>
    apiClient.delete<ApiResponse>(`/ExamCampaign/${id}`),

  // Sinh đề cho kỳ thi
  checkExamGeneration: (examCampaignId: number, config: ExamGenerationConfig) =>
    apiClient.post<ApiResponse<ExamCheckResult>>(`/ExamCampaign/${examCampaignId}/check-generation`, config),

  generateExams: (examCampaignId: number, config: ExamGenerationConfig) =>
    apiClient.post<ApiResponse>(`/ExamCampaign/${examCampaignId}/generate-exams`, config),
}
