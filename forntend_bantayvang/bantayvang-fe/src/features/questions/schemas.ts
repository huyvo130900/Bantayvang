import { z } from 'zod'

const luachonSchema = z.object({
  noiDung: z.string().min(1, 'Nội dung lựa chọn không được trống'),
  thuTu: z.number(),
  laDapAnDung: z.boolean(),
})

export const createQuestionSchema = z.object({
  noiDung: z.string().min(1, 'Nội dung câu hỏi không được trống'),
  idLoaiCauHoi: z.number().optional(),
  idDanhMuc: z.number().optional(),
  doKho: z.string().optional(),
  diem: z.number().min(0).optional(),
  khoaPhong: z.string().optional(),
  hinhAnh: z.string().optional(),
  danhSachLuaChon: z
    .array(luachonSchema)
    .min(2, 'Phải có ít nhất 2 lựa chọn')
    .refine(
      (choices) => choices.some((c) => c.laDapAnDung),
      'Phải có ít nhất 1 đáp án đúng'
    ),
})

export type CreateQuestionFormData = z.infer<typeof createQuestionSchema>
