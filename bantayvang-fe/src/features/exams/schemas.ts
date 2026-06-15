import { z } from 'zod'

export const createExamSchema = z.object({
  maDeThi: z
    .string()
    .min(1, 'Mã đề thi không được trống')
    .max(50)
    .regex(/^[A-Z0-9_-]+$/, 'Chỉ chấp nhận chữ hoa, số, gạch ngang, gạch dưới'),
  tenDeThi: z.string().max(255).optional().or(z.literal('')),
  thoiGianLamBai: z.number().min(1, 'Tối thiểu 1 phút').max(480, 'Tối đa 480 phút').optional(),
  thoiGianBatDau: z.string().optional().or(z.literal('')),
  trangThai: z.string().optional(),
  // Cấu hình câu hỏi random từ ngân hàng
  khoaPhong: z.string().min(1, 'Vui lòng chọn khoa/phòng').optional().or(z.literal('')),
  soCauRandom: z.number().min(1, 'Số câu phải >= 1').max(200, 'Tối đa 200 câu').optional(),
  // Legacy
  danhSachIdCauHoi: z.array(z.number()),
  kyThiId: z.preprocess(
    (val) => val === '' || val === null || val === undefined || Number.isNaN(Number(val)) ? undefined : Number(val),
    z.number({ message: 'Vui lòng chọn kỳ thi' }).min(1, 'Vui lòng chọn kỳ thi')
  ),
  // Số câu đúng tối thiểu để đạt
  soCauDungToiThieu: z.preprocess(
    (val) => val === '' || val === null || val === undefined ? null : Number(val),
    z.number().int('Phải là số nguyên').min(0, 'Không được âm').nullable().optional()
  ),
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
