import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { createCaThiSchema, type CreateCaThiFormData } from '../schemas'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { X } from 'lucide-react'
import { useAppSelector } from '@/app/hooks'

interface CaThiFormDialogProps {
  open: boolean
  kyThiId: number
  onClose: () => void
  onSubmit: (data: CreateCaThiFormData) => void
  isLoading: boolean
}

export function CaThiFormDialog({ open, kyThiId, onClose, onSubmit, isLoading }: CaThiFormDialogProps) {
  const { exams } = useAppSelector((state) => state.exams)

  const form = useForm<CreateCaThiFormData>({
    resolver: zodResolver(createCaThiSchema),
    defaultValues: { kyThiId, tenCa: '', thoiGianBatDau: '', thoiGianKetThuc: '', ghiChu: '' },
  })

  if (!open) return null

  const handleFormSubmit = (data: CreateCaThiFormData) => {
    onSubmit({ ...data, kyThiId })
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-md">
        <div className="flex items-center justify-between p-4 border-b">
          <h2 className="text-lg font-semibold">Thêm ca thi</h2>
          <Button variant="ghost" size="icon" onClick={onClose}><X className="h-4 w-4" /></Button>
        </div>

        <form onSubmit={form.handleSubmit(handleFormSubmit)} className="p-4 space-y-4">
          <div className="space-y-1">
            <label className="text-sm font-medium text-gray-700">Tên ca *</label>
            <Input {...form.register('tenCa')} placeholder="Ca 1 - Sáng" />
            {form.formState.errors.tenCa && <p className="text-xs text-red-500">{form.formState.errors.tenCa.message}</p>}
          </div>

          <div className="space-y-1">
            <label className="text-sm font-medium text-gray-700">Đề thi</label>
            <select
              {...form.register('deThiId', { valueAsNumber: true })}
              className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm"
            >
              <option value="">-- Chọn đề thi --</option>
              {exams.map((e) => (
                <option key={e.id} value={e.id}>{e.maDeThi} - {e.tenDeThi}</option>
              ))}
            </select>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-1">
              <label className="text-sm font-medium text-gray-700">Bắt đầu</label>
              <Input {...form.register('thoiGianBatDau')} type="datetime-local" />
            </div>
            <div className="space-y-1">
              <label className="text-sm font-medium text-gray-700">Kết thúc</label>
              <Input {...form.register('thoiGianKetThuc')} type="datetime-local" />
            </div>
          </div>

          <div className="space-y-1">
            <label className="text-sm font-medium text-gray-700">Số lượng tối đa</label>
            <Input {...form.register('soLuongToiDa', { valueAsNumber: true })} type="number" min={1} placeholder="50" />
          </div>

          <div className="space-y-1">
            <label className="text-sm font-medium text-gray-700">Ghi chú</label>
            <Input {...form.register('ghiChu')} placeholder="Ghi chú..." />
          </div>

          <div className="flex justify-end gap-2 pt-4 border-t">
            <Button type="button" variant="outline" onClick={onClose}>Hủy</Button>
            <Button type="submit" disabled={isLoading}>
              {isLoading ? 'Đang lưu...' : 'Tạo ca thi'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  )
}
