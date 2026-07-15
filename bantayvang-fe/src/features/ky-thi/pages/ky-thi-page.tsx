import { useEffect, useState } from 'react'
import { kyThiApi } from '../api'
import { KyThiTable } from '../components/ky-thi-table'
import { KyThiFormDialog } from '../components/ky-thi-form-dialog'
import { GenerateExamsDialog } from '../components/generate-exams-dialog'
import type { ExamCampaignDto, CreateKyThiDto } from '../types'
import type { CreateKyThiFormData } from '../schemas'
import { Button } from '@/components/ui/button'
import { Plus, ArrowLeft, RefreshCw, Building2, Sparkles, Search } from 'lucide-react'
import { useAppDispatch, useAppSelector } from '@/app/hooks'
import { fetchActiveExams } from '@/features/exams/slice'
import { ROLES } from '@/lib/constants'

// Imports from exams feature
import { ExamTable } from '@/features/exams/components/exam-table'
import { ExamFormDialog } from '@/features/exams/components/exam-form-dialog'
import { AssignUsersDialog } from '@/features/exams/components/assign-users-dialog'
import { ExamPreviewModal } from '@/features/exams/components/exam-preview-modal'
import { examsApi, examsApiExtended } from '@/features/exams/api'
import { departmentApi } from '@/features/departments/api'
import type { ExamPaperDto } from '@/features/exams/types'
import type { CreateExamFormData } from '@/features/exams/schemas'
import { GradingPage } from '@/features/grading/pages/grading-page'

const STATUS_OPTIONS = [
  { value: 'DangChuanBi', label: 'Đang chuẩn bị' },
  { value: 'DangDienRa', label: 'Đang diễn ra' },
  { value: 'TamDung', label: 'Tạm dừng' },
  { value: 'DaKetThuc', label: 'Đã kết thúc' },
]

export function KyThiPage() {
  const dispatch = useAppDispatch()
  const currentUser = useAppSelector((state) => state.auth.user)

  const isDeptManager = currentUser?.role === ROLES.DEPT_MANAGER || currentUser?.tenVaiTro === 'DeptManager'
  const isAdmin = !isDeptManager
  const myKhoa = currentUser?.tenKhoaQuanLy || currentUser?.department || null

  const [examCampaigns, setKyThis] = useState<ExamCampaignDto[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [formOpen, setFormOpen] = useState(false)
  const [editingKyThi, setEditingKyThi] = useState<ExamCampaignDto | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [filterStatus, setFilterStatus] = useState<string>('')
  const [khoaFilter, setKhoaFilter] = useState<string | null>(null)
  const [searchQuery, setSearchQuery] = useState('')
  const [examSearchQuery, setExamSearchQuery] = useState('')


  // Detail view state
  const [selectedKyThi, setSelectedKyThi] = useState<ExamCampaignDto | null>(null)
  const [exams, setExams] = useState<ExamPaperDto[]>([])
  const [loadingExams, setLoadingExams] = useState(false)
  const [examFormOpen, setExamFormOpen] = useState(false)

  // Dialog states for Exam operations
  const [assignOpen, setAssignOpen] = useState(false)
  const [selectedExam, setSelectedExam] = useState<ExamPaperDto | null>(null)
  const [previewExam, setPreviewExam] = useState<ExamPaperDto | null>(null)
  const [generateExamsOpen, setGenerateExamsOpen] = useState(false)
  const [statsExam, setStatsExam] = useState<ExamPaperDto | null>(null)

  useEffect(() => {
    loadKyThis()
    dispatch(fetchActiveExams())
  }, [dispatch, filterStatus]) // eslint-disable-line react-hooks/exhaustive-deps

  const loadKyThis = async () => {
    setIsLoading(true)
    try {
      const response = await kyThiApi.getAll(filterStatus || undefined)
      if (response.data.success && response.data.data) {
        setKyThis(response.data.data)
      }
    } catch {
      // silent
    } finally {
      setIsLoading(false)
    }
  }

  const loadExamsForKyThi = async (kyThiId: number) => {
    setLoadingExams(true)
    try {
      const res = await examsApiExtended.getAll()
      if (res.data.success && res.data.data) {
        // Lọc các đề thi có KyThiId khớp với kỳ thi hiện tại
        const filtered = res.data.data.filter((e) => e.kyThiId === kyThiId)
        setExams(filtered)
      }
    } catch {
      // silent
    } finally {
      setLoadingExams(false)
    }
  }

  // ---- KY THI CRUD ----
  const handleOpenCreate = () => { setEditingKyThi(null); setFormOpen(true) }
  const handleOpenEdit = (examCampaign: ExamCampaignDto) => { setEditingKyThi(examCampaign); setFormOpen(true) }

  const handleSubmitKyThi = async (data: CreateKyThiFormData) => {
    setSubmitting(true)
    try {
      const formattedData = {
        ...data,
        thoiGianBatDau: data.thoiGianBatDau ? new Date(data.thoiGianBatDau).toISOString() : null,
        thoiGianKetThuc: data.thoiGianKetThuc ? new Date(data.thoiGianKetThuc).toISOString() : null,
      }
      if (editingKyThi) {
        const updateData: any = isDeptManager
          ? { ...formattedData, khoaPhongId: currentUser?.idKhoaQuanLy || null, status: editingKyThi.status || 'DangChuanBi' }
          : { ...formattedData, status: editingKyThi.status || 'DangChuanBi' }
        await kyThiApi.update(editingKyThi.id, updateData)
      } else {
        const createData: CreateKyThiDto = isDeptManager
          ? { ...formattedData, khoaPhongId: currentUser?.idKhoaQuanLy || null }
          : formattedData
        await kyThiApi.create(createData)
      }
      setFormOpen(false)
      setEditingKyThi(null)
      loadKyThis()
    } catch (err: any) {
      alert(err?.response?.data?.message || 'Có lỗi khi lưu kỳ thi')
    } finally {
      setSubmitting(false)
    }
  }

  const handleDeleteKyThi = async (examCampaign: ExamCampaignDto) => {
    if (!window.confirm(`Xóa kỳ thi "${examCampaign.campaignName}"? Tất cả đề thi bên trong cũng sẽ bị ảnh hưởng.`)) return
    try {
      await kyThiApi.delete(examCampaign.id)
      loadKyThis()
      if (selectedKyThi?.id === examCampaign.id) setSelectedKyThi(null)
    } catch {
      alert('Không thể xóa kỳ thi này')
    }
  }

  const handleChangeStatus = async (examCampaign: ExamCampaignDto, newStatus: string) => {
    try {
      await kyThiApi.updateStatus(examCampaign.id, newStatus)
      loadKyThis()
      if (selectedKyThi?.id === examCampaign.id) {
        setSelectedKyThi((prev) => prev ? { ...prev, status: newStatus } : prev)
      }
    } catch {
      alert('Không thể đổi trạng thái')
    }
  }

  const handleViewKyThi = (examCampaign: ExamCampaignDto) => {
    setSelectedKyThi(examCampaign)
    loadExamsForKyThi(examCampaign.id)
  }

  // ---- EXAM CRUD & HANDLERS ----
  const handleToggleStatus = async (exam: ExamPaperDto) => {
    const isActive = exam.status === 'Active'
    const newStatus = isActive ? 'Inactive' : 'Active'
    const label = isActive ? 'Tắt' : 'Bật'
    if (!window.confirm(`${label} đề thi "${exam.examPaperName}"?`)) return
    try {
      await examsApiExtended.updateStatus(exam.id, newStatus)
      if (selectedKyThi) loadExamsForKyThi(selectedKyThi.id)
    } catch {
      alert('Không thể thay đổi trạng thái đề thi')
    }
  }

  const handleToggleCongBo = async (exam: ExamPaperDto) => {
    const newVal = !exam.isResultPublished
    try {
      await departmentApi.toggleExamVisibility(exam.id, { isResultPublished: newVal })
      if (selectedKyThi) loadExamsForKyThi(selectedKyThi.id)
    } catch {
      alert('Không thể cập nhật trạng thái công bố')
    }
  }

  const handleDeleteExam = async (exam: ExamPaperDto) => {
    if (!window.confirm(`Xóa đề thi "${exam.examPaperName}"? Hành động này không thể hoàn tác.`)) return
    try {
      await examsApiExtended.delete(exam.id)
      if (selectedKyThi) loadExamsForKyThi(selectedKyThi.id)
    } catch {
      alert('Không thể xóa đề thi này')
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
        alert(`Đã phân công ${res.data.data ?? userIds.length} thí sinh`)
      } else {
        alert(res.data.message || 'Phân công thất bại')
      }
    } catch {
      alert('Có lỗi khi phân công thí sinh')
    } finally {
      setSubmitting(false)
    }
  }

  const handleCreateExamSubmit = async (data: CreateExamFormData) => {
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
        kyThiId: selectedKyThi?.id,
        soCauDungToiThieu: data.soCauDungToiThieu ?? null,
      }
      const res = await examsApi.create(createDto)
      if (res.data.success) {
        setExamFormOpen(false)
        if (selectedKyThi) loadExamsForKyThi(selectedKyThi.id)
        alert('Tạo đề thi thành công')
      } else {
        alert(res.data.message || 'Tạo đề thi thất bại')
      }
    } catch (err: any) {
      alert(err?.response?.data?.message || 'Có lỗi xảy ra')
    } finally {
      setSubmitting(false)
    }
  }

  // Scope: DeptManager chỉ thấy kỳ thi do khoa mình quản lý (khoaPhongId = idKhoaQuanLy)
  const scopedKyThis = isDeptManager && currentUser?.idKhoaQuanLy
    ? examCampaigns.filter((k) => k.khoaPhongId === currentUser.idKhoaQuanLy)
    : examCampaigns

  // Danh sách khoa duy nhất cho tab admin
  const khoaList = isAdmin
    ? Array.from(new Set(examCampaigns.map((k) => k.departmentName).filter(Boolean) as string[])).sort()
    : []

  // Áp filter khoa (admin)
  const khoaFiltered = isAdmin && khoaFilter
    ? scopedKyThis.filter((k) => k.departmentName === khoaFilter)
    : scopedKyThis

  const finalFilteredKyThis = khoaFiltered.filter(k => 
    !searchQuery.trim() ||
    k.campaignName?.toLowerCase().includes(searchQuery.toLowerCase()) ||
    k.campaignCode?.toLowerCase().includes(searchQuery.toLowerCase())
  )

  const filteredExams = exams.filter(e => 
    !examSearchQuery.trim() ||
    e.examPaperName?.toLowerCase().includes(examSearchQuery.toLowerCase()) ||
    e.examPaperCode?.toLowerCase().includes(examSearchQuery.toLowerCase())
  )


  // ---- DETAIL VIEW ----
  if (selectedKyThi) {
    if (statsExam) {
      return (
        <div>
          <div className="flex items-center gap-3 mb-6">
            <Button variant="ghost" size="sm" onClick={() => setStatsExam(null)} className="gap-1">
              <ArrowLeft className="h-4 w-4" />
              Quay lại kỳ thi
            </Button>
            <span className="text-gray-300">›</span>
            <div>
              <h1 className="text-xl font-bold text-gray-900">Thống kê kết quả: {statsExam.examPaperName}</h1>
              <p className="text-sm text-gray-500">Mã đề: {statsExam.examPaperCode}</p>
            </div>
          </div>
          <GradingPage preselectedExamId={statsExam.id} />
        </div>
      )
    }

    return (
      <div>
        <div className="flex items-center gap-3 mb-6">
          <Button variant="ghost" size="sm" onClick={() => { setSelectedKyThi(null); setStatsExam(null); }} className="gap-1">
            <ArrowLeft className="h-4 w-4" />
            Danh sách kỳ thi
          </Button>
          <span className="text-gray-300">›</span>
          <div>
            <h1 className="text-xl font-bold text-gray-900">{selectedKyThi.campaignName}</h1>
            <p className="text-sm text-gray-500">
              {selectedKyThi.campaignCode} • Khoa: {selectedKyThi.departmentName || 'Tất cả các khoa'}
            </p>
          </div>
          <div className="ml-auto flex items-center gap-2">
            <select
              value={selectedKyThi.status || ''}
              onChange={(e) => handleChangeStatus(selectedKyThi, e.target.value)}
              className="h-8 rounded-md border border-gray-200 bg-white px-2 text-xs"
            >
              {STATUS_OPTIONS.map((s) => (
                <option key={s.value} value={s.value}>{s.label}</option>
              ))}
            </select>
            <Button size="sm" variant="outline" onClick={() => handleOpenEdit(selectedKyThi)}>
              Sửa kỳ thi
            </Button>
          </div>
        </div>

        {/* Exams list & Management */}
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden mb-6">
          <div className="flex items-center justify-between p-5 border-b border-gray-100 bg-gray-50/50">
            <div>
              <h2 className="text-lg font-bold text-gray-900 flex items-center gap-2">
                📋 Danh sách đề thi
              </h2>
              <p className="text-xs text-gray-500 mt-0.5">Quản lý các đề thi trực thuộc kỳ thi này</p>
            </div>
            <div className="flex items-center gap-2">
              <Button size="sm" variant="outline" onClick={() => loadExamsForKyThi(selectedKyThi.id)} className="h-9 gap-1 text-gray-600">
                <RefreshCw className="h-3.5 w-3.5" />
                Làm mới
              </Button>
              <Button size="sm" variant="outline" onClick={() => setGenerateExamsOpen(true)} className="h-9 gap-1 text-blue-600 border-blue-200 hover:bg-blue-50/50">
                <Sparkles className="h-3.5 w-3.5" />
                Sinh đề ngẫu nhiên
              </Button>
              <Button size="sm" onClick={() => setExamFormOpen(true)} className="h-9 gap-1 bg-blue-600 hover:bg-blue-700 text-white">
                <Plus className="h-3.5 w-3.5" />
                Tạo đề thủ công
              </Button>
            </div>
          </div>

          <div className="p-5 space-y-4">
            <div className="relative max-w-xs">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400" />
              <input
                type="text"
                placeholder="Tìm đề thi..."
                value={examSearchQuery}
                onChange={(e) => setExamSearchQuery(e.target.value)}
                className="w-full pl-9 pr-4 py-2 border border-gray-200 rounded-lg text-sm placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all bg-white"
              />
            </div>
            <ExamTable
              exams={filteredExams}
              isLoading={loadingExams}
              showKhoa={false}
              onViewAssignments={handleViewAssignments}
              onToggleStatus={handleToggleStatus}
              onToggleCongBo={handleToggleCongBo}
              onDelete={handleDeleteExam}
              onPreview={(exam) => setPreviewExam(exam)}
              onViewStats={(exam) => setStatsExam(exam)}
            />
          </div>

        </div>

        <KyThiFormDialog
          open={formOpen}
          examCampaign={editingKyThi}
          onClose={() => { setFormOpen(false); setEditingKyThi(null) }}
          onSubmit={handleSubmitKyThi}
          isLoading={submitting}
        />

        <GenerateExamsDialog
          open={generateExamsOpen}
          examCampaign={selectedKyThi}
          onClose={() => setGenerateExamsOpen(false)}
          onSuccess={(msg) => {
            alert(msg)
            loadExamsForKyThi(selectedKyThi.id)
            loadKyThis()
          }}
        />

        <ExamFormDialog
          open={examFormOpen}
          onClose={() => setExamFormOpen(false)}
          onSubmit={handleCreateExamSubmit}
          isLoading={submitting}
          lockedKhoa={isDeptManager && myKhoa ? myKhoa : undefined}
          kyThiList={[{ id: selectedKyThi.id, tenKyThiText: selectedKyThi.campaignName || '' }]}
          defaultKyThiId={selectedKyThi.id}
        />

        <AssignUsersDialog
          open={assignOpen}
          exam={selectedExam}
          onClose={() => setAssignOpen(false)}
          onSubmit={handleAssignUsers}
          isLoading={submitting}
        />

        <ExamPreviewModal exam={previewExam} onClose={() => setPreviewExam(null)} />
      </div>
    )
  }

  // ---- LIST VIEW ----
  return (
    <div>
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Kỳ thi</h1>
          {isDeptManager && myKhoa && (
            <p className="text-sm text-blue-600 mt-0.5">
              🏆 Khoa: <strong>{myKhoa}</strong> — Chỉ hiển thị kỳ thi của khoa bạn
            </p>
          )}
        </div>
        <div className="flex gap-2 flex-wrap">
          <Button variant="outline" size="sm" onClick={loadKyThis}>
            <RefreshCw className="h-4 w-4 mr-1" />
            Làm mới
          </Button>
          <Button onClick={handleOpenCreate}>
            <Plus className="h-4 w-4 mr-1" />
            Tạo kỳ thi
          </Button>
        </div>
      </div>

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
            Tất cả ({scopedKyThis.length})
          </button>
          {khoaList.map((khoa) => {
            const count = scopedKyThis.filter((k) => k.donViToChuc === khoa).length
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
        </div>
      )}

      {/* Filter trạng thái & Tìm kiếm */}
      <div className="flex flex-wrap items-center gap-3 mb-4">
        <div className="relative flex-1 max-w-xs">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400" />
          <input
            type="text"
            placeholder="Tìm kiếm kỳ thi..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="w-full pl-9 pr-4 py-2 border border-gray-200 rounded-lg text-sm placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all bg-white"
          />
        </div>

        <label className="text-sm text-gray-600">Lọc trạng thái:</label>
        <select
          value={filterStatus}
          onChange={(e) => setFilterStatus(e.target.value)}
          className="h-9 rounded-md border border-gray-200 px-2 text-sm bg-white"
        >
          <option value="">Tất cả</option>
          {STATUS_OPTIONS.map((s) => (
            <option key={s.value} value={s.value}>{s.label}</option>
          ))}
        </select>
        <span className="text-sm text-gray-400">{finalFilteredKyThis.length} kỳ thi</span>
      </div>

      <KyThiTable
        examCampaigns={finalFilteredKyThis}
        isLoading={isLoading}
        showKhoa={isAdmin && khoaFilter === null}
        onView={handleViewKyThi}
        onEdit={handleOpenEdit}
        onDelete={handleDeleteKyThi}
        onChangeStatus={handleChangeStatus}
      />


      <KyThiFormDialog
        open={formOpen}
        examCampaign={editingKyThi}
        onClose={() => { setFormOpen(false); setEditingKyThi(null) }}
        onSubmit={handleSubmitKyThi}
        isLoading={submitting}
      />
    </div>
  )
}
