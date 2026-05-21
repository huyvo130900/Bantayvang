import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { createUserSchema, updateUserSchema, type CreateUserFormData, type UpdateUserFormData } from '../schemas'
import type { UserDto } from '../types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { X } from 'lucide-react'

interface UserFormDialogProps {
  open: boolean
  user: UserDto | null // null = create mode
  onClose: () => void
  onSubmit: (data: CreateUserFormData | UpdateUserFormData) => void
  isLoading: boolean
}

export function UserFormDialog({ open, user, onClose, onSubmit, isLoading }: UserFormDialogProps) {
  const isEdit = !!user

  const form = useForm<CreateUserFormData>({
    resolver: zodResolver(isEdit ? updateUserSchema : createUserSchema) as never,
    defaultValues: {
      tenDangNhap: '',
      matKhau: '',
      email: '',
      hoTen: '',
      maNhanVien: '',
      chucDanh: '',
      khoaPhong: '',
      idVaiTro: 3,
      trangThai: true,
    },
  })

  useEffect(() => {
    if (user) {
      form.reset({
        tenDangNhap: user.tenDangNhap || '',
        matKhau: '',
        email: user.email || '',
        hoTen: user.hoTen || '',
        maNhanVien: user.maNhanVien || '',
        chucDanh: user.chucDanh || '',
        khoaPhong: user.khoaPhong || '',
        idVaiTro: user.idVaiTro || 3,
        trangThai: user.trangThai ?? true,
      })
    } else {
      form.reset({
        tenDangNhap: '',
        matKhau: '',
        email: '',
        hoTen: '',
        maNhanVien: '',
        chucDanh: '',
        khoaPhong: '',
        idVaiTro: 3,
        trangThai: true,
      })
    }
  }, [user, form])

  if (!open) return null

  const handleFormSubmit = (data: CreateUserFormData) => {
    if (isEdit) {
      const { tenDangNhap: _u, matKhau: _p, ...updateData } = data
      void _u
      void _p
      onSubmit(updateData as UpdateUserFormData)
    } else {
      onSubmit(data)
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-lg max-h-[90vh] overflow-y-auto">
        <div className="flex items-center justify-between p-4 border-b">
          <h2 className="text-lg font-semibold">
            {isEdit ? 'Sửa người dùng' : 'Thêm người dùng'}
          </h2>
          <Button variant="ghost" size="icon" onClick={onClose}>
            <X className="h-4 w-4" />
          </Button>
        </div>

        <form onSubmit={form.handleSubmit(handleFormSubmit)} className="p-4 space-y-4">
          {!isEdit && (
            <Field label="Tên đăng nhập *" error={form.formState.errors.tenDangNhap?.message}>
              <Input {...form.register('tenDangNhap')} placeholder="username" autoComplete="off" />
            </Field>
          )}

          {!isEdit && (
            <Field label="Mật khẩu *" error={form.formState.errors.matKhau?.message}>
              <Input {...form.register('matKhau')} type="password" placeholder="••••••" autoComplete="new-password" />
            </Field>
          )}

          <Field label="Họ tên *" error={form.formState.errors.hoTen?.message}>
            <Input {...form.register('hoTen')} placeholder="Nguyễn Văn A" />
          </Field>

          <Field label="Email *" error={form.formState.errors.email?.message}>
            <Input {...form.register('email')} type="email" placeholder="email@example.com" />
          </Field>

          <div className="grid grid-cols-2 gap-4">
            <Field label="Mã nhân viên" error={form.formState.errors.maNhanVien?.message}>
              <Input {...form.register('maNhanVien')} placeholder="NV001" />
            </Field>
            <Field label="Chức danh" error={form.formState.errors.chucDanh?.message}>
              <Input {...form.register('chucDanh')} placeholder="Bác sĩ" />
            </Field>
          </div>

          <Field label="Khoa/Phòng" error={form.formState.errors.khoaPhong?.message}>
            <Input {...form.register('khoaPhong')} placeholder="Khoa Nội" />
          </Field>

          <div className="grid grid-cols-2 gap-4">
            <Field label="Vai trò *">
              <select
                {...form.register('idVaiTro', { valueAsNumber: true })}
                className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm"
              >
                <option value={1}>Admin</option>
                <option value={2}>Teacher</option>
                <option value={3}>Student</option>
                <option value={4}>Supervisor</option>
              </select>
            </Field>
            <Field label="Trạng thái">
              <div className="flex items-center h-10 gap-2">
                <input
                  type="checkbox"
                  {...form.register('trangThai')}
                  id="trangThai"
                  className="h-4 w-4 rounded border-gray-300"
                />
                <label htmlFor="trangThai" className="text-sm">Hoạt động</label>
              </div>
            </Field>
          </div>

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

function Field({
  label,
  error,
  children,
}: {
  label: string
  error?: string
  children: React.ReactNode
}) {
  return (
    <div className="space-y-1">
      <label className="text-sm font-medium text-gray-700">{label}</label>
      {children}
      {error && <p className="text-xs text-red-500">{error}</p>}
    </div>
  )
}
