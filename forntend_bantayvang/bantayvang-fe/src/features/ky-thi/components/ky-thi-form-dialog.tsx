import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { createKyThiSchema, type CreateKyThiFormData } from '../schemas'
import type { KyThiDto } from '../types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { X } from 'lucide-react'

interface KyThiFormDialogProps {
  open: boolean
  kyThi: KyThiDto | null
  onClose: () => void
  onSubmit: (data: CreateKyThiFormData) => void
  isLoading: boolean
}

export function KyThiFormDialog({ open, kyThi, onClose, onSubmit, isLoading }: KyThiFormDialogProps) {
  const isEdit = !!kyThi

  const form = useForm<CreateKyThiFormData>({
    resolver: zodResolver(createKyThiSchema),
    defaultValues: { maKyThi: '', tenKyThi: '', moTa: '', loaiKyThi: '', thoiGianBatDau: '', thoiGianKetThuc: '', donViToChuc: '' },
  })

  useEffect(() => {
    if (kyThi) {
      form.reset({
        maKyThi: kyThi.maKyThi || '',
        tenKyThi: kyThi.tenKyThi || '',
        moTa: kyThi.moTa || '',
        loaiKyThi: kyThi.loaiKyThi || '',
        thoiGianBatDau: kyThi.thoiGianBatDau ? kyThi.thoiGianBatDau.slice(0, 16) : '',
        thoiGianKetThuc: kyThi.thoiGianKetThuc ? kyThi.thoiGianKetThuc.slice(0, 16) : '',
        donViToChuc: kyThi.donViToChuc || '',
      })
    } else {
      form.reset({ maKyThi: '', tenKyThi: '', moTa: '', loaiKyThi: '', thoiGianBatDau: '', thoiGianKetThuc: '', donViToChuc: '' })
    }
  }, [kyThi, form])

  if (!open) return null

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-lg max-h-[90vh] overflow-y-auto">
        <div className="flex items-center justify-between p-4 border-b">
          <h2 className="text-lg font-semibold">{isEdit ? 'Sửa kỳ thi' : 'Tạo kỳ thi'}</h2>
          <Button variant="ghost" size="icon" onClick={onClose}><X className="h-4 w-4" /></Button>
        </div>

        <form onSubmit={form.handleSubmit(onSubmit)} className="p-4 space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-1">
              <label className="text-sm font-medium text-gray-700">Mã kỳ thi *</label>
              <Input {...form.register('maKyThi')} placeholder="KT_Q2_2026" disabled={isEdit} />
              {form.formState.errors.maKyThi && <p className="text-xs text-red-500">{form.formState.errors.maKyThi.message}</p>}
            </div>
            <div className="space-y-1">
              <label className="text-sm font-medium text-gray-700">Loại kỳ thi</label>
              <select {...form.register('loaiKyThi')} className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm">
                <option value="">-- Chọn --</option>
                <option value="BanTayVang">Bàn Tay Vàng</option>
                <option value="KiemSoatNhiemKhuan">Kiểm soát nhiễm khuẩn</option>
                <option value="AnToanNguoiBenh">An toàn người bệnh</option>
                <option value="CNTT">CNTT</option>
              </select>
            </div>
          </div>

          <div className="space-y-1">
            <label className="text-sm font-medium text-gray-700">Tên kỳ thi *</label>
            <Input {...form.register('tenKyThi')} placeholder="Kỳ thi Bàn tay vàng Q2/2026" />
            {form.formState.errors.tenKyThi && <p className="text-xs text-red-500">{form.formState.errors.tenKyThi.message}</p>}
          </div>

          <div className="space-y-1">
            <label className="text-sm font-medium text-gray-700">Mô tả</label>
            <textarea {...form.register('moTa')} rows={2} className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm" placeholder="Mô tả kỳ thi..." />
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
            <label className="text-sm font-medium text-gray-700">Đơn vị tổ chức</label>
            <Input {...form.register('donViToChuc')} placeholder="Phòng Điều dưỡng" />
          </div>

          <div className="flex justify-end gap-2 pt-4 border-t">
            <Button type="button" variant="outline" onClick={onClose}>Hủy</Button>
            <Button type="submit" disabled={isLoading}>
              {isLoading ? 'Đang lưu...' : isEdit ? 'Cập nhật' : 'Tạo mới'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  )
}
