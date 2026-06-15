import apiClient from '@/lib/axios'
import type { ApiResponse } from '@/types'
import type {
  DethiDto,
  CreateDethiDto,
  ExamAssignmentDto,
  CreateExamAssignmentDto,
  ExtendExamTimeDto,
  ExamPreviewDtoFE,
} from './types'

export const examsApi = {
  // Lấy đề thi Active (dùng cho trang học sinh)
  getActive: () =>
    apiClient.get<ApiResponse<DethiDto[]>>('/exam/active'),

  getByCode: (maDeThi: string) =>
    apiClient.get<ApiResponse<DethiDto>>(`/exam/code/${maDeThi}`),

  create: (data: CreateDethiDto) =>
    apiClient.post<ApiResponse<DethiDto>>('/exam', data),

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
  getAll: (trangThai?: string) => {
    const q = trangThai ? `?trangThai=${trangThai}` : ''
    return apiClient.get<ApiResponse<DethiDto[]>>(`/exam${q}`)
  },
  delete: (id: number) =>
    apiClient.delete<ApiResponse>(`/exam/${id}`),
  updateStatus: (id: number, trangThai: string) =>
    apiClient.put<ApiResponse>(`/exam/${id}/status`, { trangThai }),
  // Preview exam with all questions and answers
  preview: (id: number) =>
    apiClient.get<ApiResponse<ExamPreviewDtoFE>>(`/Exam/${id}/preview`),
  // Get print URL
  getPrintUrl: (id: number) => `/api/Exam/${id}/preview-print`,
}
