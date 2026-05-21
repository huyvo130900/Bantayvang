import apiClient from '@/lib/axios'
import type { ApiResponse } from '@/types'
import type { KyThiDto, CaThiDto, CreateKyThiDto, UpdateKyThiDto, CreateCaThiDto } from './types'

export const kyThiApi = {
  getAll: (trangThai?: string) => {
    const params = trangThai ? `?trangThai=${trangThai}` : ''
    return apiClient.get<ApiResponse<KyThiDto[]>>(`/kythi${params}`)
  },

  getById: (id: number) =>
    apiClient.get<ApiResponse<KyThiDto>>(`/kythi/${id}`),

  create: (data: CreateKyThiDto) =>
    apiClient.post<ApiResponse<KyThiDto>>('/kythi', data),

  update: (id: number, data: UpdateKyThiDto) =>
    apiClient.put<ApiResponse<KyThiDto>>(`/kythi/${id}`, data),

  updateStatus: (id: number, trangThai: string) =>
    apiClient.post<ApiResponse>(`/kythi/${id}/status`, JSON.stringify(trangThai), {
      headers: { 'Content-Type': 'application/json' },
    }),

  delete: (id: number) =>
    apiClient.delete<ApiResponse>(`/kythi/${id}`),

  // Ca thi
  getCaThi: (kyThiId: number) =>
    apiClient.get<ApiResponse<CaThiDto[]>>(`/kythi/${kyThiId}/ca-thi`),

  createCaThi: (data: CreateCaThiDto) =>
    apiClient.post<ApiResponse<CaThiDto>>('/kythi/ca-thi', data),

  updateCaThi: (id: number, data: CreateCaThiDto) =>
    apiClient.put<ApiResponse<CaThiDto>>(`/kythi/ca-thi/${id}`, data),

  deleteCaThi: (id: number) =>
    apiClient.delete<ApiResponse>(`/kythi/ca-thi/${id}`),
}
