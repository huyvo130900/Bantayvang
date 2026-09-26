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

// Chế độ luyện tập tồn tại vĩnh viễn - không cần người dùng chọn ngày giờ, tự gán một khoảng
// thời gian rất dài (bắt đầu ngay bây giờ, kết thúc sau 10 năm) để hài lòng validation sẵn có
// (StartTime/EndTime bắt buộc) mà không thực sự giới hạn thời gian luyện tập.
const practiceModeStartTime = () => toLocalInputString(new Date().toISOString())
const practiceModeEndTime = () => {
  const d = new Date()
  d.setFullYear(d.getFullYear() + 10)
  return toLocalInputString(d.toISOString())
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
    resolver: zodResolver(getKyThiSchema(isEdit, examCampaign?.startTime, examCampaign?.endTime)) as any,
    defaultValues: { campaignCode: '', campaignName: '', description: '', departmentIds: [], accessMode: 'Department', isPracticeMode: false, startTime: '', endTime: '', organizedBy: '', minPassQuestions: '' as any, totalQuestions: '' as any, durationMinutes: '' as any },
  })

  // Programmatically register custom fields
  useEffect(() => {
    form.register('departmentIds')
  }, [form])

  // Load departments
  useEffect(() => {
    if (open) {
      departmentApi.getAll({ status: true, pageSize: 100 })
        .then((res) => setDepartments(res.data?.data || []))
        .catch(() => setDepartments([]))
    }
  }, [open])

  // Close dropdown on click outside
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsDropdownOpen(false)
        setSearchTerm('')
      }
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => document.removeEventListener('mousedown', handleClickOutside)
  }, [])

  useEffect(() => {
    if (examCampaign) {
      form.reset({
        campaignCode: examCampaign.campaignCode || '',
        campaignName: examCampaign.campaignName || '',
        description: examCampaign.description || '',
        departmentIds: examCampaign.departmentIds || [],
        accessMode: examCampaign.accessMode || 'Department',
        isPracticeMode: examCampaign.isPracticeMode || false,
        startTime: toLocalInputString(examCampaign.startTime),
        endTime: toLocalInputString(examCampaign.endTime),
        organizedBy: examCampaign.organizedBy || '',
        minPassQuestions: examCampaign.minPassQuestions ?? '' as any,
        totalQuestions: examCampaign.totalQuestions ?? '' as any,
        durationMinutes: examCampaign.durationMinutes ?? '' as any,
      })
    } else {
      if (isDeptManager) {
        form.reset({
          campaignCode: generateKyThiCode(),
          campaignName: '',
          description: '',
          departmentIds: currentUser?.deptManagerDeptId ? [currentUser.deptManagerDeptId] : [],
          accessMode: 'Department',
          isPracticeMode: false,
          startTime: '',
          endTime: '',
          organizedBy: currentUser?.deptManagerDeptName || currentUser?.department || '',
          minPassQuestions: '' as any,
          totalQuestions: '' as any,
          durationMinutes: '' as any
        })
      } else {
        form.reset({
          campaignCode: generateKyThiCode(),
          campaignName: '',
          description: '',
          departmentIds: [],
          accessMode: 'Department',
          isPracticeMode: false,
          startTime: '',
          endTime: '',
          organizedBy: '',
          minPassQuestions: '' as any,
          totalQuestions: '' as any,
          durationMinutes: '' as any,
        })
      }
    }
    setSearchTerm('')
  }, [open, examCampaign, form, isDeptManager, currentUser])

  if (!open) return null

  const selectedDeptIds: number[] = form.watch('departmentIds') || []
  const selectedDepts = departments.filter(d => selectedDeptIds.includes(d.id))

  const toggleDept = (deptId: number) => {
    const current: number[] = form.getValues('departmentIds') || []
    const next = current.includes(deptId) ? current.filter(id => id !== deptId) : [...current, deptId]
    form.setValue('departmentIds', next, { shouldDirty: true })
  }

  const filteredDepts = departments.filter(d =>
    d.departmentName.toLowerCase().includes(searchTerm.toLowerCase()) ||
    d.deptCode.toLowerCase().includes(searchTerm.toLowerCase())
  )

  const accessMode = form.watch('accessMode')
  const isPracticeMode = form.watch('isPracticeMode')

  const setAccessMode = (value: 'Department' | 'AssignedList') => {
    form.setValue('accessMode', value, { shouldDirty: true })
    // Danh sách chỉ định không dùng khoa - xóa lựa chọn cũ để tránh gửi dữ liệu khoa còn sót lại
    // từ lúc trước đó đang ở chế độ "Theo khoa".
    if (value === 'AssignedList' && !isDeptManager) {
      form.setValue('departmentIds', [])
    }
  }

  const togglePracticeMode = (value: boolean) => {
    form.setValue('isPracticeMode', value, { shouldDirty: true })
    if (value) {
      // Luyện tập tồn tại vĩnh viễn - tự gán 1 khoảng thời gian rất dài thay vì bắt người dùng chọn.
      form.setValue('startTime', practiceModeStartTime())
      form.setValue('endTime', practiceModeEndTime())
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-lg max-h-[90vh] overflow-y-auto">
        <div className="flex items-center justify-between p-4 border-b">
          <h2 className="text-lg font-semibold">{isEdit ? 'Sửa kỳ thi' : 'Tạo kỳ thi'}</h2>
          <Button variant="ghost" size="icon" onClick={onClose}><X className="h-4 w-4" /></Button>
        </div>

        <form onSubmit={form.handleSubmit(onSubmit)} className="p-4 space-y-4">
          <label className="flex items-start gap-2 rounded-md border border-input p-3 cursor-pointer hover:bg-gray-50">
            <input
              type="checkbox"
              checked={isPracticeMode}
              onChange={(e) => togglePracticeMode(e.target.checked)}
              className="mt-0.5 h-4 w-4 rounded border-gray-300"
            />
            <span>
              <span className="text-sm font-medium text-gray-700 block">Chế độ luyện tập</span>
              <span className="text-xs text-gray-400">
                Tồn tại vĩnh viễn để học viên tự luyện bất cứ lúc nào - không giới hạn thời gian, không giới hạn số lần làm lại, không tính vào thống kê đạt/không đạt chính thức.
              </span>
            </span>
          </label>

          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-1">
              <label className="text-sm font-medium text-gray-700">Mã kỳ thi *</label>
              <Input {...form.register('campaignCode')} placeholder="KT_Q2_2026" readOnly className="bg-gray-100 cursor-not-allowed" />
              {form.formState.errors.campaignCode && <p className="text-xs text-red-500">{form.formState.errors.campaignCode.message}</p>}
            </div>
            <div className="space-y-1">
              <label className="text-sm font-medium text-gray-700">Chế độ xác định người được thi</label>
              <div className="flex rounded-md border border-input overflow-hidden h-10 text-sm">
                <button
                  type="button"
                  onClick={() => setAccessMode('Department')}
                  className={`flex-1 transition-colors ${accessMode === 'Department' ? 'bg-primary text-white' : 'bg-white text-gray-600 hover:bg-gray-50'}`}
                >
                  Theo khoa
                </button>
                <button
                  type="button"
                  onClick={() => setAccessMode('AssignedList')}
                  className={`flex-1 transition-colors border-l ${accessMode === 'AssignedList' ? 'bg-primary text-white' : 'bg-white text-gray-600 hover:bg-gray-50'}`}
                >
                  Danh sách chỉ định
                </button>
              </div>
            </div>
          </div>

          {accessMode === 'AssignedList' ? (
            <p className="text-[11px] text-gray-400 -mt-2">
              Chỉ những người có trong danh sách được úp lên mới được thi. Lưu kỳ thi trước, sau đó dùng nút "Ai được thi" ở trang chi tiết kỳ thi để úp danh sách Excel/CSV.
            </p>
          ) : (
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-1 relative" ref={dropdownRef}>
              <label className="text-sm font-medium text-gray-700">Khoa / Phòng ban</label>
              <div className="relative">
                <input
                  type="text"
                  placeholder={selectedDepts.length > 0 ? `Đã chọn ${selectedDepts.length} khoa...` : 'Để trống = tất cả các khoa'}
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

              {!isDeptManager && selectedDepts.length > 0 && (
                <div className="flex flex-wrap gap-1 pt-1">
                  {selectedDepts.map((d) => (
                    <span key={d.id} className="inline-flex items-center gap-1 rounded-full bg-blue-50 text-blue-900 text-xs px-2 py-0.5">
                      {d.departmentName}
                      <button type="button" onClick={() => toggleDept(d.id)} className="hover:text-red-600">
                        <X className="h-3 w-3" />
                      </button>
                    </span>
                  ))}
                </div>
              )}

              <p className="text-[11px] text-gray-400">Chọn 1-n khoa được thấy/thi kỳ thi này. Để trống = tất cả các khoa.</p>

              {isDropdownOpen && (
                <div className="absolute z-50 w-full mt-1 max-h-60 overflow-y-auto rounded-md border bg-white shadow-lg">
                  {filteredDepts.length === 0 ? (
                    <div className="p-3 text-sm text-gray-500 text-center">Không tìm thấy khoa nào</div>
                  ) : (
                    filteredDepts.map((d) => {
                      const isSelected = selectedDeptIds.includes(d.id)
                      return (
                        <div
                          key={d.id}
                          className={`flex items-center justify-between px-3 py-2 cursor-pointer text-sm hover:bg-gray-100 transition-colors ${
                            isSelected ? 'bg-blue-50 text-blue-900 font-semibold' : 'text-gray-900'
                          }`}
                          onClick={() => toggleDept(d.id)}
                        >
                          <span>{d.departmentName} ({d.deptCode})</span>
                          {isSelected && <Check className="h-4 w-4 text-blue-600" />}
                        </div>
                      )
                    })
                  )}
                </div>
              )}
            </div>
          </div>
          )}

          {!isDeptManager && (
            <div className="space-y-1">
              <label className="text-sm font-medium text-gray-700">Đơn vị tổ chức</label>
              <Input {...form.register('organizedBy')} placeholder="Ví dụ: Phòng KHTH" />
              {form.formState.errors.organizedBy && (
                <p className="text-xs text-red-500">{form.formState.errors.organizedBy.message}</p>
              )}
            </div>
          )}

          <div className="space-y-1">
            <label className="text-sm font-medium text-gray-700">Tên kỳ thi *</label>
            <Input {...form.register('campaignName')} placeholder="Kỳ thi nội bộ Q2/2026" />
            {form.formState.errors.campaignName && <p className="text-xs text-red-500">{form.formState.errors.campaignName.message}</p>}
          </div>

          <div className="space-y-1">
            <label className="text-sm font-medium text-gray-700">Mô tả</label>
            <textarea {...form.register('description')} rows={2} className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm" placeholder="Mô tả kỳ thi..." />
          </div>

          {isPracticeMode ? (
            <p className="text-[11px] text-gray-400">
              Chế độ luyện tập không cần chọn thời gian - hệ thống tự gán khoảng thời gian rất dài để luôn sẵn sàng.
            </p>
          ) : (
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-1">
              <label className="text-sm font-medium text-gray-700">Bắt đầu *</label>
              <Input {...form.register('startTime')} type="datetime-local" />
              {form.formState.errors.startTime && (
                <p className="text-xs text-red-500">{form.formState.errors.startTime.message}</p>
              )}
            </div>
            <div className="space-y-1">
              <label className="text-sm font-medium text-gray-700">Kết thúc *</label>
              <Input {...form.register('endTime')} type="datetime-local" />
              {form.formState.errors.endTime && (
                <p className="text-xs text-red-500">{form.formState.errors.endTime.message}</p>
              )}
            </div>
          </div>
          )}

          <div className="space-y-1">
            <label className="text-sm font-medium text-gray-700">Tổng số câu hỏi mỗi đề *</label>
            <Input
              {...form.register('totalQuestions')}
              type="number"
              placeholder="VD: 50"
            />
            {form.formState.errors.totalQuestions && (
              <p className="text-xs text-red-500">{form.formState.errors.totalQuestions.message}</p>
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
              {...form.register('minPassQuestions')}
              type="number"
              placeholder="Để trống nếu không xét đạt/không đạt"
            />
            {form.formState.errors.minPassQuestions && (
              <p className="text-xs text-red-500">{form.formState.errors.minPassQuestions.message}</p>
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
