import { z } from 'zod'

export const createKyThiSchema = z.object({
  campaignCode: z.string().min(1, 'Mã kỳ thi không được trống').max(50),
  campaignName: z.string().min(1, 'Tên kỳ thi không được trống').max(255),
  description: z.string().max(1000).optional().or(z.literal('')),
  departmentIds: z.array(z.number()).optional(),
  accessMode: z.enum(['Department', 'AssignedList']).default('Department'),
  isPracticeMode: z.boolean().default(false),
  startTime: z.string().min(1, 'Thời gian bắt đầu không được trống'),
  endTime: z.string().min(1, 'Thời gian kết thúc không được trống'),
  organizedBy: z.string().optional().or(z.literal('')),
  minPassQuestions: z.preprocess(
    (val) => val === '' || val === null || val === undefined ? null : Number(val),
    z.number().min(0, 'Số câu đúng tối thiểu không được âm').max(200, 'Tối đa 200 câu').nullable().optional()
  ),
  totalQuestions: z.preprocess(
    (val) => val === '' || val === null || val === undefined ? null : Number(val),
    z.number().min(1, 'Tổng số câu hỏi mỗi đề phải lớn hơn 0').max(200, 'Tối đa 200 câu').nullable().optional()
  ),
  durationMinutes: z.preprocess(
    (val) => val === '' || val === null || val === undefined ? null : Number(val),
    z.number().min(1, 'Thời gian làm bài tối thiểu là 1 phút').max(1440, 'Tối đa 1440 phút').nullable().optional()
  ),
})

export type CreateKyThiFormData = z.infer<typeof createKyThiSchema>

export const getKyThiSchema = (isEdit: boolean, initialStart?: string | null, initialEnd?: string | null) => {
  return z.object({
    campaignCode: z.string().min(1, 'Mã kỳ thi không được trống').max(50),
    campaignName: z.string().min(1, 'Tên kỳ thi không được trống').max(255),
    description: z.string().max(1000).optional().or(z.literal('')),
    departmentIds: z.array(z.number()).optional(),
    accessMode: z.enum(['Department', 'AssignedList']).default('Department'),
    isPracticeMode: z.boolean().default(false),
    startTime: z.string().min(1, 'Thời gian bắt đầu không được trống'),
    endTime: z.string().min(1, 'Thời gian kết thúc không được trống'),
    organizedBy: z.string().optional().or(z.literal('')),
    minPassQuestions: z.preprocess(
      (val) => val === '' || val === null || val === undefined ? null : Number(val),
      z.number().min(0, 'Số câu đúng tối thiểu không được âm').max(200, 'Tối đa 200 câu').nullable().optional()
    ),
    totalQuestions: z.preprocess(
      (val) => val === '' || val === null || val === undefined ? null : Number(val),
      z.number().min(1, 'Tổng số câu hỏi mỗi đề phải lớn hơn 0').max(200, 'Tối đa 200 câu').nullable().optional()
    ),
    durationMinutes: z.preprocess(
      (val) => val === '' || val === null || val === undefined ? null : Number(val),
      z.number().min(1, 'Thời gian làm bài tối thiểu là 1 phút').max(1440, 'Tối đa 1440 phút').nullable().optional()
    ),
  }).refine((data) => {
    const start = new Date(data.startTime)
    const end = new Date(data.endTime)
    return end > start
  }, {
    message: 'Thời gian kết thúc phải sau thời gian bắt đầu',
    path: ['endTime'],
  }).refine((data) => {
    if (isEdit && initialStart) {
      const initDate = new Date(initialStart)
      const dataDate = new Date(data.startTime)
      if (Math.abs(initDate.getTime() - dataDate.getTime()) < 60000) return true
    }
    const start = new Date(data.startTime)
    const now = new Date()
    // 2 minutes buffer for latency
    now.setMinutes(now.getMinutes() - 2)
    return start >= now
  }, {
    message: 'Thời gian bắt đầu không được trước thời gian hiện tại',
    path: ['startTime'],
  }).refine((data) => {
    if (isEdit && initialEnd) {
      const initDate = new Date(initialEnd)
      const dataDate = new Date(data.endTime)
      if (Math.abs(initDate.getTime() - dataDate.getTime()) < 60000) return true
    }
    const end = new Date(data.endTime)
    const now = new Date()
    // 2 minutes buffer for latency
    now.setMinutes(now.getMinutes() - 2)
    return end >= now
  }, {
    message: 'Thời gian kết thúc không được trước thời gian hiện tại',
    path: ['endTime'],
  })
}

