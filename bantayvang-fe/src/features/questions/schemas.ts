import { z } from 'zod'

const luachonSchema = z.object({
  noiDung: z.string().min(1, 'Nội dung lựa chọn không được trống'),
  thuTu: z.number(),
  laDapAnDung: z.boolean(),
})

export const createQuestionSchema = z.object({
  noiDung: z.string().min(1, 'Nội dung câu hỏi không được trống'),
  idLoaiCauHoi: z.number().optional(),
  doKho: z.string().optional(),
  khoaPhong: z.string().optional(),
  hinhAnh: z.string().optional(),
  danhSachLuaChon: z.array(luachonSchema).optional(),
})

export type CreateQuestionFormData = z.infer<typeof createQuestionSchema>
