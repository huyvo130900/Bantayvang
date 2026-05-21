export interface KyThiDto {
  id: number
  maKyThi: string | null
  tenKyThi: string | null
  moTa: string | null
  loaiKyThi: string | null
  trangThai: string | null
  thoiGianBatDau: string | null
  thoiGianKetThuc: string | null
  donViToChuc: string | null
  ngayTao: string
  soCaThi: number
  tongThiSinh: number
  danhSachCaThi: CaThiDto[]
}

export interface CaThiDto {
  id: number
  kyThiId: number
  deThiId: number | null
  maDeThi: string | null
  tenCa: string | null
  thoiGianBatDau: string | null
  thoiGianKetThuc: string | null
  soLuongToiDa: number | null
  trangThai: string | null
  ghiChu: string | null
  soThiSinhDaDangKy: number
}

export interface CreateKyThiDto {
  maKyThi: string
  tenKyThi: string
  moTa?: string
  loaiKyThi?: string
  thoiGianBatDau?: string
  thoiGianKetThuc?: string
  donViToChuc?: string
}

export interface UpdateKyThiDto extends CreateKyThiDto {
  trangThai: string
}

export interface CreateCaThiDto {
  kyThiId: number
  deThiId?: number
  tenCa: string
  thoiGianBatDau?: string
  thoiGianKetThuc?: string
  soLuongToiDa?: number
  ghiChu?: string
}
