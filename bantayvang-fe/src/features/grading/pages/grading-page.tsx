import { useEffect, useState, useMemo } from 'react'
import { useAppDispatch, useAppSelector } from '@/app/hooks'
import { fetchActiveExams } from '@/features/exams/slice'
import { gradingApi } from '../api'
import { ExamResultsTable } from '../components/exam-results-table'
import { ResultDetailDialog } from '../components/result-detail-dialog'
import type { ExamResultDetailDto } from '../types'
import { Button } from '@/components/ui/button'
import { Download, RefreshCw, Eye, EyeOff, Building2, CalendarDays } from 'lucide-react'
import { departmentApi } from '@/features/departments/api'
import { kyThiApi } from '@/features/ky-thi/api'
import type { KyThiDto } from '@/features/ky-thi/types'
import { ROLES } from '@/lib/constants'

export function GradingPage({ preselectedExamId }: { preselectedExamId?: number }) {
  const dispatch = useAppDispatch()
  const { exams } = useAppSelector((state) => state.exams)
  const currentUser = useAppSelector((state) => state.auth.user)
  const isDeptManager = currentUser?.role === ROLES.DEPT_MANAGER || currentUser?.tenVaiTro === 'DeptManager'
  const myKhoa = currentUser?.tenKhoaQuanLy || currentUser?.khoaPhong || null

  const [selectedKhoa, setSelectedKhoa] = useState<string | null>(isDeptManager && myKhoa ? myKhoa : null)
  const [kyThiList, setKyThiList] = useState<KyThiDto[]>([])
  const [selectedKyThiId, setSelectedKyThiId] = useState<number | null>(null)
  
  const [results, setResults] = useState<ExamResultDetailDto[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [detailBaiThiId, setDetailBaiThiId] = useState<number | null>(null)

  const [togglingVisibility, setTogglingVisibility] = useState(false)
  


  const [successMsg, setSuccessMsg] = useState<string | null>(null)
  const [errorMsg, setErrorMsg] = useState<string | null>(null)

  // Load KyThi list
  const loadKyThiList = async () => {
    try {
      const response = await kyThiApi.getAll()
      if (response.data.success && response.data.data) {
        setKyThiList(response.data.data)
      }
    } catch { /* silent */ }
  }

  useEffect(() => {
    dispatch(fetchActiveExams())
    loadKyThiList()
  }, [dispatch])

  // Sync selectedKyThiId from preselectedExamId if provided
  useEffect(() => {
    if (preselectedExamId && exams.length > 0) {
      const exam = exams.find(e => e.id === preselectedExamId)
      if (exam?.kyThiId) {
        setSelectedKyThiId(exam.kyThiId)
      }
    }
  }, [preselectedExamId, exams])

  // Fetch results when selectedKyThiId changes
  useEffect(() => {
    if (selectedKyThiId) {
      loadKyThiResults(selectedKyThiId)
    } else {
      setResults([])
    }
  }, [selectedKyThiId])

  const loadKyThiResults = async (kyThiId: number) => {
    setIsLoading(true)
    try {
      const response = await gradingApi.getByKyThi(kyThiId)
      if (response.data.success && response.data.data) {
        setResults(response.data.data)
      }
    } catch { /* silent */ } finally {
      setIsLoading(false)
    }
  }

  // Departments list from KyThi
  const khoaList = Array.from(new Set(kyThiList.map(k => k.tenKhoa || k.donViToChuc).filter(Boolean) as string[])).sort()
  const hasUnassigned = kyThiList.some(k => !k.tenKhoa && !k.donViToChuc)

  // Filter KyThi list by department
  const scopedKyThis = isDeptManager && myKhoa
    ? kyThiList.filter(k => k.tenKhoa === myKhoa || k.donViToChuc === myKhoa)
    : selectedKhoa === '__unassigned__'
      ? kyThiList.filter(k => !k.tenKhoa && !k.donViToChuc)
      : selectedKhoa
        ? kyThiList.filter(k => k.tenKhoa === selectedKhoa || k.donViToChuc === selectedKhoa)
        : kyThiList

  // All exams associated with the selected KyThi
  const examsInSelectedKyThi = useMemo(() => {
    if (!selectedKyThiId) return []
    return exams.filter(e => e.kyThiId === selectedKyThiId)
  }, [selectedKyThiId, exams])

  // Submissions grouped by examId
  const resultsByExam = useMemo(() => {
    const groups: Record<number, ExamResultDetailDto[]> = {}
    results.forEach(r => {
      if (r.examId) {
        if (!groups[r.examId]) groups[r.examId] = []
        groups[r.examId].push(r)
      }
    })
    return groups
  }, [results])

  // Determine if the entire Kỳ thi is published (all associated exams are published)
  const isAllPublished = useMemo(() => {
    if (examsInSelectedKyThi.length === 0) return false
    return examsInSelectedKyThi.every(e => e.congBoKetQua)
  }, [examsInSelectedKyThi])

  // Toggle publishing grades for the entire KyThi
  const handleToggleKyThiVisibility = async () => {
    if (!selectedKyThiId || examsInSelectedKyThi.length === 0) return
    setTogglingVisibility(true)
    setErrorMsg(null)
    const nextState = !isAllPublished
    try {
      // Toggle visibility for each exam paper belonging to this KyThi
      await Promise.all(
        examsInSelectedKyThi.map(async (exam) => {
          if (exam.congBoKetQua !== nextState) {
            await departmentApi.toggleExamVisibility(exam.id, { congBoKetQua: nextState })
          }
        })
      )
      // Reload results and sync exams
      await Promise.all([
        loadKyThiResults(selectedKyThiId),
        dispatch(fetchActiveExams())
      ])
      setSuccessMsg(nextState ? 'Đã bật công bố điểm cho toàn bộ kỳ thi' : 'Đã tắt công bố điểm cho toàn bộ kỳ thi')
      setTimeout(() => setSuccessMsg(null), 3000)
    } catch (err: any) {
      const msg = err?.response?.data?.message || 'Không thể công bố điểm'
      setErrorMsg(msg)
    } finally {
      setTogglingVisibility(false)
    }
  }

  const handleRegrade = async (baiThiId: number) => {
    if (!window.confirm('Chấm lại bài thi này?')) return
    try {
      await gradingApi.regrade(baiThiId)
      if (selectedKyThiId) {
        loadKyThiResults(selectedKyThiId)
      }
    } catch { /* silent */ }
  }


  const handleExportResults = async (examId: number, tenDeThi: string) => {
    try {
      const response = await gradingApi.exportResults(examId)
      const url = window.URL.createObjectURL(new Blob([response.data]))
      const link = document.createElement('a')
      link.href = url
      link.download = `KetQua_${tenDeThi}_${new Date().toLocaleDateString('vi-VN').replace(/\//g, '-')}.xlsx`
      link.click()
      window.URL.revokeObjectURL(url)
    } catch { /* silent */ }
  }


  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between mb-2">
        <h1 className="text-2xl font-bold text-gray-900">Kết quả & Chấm điểm</h1>
      </div>

      {successMsg && (
        <div className="bg-green-50 border border-green-200 text-green-700 rounded-lg px-4 py-2.5 text-sm transition-all duration-300">
          ✓ {successMsg}
        </div>
      )}

      {errorMsg && (
        <div className="bg-red-50 border border-red-200 text-red-700 rounded-lg px-4 py-2.5 text-sm flex items-center justify-between transition-all duration-300">
          <span>⚠️ {errorMsg}</span>
          <button onClick={() => setErrorMsg(null)} className="ml-2 text-red-400 hover:text-red-600 font-bold">×</button>
        </div>
      )}

      {/* Admin: Tab lọc theo khoa */}
      {!isDeptManager && khoaList.length > 0 && (
        <div className="flex items-center gap-2 mb-2 flex-wrap">
          <Building2 className="h-4 w-4 text-gray-400 shrink-0" />
          <span className="text-sm text-gray-500 font-medium">Khoa:</span>
          <button
            onClick={() => { setSelectedKhoa(null); setSelectedKyThiId(null) }}
            className={`px-3 py-1 rounded-full text-xs font-medium border transition-colors ${
              selectedKhoa === null
                ? 'bg-blue-600 text-white border-blue-600'
                : 'bg-white text-gray-600 border-gray-200 hover:border-blue-300 hover:text-blue-600'
            }`}
          >
            Tất cả
          </button>
          {khoaList.map((khoa) => (
            <button
              key={khoa}
              onClick={() => { setSelectedKhoa(khoa); setSelectedKyThiId(null) }}
              className={`px-3 py-1 rounded-full text-xs font-medium border transition-colors ${
                selectedKhoa === khoa
                  ? 'bg-blue-600 text-white border-blue-600'
                  : 'bg-white text-gray-600 border-gray-200 hover:border-blue-300 hover:text-blue-600'
              }`}
            >
              {khoa}
            </button>
          ))}
          {hasUnassigned && (
            <button
              onClick={() => { setSelectedKhoa('__unassigned__'); setSelectedKyThiId(null) }}
              className={`px-3 py-1 rounded-full text-xs font-medium border transition-colors ${
                selectedKhoa === '__unassigned__'
                  ? 'bg-gray-700 text-white border-gray-700'
                  : 'bg-white text-gray-400 border-gray-200 hover:border-gray-400'
              }`}
            >
              Chưa gán khoa
            </button>
          )}
        </div>
      )}

      {/* Selector + Actions */}
      <div className="flex flex-wrap items-center gap-3 bg-white p-4 rounded-xl border shadow-sm">
        <select
          className="h-10 rounded-lg border border-input bg-background px-3 text-sm min-w-[280px] focus:outline-none focus:ring-2 focus:ring-primary/30"
          value={selectedKyThiId ?? ''}
          onChange={(e) => setSelectedKyThiId(e.target.value ? Number(e.target.value) : null)}
        >
          <option value="">— Chọn kỳ thi —</option>
          {scopedKyThis.map((kt) => (
            <option key={kt.id} value={kt.id}>
              {kt.maKyThi} — {kt.tenKyThi}{kt.tenKhoa ? ` [${kt.tenKhoa}]` : kt.donViToChuc ? ` [${kt.donViToChuc}]` : ''}
            </option>
          ))}
        </select>

        {selectedKyThiId && examsInSelectedKyThi.length > 0 && (
          <div className="flex gap-2 ml-auto flex-wrap">
            <Button variant="outline" size="sm" onClick={() => loadKyThiResults(selectedKyThiId)}>
              <RefreshCw className="h-4 w-4 mr-1" />
              Làm mới
            </Button>
            {/* Toggle công bố toàn bộ kỳ thi */}
            <Button
              variant="outline" size="sm"
              className={isAllPublished ? 'border-green-400 text-green-700 bg-green-50 hover:bg-green-100' : 'border-gray-300 text-gray-600 hover:bg-gray-50'}
              disabled={togglingVisibility}
              onClick={handleToggleKyThiVisibility}
            >
              {isAllPublished ? <Eye className="h-4 w-4 mr-1" /> : <EyeOff className="h-4 w-4 mr-1" />}
              {isAllPublished ? 'Đang công bố (tất cả)' : 'Công bố điểm (tất cả)'}
            </Button>
          </div>
        )}
      </div>

      {/* Info: Trạng thái công bố & Tổng bài thi */}
      {selectedKyThiId && examsInSelectedKyThi.length > 0 && (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div className={`md:col-span-2 px-4 py-3 rounded-xl text-sm border flex items-center ${
            isAllPublished
              ? 'bg-green-50 border-green-200 text-green-700'
              : 'bg-yellow-50 border-yellow-200 text-yellow-700'
          }`}>
            {isAllPublished
              ? '✅ Điểm đã được công bố cho tất cả đề thi của kỳ thi này — thí sinh có thể xem kết quả.'
              : '⚠️ Kỳ thi này chưa được công bố hoặc một số đề chưa công bố. Thí sinh chưa thể xem đầy đủ kết quả.'}
          </div>
          <div className="bg-blue-50/50 border border-blue-100 rounded-xl px-4 py-3 flex items-center justify-between shadow-sm">
            <span className="text-sm font-semibold text-gray-700">Tổng bài thi của kỳ thi:</span>
            <span className="text-xl font-extrabold text-blue-700 bg-white border border-blue-200/60 px-3.5 py-1 rounded-lg shadow-sm">
              {results.length}
            </span>
          </div>
        </div>
      )}

      {/* Content area */}
      {!selectedKyThiId ? (
        <div className="text-center py-16 border-2 border-dashed rounded-xl text-gray-400 bg-white">
          <CalendarDays className="h-14 w-14 mx-auto mb-3 opacity-30 text-gray-400" />
          <p className="font-medium text-gray-500">Chọn kỳ thi để xem kết quả & chấm điểm</p>
        </div>
      ) : examsInSelectedKyThi.length === 0 ? (
        <div className="text-center py-16 border-2 border-dashed rounded-xl text-gray-400 bg-white">
          <p className="font-medium text-gray-500">Kỳ thi này chưa có đề thi nào được tạo</p>
        </div>
      ) : (
        <div className="space-y-6">
          {examsInSelectedKyThi.map((exam) => {
            const resultsForExam = resultsByExam[exam.id] || []
            const hasSubmissions = resultsForExam.length > 0

            // Check if all submissions of this exam are fully graded
            const allGradedCount = resultsForExam.filter(r => r.soCauDaCham === r.tongSoCau && (r.tongSoCau ?? 0) > 0).length
            const isFullyGraded = hasSubmissions && allGradedCount === resultsForExam.length

            return (
              <div key={exam.id} className="bg-white rounded-xl border shadow-sm overflow-hidden transition-all duration-300 hover:shadow-md">
                {/* DeThi Header */}
                <div className="bg-gray-50 border-b px-5 py-4 flex flex-wrap items-center justify-between gap-3">
                  <div className="space-y-1">
                    <h3 className="text-lg font-bold text-gray-900">
                      {exam.tenDeThi}
                    </h3>
                    <div className="flex flex-wrap items-center gap-2 text-xs">
                      <span className="px-2 py-0.5 rounded bg-gray-200 text-gray-700 font-mono font-medium">
                        {exam.maDeThi}
                      </span>
                      {exam.khoaPhong && (
                        <span className="px-2 py-0.5 rounded bg-blue-50 text-blue-700 font-medium">
                          🏢 {exam.khoaPhong}
                        </span>
                      )}
                      <span className={`px-2 py-0.5 rounded font-semibold ${
                        exam.congBoKetQua ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'
                      }`}>
                        {exam.congBoKetQua ? '👁 Đã công bố điểm' : '🔒 Chưa công bố điểm'}
                      </span>
                      {hasSubmissions && (
                        <span className={`px-2 py-0.5 rounded font-semibold ${
                          isFullyGraded ? 'bg-emerald-100 text-emerald-800' : 'bg-amber-100 text-amber-800'
                        }`}>
                          {isFullyGraded ? '✓ Đã chấm xong' : `Chưa chấm xong (${allGradedCount}/${resultsForExam.length} bài)`}
                        </span>
                      )}
                    </div>
                  </div>

                  <div className="flex items-center gap-2">
                    <Button variant="outline" size="sm" onClick={() => handleExportResults(exam.id, exam.tenDeThi || '')}>
                      <Download className="h-3.5 w-3.5 mr-1" />
                      Kết quả
                    </Button>
                  </div>
                </div>

                {/* DeThi Content */}
                <div className="p-5">
                  {!hasSubmissions ? (
                    <div className="text-center py-8 text-gray-400 text-sm">
                      Đề thi này chưa có bài làm nào nộp
                    </div>
                  ) : (
                    <ExamResultsTable
                      results={resultsForExam}
                      isLoading={isLoading}
                      onViewDetail={(r) => setDetailBaiThiId(r.baiThiId)}
                      onRegrade={handleRegrade}
                    />
                  )}
                </div>
              </div>
            )
          })}
        </div>
      )}

      <ResultDetailDialog
        open={!!detailBaiThiId}
        baiThiId={detailBaiThiId}
        onClose={() => {
          setDetailBaiThiId(null)
          // Reload kết quả sau khi chấm xong để cập nhật điểm mới
          if (selectedKyThiId) {
            loadKyThiResults(selectedKyThiId)
          }
        }}
        isAdmin={true}
      />
    </div>
  )
}
