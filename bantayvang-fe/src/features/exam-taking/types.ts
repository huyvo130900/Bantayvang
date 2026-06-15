export interface ExamQuestionDto {
  id: number
  noiDung: string | null
  hinhAnh: string | null
  thuTuCau: number
  danhSachLuaChon: ExamChoiceDto[]
  idLuaChonDaChon: number | null
  cauTraLoiTuLuan: string | null
  daLuu: boolean
}

export interface ExamChoiceDto {
  id: number
  noiDung: string | null
  thuTu: number
}

export interface BaithiDto {
  id: number
  idTaiKhoan: number
  idDeThi: number
  idKyThi?: number | null
  trangThai: string | null
  thoiGianNop: string | null
  tongDiem: number | null
  diemSo?: number | null  // computed: (soCauDung/tongSoCau)*10
  soCauDung: number | null
  tongSoCau: number | null
  tongSoCanhBao: number | null
  tenDeThi: string | null
  maDeThi: string | null
  thoiGianLamBai: number | null
  thoiGianBatDau: string | null
  thoiGianConLai: number | null // seconds
  congBoKetQua?: boolean
  pass?: boolean | null
}

export interface StartExamDto {
  maDeThi?: string
  kyThiId?: number
}

export interface SubmitAnswerDto {
  idBaiThi: number
  idCauHoi: number
  idLuaChonDaChon: number | null
  cauTraLoiTuLuan?: string
  daLuu: boolean
}

export interface SubmitExamDto {
  idBaiThi: number
  danhSachCauTraLoi: SubmitAnswerDto[]
}

export interface CheatingWarningDto {
  idBaiThi: number
  loaiCanhBao: string
  moTa?: string
}