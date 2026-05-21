export interface ExamQuestionDto {
  id: number
  noiDung: string | null
  diem: number | null
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
  trangThai: string | null
  thoiGianNop: string | null
  tongDiem: number | null
  soCauDung: number | null
  tongSoCau: number | null
  tongSoCanhBao: number | null
  tenDeThi: string | null
  thoiGianLamBai: number | null
  thoiGianBatDau: string | null
  thoiGianConLai: number | null // seconds
}

export interface StartExamDto {
  maDeThi: string
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
