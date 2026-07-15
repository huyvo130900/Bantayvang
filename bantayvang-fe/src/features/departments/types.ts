export interface DepartmentDto {
  id: number
  maKhoa: string
  departmentName: string
  moTa?: string
  status: boolean
  deptManagerId?: number
  tenQuanLy?: string
  createdAt: string
  updatedAt?: string
}

export interface CreateDepartmentDto {
  maKhoa: string
  departmentName: string
  moTa?: string
  status: boolean
}

export interface UpdateDepartmentDto {
  departmentName: string
  moTa?: string
  status: boolean
}

export interface AssignManagerDto {
  deptManagerId: number
}

export interface ExamVisibilityDto {
  isResultPublished: boolean
}

export interface DepartmentDashboardDto {
  idKhoa: number
  departmentName: string
  tongSoCauHoi: number
  tongSoDeThi: number
  tongSoThiSinh: number
  diemTrungBinh: number
  kyThiGanDay: KyThiSummaryDto[]
}

export interface KyThiSummaryDto {
  id: number
  campaignName: string
  thoiGianBatDau?: string
  thoiGianKetThuc?: string
  status: string
  soDeThi: number
  soThiSinh: number
}
