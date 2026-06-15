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
  khoaPhong?: string | null
  soCauRandom?: number | null
  congBoKetQua?: boolean
  thoiGianCongBo?: string | null
  kyThiId?: number | null
  soCauDungToiThieu?: number | null
}

export interface CreateDethiDto {
  maDeThi: string
  tenDeThi?: string
  thoiGianLamBai: number
  thoiGianBatDau?: string
  trangThai?: string
  // Mới: chọn câu hỏi theo khoa + số câu random
  khoaPhong?: string
  soCauRandom?: number
  // Legacy: chọn tay (không dùng nữa nhưng giữ tương thích)
  danhSachIdCauHoi: number[]
  kyThiId?: number
  soCauDungToiThieu?: number | null
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
  // Trạng thái bài thi
  trangThai: string  // Pending | InProgress | Completed | AutoSubmitted
  // Kết quả nếu đã thi xong
  baithiId: number | null
  diemSo: number | null
  tongDiem: number | null
  soCauDung: number | null
  tongSoCau: number | null
  ngayHoanThanh: string | null
  datYeuCau: boolean | null
  thoiGianBatDau: string | null
  thoiGianKetThuc: string | null
  thoiGianLamBai: number | null
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

// Extended assignment type with exam session info (legacy, kept for compatibility)
export interface MyExamDto {
  id: number
  examId: number
  maDeThi: string | null
  tenDeThi: string | null
  thoiGianBatDau: string | null
  thoiGianKetThuc: string | null
  thoiGianLamBai: number | null
  trangThai: string | null
  baithiId: number | null
  ghiChu: string | null
  extraMinutes: number | null
}

export interface ExamPreviewDtoFE {
  id: number
  maDeThi: string | null
  tenDeThi: string | null
  thoiGianLamBai: number | null
  trangThai: string | null
  khoaPhong: string | null
  congBoKetQua: boolean
  cauHois: QuestionPreviewFE[]
}

export interface QuestionPreviewFE {
  id: number
  noiDung: string | null
  chuDe: string | null
  luachons: ChoicePreviewFE[]
}

export interface ChoicePreviewFE {
  id: number
  noiDung: string | null
  laDapAnDung: boolean | null
}
