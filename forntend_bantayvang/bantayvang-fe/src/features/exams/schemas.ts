import { z } from 'zod'

export const createExamSchema = z.object({
  maDeThi: z
    .string()
    .min(1, 'Mã đề thi không được trống')
    .max(50)
    .regex(/^[A-Z0-9_-]+$/, 'Chỉ chấp nhận chữ hoa, số, gạch ngang, gạch dưới'),
  tenDeThi: z.string().min(1, 'Tên đề thi không được trống').max(255),
  thoiGianLamBai: z.number().min(1, 'Tối thiểu 1 phút').max(480, 'Tối đa 480 phút'),
  thoiGianBatDau: z.string().min(1, 'Vui lòng chọn thời gian bắt đầu'),
  trangThai: z.string(),
  danhSachIdCauHoi: z.array(z.number()).min(1, 'Phải chọn ít nhất 1 câu hỏi'),
})

export const assignUsersSchema = z.object({
  examId: z.number(),
  userIds: z.array(z.number()).min(1, 'Phải chọn ít nhất 1 thí sinh'),
  customStartTime: z.string().optional(),
  note: z.string().optional(),
})

export const extendTimeSchema = z.object({
  baiThiId: z.number(),
  additionalMinutes: z.number().min(1, 'Tối thiểu 1 phút').max(120, 'Tối đa 120 phút'),
  reason: z.string().optional(),
})

export type CreateExamFormData = z.infer<typeof createExamSchema>
export type AssignUsersFormData = z.infer<typeof assignUsersSchema>
export type ExtendTimeFormData = z.infer<typeof extendTimeSchema>
