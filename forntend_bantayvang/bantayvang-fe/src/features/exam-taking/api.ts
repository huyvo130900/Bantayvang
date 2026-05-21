import apiClient from '@/lib/axios'
import type { ApiResponse } from '@/types'
import type {
  BaithiDto,
  ExamQuestionDto,
  StartExamDto,
  SubmitAnswerDto,
  SubmitExamDto,
  CheatingWarningDto,
} from './types'

export const examTakingApi = {
  start: (data: StartExamDto) =>
    apiClient.post<ApiResponse<BaithiDto>>('/exam/start', data),

  getQuestions: (baithiId: number) =>
    apiClient.get<ApiResponse<ExamQuestionDto[]>>(`/exam/${baithiId}/questions`),

  saveAnswer: (data: SubmitAnswerDto) =>
    apiClient.post<ApiResponse>('/exam/answer', data),

  getProgress: (baithiId: number) =>
    apiClient.get<ApiResponse<BaithiDto>>(`/exam/${baithiId}/progress`),

  submit: (data: SubmitExamDto) =>
    apiClient.post<ApiResponse<BaithiDto>>('/exam/submit', data),

  logWarning: (data: CheatingWarningDto) =>
    apiClient.post<ApiResponse>('/exam/warning', data),

  getWarningCount: (baithiId: number) =>
    apiClient.get<ApiResponse<number>>(`/exam/${baithiId}/warnings`),
}
