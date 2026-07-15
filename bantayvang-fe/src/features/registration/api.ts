import apiClient from '@/lib/axios'
import type { DangKyThiDto, CreateDangKyThiDto, RejectDto } from './types'

export const registrationApi = {
  create: (dto: CreateDangKyThiDto) =>
    apiClient.post<{ message: string; data: DangKyThiDto }>('/DangKyThi/public', dto),

  getPending: () =>
    apiClient.get<DangKyThiDto[]>('/DangKyThi/pending'),

  approve: (id: number) =>
    apiClient.post<{ message: string }>(`/DangKyThi/${id}/approve`),

  reject: (id: number, dto: RejectDto) =>
    apiClient.post<{ message: string }>(`/DangKyThi/${id}/reject`, dto),
}
