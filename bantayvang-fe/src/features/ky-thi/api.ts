import apiClient from '@/lib/axios'
import type { ApiResponse } from '@/types'
import type { ExamCampaignDto, CreateKyThiDto, UpdateKyThiDto, ExamGenerationConfig, ExamCheckResult, ExamCampaignEligibilityDto, AssignFromExcelResultDto } from './types'

export const examCampaignApi = {
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

  // Báo cáo "ai đủ điều kiện thi / ai đã thi" theo (các) khoa của kỳ thi
  getEligibility: (examCampaignId: number) =>
    apiClient.get<ApiResponse<ExamCampaignEligibilityDto[]>>(`/ExamCampaign/${examCampaignId}/eligibility`),

  // Úp danh sách Excel/CSV để chỉ định người được thi (chế độ AccessMode = AssignedList)
  assignFromExcel: (examCampaignId: number, file: File) => {
    const formData = new FormData()
    formData.append('file', file)
    return apiClient.post<ApiResponse<AssignFromExcelResultDto>>(
      `/ExamCampaign/${examCampaignId}/assign-from-excel`,
      formData,
      // BUG FIX: literal 'multipart/form-data' (no boundary) makes the browser send that exact
      // Content-Type instead of auto-generating one with a boundary - `undefined` deletes
      // apiClient's default 'application/json' header so the browser computes it correctly.
      { headers: { 'Content-Type': undefined } }
    )
  },
}

export const examMonitorApi = {
  getActiveSessions: (examCampaignId: number) =>
    apiClient.get<ApiResponse<any[]>>(`/Exam/campaign/${examCampaignId}/monitor/active`),

  forceSubmitSession: (examSubmissionId: number) =>
    apiClient.post<ApiResponse>(`/Exam/${examSubmissionId}/force-submit`),
}
