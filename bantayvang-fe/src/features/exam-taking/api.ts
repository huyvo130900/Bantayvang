import apiClient from '@/lib/axios'
import type { ApiResponse } from '@/types'
import type {
  ExamSubmissionDto,
  ExamQuestionDto,
  StartExamDto,
  SubmitAnswerDto,
  SubmitExamDto,
  SubmitMultipleAnswerDto,
  CheatingWarningDto,
} from './types'

export const examTakingApi = {
  start: (data: StartExamDto) =>
    apiClient.post<ApiResponse<ExamSubmissionDto>>('/exam/start', data),

  getQuestions: (examSubmissionId: number) =>
    apiClient.get<ApiResponse<ExamQuestionDto[]>>(`/exam/${examSubmissionId}/questions`),

  saveAnswer: (data: SubmitAnswerDto) =>
    apiClient.post<ApiResponse>('/exam/answer', data),

  saveAnswerMultiple: (data: SubmitMultipleAnswerDto) =>
    apiClient.post<ApiResponse>('/exam/answer-multiple', data),

  getProgress: (examSubmissionId: number) =>
    apiClient.get<ApiResponse<ExamSubmissionDto>>(`/exam/${examSubmissionId}/progress`),

  submit: (data: SubmitExamDto) =>
    apiClient.post<ApiResponse<ExamSubmissionDto>>('/exam/submit', data),

  logWarning: (data: CheatingWarningDto) =>
    apiClient.post<ApiResponse>('/exam/warning', data),

  getWarningCount: (examSubmissionId: number) =>
    apiClient.get<ApiResponse<number>>(`/exam/${examSubmissionId}/warnings`),

  // THÊM MỚI: lấy danh sách bài thi đã hoàn thành của user hiện tại
  getMyResults: () =>
    apiClient.get<ApiResponse<ExamSubmissionDto[]>>('/exam/my-results'),
}
