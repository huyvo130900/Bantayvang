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
  const myKhoa = currentUser?.tenKhoaQuanLy || currentUser?.department || null

  const [departments, setDepartments] = useState<DepartmentDto[]>([])
  const [searchTerm, setSearchTerm] = useState('')
  const [isDropdownOpen, setIsDropdownOpen] = useState(false)
  const dropdownRef = useRef<HTMLDivElement>(null)

  const form = useForm<CreateUserFormData>({
    resolver: zodResolver(isEdit ? updateUserSchema : createUserSchema) as never,
    defaultValues: {
      username: '', password: '', fullName: '',
      employeeCode: '', jobTitle: '', department: '',
      roleId: 3, idKhoaQuanLy: null as any, status: true,
      email: '', soDienThoai: '',
    },
  })

  useEffect(() => {
    // Load departments for DeptManager assignment
    departmentApi.getAll({ status: true, pageSize: 100 })
      .then(res => setDepartments(res.data?.data || []))
      .catch(() => {})
  }, [])

  // Close dropdown on click outside
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsDropdownOpen(false)
        if (searchTerm === '') {
          form.setValue('department', '')
        } else {
          const currentKhoaPhong = form.getValues('department')
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
          username: user.username || '', password: '',
          fullName: user.fullName || '',
          employeeCode: user.employeeCode || '', jobTitle: user.jobTitle || '',
          department: user.department || '', roleId: user.roleId || 3,
          idKhoaQuanLy: user.idKhoaQuanLy || null as any,
          status: user.status ?? true,
          email: user.email || '',
          soDienThoai: user.soDienThoai || '',
        })
        setSearchTerm(user.department || '')
      } else {
        form.reset({
          username: '', password: '', fullName: '',
          employeeCode: '', jobTitle: '', department: isUserDeptManager && myKhoa ? myKhoa : '', roleId: 3,
          idKhoaQuanLy: null as any,
          status: true,
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
      const { username: _u, password: _p, ...updateData } = data
      void _u; void _p
      onSubmit(updateData as UpdateUserFormData)
    } else {
      onSubmit({
        ...data,
        employeeCode: data.username,
      })
    }
  }

  const watchedRole = form.watch('roleId')
  const isDeptManager = Number(watchedRole) === ROLE_IDS.DEPT_MANAGER
  const selectedKhoaPhong = form.watch('department')
  const filteredDepts = departments.filter(d => {
    if (searchTerm === selectedKhoaPhong) return true
    return d.departmentName.toLowerCase().includes(searchTerm.toLowerCase()) ||
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
            <Field label="Mã nhân viên *" error={form.formState.errors.username?.message}>
              <Input {...form.register('username')} placeholder="NV001" autoComplete="off" />
            </Field>
          )}
          {!isEdit && (
            <Field label="Mật khẩu *" error={form.formState.errors.password?.message}>
              <Input {...form.register('password')} type="password" placeholder="••••••" autoComplete="new-password" />
            </Field>
          )}

          <Field label="Họ tên *" error={form.formState.errors.fullName?.message}>
            <Input {...form.register('fullName')} placeholder="Nguyễn Văn A" />
          </Field>

          <div className="grid grid-cols-2 gap-4">
            {isEdit ? (
              <>
                <Field label="Mã nhân viên" error={form.formState.errors.employeeCode?.message}>
                  <Input {...form.register('employeeCode')} placeholder="NV001" />
                </Field>
                <Field label="Chức danh" error={form.formState.errors.jobTitle?.message}>
                  <Input {...form.register('jobTitle')} placeholder="Bác sĩ" />
                </Field>
              </>
            ) : (
              <div className="col-span-2">
                <Field label="Chức danh" error={form.formState.errors.jobTitle?.message}>
                  <Input {...form.register('jobTitle')} placeholder="Bác sĩ" />
                </Field>
              </div>
            )}
          </div>

          <Field label="Khoa/Phòng (hiển thị)" error={form.formState.errors.department?.message}>
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
              <input type="hidden" {...form.register('department')} />

              {isDropdownOpen && (
                <div className="absolute z-50 w-full mt-1 max-h-60 overflow-y-auto rounded-md border bg-white shadow-lg">
                  {filteredDepts.length === 0 ? (
                    <div className="p-3 text-sm text-gray-500 text-center">Không tìm thấy khoa/phòng nào</div>
                  ) : (
                    filteredDepts.map((d) => (
                      <div
                        key={d.id}
                        className={`flex items-center justify-between px-3 py-2 cursor-pointer text-sm hover:bg-gray-100 transition-colors ${
                          selectedKhoaPhong === d.departmentName ? 'bg-blue-50 text-blue-900 font-semibold' : 'text-gray-900'
                        }`}
                        onClick={() => {
                          form.setValue('department', d.departmentName)
                          setSearchTerm(d.departmentName)
                          setIsDropdownOpen(false)
                        }}
                      >
                        <span>{d.departmentName} ({d.maKhoa})</span>
                        {selectedKhoaPhong === d.departmentName && <Check className="h-4 w-4 text-blue-600" />}
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
                {...form.register('roleId', { valueAsNumber: true })}
                value={form.watch('roleId')}
                onChange={e => {
                  form.setValue('roleId', parseInt(e.target.value))
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
                <input type="checkbox" {...form.register('status')} id="status" className="h-4 w-4 rounded border-gray-300" />
                <label htmlFor="status" className="text-sm">Hoạt động</label>
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
                  <option key={d.id} value={d.id}>{d.departmentName} ({d.maKhoa})</option>
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
