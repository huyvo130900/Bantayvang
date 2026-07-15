export interface ExamRegistrationDto {
  id: number;
  fullName: string;
  cccd: string;
  soDienThoai: string;
  email?: string;
  donViCongTac?: string;
  chuyenNganh?: string;
  khoaPhongId?: number;
  tenKhoaPhong?: string;
  mucDichThi?: string;
  status: string;
  ngayDangKy: string;
  ghiChu?: string;
}

export interface CreateExamRegistrationDto {
  fullName: string;
  cccd: string;
  soDienThoai: string;
  email?: string;
  password: string;
  donViCongTac?: string;
  chuyenNganh?: string;
  khoaPhongId?: number;
  mucDichThi?: string;
}

export interface RejectDto {
  reason?: string;
}
