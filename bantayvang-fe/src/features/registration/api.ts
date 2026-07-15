import apiClient from '@/lib/axios'
import type { ExamRegistrationDto, CreateExamRegistrationDto, RejectDto } from './types'

export const registrationApi = {
  create: (dto: CreateExamRegistrationDto) =>
    apiClient.post<{ message: string; data: ExamRegistrationDto }>('/ExamRegistration/public', dto),

  getPending: () =>
    apiClient.get<ExamRegistrationDto[]>('/ExamRegistration/pending'),

  approve: (id: number) =>
    apiClient.post<{ message: string }>(`/ExamRegistration/${id}/approve`),

  reject: (id: number, dto: RejectDto) =>
    apiClient.post<{ message: string }>(`/ExamRegistration/${id}/reject`, dto),
}
