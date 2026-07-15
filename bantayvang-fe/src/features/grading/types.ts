export interface ExamResultDetailDto {
  baiThiId: number
  userId: number | null
  username: string | null
  fullName: string | null
  maNhanVien?: string | null
  department: string | null
  examId: number
  examPaperId?: number | null
  examPaperCode: string | null
  examPaperName: string | null
  thoiGianBatDau: string | null
  submitTime?: string
  durationMinutes?: number
  durationSeconds?: number
  totalScore?: number | null
  correctAnswers: number | null
  tongSoCau: number | null
  status: string | null
  pass: boolean
  soCauDungToiThieu?: number | null
  soCanhBao: number | null
  soLanThi?: number        // số lần đã thi
  soLanGianLan?: number    // tổng số lần gian lận
  soLanThiLai?: number
  isResultPublished?: boolean
  soCauDaCham?: number
  tongSoCauTracNghiem?: number
  soCauTracNghiemDaCham?: number
  tongSoCauTuLuan?: number
  soCauTuLuanDaCham?: number
  thoiGianKetThucCaThi?: string | null  // thời gian kết thúc ca thi để kiểm tra còn hạn không
  danhGiaKhoa?: string    // nhận xét của quản lý khoa
  answers?: AnswerDetailDto[]
}

export interface AnswerDetailDto {
  cauHoiId: number
  noiDungCauHoi: string | null
  questionCategory?: string | null
  idLuaChonDaChon: number | null
  noiDungDapAn: string | null
  cauTraLoiTuLuan: string | null
  isCorrect: boolean
  scoreObtained: number | null
  idLuaChonDung: number | null
  noiDungDapAnDung: string | null
  chiTietLamBaiId?: number | null
}

export interface ManualGradingDto {
  chiTietLamBaiId: number
  isCorrect: boolean | null
  nhanXet?: string
}

export interface PendingEssayDto {
  baiThiId: number
  userId: number | null
  username: string | null
  fullName: string | null
  maNhanVien?: string | null
  department: string | null
  examPaperCode: string | null
  examPaperName: string | null
  submitTime: string | null
  totalScore?: number | null
  correctAnswers: number | null
  tongSoCau: number | null
  status: string | null
  soCauTuLuanChuaCham: number
  tongSoCauTuLuan: number
  campaignName?: string | null
}
