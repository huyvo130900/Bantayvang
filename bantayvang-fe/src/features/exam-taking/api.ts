import apiClient from '@/lib/axios'
import type { ApiResponse } from '@/types'
import type {
  ExamSubmissionDto,
  ExamQuestionDto,
  StartExamDto,
  SubmitAnswerDto,
  SubmitExamDto,
  CheatingWarningDto,
} from './types'

export const examTakingApi = {
  start: (data: StartExamDto) =>
    apiClient.post<ApiResponse<ExamSubmissionDto>>('/exam/start', data),

  getQuestions: (baithiId: number) =>
    apiClient.get<ApiResponse<ExamQuestionDto[]>>(`/exam/${baithiId}/questions`),

  saveAnswer: (data: SubmitAnswerDto) =>
    apiClient.post<ApiResponse>('/exam/answer', data),

  getProgress: (baithiId: number) =>
    apiClient.get<ApiResponse<ExamSubmissionDto>>(`/exam/${baithiId}/progress`),

  submit: (data: SubmitExamDto) =>
    apiClient.post<ApiResponse<ExamSubmissionDto>>('/exam/submit', data),

  logWarning: (data: CheatingWarningDto) =>
    apiClient.post<ApiResponse>('/exam/warning', data),

  getWarningCount: (baithiId: number) =>
    apiClient.get<ApiResponse<number>>(`/exam/${baithiId}/warnings`),

  // THÊM MỚI: lấy danh sách bài thi đã hoàn thành của user hiện tại
  getMyResults: () =>
    apiClient.get<ApiResponse<ExamSubmissionDto[]>>('/exam/my-results'),
}
