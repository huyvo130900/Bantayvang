import { useEffect, useState, useRef } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useAppSelector } from '@/app/hooks'
import { getKyThiSchema, type CreateKyThiFormData } from '../schemas'
import type { ExamCampaignDto } from '../types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { X, Check, ChevronDown } from 'lucide-react'
import { departmentApi } from '@/features/departments/api'
import type { DepartmentDto } from '@/features/departments/types'

interface KyThiFormDialogProps {
  open: boolean
  examCampaign: ExamCampaignDto | null
  onClose: () => void
  onSubmit: (data: CreateKyThiFormData) => void
  isLoading: boolean
}

const toLocalInputString = (dateStr: string | null | undefined): string => {
  if (!dateStr) return ''
  const date = new Date(dateStr)
  if (isNaN(date.getTime())) return ''
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  const hours = String(date.getHours()).padStart(2, '0')
  const minutes = String(date.getMinutes()).padStart(2, '0')
  return `${year}-${month}-${day}T${hours}:${minutes}`
}

const generateKyThiCode = () => {
  const now = new Date()
  const year = now.getFullYear()
  const month = String(now.getMonth() + 1).padStart(2, '0')
  const day = String(now.getDate()).padStart(2, '0')
  const hours = String(now.getHours()).padStart(2, '0')
  const minutes = String(now.getMinutes()).padStart(2, '0')
  const seconds = String(now.getSeconds()).padStart(2, '0')
  const rand = Math.random().toString(36).substring(2, 6).toUpperCase()
  return `KT_${year}${month}${day}_${hours}${minutes}${seconds}_${rand}`
}

export function KyThiFormDialog({ open, examCampaign, onClose, onSubmit, isLoading }: KyThiFormDialogProps) {
  const isEdit = !!examCampaign
  const currentUser = useAppSelector((state) => state.auth.user)
  const isDeptManager = currentUser?.role === 'DeptManager' || currentUser?.roleName === 'DeptManager'

  const [departments, setDepartments] = useState<DepartmentDto[]>([])
  const [searchTerm, setSearchTerm] = useState('')
  const [isDropdownOpen, setIsDropdownOpen] = useState(false)
  const dropdownRef = useRef<HTMLDivElement>(null)

  const form = useForm<CreateKyThiFormData>({
    resolver: zodResolver(getKyThiSchema(isEdit, examCampaign?.thoiGianBatDau, examCampaign?.thoiGianKetThuc)) as any,
    defaultValues: { campaignCode: '', campaignName: '', description: '', departmentId: '' as any, thoiGianBatDau: '', thoiGianKetThuc: '', donViToChuc: '', soCauDungToiThieu: '' as any, tongSoCauHoi: '' as any, durationMinutes: '' as any },
  })

  // Programmatically register custom fields
  useEffect(() => {
    form.register('departmentId')
  }, [form])

  // Load departments
  useEffect(() => {
    if (open) {
      departmentApi.getAll({ status: true, pageSize: 100 })
        .then((res) => setDepartments(res.data?.data || []))
        .catch(() => {})
    }
  }, [open])

  // Close dropdown on click outside
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsDropdownOpen(false)
        const currentId = form.getValues('departmentId')
        const currentDept = departments.find(d => d.id === currentId)
        setSearchTerm(!currentId ? 'Tất cả các khoa' : (currentDept ? currentDept.departmentName : ''))
      }
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [departments, form])

  useEffect(() => {
    if (examCampaign) {
      form.reset({
        campaignCode: examCampaign.campaignCode || '',
        campaignName: examCampaign.campaignName || '',
        description: examCampaign.description || '',
        departmentId: examCampaign.departmentId || '' as any,
        thoiGianBatDau: toLocalInputString(examCampaign.thoiGianBatDau),
        thoiGianKetThuc: toLocalInputString(examCampaign.thoiGianKetThuc),
        donViToChuc: examCampaign.donViToChuc || '',
        soCauDungToiThieu: examCampaign.soCauDungToiThieu ?? '' as any,
        tongSoCauHoi: examCampaign.tongSoCauHoi ?? '' as any,
        durationMinutes: examCampaign.durationMinutes ?? '' as any,
      })
      setSearchTerm(examCampaign.departmentName || 'Tất cả các khoa')
    } else {
      if (isDeptManager) {
        form.reset({
          campaignCode: generateKyThiCode(),
          campaignName: '',
          description: '',
          departmentId: currentUser?.deptManagerDeptId || '' as any,
          thoiGianBatDau: '',
          thoiGianKetThuc: '',
          donViToChuc: currentUser?.deptManagerDeptName || currentUser?.department || '',
          soCauDungToiThieu: '' as any,
          tongSoCauHoi: '' as any,
          durationMinutes: '' as any
        })
        setSearchTerm(currentUser?.deptManagerDeptName || currentUser?.department || '')
      } else {
        form.reset({
          campaignCode: generateKyThiCode(),
          campaignName: '',
          description: '',
          departmentId: '' as any,
          thoiGianBatDau: '',
          thoiGianKetThuc: '',
          donViToChuc: '',
          soCauDungToiThieu: '' as any,
          tongSoCauHoi: '' as any,
          durationMinutes: '' as any,
        })
        setSearchTerm('Tất cả các khoa')
      }
    }
  }, [open, examCampaign, form, isDeptManager, currentUser])

  if (!open) return null

  const selectedDeptId = form.watch('departmentId')
  const selectedDept = departments.find(d => d.id === selectedDeptId)
  const selectedDeptName = !selectedDeptId ? 'Tất cả các khoa' : (selectedDept ? selectedDept.departmentName : '')

  const filteredDepts = departments.filter(d => {
    if (searchTerm === selectedDeptName) return true
    return d.departmentName.toLowerCase().includes(searchTerm.toLowerCase()) || 
           d.deptCode.toLowerCase().includes(searchTerm.toLowerCase())
  })

  const showAllDeptsOption = searchTerm === '' || searchTerm === selectedDeptName || 'tất cả các khoa'.includes(searchTerm.toLowerCase())

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
              <Input {...form.register('campaignCode')} placeholder="KT_Q2_2026" readOnly className="bg-gray-100 cursor-not-allowed" />
              {form.formState.errors.campaignCode && <p className="text-xs text-red-500">{form.formState.errors.campaignCode.message}</p>}
            </div>
            <div className="space-y-1 relative" ref={dropdownRef}>
              <label className="text-sm font-medium text-gray-700">Khoa / Phòng ban</label>
              <div className="relative">
                <input
                  type="text"
                  placeholder="Tìm và chọn khoa..."
                  value={searchTerm}
                  onFocus={() => !isDeptManager && setIsDropdownOpen(true)}
                  onChange={(e) => {
                    if (!isDeptManager) {
                      setSearchTerm(e.target.value)
                      setIsDropdownOpen(true)
                    }
                  }}
                  disabled={isDeptManager}
                  className="h-10 w-full rounded-md border border-input bg-background pl-3 pr-10 text-sm focus:outline-none focus:ring-2 focus:ring-ring disabled:bg-gray-100 disabled:text-gray-500 disabled:cursor-not-allowed"
                />
                {!isDeptManager && (
                  <button
                    type="button"
                    className="absolute right-0 top-0 h-10 px-3 flex items-center justify-center text-gray-400 hover:text-gray-600"
                    onClick={() => setIsDropdownOpen(!isDropdownOpen)}
                  >
                    <ChevronDown className="h-4 w-4" />
                  </button>
                )}
              </div>
              {form.formState.errors.departmentId && (
                <p className="text-xs text-red-500">{form.formState.errors.departmentId.message}</p>
              )}

              {isDropdownOpen && (
                <div className="absolute z-50 w-full mt-1 max-h-60 overflow-y-auto rounded-md border bg-white shadow-lg">
                  {showAllDeptsOption && (
                    <div
                      className={`flex items-center justify-between px-3 py-2 cursor-pointer text-sm hover:bg-gray-100 transition-colors ${
                        !selectedDeptId ? 'bg-blue-50 text-blue-900 font-semibold' : 'text-gray-900'
                      }`}
                      onClick={() => {
                        form.setValue('departmentId', '' as any)
                        setSearchTerm('Tất cả các khoa')
                        setIsDropdownOpen(false)
                      }}
                    >
                      <span>Tất cả các khoa</span>
                      {!selectedDeptId && <Check className="h-4 w-4 text-blue-600" />}
                    </div>
                  )}

                  {filteredDepts.length === 0 && !showAllDeptsOption ? (
                    <div className="p-3 text-sm text-gray-500 text-center">Không tìm thấy khoa nào</div>
                  ) : (
                    filteredDepts.map((d) => (
                      <div
                        key={d.id}
                        className={`flex items-center justify-between px-3 py-2 cursor-pointer text-sm hover:bg-gray-100 transition-colors ${
                          selectedDeptId === d.id ? 'bg-blue-50 text-blue-900 font-semibold' : 'text-gray-900'
                        }`}
                        onClick={() => {
                          form.setValue('departmentId', d.id)
                          setSearchTerm(d.departmentName)
                          setIsDropdownOpen(false)
                        }}
                      >
                        <span>{d.departmentName} ({d.deptCode})</span>
                        {selectedDeptId === d.id && <Check className="h-4 w-4 text-blue-600" />}
                      </div>
                    ))
                  )}
                </div>
              )}
            </div>
          </div>

          <div className="space-y-1">
            <label className="text-sm font-medium text-gray-700">Tên kỳ thi *</label>
            <Input {...form.register('campaignName')} placeholder="Kỳ thi nội bộ Q2/2026" />
            {form.formState.errors.campaignName && <p className="text-xs text-red-500">{form.formState.errors.campaignName.message}</p>}
          </div>

          <div className="space-y-1">
            <label className="text-sm font-medium text-gray-700">Mô tả</label>
            <textarea {...form.register('description')} rows={2} className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm" placeholder="Mô tả kỳ thi..." />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-1">
              <label className="text-sm font-medium text-gray-700">Bắt đầu *</label>
              <Input {...form.register('thoiGianBatDau')} type="datetime-local" />
              {form.formState.errors.thoiGianBatDau && (
                <p className="text-xs text-red-500">{form.formState.errors.thoiGianBatDau.message}</p>
              )}
            </div>
            <div className="space-y-1">
              <label className="text-sm font-medium text-gray-700">Kết thúc *</label>
              <Input {...form.register('thoiGianKetThuc')} type="datetime-local" />
              {form.formState.errors.thoiGianKetThuc && (
                <p className="text-xs text-red-500">{form.formState.errors.thoiGianKetThuc.message}</p>
              )}
            </div>
          </div>

          <div className="space-y-1">
            <label className="text-sm font-medium text-gray-700">Tổng số câu hỏi mỗi đề *</label>
            <Input
              type="number"
              min={1}
              {...form.register('tongSoCauHoi')}
              placeholder="VD: 50"
            />
            {form.formState.errors.tongSoCauHoi && (
              <p className="text-xs text-red-500">{form.formState.errors.tongSoCauHoi.message}</p>
            )}
            <p className="text-[11px] text-gray-400">
              Số câu hỏi bắt buộc khi tạo đề thi cho kỳ thi này.
            </p>
          </div>

          <div className="space-y-1">
            <label className="text-sm font-medium text-gray-700">Thời gian làm bài (phút)</label>
            <Input
              type="number"
              min={1}
              {...form.register('durationMinutes')}
              placeholder="VD: 60"
            />
            {form.formState.errors.durationMinutes && (
              <p className="text-xs text-red-500">{form.formState.errors.durationMinutes.message}</p>
            )}
            <p className="text-[11px] text-gray-400">
              Mặc định 60 phút nếu để trống. Áp dụng cho các đề thi.
            </p>
          </div>

          <div className="space-y-1">
            <label className="text-sm font-medium text-gray-700">Số câu đúng tối thiểu để ĐẠT</label>
            <Input
              type="number"
              min={0}
              {...form.register('soCauDungToiThieu')}
              placeholder="Để trống nếu không xét đạt/không đạt"
            />
            {form.formState.errors.soCauDungToiThieu && (
              <p className="text-xs text-red-500">{form.formState.errors.soCauDungToiThieu.message}</p>
            )}
            <p className="text-[11px] text-gray-400">
              Áp dụng một lần duy nhất cho toàn bộ đề thi thuộc kỳ thi này.
            </p>
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
