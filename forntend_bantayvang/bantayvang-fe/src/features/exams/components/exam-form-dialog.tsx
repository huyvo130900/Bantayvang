import { useState, useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { createExamSchema, type CreateExamFormData } from '../schemas'
import { questionsApi } from '@/features/questions/api'
import type { CauhoiDto } from '@/features/questions/types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { X, Search, Check } from 'lucide-react'

interface ExamFormDialogProps {
  open: boolean
  onClose: () => void
  onSubmit: (data: CreateExamFormData) => void
  isLoading: boolean
}

export function ExamFormDialog({ open, onClose, onSubmit, isLoading }: ExamFormDialogProps) {
  const [questions, setQuestions] = useState<CauhoiDto[]>([])
  const [selectedIds, setSelectedIds] = useState<number[]>([])
  const [searchKeyword, setSearchKeyword] = useState('')
  const [loadingQuestions, setLoadingQuestions] = useState(false)

  const form = useForm<CreateExamFormData>({
    resolver: zodResolver(createExamSchema),
    defaultValues: {
      maDeThi: '',
      tenDeThi: '',
      thoiGianLamBai: 60,
      thoiGianBatDau: '',
      trangThai: 'Draft',
      danhSachIdCauHoi: [],
    },
  })

  useEffect(() => {
    if (open) {
      loadQuestions()
    }
  }, [open])

  useEffect(() => {
    form.setValue('danhSachIdCauHoi', selectedIds)
  }, [selectedIds, form])

  const loadQuestions = async () => {
    setLoadingQuestions(true)
    try {
      const response = await questionsApi.list({
        pageNumber: 1,
        pageSize: 100,
        searchKeyword: searchKeyword || undefined,
      })
      if (response.data.success && response.data.data) {
        setQuestions(response.data.data.items)
      }
    } catch {
      // handle error
    } finally {
      setLoadingQuestions(false)
    }
  }

  const toggleQuestion = (id: number) => {
    setSelectedIds((prev) =>
      prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id]
    )
  }

  const handleSearchQuestions = () => {
    loadQuestions()
  }

  if (!open) return null

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-3xl max-h-[90vh] overflow-y-auto">
        <div className="flex items-center justify-between p-4 border-b sticky top-0 bg-white z-10">
          <h2 className="text-lg font-semibold">Tạo đề thi mới</h2>
          <Button variant="ghost" size="icon" onClick={onClose}>
            <X className="h-4 w-4" />
          </Button>
        </div>

        <form onSubmit={form.handleSubmit(onSubmit)} className="p-4 space-y-4">
          {/* Basic info */}
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-1">
              <label className="text-sm font-medium text-gray-700">Mã đề thi *</label>
              <Input {...form.register('maDeThi')} placeholder="DETHI_001" />
              {form.formState.errors.maDeThi && (
                <p className="text-xs text-red-500">{form.formState.errors.maDeThi.message}</p>
              )}
            </div>
            <div className="space-y-1">
              <label className="text-sm font-medium text-gray-700">Thời gian (phút) *</label>
              <Input {...form.register('thoiGianLamBai', { valueAsNumber: true })} type="number" min={1} max={480} />
              {form.formState.errors.thoiGianLamBai && (
                <p className="text-xs text-red-500">{form.formState.errors.thoiGianLamBai.message}</p>
              )}
            </div>
          </div>

          <div className="space-y-1">
            <label className="text-sm font-medium text-gray-700">Tên đề thi *</label>
            <Input {...form.register('tenDeThi')} placeholder="Kiểm tra kiến thức KSNK Q2/2026" />
            {form.formState.errors.tenDeThi && (
              <p className="text-xs text-red-500">{form.formState.errors.tenDeThi.message}</p>
            )}
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-1">
              <label className="text-sm font-medium text-gray-700">Thời gian bắt đầu *</label>
              <Input {...form.register('thoiGianBatDau')} type="datetime-local" />
              {form.formState.errors.thoiGianBatDau && (
                <p className="text-xs text-red-500">{form.formState.errors.thoiGianBatDau.message}</p>
              )}
            </div>
            <div className="space-y-1">
              <label className="text-sm font-medium text-gray-700">Trạng thái</label>
              <select
                {...form.register('trangThai')}
                className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm"
              >
                <option value="Draft">Nháp</option>
                <option value="Active">Kích hoạt</option>
              </select>
            </div>
          </div>

          {/* Question picker */}
          <div className="border rounded-lg p-4 space-y-3">
            <div className="flex items-center justify-between">
              <label className="text-sm font-medium text-gray-700">
                Chọn câu hỏi ({selectedIds.length} đã chọn)
              </label>
            </div>

            <div className="flex gap-2">
              <div className="relative flex-1">
                <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400" />
                <Input
                  placeholder="Tìm câu hỏi..."
                  className="pl-9"
                  value={searchKeyword}
                  onChange={(e) => setSearchKeyword(e.target.value)}
                  onKeyDown={(e) => e.key === 'Enter' && (e.preventDefault(), handleSearchQuestions())}
                />
              </div>
              <Button type="button" variant="outline" onClick={handleSearchQuestions}>
                Tìm
              </Button>
            </div>

            <div className="max-h-48 overflow-y-auto border rounded divide-y">
              {loadingQuestions ? (
                <p className="p-3 text-sm text-gray-500">Đang tải...</p>
              ) : questions.length === 0 ? (
                <p className="p-3 text-sm text-gray-500">Không có câu hỏi</p>
              ) : (
                questions.map((q) => (
                  <div
                    key={q.id}
                    className="flex items-center gap-3 px-3 py-2 hover:bg-gray-50 cursor-pointer"
                    onClick={() => toggleQuestion(q.id)}
                  >
                    <div
                      className={`h-5 w-5 rounded border flex items-center justify-center shrink-0 ${
                        selectedIds.includes(q.id)
                          ? 'bg-primary border-primary text-white'
                          : 'border-gray-300'
                      }`}
                    >
                      {selectedIds.includes(q.id) && <Check className="h-3 w-3" />}
                    </div>
                    <div className="flex-1 min-w-0">
                      <p className="text-sm truncate">{q.noiDung}</p>
                      <p className="text-xs text-gray-400">
                        {q.tenDanhMuc} • {q.doKho} • {q.diem} điểm
                      </p>
                    </div>
                  </div>
                ))
              )}
            </div>

            {form.formState.errors.danhSachIdCauHoi && (
              <p className="text-xs text-red-500">{form.formState.errors.danhSachIdCauHoi.message}</p>
            )}
          </div>

          {/* Actions */}
          <div className="flex justify-end gap-2 pt-4 border-t">
            <Button type="button" variant="outline" onClick={onClose}>Hủy</Button>
            <Button type="submit" disabled={isLoading}>
              {isLoading ? 'Đang tạo...' : 'Tạo đề thi'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  )
}
