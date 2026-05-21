export interface UserDto {
  id: number
  maNhanVien: string | null
  tenDangNhap: string | null
  email: string | null
  hoTen: string | null
  chucDanh: string | null
  khoaPhong: string | null
  idVaiTro: number | null
  tenVaiTro: string | null
  trangThai: boolean | null
  ngayTao: string | null
  lanDangNhapCuoi: string | null
}

export interface CreateUserDto {
  tenDangNhap: string
  matKhau: string
  email: string
  hoTen: string
  maNhanVien?: string
  chucDanh?: string
  khoaPhong?: string
  idVaiTro: number
  trangThai: boolean
}

export interface UpdateUserDto {
  email: string
  hoTen: string
  maNhanVien?: string
  chucDanh?: string
  khoaPhong?: string
  idVaiTro: number
  trangThai: boolean
}

export interface UserFilterDto {
  pageNumber: number
  pageSize: number
  idVaiTro?: number
  trangThai?: boolean
  khoaPhong?: string
  searchKeyword?: string
}
