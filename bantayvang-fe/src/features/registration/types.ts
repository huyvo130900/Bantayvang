export interface ExamRegistrationDto {
  id: number;
  fullName: string;
  cccd: string;
  phoneNumber: string;
  email?: string;
  workUnit?: string;
  chuyenNganh?: string;
  departmentId?: number;
  tenKhoaPhong?: string;
  mucDichThi?: string;
  status: string;
  ngayDangKy: string;
  ghiChu?: string;
}

export interface CreateExamRegistrationDto {
  fullName: string;
  cccd: string;
  phoneNumber: string;
  email?: string;
  password: string;
  workUnit?: string;
  chuyenNganh?: string;
  departmentId?: number;
  mucDichThi?: string;
}

export interface RejectDto {
  reason?: string;
}
