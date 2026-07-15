import apiClient from '@/lib/axios'
import type { ApiResponse, PagedResult } from '@/types'
import type {
  QuestionDto,
  CreateQuestionDto,
  UpdateQuestionDto,
  QuestionFilterDto,
  LoaicauhoiDto,
  CreateLoaicauhoiDto,
} from './types'

const buildQueryString = (filter: QuestionFilterDto): string => {
  const params = new URLSearchParams()
  params.set('pageNumber', String(filter.pageNumber))
  params.set('pageSize', String(filter.pageSize))
  if (filter.questionCategoryId) params.set('questionCategoryId', String(filter.questionCategoryId))
  if (filter.difficulty) params.set('difficulty', filter.difficulty)
  if (filter.department) params.set('department', filter.department)
  if (filter.searchKeyword) params.set('searchKeyword', filter.searchKeyword)
  if (filter.showDuplicatesOnly) params.set('showDuplicatesOnly', String(filter.showDuplicatesOnly))
  if (filter.kyThiId) params.set('kyThiId', String(filter.kyThiId))
  if (filter.deThiId) params.set('deThiId', String(filter.deThiId))
  return params.toString()
}

export const questionsApi = {
  // Questions
  list: (filter: QuestionFilterDto) =>
    apiClient.get<ApiResponse<PagedResult<QuestionDto>>>(`/question?${buildQueryString(filter)}`),

  getById: (id: number) =>
    apiClient.get<ApiResponse<QuestionDto>>(`/question/${id}`),

  create: (data: CreateQuestionDto) =>
    apiClient.post<ApiResponse<QuestionDto>>('/question', data),

  update: (id: number, data: UpdateQuestionDto) =>
    apiClient.put<ApiResponse<QuestionDto>>(`/question/${id}`, data),

  delete: (id: number) =>
    apiClient.delete<ApiResponse>(`/question/${id}`),

  checkDuplicate: (content: string, department?: string, excludeId?: number) => {
    const params = new URLSearchParams()
    params.set('content', content)
    if (department) params.set('department', department)
    if (excludeId) params.set('excludeId', String(excludeId))
    return apiClient.get<ApiResponse<boolean>>(`/question/check-duplicate?${params.toString()}`)
  },

  importExcel: (file: File, department: string, questionCategoryId: number, isExamImport?: boolean, expectedCount?: number) => {
    const formData = new FormData()
    formData.append('file', file)
    formData.append('department', department)
    formData.append('questionCategoryId', String(questionCategoryId))
    if (isExamImport !== undefined) {
      formData.append('isExamImport', String(isExamImport))
    }
    if (expectedCount !== undefined && expectedCount !== null) {
      formData.append('expectedCount', String(expectedCount))
    }
    return apiClient.post<ApiResponse<QuestionDto[]>>('/question/import', formData, {
      headers: { 'Content-Type': undefined },
    })
  },

  downloadTemplate: (questionCategoryId: number, isExamImport?: boolean) => {
    const params = new URLSearchParams()
    params.set('questionCategoryId', String(questionCategoryId))
    if (isExamImport !== undefined) {
      params.set('isExamImport', String(isExamImport))
    }
    return apiClient.get(`/question/import-template?${params.toString()}`, { responseType: 'blob' })
  },

  // ✨ Template soạn sẵn đề thi (thông tin đề + danh sách câu hỏi)
  downloadDeThiTemplate: () =>
    apiClient.get('/question/import-template-examPaper', { responseType: 'blob' }),

  getRandom: (count: number) => {
    const params = new URLSearchParams({ count: String(count) })
    return apiClient.get<ApiResponse<QuestionDto[]>>(`/question/random?${params}`)
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
