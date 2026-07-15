export interface ExamCampaignDto {
  id: number
  campaignCode: string | null
  campaignName: string | null
  moTa: string | null
  khoaPhongId: number | null
  departmentName: string | null
  status: string | null
  thoiGianBatDau: string | null
  thoiGianKetThuc: string | null
  donViToChuc: string | null
  createdAt: string
  soLuongDeThi: number
  tongThiSinh: number
  danhSachMaDeThi: string[]
  examPaperCode: string | null
  soCauDungToiThieu?: number | null
  tongSoCauHoi?: number | null
  durationMinutes?: number | null
}

export interface ExamGenerationConfig {
  soLuongDe: number
  tongSoCau: number
  soCauMC: number
  soCauEssay: number
  soCauEasy: number
  soCauMedium: number
  soCauHard: number
  department?: string
}

export interface ExamCheckResult {
  canGenerate: boolean
  warnings: string[]
}

export interface CreateKyThiDto {
  campaignCode: string
  campaignName: string
  moTa?: string
  khoaPhongId?: number | null
  thoiGianBatDau?: string | null
  thoiGianKetThuc?: string | null
  donViToChuc?: string
  soCauDungToiThieu?: number | null
  tongSoCauHoi?: number | null
  durationMinutes?: number | null
}

export interface UpdateKyThiDto extends CreateKyThiDto {
  status: string
}
