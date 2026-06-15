export interface LuachonDto {
  id: number
  idCauHoi: number | null
  noiDung: string | null
  laDapAnDung: boolean | null
  thuTu: number | null
}

export interface CauhoiDto {
  id: number
  idLoaiCauHoi: number | null
  tenLoaiCauHoi: string | null
  noiDung: string | null
  doKho: string | null
  khoaPhong: string | null
  hinhAnh: string | null
  ngayTao: string | null
  ngayCapNhat: string | null
  danhSachLuaChon: LuachonDto[]
  danhSachKyThi?: string[]
  danhSachDeThi?: string[]
}

export interface CreateCauhoiDto {
  noiDung: string
  idLoaiCauHoi?: number
  doKho?: string
  mucDo?: string
  hinhAnh?: string
  khoaPhong?: string
  danhSachLuaChon?: CreateLuachonDto[]
}

export interface CreateLuachonDto {
  noiDung: string
  thuTu: number
  laDapAnDung: boolean
}

export interface UpdateCauhoiDto extends CreateCauhoiDto {
  id: number
}

export interface QuestionFilterDto {
  idLoaiCauHoi?: number
  doKho?: string
  khoaPhong?: string
  searchKeyword?: string
  showDuplicatesOnly?: boolean
  pageNumber: number
  pageSize: number
}



export interface LoaicauhoiDto {
  id: number
  tenLoai: string | null
  moTa: string | null
  soCauHoi?: number
}



export interface CreateLoaicauhoiDto {
  tenLoai: string
  moTa?: string
}
