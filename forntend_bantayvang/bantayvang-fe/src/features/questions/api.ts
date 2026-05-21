import apiClient from '@/lib/axios'
import type { ApiResponse, PagedResult } from '@/types'
import type {
  CauhoiDto,
  CreateCauhoiDto,
  UpdateCauhoiDto,
  QuestionFilterDto,
  DanhmucauhoiDto,
  LoaicauhoiDto,
  CreateDanhmucDto,
  CreateLoaicauhoiDto,
} from './types'

const buildQueryString = (filter: QuestionFilterDto): string => {
  const params = new URLSearchParams()
  params.set('pageNumber', String(filter.pageNumber))
  params.set('pageSize', String(filter.pageSize))
  if (filter.idDanhMuc) params.set('idDanhMuc', String(filter.idDanhMuc))
  if (filter.idLoaiCauHoi) params.set('idLoaiCauHoi', String(filter.idLoaiCauHoi))
  if (filter.doKho) params.set('doKho', filter.doKho)
  if (filter.khoaPhong) params.set('khoaPhong', filter.khoaPhong)
  if (filter.searchKeyword) params.set('searchKeyword', filter.searchKeyword)
  return params.toString()
}

export const questionsApi = {
  // Questions
  list: (filter: QuestionFilterDto) =>
    apiClient.get<ApiResponse<PagedResult<CauhoiDto>>>(`/cauhoi?${buildQueryString(filter)}`),

  getById: (id: number) =>
    apiClient.get<ApiResponse<CauhoiDto>>(`/cauhoi/${id}`),

  create: (data: CreateCauhoiDto) =>
    apiClient.post<ApiResponse<CauhoiDto>>('/cauhoi', data),

  update: (id: number, data: UpdateCauhoiDto) =>
    apiClient.put<ApiResponse<CauhoiDto>>(`/cauhoi/${id}`, data),

  delete: (id: number) =>
    apiClient.delete<ApiResponse>(`/cauhoi/${id}`),

  importExcel: (file: File) => {
    const formData = new FormData()
    formData.append('file', file)
    return apiClient.post<ApiResponse<CauhoiDto[]>>('/cauhoi/import', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
  },

  downloadTemplate: () =>
    apiClient.get('/cauhoi/import-template', { responseType: 'blob' }),

  getRandom: (count: number, danhMucId?: number) => {
    const params = new URLSearchParams({ count: String(count) })
    if (danhMucId) params.set('danhMucId', String(danhMucId))
    return apiClient.get<ApiResponse<CauhoiDto[]>>(`/cauhoi/random?${params}`)
  },

  // Categories
  getCategories: () =>
    apiClient.get<ApiResponse<DanhmucauhoiDto[]>>('/category/categories'),

  createCategory: (data: CreateDanhmucDto) =>
    apiClient.post<ApiResponse<DanhmucauhoiDto>>('/category/categories', data),

  updateCategory: (id: number, data: CreateDanhmucDto) =>
    apiClient.put<ApiResponse<DanhmucauhoiDto>>(`/category/categories/${id}`, data),

  deleteCategory: (id: number) =>
    apiClient.delete<ApiResponse>(`/category/categories/${id}`),

  // Question types
  getQuestionTypes: () =>
    apiClient.get<ApiResponse<LoaicauhoiDto[]>>('/category/types'),

  createQuestionType: (data: CreateLoaicauhoiDto) =>
    apiClient.post<ApiResponse<LoaicauhoiDto>>('/category/types', data),

  updateQuestionType: (id: number, data: CreateLoaicauhoiDto) =>
    apiClient.put<ApiResponse<LoaicauhoiDto>>(`/category/types/${id}`, data),

  deleteQuestionType: (id: number) =>
    apiClient.delete<ApiResponse>(`/category/types/${id}`),

  // Upload
  uploadImage: (file: File) => {
    const formData = new FormData()
    formData.append('file', file)
    return apiClient.post<ApiResponse<{ url: string; message: string }>>('/upload/image?folder=questions', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
  },
}
