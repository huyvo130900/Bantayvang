import { z } from 'zod'

const luachonSchema = z.object({
  content: z.string().min(1, 'Nội dung lựa chọn không được trống'),
  orderIndex: z.number(),
  isCorrect: z.boolean(),
})

export const createQuestionSchema = z.object({
  content: z.string().min(1, 'Nội dung câu hỏi không được trống'),
  questionCategoryId: z.number().optional(),
  difficulty: z.string().optional(),
  department: z.string().optional(),
  imageUrl: z.string().optional(),
  options: z.array(luachonSchema).optional(),
})

export type CreateQuestionFormData = z.infer<typeof createQuestionSchema>
