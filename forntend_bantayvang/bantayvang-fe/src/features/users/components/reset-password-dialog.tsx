import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { resetPasswordSchema, type ResetPasswordFormData } from '../schemas'
import type { UserDto } from '../types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { X } from 'lucide-react'

interface ResetPasswordDialogProps {
  open: boolean
  user: UserDto | null
  onClose: () => void
  onSubmit: (userId: number, newPassword: string) => void
  isLoading: boolean
}

export function ResetPasswordDialog({
  open,
  user,
  onClose,
  onSubmit,
  isLoading,
}: ResetPasswordDialogProps) {
  const {
    register,
    handleSubmit,
    formState: { errors },
    reset,
  } = useForm<ResetPasswordFormData>({
    resolver: zodResolver(resetPasswordSchema),
    defaultValues: { newPassword: '' },
  })

  if (!open || !user) return null

  const handleFormSubmit = (data: ResetPasswordFormData) => {
    onSubmit(user.id, data.newPassword)
    reset()
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-sm">
        <div className="flex items-center justify-between p-4 border-b">
          <h2 className="text-lg font-semibold">Reset mật khẩu</h2>
          <Button variant="ghost" size="icon" onClick={onClose}>
            <X className="h-4 w-4" />
          </Button>
        </div>

        <form onSubmit={handleSubmit(handleFormSubmit)} className="p-4 space-y-4">
          <p className="text-sm text-gray-600">
            Đặt lại mật khẩu cho: <strong>{user.hoTen || user.tenDangNhap}</strong>
          </p>

          <div className="space-y-1">
            <label className="text-sm font-medium text-gray-700">Mật khẩu mới</label>
            <Input
              {...register('newPassword')}
              type="password"
              placeholder="Nhập mật khẩu mới"
              autoComplete="new-password"
            />
            {errors.newPassword && (
              <p className="text-xs text-red-500">{errors.newPassword.message}</p>
            )}
          </div>

          <div className="flex justify-end gap-2 pt-2">
            <Button type="button" variant="outline" onClick={onClose}>
              Hủy
            </Button>
            <Button type="submit" disabled={isLoading}>
              {isLoading ? 'Đang lưu...' : 'Đặt lại'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  )
}
