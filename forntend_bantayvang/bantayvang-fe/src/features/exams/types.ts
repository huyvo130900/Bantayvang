export interface DethiDto {
  id: number
  maDeThi: string | null
  tenDeThi: string | null
  thoiGianLamBai: number | null
  tongDiem: number | null
  thoiGianBatDau: string | null
  linkTruyCap: string | null
  trangThai: string | null
  ngayTao: string | null
  soCauHoi: number
}

export interface CreateDethiDto {
  maDeThi: string
  tenDeThi: string
  thoiGianLamBai: number
  thoiGianBatDau: string
  trangThai?: string
  danhSachIdCauHoi: number[]
}

export interface ExamAssignmentDto {
  id: number
  examId: number
  maDeThi: string | null
  tenDeThi: string | null
  userId: number
  username: string | null
  fullName: string | null
  assignedAt: string
  customStartTime: string | null
  extraMinutes: number | null
  isActive: boolean
  note: string | null
}

export interface CreateExamAssignmentDto {
  examId: number
  userIds: number[]
  customStartTime?: string
  note?: string
}

export interface ExtendExamTimeDto {
  baiThiId: number
  additionalMinutes: number
  reason?: string
}
