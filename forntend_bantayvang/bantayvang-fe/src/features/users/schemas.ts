import { z } from 'zod'

export const createUserSchema = z.object({
  tenDangNhap: z
    .string()
    .min(3, 'Tên đăng nhập tối thiểu 3 ký tự')
    .max(100, 'Tên đăng nhập tối đa 100 ký tự')
    .regex(/^[a-zA-Z0-9_.-]+$/, 'Chỉ chấp nhận chữ, số, dấu chấm, gạch dưới, gạch ngang'),
  matKhau: z
    .string()
    .min(6, 'Mật khẩu tối thiểu 6 ký tự')
    .max(100, 'Mật khẩu tối đa 100 ký tự'),
  email: z.string().email('Email không hợp lệ'),
  hoTen: z.string().min(1, 'Vui lòng nhập họ tên').max(255),
  maNhanVien: z.string().max(50).optional().or(z.literal('')),
  chucDanh: z.string().max(100).optional().or(z.literal('')),
  khoaPhong: z.string().max(100).optional().or(z.literal('')),
  idVaiTro: z.number().min(1).max(4),
  trangThai: z.boolean(),
})

export const updateUserSchema = z.object({
  email: z.string().email('Email không hợp lệ'),
  hoTen: z.string().min(1, 'Vui lòng nhập họ tên').max(255),
  maNhanVien: z.string().max(50).optional().or(z.literal('')),
  chucDanh: z.string().max(100).optional().or(z.literal('')),
  khoaPhong: z.string().max(100).optional().or(z.literal('')),
  idVaiTro: z.number().min(1).max(4),
  trangThai: z.boolean(),
})

export const resetPasswordSchema = z.object({
  newPassword: z
    .string()
    .min(6, 'Mật khẩu tối thiểu 6 ký tự')
    .max(100, 'Mật khẩu tối đa 100 ký tự'),
})

export type CreateUserFormData = z.infer<typeof createUserSchema>
export type UpdateUserFormData = z.infer<typeof updateUserSchema>
export type ResetPasswordFormData = z.infer<typeof resetPasswordSchema>
