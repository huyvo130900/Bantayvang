import apiClient from '@/lib/axios'
import type { ApiResponse } from '@/types'
import type {
  ExamPaperDto,
  CreateExamPaperDto,
  ExamAssignmentDto,
  CreateExamAssignmentDto,
  ExtendExamTimeDto,
  ExamPreviewDtoFE,
} from './types'

export const examsApi = {
  // Lấy đề thi Active (dùng cho trang học sinh)
  getActive: () =>
    apiClient.get<ApiResponse<ExamPaperDto[]>>('/exam/active'),

  getByCode: (examPaperCode: string) =>
    apiClient.get<ApiResponse<ExamPaperDto>>(`/exam/code/${examPaperCode}`),

  create: (data: CreateExamPaperDto) =>
    apiClient.post<ApiResponse<ExamPaperDto>>('/exam', data),
    
  update: (id: number, data: CreateExamPaperDto) =>
    apiClient.put<ApiResponse<ExamPaperDto>>(`/exam/${id}`, { ...data, id }),

  // Assignments
  getAssignmentsByExam: (examId: number) =>
    apiClient.get<ApiResponse<ExamAssignmentDto[]>>(`/examassignment/exam/${examId}`),

  getAssignmentsByUser: (userId: number) =>
    apiClient.get<ApiResponse<ExamAssignmentDto[]>>(`/examassignment/user/${userId}`),

  // Lấy danh sách đề thi + trạng thái + kết quả của học sinh hiện tại
  getMyExams: () =>
    apiClient.get<ApiResponse<ExamAssignmentDto[]>>('/examassignment/my-exams'),

  assignUsers: (data: CreateExamAssignmentDto) =>
    apiClient.post<ApiResponse<number>>('/examassignment/assign', data),

  removeAssignment: (assignmentId: number) =>
    apiClient.delete<ApiResponse>(`/examassignment/${assignmentId}`),

  checkAssignment: (examId: number, userId: number) =>
    apiClient.get<ApiResponse<boolean>>(`/examassignment/check/${examId}/${userId}`),

  extendTime: (data: ExtendExamTimeDto) =>
    apiClient.post<ApiResponse>('/examassignment/extend-time', data),
}

export const examsApiExtended = {
  // Lấy TẤT CẢ đề thi (dùng cho trang admin)
  getAll: (status?: string) => {
    const q = status ? `?status=${status}` : ''
    return apiClient.get<ApiResponse<ExamPaperDto[]>>(`/exam${q}`)
  },
  delete: (id: number) =>
    apiClient.delete<ApiResponse>(`/exam/${id}`),
  updateStatus: (id: number, status: string) =>
    apiClient.put<ApiResponse>(`/exam/${id}/status`, { status }),
  // Preview exam with all questions and answers
  preview: (id: number) =>
    apiClient.get<ApiResponse<ExamPreviewDtoFE>>(`/Exam/${id}/preview`),
  // Get print URL
  getPrintUrl: (id: number) => `/api/Exam/${id}/preview-print`,
}
