import { z } from 'zod'

export const createExamSchema = z.object({
  examPaperCode: z
    .string()
    .min(1, 'Mã đề thi không được trống')
    .max(50)
    .regex(/^[A-Z0-9_-]+$/, 'Chỉ chấp nhận chữ hoa, số, gạch ngang, gạch dưới'),
  examPaperName: z.string().max(255).optional().or(z.literal('')),
  durationMinutes: z.number().min(1, 'Tối thiểu 1 phút').max(1008000, 'Tối đa 1008000 phút').optional(),
  startTime: z.string().optional().or(z.literal('')),
  status: z.string().optional(),
  // Cấu hình câu hỏi random từ ngân hàng
  department: z.string().min(1, 'Vui lòng chọn khoa/phòng').optional().or(z.literal('')),
  randomQuestionCount: z.number().min(1, 'Số câu phải >= 1').max(200, 'Tối đa 200 câu').optional(),
  // Legacy
  questionIds: z.array(z.number()),
  examCampaignId: z.preprocess(
    (val) => val === '' || val === null || val === undefined || Number.isNaN(Number(val)) ? undefined : Number(val),
    z.number({ message: 'Vui lòng chọn kỳ thi' }).min(1, 'Vui lòng chọn kỳ thi')
  ),
  // Số câu đúng tối thiểu để đạt
  minPassQuestions: z.preprocess(
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
  examSubmissionId: z.number(),
  additionalMinutes: z.number().min(1, 'Tối thiểu 1 phút').max(120, 'Tối đa 120 phút'),
  reason: z.string().optional(),
})

export type CreateExamFormData = z.infer<typeof createExamSchema>
export type AssignUsersFormData = z.infer<typeof assignUsersSchema>
export type ExtendTimeFormData = z.infer<typeof extendTimeSchema>
