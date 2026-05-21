export interface ExamResultDetailDto {
  baiThiId: number
  userId: number | null
  username: string | null
  fullName: string | null
  khoaPhong: string | null
  examId: number
  maDeThi: string | null
  tenDeThi: string | null
  thoiGianBatDau: string | null
  thoiGianNop: string | null
  durationMinutes: number | null
  tongDiem: number | null
  soCauDung: number | null
  tongSoCau: number | null
  trangThai: string | null
  pass: boolean
  soCanhBao: number | null
  answers?: AnswerDetailDto[]
}

export interface AnswerDetailDto {
  cauHoiId: number
  noiDungCauHoi: string | null
  diem: number | null
  idLuaChonDaChon: number | null
  noiDungDapAn: string | null
  cauTraLoiTuLuan: string | null
  isCorrect: boolean
  diemDatDuoc: number | null
  idLuaChonDung: number | null
  noiDungDapAnDung: string | null
}

export interface ManualGradingDto {
  chiTietLamBaiId: number
  diemDatDuoc: number
  nhanXet?: string
}
