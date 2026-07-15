import { useEffect, useState } from 'react'
import { useAppDispatch, useAppSelector } from '@/app/hooks'
import { fetchAllExams } from '../slice'
import { examsApi, examsApiExtended } from '../api'
import { ExamTable } from '../components/exam-table'
import { ExamFormDialog } from '../components/exam-form-dialog'
import { AssignUsersDialog } from '../components/assign-users-dialog'
import type { ExamPaperDto } from '../types'
import type { CreateExamFormData } from '../schemas'
import { Button } from '@/components/ui/button'
import { Plus, Search, RefreshCw, Building2 } from 'lucide-react'
import { GradingPage } from '@/features/grading/pages/grading-page'
import { departmentApi } from '@/features/departments/api'
import { ExamPreviewModal } from '../components/exam-preview-modal'
import { ROLES } from '@/lib/constants'
import { kyThiApi } from '@/features/ky-thi/api'

export function ExamsPage() {
  const dispatch = useAppDispatch()
  const { exams, isLoading } = useAppSelector((state) => state.exams)
  const currentUser = useAppSelector((state) => state.auth.user)

  const isDeptManager = currentUser?.role === ROLES.DEPT_MANAGER || currentUser?.tenVaiTro === 'DeptManager'
  const isAdmin = !isDeptManager
  const myKhoa = currentUser?.tenKhoaQuanLy || currentUser?.department || null

  const [formOpen, setFormOpen] = useState(false)
  const [assignOpen, setAssignOpen] = useState(false)
  const [statsExam, setStatsExam] = useState<ExamPaperDto | null>(null)
  const [selectedExam, setSelectedExam] = useState<ExamPaperDto | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [previewExam, setPreviewExam] = useState<ExamPaperDto | null>(null)
  const [search, setSearch] = useState('')
  const [statusFilter, setStatusFilter] = useState<string>('all')
  // Admin: filter theo khoa (null = tất cả)
  const [khoaFilter, setKhoaFilter] = useState<string | null>(null)
  const [toast, setToast] = useState<{ msg: string; ok: boolean } | null>(null)
  const [kyThiList, setKyThiList] = useState<{ id: number; tenKyThiText: string }[]>([])
  const [defaultKyThiId, setDefaultKyThiId] = useState<number | undefined>(() => {
    const saved = localStorage.getItem('bantayvang_last_kythi_id')
    return saved ? Number(saved) : undefined
  })

  const showToast = (msg: string, ok = true) => {
    setToast({ msg, ok })
    setTimeout(() => setToast(null), 3500)
  }

  useEffect(() => {
    dispatch(fetchAllExams())
    const loadKyThis = async () => {
      try {
        const res = await kyThiApi.getAll()
        if (res.data.success && res.data.data) {
          const list = res.data.data.map(k => ({
            id: k.id,
            tenKyThiText: k.campaignName || ''
          }))
          setKyThiList(list)
        }
      } catch {
        // silent
      }
    }
    loadKyThis()
  }, [dispatch])

  const handleCreateExam = async (data: CreateExamFormData) => {
    setSubmitting(true)
    try {
      const createDto = {
        examPaperCode: data.examPaperCode,
        examPaperName: data.examPaperName,
        durationMinutes: data.durationMinutes ?? 60,
        thoiGianBatDau: data.thoiGianBatDau ? data.thoiGianBatDau : undefined,
        status: data.status,
        department: isDeptManager && myKhoa ? myKhoa : data.department,
        soCauRandom: data.soCauRandom,
        danhSachIdCauHoi: data.danhSachIdCauHoi ?? [],
        kyThiId: data.kyThiId,
        soCauDungToiThieu: data.soCauDungToiThieu ?? null,
      }
      const res = await examsApi.create(createDto)
      if (res.data.success) {
        if (data.kyThiId) {
          setDefaultKyThiId(data.kyThiId)
          localStorage.setItem('bantayvang_last_kythi_id', String(data.kyThiId))
        }
        setFormOpen(false)
        dispatch(fetchAllExams())
        showToast('Tạo đề thi thành công')
      } else {
        showToast(res.data.message || 'Tạo đề thi thất bại', false)
      }
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message || 'Có lỗi xảy ra'
      showToast(msg, false)
    } finally {
      setSubmitting(false)
    }
  }

  const handleToggleStatus = async (exam: ExamPaperDto) => {
    const isActive = exam.status === 'Active'
    const newStatus = isActive ? 'Inactive' : 'Active'
    const label = isActive ? 'Tắt' : 'Bật'
    if (!window.confirm(`${label} đề thi "${exam.examPaperName}"?`)) return
    try {
      await examsApiExtended.updateStatus(exam.id, newStatus)
      dispatch(fetchAllExams())
      showToast(`Đã ${label.toLowerCase()} đề thi "${exam.examPaperName}"`)
    } catch {
      showToast('Không thể thay đổi trạng thái đề thi', false)
    }
  }

  const handleToggleCongBo = async (exam: ExamPaperDto) => {
    const newVal = !exam.isResultPublished
    try {
      await departmentApi.toggleExamVisibility(exam.id, { isResultPublished: newVal })
      dispatch(fetchAllExams())
      showToast(newVal ? `Đã bật công bố điểm cho "${exam.examPaperName}"` : `Đã tắt công bố điểm cho "${exam.examPaperName}"`)
    } catch {
      showToast('Không thể cập nhật trạng thái công bố', false)
    }
  }

  const handleDelete = async (exam: ExamPaperDto) => {
    if (!window.confirm(`Xóa đề thi "${exam.examPaperName}"? Hành động này không thể hoàn tác.`)) return
    try {
      await examsApiExtended.delete(exam.id)
      dispatch(fetchAllExams())
      showToast('Đã xóa đề thi')
    } catch {
      showToast('Không thể xóa đề thi này', false)
    }
  }

  const handleViewAssignments = (exam: ExamPaperDto) => {
    setSelectedExam(exam)
    setAssignOpen(true)
  }

  const handleAssignUsers = async (examId: number, userIds: number[], note?: string) => {
    setSubmitting(true)
    try {
      const res = await examsApi.assignUsers({ examId, userIds, note })
      if (res.data.success) {
        setAssignOpen(false)
        showToast(`Đã phân công ${res.data.data ?? userIds.length} thí sinh`)
      } else {
        showToast(res.data.message || 'Phân công thất bại', false)
      }
    } catch {
      showToast('Có lỗi khi phân công thí sinh', false)
    } finally {
      setSubmitting(false)
    }
  }

  // Lấy danh sách khoa duy nhất từ dữ liệu thực tế
  const khoaList = Array.from(
    new Set(exams.map((e) => e.department).filter(Boolean) as string[])
  ).sort()
  // Có đề thi chưa gán khoa không?
  const hasUnassigned = exams.some(e => !e.department)

  // Scope đề thi theo role
  const scopedExams = isDeptManager && myKhoa
    ? exams.filter((e) => e.department === myKhoa)
    : exams

  // Filter theo khoa (chỉ admin dùng)
  const khoaFilteredExams = isAdmin && khoaFilter
    ? khoaFilter === '__unassigned__'
      ? scopedExams.filter((e) => !e.department)
      : scopedExams.filter((e) => e.department === khoaFilter)
    : scopedExams

  // Filter theo tên/mã + trạng thái
  const filtered = khoaFilteredExams.filter((e) => {
    const matchSearch =
      !search ||
      e.examPaperName?.toLowerCase().includes(search.toLowerCase()) ||
      e.examPaperCode?.toLowerCase().includes(search.toLowerCase())
    const matchStatus = statusFilter === 'all' || e.status === statusFilter
    return matchSearch && matchStatus
  })

  const countByStatus = (s: string) => khoaFilteredExams.filter((e) => e.status === s).length

  if (statsExam) {
    return (
      <div>
        <div className="flex items-center gap-3 mb-6">
          <button
            onClick={() => setStatsExam(null)}
            className="text-sm text-gray-500 hover:text-primary transition-colors flex items-center gap-1"
          >
            ← Quay lại danh sách
          </button>
          <span className="text-gray-300">|</span>
          <h1 className="text-xl font-bold text-gray-900">Kết quả: {statsExam.examPaperName}</h1>
        </div>
        <GradingPage preselectedExamId={statsExam.id} />
      </div>
    )
  }

  return (
    <div>
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Quản lý đề thi</h1>
          {isDeptManager && myKhoa && (
            <p className="text-sm text-blue-600 mt-0.5">
              📋 Khoa: <strong>{myKhoa}</strong> — Chỉ hiển thị đề thi của khoa bạn
            </p>
          )}
        </div>
        <div className="flex gap-2 flex-wrap">
          <Button variant="outline" size="sm" onClick={() => dispatch(fetchAllExams())}>
            <RefreshCw className="h-4 w-4 mr-1" />
            Làm mới
          </Button>
          <Button onClick={() => setFormOpen(true)}>
            <Plus className="h-4 w-4 mr-1" />
            Tạo đề thi
          </Button>
        </div>
      </div>

      {/* Toast */}
      {toast && (
        <div
          className={`mb-4 flex items-center gap-2 rounded-xl px-4 py-2.5 text-sm border ${
            toast.ok
              ? 'bg-green-50 border-green-200 text-green-700'
              : 'bg-red-50 border-red-200 text-red-700'
          }`}
        >
          {toast.ok ? '✓' : '⚠'} {toast.msg}
        </div>
      )}

      {/* ===== ADMIN: Tab lọc theo Khoa ===== */}
      {isAdmin && khoaList.length > 0 && (
        <div className="flex items-center gap-2 mb-4 flex-wrap">
          <Building2 className="h-4 w-4 text-gray-400 shrink-0" />
          <button
            onClick={() => setKhoaFilter(null)}
            className={`px-3 py-1 rounded-full text-xs font-medium border transition-colors ${
              khoaFilter === null
                ? 'bg-blue-600 text-white border-blue-600'
                : 'bg-white text-gray-600 border-gray-200 hover:border-blue-300 hover:text-blue-600'
            }`}
          >
            Tất cả ({scopedExams.length})
          </button>
          {khoaList.map((khoa) => {
            const count = scopedExams.filter((e) => e.department === khoa).length
            return (
              <button
                key={khoa}
                onClick={() => setKhoaFilter(khoa)}
                className={`px-3 py-1 rounded-full text-xs font-medium border transition-colors ${
                  khoaFilter === khoa
                    ? 'bg-blue-600 text-white border-blue-600'
                    : 'bg-white text-gray-600 border-gray-200 hover:border-blue-300 hover:text-blue-600'
                }`}
              >
                {khoa} ({count})
              </button>
            )
          })}
          {hasUnassigned && (
            <button
              onClick={() => setKhoaFilter('__unassigned__')}
              className={`px-3 py-1 rounded-full text-xs font-medium border transition-colors ${
                khoaFilter === '__unassigned__'
                  ? 'bg-gray-700 text-white border-gray-700'
                  : 'bg-white text-gray-400 border-gray-200 hover:border-gray-400 hover:text-gray-600'
              }`}
            >
              Chưa gán khoa ({scopedExams.filter(e => !e.department).length})
            </button>
          )}
        </div>
      )}

      {/* Search + Status filter */}
      <div className="flex gap-3 mb-4">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400" />
          <input
            type="text"
            placeholder="Tìm kiếm theo tên hoặc mã đề..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="w-full pl-9 pr-4 py-2 border border-gray-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary"
          />
        </div>
        <select
          value={statusFilter}
          onChange={(e) => setStatusFilter(e.target.value)}
          className="px-3 py-2 border border-gray-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary bg-white"
        >
          <option value="all">Tất cả ({khoaFilteredExams.length})</option>
          <option value="Active">Đang mở ({countByStatus('Active')})</option>
          <option value="Inactive">Đã tắt ({countByStatus('Inactive')})</option>
          <option value="Draft">Nháp ({countByStatus('Draft')})</option>
          <option value="Completed">Hoàn thành ({countByStatus('Completed')})</option>
        </select>
      </div>

      {/* Stats summary */}
      {!isLoading && khoaFilteredExams.length > 0 && (
        <div className="flex gap-4 mb-4 text-sm text-gray-500">
          <span>{khoaFilteredExams.length} đề thi</span>
          <span>·</span>
          <span className="text-green-600">{countByStatus('Active')} đang mở</span>
          <span>·</span>
          <span className="text-gray-400">{countByStatus('Inactive')} đã tắt</span>
          <span>·</span>
          <span>{countByStatus('Draft')} nháp</span>
        </div>
      )}

      <ExamTable
        exams={filtered}
        isLoading={isLoading}
        showKhoa={isAdmin && khoaFilter === null}   // hiện cột Khoa khi admin đang xem "Tất cả"
        onViewAssignments={handleViewAssignments}
        onToggleStatus={handleToggleStatus}
        onToggleCongBo={handleToggleCongBo}
        onDelete={handleDelete}
        onViewStats={(exam) => setStatsExam(exam)}
        onPreview={(exam) => setPreviewExam(exam)}
      />

      <ExamPreviewModal exam={previewExam} onClose={() => setPreviewExam(null)} />

      <ExamFormDialog
        open={formOpen}
        onClose={() => setFormOpen(false)}
        onSubmit={handleCreateExam}
        isLoading={submitting}
        lockedKhoa={isDeptManager && myKhoa ? myKhoa : undefined}
        kyThiList={kyThiList}
        defaultKyThiId={defaultKyThiId}
      />

      <AssignUsersDialog
        open={assignOpen}
        exam={selectedExam}
        onClose={() => setAssignOpen(false)}
        onSubmit={handleAssignUsers}
        isLoading={submitting}
      />
    </div>
  )
}
