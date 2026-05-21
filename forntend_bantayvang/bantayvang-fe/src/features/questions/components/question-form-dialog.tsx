import { useEffect } from 'react'
import { useForm, Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { createQuestionSchema, type CreateQuestionFormData } from '../schemas'
import type { CauhoiDto, DanhmucauhoiDto, LoaicauhoiDto } from '../types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { ChoiceEditor } from './choice-editor'
import { X } from 'lucide-react'

interface QuestionFormDialogProps {
  open: boolean
  question: CauhoiDto | null
  categories: DanhmucauhoiDto[]
  questionTypes: LoaicauhoiDto[]
  onClose: () => void
  onSubmit: (data: CreateQuestionFormData) => void
  isLoading: boolean
}

export function QuestionFormDialog({
  open,
  question,
  categories,
  questionTypes,
  onClose,
  onSubmit,
  isLoading,
}: QuestionFormDialogProps) {
  const isEdit = !!question

  const form = useForm<CreateQuestionFormData>({
    resolver: zodResolver(createQuestionSchema),
    defaultValues: getDefaults(null),
  })

  useEffect(() => {
    form.reset(getDefaults(question))
  }, [question, form])

  if (!open) return null

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-2xl max-h-[90vh] overflow-y-auto">
        <div className="flex items-center justify-between p-4 border-b sticky top-0 bg-white z-10">
          <h2 className="text-lg font-semibold">
            {isEdit ? 'Sửa câu hỏi' : 'Thêm câu hỏi'}
          </h2>
          <Button variant="ghost" size="icon" onClick={onClose}>
            <X className="h-4 w-4" />
          </Button>
        </div>

        <form onSubmit={form.handleSubmit(onSubmit)} className="p-4 space-y-4">
          {/* Nội dung */}
          <div className="space-y-1">
            <label className="text-sm font-medium text-gray-700">Nội dung câu hỏi *</label>
            <textarea
              {...form.register('noiDung')}
              rows={3}
              className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
              placeholder="Nhập nội dung câu hỏi..."
            />
            {form.formState.errors.noiDung && (
              <p className="text-xs text-red-500">{form.formState.errors.noiDung.message}</p>
            )}
          </div>

          {/* Metadata */}
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-1">
              <label className="text-sm font-medium text-gray-700">Danh mục</label>
              <select
                {...form.register('idDanhMuc', { valueAsNumber: true })}
                className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm"
              >
                <option value="">-- Chọn danh mục --</option>
                {categories.map((c) => (
                  <option key={c.id} value={c.id}>{c.tenDanhMuc}</option>
                ))}
              </select>
            </div>

            <div className="space-y-1">
              <label className="text-sm font-medium text-gray-700">Loại câu hỏi</label>
              <select
                {...form.register('idLoaiCauHoi', { valueAsNumber: true })}
                className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm"
              >
                <option value="">-- Chọn loại --</option>
                {questionTypes.map((t) => (
                  <option key={t.id} value={t.id}>{t.tenLoai}</option>
                ))}
              </select>
            </div>
          </div>

          <div className="grid grid-cols-3 gap-4">
            <div className="space-y-1">
              <label className="text-sm font-medium text-gray-700">Độ khó</label>
              <select
                {...form.register('doKho')}
                className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm"
              >
                <option value="">-- Chọn --</option>
                <option value="De">Dễ</option>
                <option value="TrungBinh">Trung bình</option>
                <option value="Kho">Khó</option>
              </select>
            </div>

            <div className="space-y-1">
              <label className="text-sm font-medium text-gray-700">Điểm</label>
              <Input
                {...form.register('diem', { valueAsNumber: true })}
                type="number"
                step="0.5"
                min="0"
                placeholder="1"
              />
            </div>

            <div className="space-y-1">
              <label className="text-sm font-medium text-gray-700">Khoa/Phòng</label>
              <Input {...form.register('khoaPhong')} placeholder="Khoa Nội" />
            </div>
          </div>

          {/* Choices */}
          <Controller
            name="danhSachLuaChon"
            control={form.control}
            render={({ field, fieldState }) => (
              <ChoiceEditor
                choices={field.value}
                onChange={field.onChange}
                error={fieldState.error?.message || fieldState.error?.root?.message}
              />
            )}
          />

          {/* Actions */}
          <div className="flex justify-end gap-2 pt-4 border-t">
            <Button type="button" variant="outline" onClick={onClose}>
              Hủy
            </Button>
            <Button type="submit" disabled={isLoading}>
              {isLoading ? 'Đang lưu...' : isEdit ? 'Cập nhật' : 'Tạo mới'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  )
}

function getDefaults(question: CauhoiDto | null): CreateQuestionFormData {
  if (question) {
    return {
      noiDung: question.noiDung || '',
      idLoaiCauHoi: question.idLoaiCauHoi || undefined,
      idDanhMuc: question.idDanhMuc || undefined,
      doKho: question.doKho || undefined,
      diem: question.diem || undefined,
      khoaPhong: question.khoaPhong || undefined,
      hinhAnh: question.hinhAnh || undefined,
      danhSachLuaChon: question.danhSachLuaChon.map((l) => ({
        noiDung: l.noiDung || '',
        thuTu: l.thuTu || 1,
        laDapAnDung: l.laDapAnDung || false,
      })),
    }
  }
  return {
    noiDung: '',
    idLoaiCauHoi: undefined,
    idDanhMuc: undefined,
    doKho: undefined,
    diem: 1,
    khoaPhong: undefined,
    hinhAnh: undefined,
    danhSachLuaChon: [
      { noiDung: '', thuTu: 1, laDapAnDung: true },
      { noiDung: '', thuTu: 2, laDapAnDung: false },
      { noiDung: '', thuTu: 3, laDapAnDung: false },
      { noiDung: '', thuTu: 4, laDapAnDung: false },
    ],
  }
}
