import apiClient from '@/lib/axios'
import type { ApiResponse } from '@/types'
import type { ExamResultDetailDto, ManualGradingDto } from './types'

export const gradingApi = {
  getResultDetail: (baiThiId: number) =>
    apiClient.get<ApiResponse<ExamResultDetailDto>>(`/grading/result/${baiThiId}`),

  getResultsByExam: (examId: number) =>
    apiClient.get<ApiResponse<ExamResultDetailDto[]>>(`/grading/exam/${examId}/results`),

  getRanking: (examId: number, top = 50) =>
    apiClient.get<ApiResponse<ExamResultDetailDto[]>>(`/grading/exam/${examId}/ranking?top=${top}`),

  regrade: (baiThiId: number) =>
    apiClient.post<ApiResponse<ExamResultDetailDto>>(`/grading/regrade/${baiThiId}`),

  manualGrade: (data: ManualGradingDto) =>
    apiClient.post<ApiResponse>('/grading/manual-grade', data),

  autoGradeAll: () =>
    apiClient.post<ApiResponse<number>>('/grading/auto-grade-all'),

  exportResults: (examId: number) =>
    apiClient.get(`/grading/exam/${examId}/export`, { responseType: 'blob' }),

  exportRanking: (examId: number, top = 50) =>
    apiClient.get(`/grading/exam/${examId}/ranking/export?top=${top}`, { responseType: 'blob' }),
}
