export interface DangKyThiDto {
  id: number;
  hoTen: string;
  cccd: string;
  soDienThoai: string;
  email?: string;
  donViCongTac?: string;
  chuyenNganh?: string;
  khoaPhongId?: number;
  tenKhoaPhong?: string;
  mucDichThi?: string;
  trangThai: string;
  ngayDangKy: string;
  ghiChu?: string;
}

export interface CreateDangKyThiDto {
  hoTen: string;
  cccd: string;
  soDienThoai: string;
  email?: string;
  matKhau: string;
  donViCongTac?: string;
  chuyenNganh?: string;
  khoaPhongId?: number;
  mucDichThi?: string;
}

export interface RejectDto {
  reason?: string;
}
