import apiClient from '@/lib/axios'
import type { ExamResultDetailDto, ManualGradingDto, PendingEssayDto, BulkEssayItem } from './types'
import type { ApiResponse } from '@/types'

export const gradingApi = {
  getResultsByExam: (examId: number) =>
    apiClient.get<ApiResponse<ExamResultDetailDto[]>>(`/grading/exam/${examId}/results`),

  getResultDetail: (examSubmissionId: number) =>
    apiClient.get<ApiResponse<ExamResultDetailDto>>(`/grading/result/${examSubmissionId}`),

  getRanking: (examId: number, top = 50) =>
    apiClient.get<ApiResponse<ExamResultDetailDto[]>>(`/grading/exam/${examId}/ranking?top=${top}`),

  regrade: (examSubmissionId: number) =>
    apiClient.post<ApiResponse<ExamResultDetailDto>>(`/grading/regrade/${examSubmissionId}`),

  manualGrade: (dto: ManualGradingDto) =>
    apiClient.post<ApiResponse>('/grading/manual-grade', dto),

  exportResults: (examId: number) =>
    apiClient.get(`/grading/exam/${examId}/export`, { responseType: 'blob' }),

  exportRanking: (examId: number, top = 50) =>
    apiClient.get(`/grading/exam/${examId}/ranking/export?top=${top}`, { responseType: 'blob' }),

  getByKyThi: (examCampaignId: number) =>
    apiClient.get<ApiResponse<ExamResultDetailDto[]>>(`/grading/by-exam-campaign/${examCampaignId}`),

  // ✨ MỚI: Công bố điểm cho từng thí sinh
  publishSingle: (examSubmissionId: number) =>
    apiClient.post<ApiResponse>(`/grading/publish-single/${examSubmissionId}`),

  unpublishSingle: (examSubmissionId: number) =>
    apiClient.post<ApiResponse>(`/grading/unpublish-single/${examSubmissionId}`),

  // ✨ Danh sách bài thi còn câu tự luận chưa được chấm
  getPendingEssay: (isGraded?: boolean) =>
    apiClient.get<ApiResponse<PendingEssayDto[]>>(`/grading/pending-essay?isGraded=${isGraded ? 'true' : 'false'}`),

  getPendingEssayAnswers: (examCampaignId?: number, examId?: number, ungradedOnly?: boolean) => {
    const params = new URLSearchParams()
    if (examCampaignId) params.append('examCampaignId', examCampaignId.toString())
    if (examId) params.append('examId', examId.toString())
    if (ungradedOnly) params.append('ungradedOnly', 'true')
    return apiClient.get<ApiResponse<BulkEssayItem[]>>(`/grading/pending-essay-answers?${params.toString()}`)
  },

  // ✨ AI Chấm tự động hàng loạt câu tự luận
  // autoFinalize=true: điểm AI đưa ra được chốt luôn. false: chỉ lưu làm gợi ý, cần người chấm duyệt lại.
  aiGradeBatch: (submissionDetailIds: number[], autoFinalize: boolean = true) =>
    apiClient.post<ApiResponse>('/grading/ai-grade-batch', { submissionDetailIds, autoFinalize }),
}
