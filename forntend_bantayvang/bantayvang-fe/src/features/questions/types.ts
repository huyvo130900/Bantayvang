export interface LuachonDto {
  id: number
  idCauHoi: number | null
  noiDung: string | null
  laDapAnDung: boolean | null
  thuTu: number | null
}

export interface CauhoiDto {
  id: number
  idDanhMuc: number | null
  tenDanhMuc: string | null
  idLoaiCauHoi: number | null
  tenLoaiCauHoi: string | null
  noiDung: string | null
  diem: number | null
  doKho: string | null
  khoaPhong: string | null
  hinhAnh: string | null
  ngayTao: string | null
  ngayCapNhat: string | null
  danhSachLuaChon: LuachonDto[]
}

export interface CreateCauhoiDto {
  noiDung: string
  idLoaiCauHoi?: number
  doKho?: string
  mucDo?: string
  diem?: number
  hinhAnh?: string
  idDanhMuc?: number
  khoaPhong?: string
  danhSachLuaChon: CreateLuachonDto[]
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
  idDanhMuc?: number
  idLoaiCauHoi?: number
  doKho?: string
  khoaPhong?: string
  searchKeyword?: string
  pageNumber: number
  pageSize: number
}

export interface DanhmucauhoiDto {
  id: number
  tenDanhMuc: string | null
  mota: string | null
}

export interface LoaicauhoiDto {
  id: number
  tenLoai: string | null
  moTa: string | null
}

export interface CreateDanhmucDto {
  tenDanhMuc: string
  mota?: string
}

export interface CreateLoaicauhoiDto {
  tenLoai: string
  moTa?: string
}
