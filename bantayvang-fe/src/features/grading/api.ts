import apiClient from '@/lib/axios'
import type { ExamResultDetailDto, ManualGradingDto, PendingEssayDto } from './types'
import type { ApiResponse } from '@/types'

export const gradingApi = {
  getResultsByExam: (examId: number) =>
    apiClient.get<ApiResponse<ExamResultDetailDto[]>>(`/grading/exam/${examId}/results`),

  getResultDetail: (baiThiId: number) =>
    apiClient.get<ApiResponse<ExamResultDetailDto>>(`/grading/result/${baiThiId}`),

  getRanking: (examId: number, top = 50) =>
    apiClient.get<ApiResponse<ExamResultDetailDto[]>>(`/grading/exam/${examId}/ranking?top=${top}`),

  regrade: (baiThiId: number) =>
    apiClient.post<ApiResponse<ExamResultDetailDto>>(`/grading/regrade/${baiThiId}`),

  manualGrade: (dto: ManualGradingDto) =>
    apiClient.post<ApiResponse>('/grading/manual-grade', dto),

  exportResults: (examId: number) =>
    apiClient.get(`/grading/exam/${examId}/export`, { responseType: 'blob' }),

  exportRanking: (examId: number, top = 50) =>
    apiClient.get(`/grading/exam/${examId}/ranking/export?top=${top}`, { responseType: 'blob' }),

  getByKyThi: (examCampaignId: number) =>
    apiClient.get<ApiResponse<ExamResultDetailDto[]>>(`/grading/by-campaign/${examCampaignId}`),

  // ✨ MỚI: Công bố điểm cho từng thí sinh
  publishSingle: (baiThiId: number) =>
    apiClient.post<ApiResponse>(`/grading/publish-single/${baiThiId}`),

  unpublishSingle: (baiThiId: number) =>
    apiClient.post<ApiResponse>(`/grading/unpublish-single/${baiThiId}`),

  // ✨ Danh sách bài thi còn câu tự luận chưa được chấm
  getPendingEssay: (isGraded?: boolean) =>
    apiClient.get<ApiResponse<PendingEssayDto[]>>(`/grading/pending-essay?isGraded=${isGraded ? 'true' : 'false'}`),
}
