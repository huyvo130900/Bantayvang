export interface DepartmentDto {
  id: number
  maKhoa: string
  tenKhoa: string
  moTa?: string
  trangThai: boolean
  deptManagerId?: number
  tenQuanLy?: string
  ngayTao: string
  ngayCapNhat?: string
}

export interface CreateDepartmentDto {
  maKhoa: string
  tenKhoa: string
  moTa?: string
  trangThai: boolean
}

export interface UpdateDepartmentDto {
  tenKhoa: string
  moTa?: string
  trangThai: boolean
}

export interface AssignManagerDto {
  deptManagerId: number
}

export interface ExamVisibilityDto {
  congBoKetQua: boolean
}

export interface DepartmentDashboardDto {
  idKhoa: number
  tenKhoa: string
  tongSoCauHoi: number
  tongSoDeThi: number
  tongSoThiSinh: number
  diemTrungBinh: number
  kyThiGanDay: KyThiSummaryDto[]
}

export interface KyThiSummaryDto {
  id: number
  tenKyThi: string
  thoiGianBatDau?: string
  thoiGianKetThuc?: string
  trangThai: string
  soDeThi: number
  soThiSinh: number
}
