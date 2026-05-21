import { z } from 'zod'

export const loginSchema = z.object({
  username: z
    .string()
    .min(3, 'Tên đăng nhập tối thiểu 3 ký tự')
    .max(100, 'Tên đăng nhập tối đa 100 ký tự'),
  password: z
    .string()
    .min(6, 'Mật khẩu tối thiểu 6 ký tự')
    .max(100, 'Mật khẩu tối đa 100 ký tự'),
  rememberMe: z.boolean(),
})

export type LoginFormData = z.infer<typeof loginSchema>
