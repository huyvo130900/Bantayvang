export interface UserDto {
  id: number
  maNhanVien: string | null
  username: string | null
  email: string | null
  soDienThoai: string | null
  fullName: string | null
  chucDanh: string | null
  department: string | null
  roleId: number | null
  tenVaiTro: string | null
  idKhoaQuanLy?: number | null
  tenKhoaQuanLy?: string | null
  status: boolean | null
  createdAt: string | null
  lanDangNhapCuoi: string | null
  isDeleted: boolean
}

export interface CreateUserDto {
  username: string
  password: string
  email?: string
  soDienThoai?: string
  fullName: string
  maNhanVien?: string
  chucDanh?: string
  department?: string
  roleId: number
  idKhoaQuanLy?: number
  status: boolean
}

export interface UpdateUserDto {
  email?: string
  soDienThoai?: string
  fullName: string
  maNhanVien?: string
  chucDanh?: string
  department?: string
  roleId: number
  idKhoaQuanLy?: number
  status: boolean
}

export interface UserFilterDto {
  pageNumber: number
  pageSize: number
  roleId?: number
  status?: boolean
  department?: string
  searchKeyword?: string
  includeDeleted?: boolean
}
