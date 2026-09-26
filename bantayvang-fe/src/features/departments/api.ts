import apiClient from '@/lib/axios'
import type { DepartmentDto, CreateDepartmentDto, UpdateDepartmentDto, AssignManagerDto, ExamVisibilityDto, DepartmentDashboardDto } from './types'

export const departmentApi = {
  getAll: (params?: { status?: boolean; search?: string; page?: number; pageSize?: number }) =>
    apiClient.get('/Department', { params }),

  getById: (id: number) =>
    apiClient.get<{ success: boolean; data: DepartmentDto }>(`/Department/${id}`),

  // Dashboard dành cho DeptManager — trả về dữ liệu thực của khoa mình
  getMyDashboard: () =>
    apiClient.get<{ success: boolean; message: string; data: DepartmentDashboardDto }>('/Department/my-dashboard'),

  create: (dto: CreateDepartmentDto) =>
    apiClient.post('/Department', dto),

  update: (id: number, dto: UpdateDepartmentDto) =>
    apiClient.put(`/Department/${id}`, dto),

  delete: (id: number) =>
    apiClient.delete(`/Department/${id}`),

  assignManager: (id: number, dto: AssignManagerDto) =>
    apiClient.post(`/Department/${id}/assign-manager`, dto),

  toggleExamVisibility: (examPaperId: number, dto: ExamVisibilityDto) =>
    apiClient.post(`/Department/exam/${examPaperId}/toggle-visibility`, dto),

  // ✨ Import danh sách khoa/phòng từ Excel (Admin only)
  importDepartments: (file: File) => {
    const formData = new FormData()
    formData.append('file', file)
    return apiClient.post('/Department/import', formData, {
      // BUG FIX: was hardcoded to the literal string 'multipart/form-data' (no boundary), which
      // makes axios/the browser send that Content-Type verbatim instead of auto-generating
      // 'multipart/form-data; boundary=...'. The browser only appends a boundary when it computes
      // the header itself - `undefined` deletes apiClient's default 'application/json' header so
      // that happens; the literal string suppressed it and silently broke every file upload.
      headers: { 'Content-Type': undefined },
    })
  },

  // ✨ Download template Excel import khoa/phòng
  downloadImportTemplate: () =>
    apiClient.get('/Department/import-template', { responseType: 'blob' }),
}
