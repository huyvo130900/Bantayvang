export interface ExamResultDetailDto {
  baiThiId: number
  userId: number | null
  username: string | null
  fullName: string | null
  maNhanVien?: string | null
  khoaPhong: string | null
  examId: number
  idDeThi?: number | null
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
  soCauDungToiThieu?: number | null
  soCanhBao: number | null
  soLanThi?: number        // số lần đã thi
  soLanGianLan?: number    // tổng số lần gian lận
  soLanThiLai?: number
  congBoKetQua?: boolean
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
  loaiCauHoi?: string | null
  idLuaChonDaChon: number | null
  noiDungDapAn: string | null
  cauTraLoiTuLuan: string | null
  isCorrect: boolean
  diemDatDuoc: number | null
  idLuaChonDung: number | null
  noiDungDapAnDung: string | null
  chiTietLamBaiId?: number | null
}

export interface ManualGradingDto {
  chiTietLamBaiId: number
  isCorrect: boolean
  nhanXet?: string
}
