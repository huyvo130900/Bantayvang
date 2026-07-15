import { useEffect, useState, useRef } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useAppSelector } from '@/app/hooks'
import { createUserSchema, updateUserSchema, type CreateUserFormData, type UpdateUserFormData } from '../schemas'
import type { UserDto } from '../types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { X, Check, ChevronDown } from 'lucide-react'
import { departmentApi } from '@/features/departments/api'
import type { DepartmentDto } from '@/features/departments/types'
import { ROLE_IDS } from '@/lib/constants'

interface UserFormDialogProps {
  open: boolean
  user: UserDto | null
  onClose: () => void
  onSubmit: (data: CreateUserFormData | UpdateUserFormData) => void
  isLoading: boolean
}

export function UserFormDialog({ open, user, onClose, onSubmit, isLoading }: UserFormDialogProps) {
  const isEdit = !!user
  const currentUser = useAppSelector((state) => state.auth.user)
  const isUserDeptManager = currentUser?.role === 'DeptManager' || currentUser?.tenVaiTro === 'DeptManager'
  const myKhoa = currentUser?.tenKhoaQuanLy || currentUser?.khoaPhong || null

  const [departments, setDepartments] = useState<DepartmentDto[]>([])
  const [searchTerm, setSearchTerm] = useState('')
  const [isDropdownOpen, setIsDropdownOpen] = useState(false)
  const dropdownRef = useRef<HTMLDivElement>(null)

  const form = useForm<CreateUserFormData>({
    resolver: zodResolver(isEdit ? updateUserSchema : createUserSchema) as never,
    defaultValues: {
      tenDangNhap: '', matKhau: '', hoTen: '',
      maNhanVien: '', chucDanh: '', khoaPhong: '',
      idVaiTro: 3, idKhoaQuanLy: null as any, trangThai: true,
      email: '', soDienThoai: '',
    },
  })

  useEffect(() => {
    // Load departments for DeptManager assignment
    departmentApi.getAll({ trangThai: true, pageSize: 100 })
      .then(res => setDepartments(res.data?.data || []))
      .catch(() => {})
  }, [])

  // Close dropdown on click outside
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsDropdownOpen(false)
        if (searchTerm === '') {
          form.setValue('khoaPhong', '')
        } else {
          const currentKhoaPhong = form.getValues('khoaPhong')
          setSearchTerm(currentKhoaPhong || '')
        }
      }
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [searchTerm, form])

  useEffect(() => {
    if (open) {
      if (user) {
        form.reset({
          tenDangNhap: user.tenDangNhap || '', matKhau: '',
          hoTen: user.hoTen || '',
          maNhanVien: user.maNhanVien || '', chucDanh: user.chucDanh || '',
          khoaPhong: user.khoaPhong || '', idVaiTro: user.idVaiTro || 3,
          idKhoaQuanLy: user.idKhoaQuanLy || null as any,
          trangThai: user.trangThai ?? true,
          email: user.email || '',
          soDienThoai: user.soDienThoai || '',
        })
        setSearchTerm(user.khoaPhong || '')
      } else {
        form.reset({
          tenDangNhap: '', matKhau: '', hoTen: '',
          maNhanVien: '', chucDanh: '', khoaPhong: isUserDeptManager && myKhoa ? myKhoa : '', idVaiTro: 3,
          idKhoaQuanLy: null as any,
          trangThai: true,
          email: '',
          soDienThoai: '',
        })
        setSearchTerm(isUserDeptManager && myKhoa ? myKhoa : '')
      }
      setIsDropdownOpen(false)
    }
  }, [open, user, form, isUserDeptManager, myKhoa])

  if (!open) return null

  const handleFormSubmit = (data: CreateUserFormData) => {
    if (isEdit) {
      const { tenDangNhap: _u, matKhau: _p, ...updateData } = data
      void _u; void _p
      onSubmit(updateData as UpdateUserFormData)
    } else {
      onSubmit({
        ...data,
        maNhanVien: data.tenDangNhap,
      })
    }
  }

  const watchedRole = form.watch('idVaiTro')
  const isDeptManager = Number(watchedRole) === ROLE_IDS.DEPT_MANAGER
  const selectedKhoaPhong = form.watch('khoaPhong')
  const filteredDepts = departments.filter(d => {
    if (searchTerm === selectedKhoaPhong) return true
    return d.tenKhoa.toLowerCase().includes(searchTerm.toLowerCase()) ||
           d.maKhoa.toLowerCase().includes(searchTerm.toLowerCase())
  })

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-lg max-h-[90vh] overflow-y-auto">
        <div className="flex items-center justify-between p-4 border-b">
          <h2 className="text-lg font-semibold">{isEdit ? 'Sửa người dùng' : 'Thêm người dùng'}</h2>
          <Button variant="ghost" size="icon" onClick={onClose}><X className="h-4 w-4" /></Button>
        </div>

        <form onSubmit={form.handleSubmit(handleFormSubmit)} className="p-4 space-y-4">
          {!isEdit && (
            <Field label="Mã nhân viên *" error={form.formState.errors.tenDangNhap?.message}>
              <Input {...form.register('tenDangNhap')} placeholder="NV001" autoComplete="off" />
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

          <div className="grid grid-cols-2 gap-4">
            {isEdit ? (
              <>
                <Field label="Mã nhân viên" error={form.formState.errors.maNhanVien?.message}>
                  <Input {...form.register('maNhanVien')} placeholder="NV001" />
                </Field>
                <Field label="Chức danh" error={form.formState.errors.chucDanh?.message}>
                  <Input {...form.register('chucDanh')} placeholder="Bác sĩ" />
                </Field>
              </>
            ) : (
              <div className="col-span-2">
                <Field label="Chức danh" error={form.formState.errors.chucDanh?.message}>
                  <Input {...form.register('chucDanh')} placeholder="Bác sĩ" />
                </Field>
              </div>
            )}
          </div>

          <Field label="Khoa/Phòng (hiển thị)" error={form.formState.errors.khoaPhong?.message}>
            <div className="relative" ref={dropdownRef}>
              <div className="relative">
                <Input
                  type="text"
                  placeholder="Tìm và chọn khoa/phòng..."
                  value={searchTerm}
                  onFocus={() => !isUserDeptManager && setIsDropdownOpen(true)}
                  onChange={(e) => {
                    if (!isUserDeptManager) {
                      setSearchTerm(e.target.value)
                      setIsDropdownOpen(true)
                    }
                  }}
                  disabled={isUserDeptManager}
                  className="pr-10 disabled:bg-gray-100 disabled:text-gray-500 disabled:cursor-not-allowed"
                />
                {!isUserDeptManager && (
                  <button
                    type="button"
                    className="absolute right-0 top-0 h-10 px-3 flex items-center justify-center text-gray-400 hover:text-gray-600"
                    onClick={() => setIsDropdownOpen(!isDropdownOpen)}
                  >
                    <ChevronDown className="h-4 w-4" />
                  </button>
                )}
              </div>
              <input type="hidden" {...form.register('khoaPhong')} />

              {isDropdownOpen && (
                <div className="absolute z-50 w-full mt-1 max-h-60 overflow-y-auto rounded-md border bg-white shadow-lg">
                  {filteredDepts.length === 0 ? (
                    <div className="p-3 text-sm text-gray-500 text-center">Không tìm thấy khoa/phòng nào</div>
                  ) : (
                    filteredDepts.map((d) => (
                      <div
                        key={d.id}
                        className={`flex items-center justify-between px-3 py-2 cursor-pointer text-sm hover:bg-gray-100 transition-colors ${
                          selectedKhoaPhong === d.tenKhoa ? 'bg-blue-50 text-blue-900 font-semibold' : 'text-gray-900'
                        }`}
                        onClick={() => {
                          form.setValue('khoaPhong', d.tenKhoa)
                          setSearchTerm(d.tenKhoa)
                          setIsDropdownOpen(false)
                        }}
                      >
                        <span>{d.tenKhoa} ({d.maKhoa})</span>
                        {selectedKhoaPhong === d.tenKhoa && <Check className="h-4 w-4 text-blue-600" />}
                      </div>
                    ))
                  )}
                </div>
              )}
            </div>
          </Field>

          <div className="grid grid-cols-2 gap-4">
            <Field label="Số điện thoại" error={form.formState.errors.soDienThoai?.message}>
              <Input {...form.register('soDienThoai')} placeholder="0912345678" />
            </Field>
            <Field label="Email" error={form.formState.errors.email?.message}>
              <Input {...form.register('email')} placeholder="nguyenvana@example.com" />
            </Field>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <Field label="Vai trò *">
              <select
                {...form.register('idVaiTro', { valueAsNumber: true })}
                value={form.watch('idVaiTro')}
                onChange={e => {
                  form.setValue('idVaiTro', parseInt(e.target.value))
                }}
                disabled={isUserDeptManager}
                className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm disabled:bg-gray-100 disabled:text-gray-500 disabled:cursor-not-allowed"
              >
                <option value={ROLE_IDS.ADMIN}>Admin</option>
                <option value={ROLE_IDS.DEPT_MANAGER}>Quản lý Khoa</option>
                <option value={ROLE_IDS.STUDENT}>Thí sinh</option>
                <option value={ROLE_IDS.THI_SINH_NGOAI}>Thí sinh ngoài</option>
              </select>
            </Field>
            <Field label="Trạng thái">
              <div className="flex items-center h-10 gap-2">
                <input type="checkbox" {...form.register('trangThai')} id="trangThai" className="h-4 w-4 rounded border-gray-300" />
                <label htmlFor="trangThai" className="text-sm">Hoạt động</label>
              </div>
            </Field>
          </div>

          {/* Chọn Khoa khi role = DeptManager */}
          {isDeptManager && (
            <Field label="Khoa quản lý *" error={form.formState.errors.idKhoaQuanLy?.message}>
              <select
                {...form.register('idKhoaQuanLy', { setValueAs: (v) => v === "" || Number.isNaN(parseInt(v)) ? null : parseInt(v) })}
                value={form.watch('idKhoaQuanLy') ?? ''}
                className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm"
              >
                <option value="">— Chọn khoa —</option>
                {departments.map(d => (
                  <option key={d.id} value={d.id}>{d.tenKhoa} ({d.maKhoa})</option>
                ))}
              </select>
              <p className="text-xs text-blue-600 mt-1">Quản lý Khoa chỉ thấy dữ liệu của khoa được gán</p>
            </Field>
          )}

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

function Field({ label, error, children }: { label: string; error?: string; children: React.ReactNode }) {
  return (
    <div className="space-y-1">
      <label className="text-sm font-medium text-gray-700">{label}</label>
      {children}
      {error && <p className="text-xs text-red-500">{error}</p>}
    </div>
  )
}
