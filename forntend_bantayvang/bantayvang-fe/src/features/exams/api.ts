import apiClient from '@/lib/axios'
import type { ApiResponse } from '@/types'
import type {
  DethiDto,
  CreateDethiDto,
  ExamAssignmentDto,
  CreateExamAssignmentDto,
  ExtendExamTimeDto,
} from './types'

export const examsApi = {
  // Exam CRUD
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
