import apiClient from '@/lib/axios'
import type { ApiResponse, PagedResult } from '@/types'
import type {
  CauhoiDto,
  CreateCauhoiDto,
  UpdateCauhoiDto,
  QuestionFilterDto,
  LoaicauhoiDto,
  CreateLoaicauhoiDto,
} from './types'

const buildQueryString = (filter: QuestionFilterDto): string => {
  const params = new URLSearchParams()
  params.set('pageNumber', String(filter.pageNumber))
  params.set('pageSize', String(filter.pageSize))
  if (filter.idLoaiCauHoi) params.set('idLoaiCauHoi', String(filter.idLoaiCauHoi))
  if (filter.doKho) params.set('doKho', filter.doKho)
  if (filter.khoaPhong) params.set('khoaPhong', filter.khoaPhong)
  if (filter.searchKeyword) params.set('searchKeyword', filter.searchKeyword)
  if (filter.showDuplicatesOnly) params.set('showDuplicatesOnly', String(filter.showDuplicatesOnly))
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

  checkDuplicate: (noiDung: string, khoaPhong?: string, excludeId?: number) => {
    const params = new URLSearchParams()
    params.set('noiDung', noiDung)
    if (khoaPhong) params.set('khoaPhong', khoaPhong)
    if (excludeId) params.set('excludeId', String(excludeId))
    return apiClient.get<ApiResponse<boolean>>(`/cauhoi/check-duplicate?${params.toString()}`)
  },

  importExcel: (file: File, khoaPhong: string, idLoaiCauHoi: number, isExamImport?: boolean, expectedCount?: number) => {
    const formData = new FormData()
    formData.append('file', file)
    formData.append('khoaPhong', khoaPhong)
    formData.append('idLoaiCauHoi', String(idLoaiCauHoi))
    if (isExamImport !== undefined) {
      formData.append('isExamImport', String(isExamImport))
    }
    if (expectedCount !== undefined && expectedCount !== null) {
      formData.append('expectedCount', String(expectedCount))
    }
    return apiClient.post<ApiResponse<CauhoiDto[]>>('/cauhoi/import', formData, {
      headers: { 'Content-Type': undefined },
    })
  },

  downloadTemplate: (idLoaiCauHoi: number, isExamImport?: boolean) => {
    const params = new URLSearchParams()
    params.set('idLoaiCauHoi', String(idLoaiCauHoi))
    if (isExamImport !== undefined) {
      params.set('isExamImport', String(isExamImport))
    }
    return apiClient.get(`/cauhoi/import-template?${params.toString()}`, { responseType: 'blob' })
  },

  // ✨ Template soạn sẵn đề thi (thông tin đề + danh sách câu hỏi)
  downloadDeThiTemplate: () =>
    apiClient.get('/cauhoi/import-template-dethi', { responseType: 'blob' }),

  getRandom: (count: number) => {
    const params = new URLSearchParams({ count: String(count) })
    return apiClient.get<ApiResponse<CauhoiDto[]>>(`/cauhoi/random?${params}`)
  },

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
      headers: { 'Content-Type': undefined },
    })
  },
}
