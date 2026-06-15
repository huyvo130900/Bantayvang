export interface KyThiDto {
  id: number
  maKyThi: string | null
  tenKyThi: string | null
  moTa: string | null
  khoaPhongId: number | null
  tenKhoa: string | null
  trangThai: string | null
  thoiGianBatDau: string | null
  thoiGianKetThuc: string | null
  donViToChuc: string | null
  ngayTao: string
  soLuongDeThi: number
  tongThiSinh: number
  danhSachMaDeThi: string[]
  maDeThi: string | null
  soCauDungToiThieu?: number | null
  tongSoCauHoi?: number | null
}

export interface ExamGenerationConfig {
  soLuongDe: number
  tongSoCau: number
  soCauMC: number
  soCauEssay: number
  soCauEasy: number
  soCauMedium: number
  soCauHard: number
  khoaPhong?: string
}

export interface ExamCheckResult {
  canGenerate: boolean
  warnings: string[]
}

export interface CreateKyThiDto {
  maKyThi: string
  tenKyThi: string
  moTa?: string
  khoaPhongId?: number | null
  thoiGianBatDau?: string | null
  thoiGianKetThuc?: string | null
  donViToChuc?: string
  soCauDungToiThieu?: number | null
  tongSoCauHoi?: number | null
}

export interface UpdateKyThiDto extends CreateKyThiDto {
  trangThai: string
}
