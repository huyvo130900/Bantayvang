import { z } from 'zod'
import { ROLE_IDS } from '@/lib/constants'

export const createUserSchema = z.object({
  tenDangNhap: z
    .string()
    .min(3, 'Mã nhân viên tối thiểu 3 ký tự')
    .max(100, 'Mã nhân viên tối đa 100 ký tự')
    .regex(/^[a-zA-Z0-9_.-]+$/, 'Chỉ chấp nhận chữ, số, dấu chấm, gạch dưới, gạch ngang'),
  matKhau: z
    .string()
    .min(6, 'Mật khẩu tối thiểu 6 ký tự')
    .max(100, 'Mật khẩu tối đa 100 ký tự'),
  hoTen: z.string().min(1, 'Vui lòng nhập họ tên').max(255),
  maNhanVien: z.string().max(50).optional().or(z.literal('')),
  chucDanh: z.string().max(100).optional().or(z.literal('')),
  khoaPhong: z.string().max(100).optional().or(z.literal('')),
  // Allow 1,3,5 (Admin, Student, DeptManager) - 2 and 4 are obsolete
  idVaiTro: z.number().min(1).max(5),
  idKhoaQuanLy: z.number().optional().nullable(),
  trangThai: z.boolean(),
}).superRefine((data, ctx) => {
  // If DeptManager role, idKhoaQuanLy is required
  if (data.idVaiTro === ROLE_IDS.DEPT_MANAGER && !data.idKhoaQuanLy) {
    ctx.addIssue({
      code: z.ZodIssueCode.custom,
      message: 'Vui lòng chọn khoa quản lý cho tài khoản Quản lý Khoa',
      path: ['idKhoaQuanLy'],
    })
  }
})

export const updateUserSchema = z.object({
  hoTen: z.string().min(1, 'Vui lòng nhập họ tên').max(255),
  maNhanVien: z.string().max(50).optional().or(z.literal('')),
  chucDanh: z.string().max(100).optional().or(z.literal('')),
  khoaPhong: z.string().max(100).optional().or(z.literal('')),
  idVaiTro: z.number().min(1).max(5),
  idKhoaQuanLy: z.number().optional().nullable(),
  trangThai: z.boolean(),
}).superRefine((data, ctx) => {
  // If DeptManager role, idKhoaQuanLy is required
  if (data.idVaiTro === ROLE_IDS.DEPT_MANAGER && !data.idKhoaQuanLy) {
    ctx.addIssue({
      code: z.ZodIssueCode.custom,
      message: 'Vui lòng chọn khoa quản lý cho tài khoản Quản lý Khoa',
      path: ['idKhoaQuanLy'],
    })
  }
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
