import { z } from 'zod'
import { ROLE_IDS } from '@/lib/constants'

export const createUserSchema = z.object({
  username: z
    .string()
    .min(3, 'Mã nhân viên tối thiểu 3 ký tự')
    .max(100, 'Mã nhân viên tối đa 100 ký tự')
    .regex(/^[a-zA-Z0-9_.-]+$/, 'Chỉ chấp nhận chữ, số, dấu chấm, gạch dưới, gạch ngang'),
  password: z
    .string()
    .min(6, 'Mật khẩu tối thiểu 6 ký tự')
    .max(100, 'Mật khẩu tối đa 100 ký tự'),
  fullName: z.string().min(1, 'Vui lòng nhập họ tên').max(255),
  employeeCode: z.string().max(50).optional().or(z.literal('')),
  jobTitle: z.string().max(100).optional().or(z.literal('')),
  department: z.string().max(100).optional().or(z.literal('')),
  email: z.union([z.string().email('Email không hợp lệ'), z.literal('')]).optional(),
  phoneNumber: z.string().max(20, 'Số điện thoại tối đa 20 ký tự').optional().or(z.literal('')),
  // Allow 1,3,5,6 (Admin, Student, DeptManager, ThiSinhNgoai) - 2 and 4 are obsolete
  roleId: z.number().min(1).max(6),
  idKhoaQuanLy: z.number().optional().nullable(),
  status: z.boolean(),
}).superRefine((data, ctx) => {
  // If DeptManager role, idKhoaQuanLy is required
  if (data.roleId === ROLE_IDS.DEPT_MANAGER && !data.idKhoaQuanLy) {
    ctx.addIssue({
      code: z.ZodIssueCode.custom,
      message: 'Vui lòng chọn khoa quản lý cho tài khoản Quản lý Khoa',
      path: ['idKhoaQuanLy'],
    })
  }
})

export const updateUserSchema = z.object({
  fullName: z.string().min(1, 'Vui lòng nhập họ tên').max(255),
  employeeCode: z.string().max(50).optional().or(z.literal('')),
  jobTitle: z.string().max(100).optional().or(z.literal('')),
  department: z.string().max(100).optional().or(z.literal('')),
  email: z.union([z.string().email('Email không hợp lệ'), z.literal('')]).optional(),
  phoneNumber: z.string().max(20, 'Số điện thoại tối đa 20 ký tự').optional().or(z.literal('')),
  roleId: z.number().min(1).max(5),
  idKhoaQuanLy: z.number().optional().nullable(),
  status: z.boolean(),
}).superRefine((data, ctx) => {
  // If DeptManager role, idKhoaQuanLy is required
  if (data.roleId === ROLE_IDS.DEPT_MANAGER && !data.idKhoaQuanLy) {
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
