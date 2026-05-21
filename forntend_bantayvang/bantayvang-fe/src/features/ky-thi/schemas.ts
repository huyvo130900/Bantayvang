import { z } from 'zod'

export const createKyThiSchema = z.object({
  maKyThi: z.string().min(1, 'Mã kỳ thi không được trống').max(50),
  tenKyThi: z.string().min(1, 'Tên kỳ thi không được trống').max(255),
  moTa: z.string().max(1000).optional().or(z.literal('')),
  loaiKyThi: z.string().optional().or(z.literal('')),
  thoiGianBatDau: z.string().optional().or(z.literal('')),
  thoiGianKetThuc: z.string().optional().or(z.literal('')),
  donViToChuc: z.string().optional().or(z.literal('')),
})

export const createCaThiSchema = z.object({
  kyThiId: z.number(),
  deThiId: z.number().optional(),
  tenCa: z.string().min(1, 'Tên ca thi không được trống').max(100),
  thoiGianBatDau: z.string().optional().or(z.literal('')),
  thoiGianKetThuc: z.string().optional().or(z.literal('')),
  soLuongToiDa: z.number().optional(),
  ghiChu: z.string().optional().or(z.literal('')),
})

export type CreateKyThiFormData = z.infer<typeof createKyThiSchema>
export type CreateCaThiFormData = z.infer<typeof createCaThiSchema>
