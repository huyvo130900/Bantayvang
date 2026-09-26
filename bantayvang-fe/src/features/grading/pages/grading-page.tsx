import { useEffect, useState, useMemo } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAppDispatch, useAppSelector } from '@/app/hooks'
import { fetchActiveExams } from '@/features/exams/slice'
import { gradingApi } from '../api'
import { ExamResultsTable } from '../components/exam-results-table'
import { ResultDetailDialog } from '../components/result-detail-dialog'
import { PendingEssayTable } from '../components/pending-essay-table'
import type { ExamResultDetailDto, PendingEssayDto } from '../types'
import { Button } from '@/components/ui/button'
import { Download, RefreshCw, Eye, EyeOff, Building2, CalendarDays, PenLine } from 'lucide-react'
import { departmentApi } from '@/features/departments/api'
import { examCampaignApi } from '@/features/ky-thi/api'
import type { ExamCampaignDto } from '@/features/ky-thi/types'
import { ROLES } from '@/lib/constants'

export function GradingPage({ preselectedExamId }: { preselectedExamId?: number }) {
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const { exams } = useAppSelector((state) => state.exams)
  const currentUser = useAppSelector((state) => state.auth.user)
  const isDeptManager = currentUser?.role === ROLES.DEPT_MANAGER || currentUser?.roleName === 'DeptManager'
  const myKhoa = currentUser?.deptManagerDeptName || currentUser?.department || null

  const [selectedKhoa, setSelectedKhoa] = useState<string | null>(isDeptManager && myKhoa ? myKhoa : null)
  const [kyThiList, setKyThiList] = useState<ExamCampaignDto[]>([])
  const [selectedExamCampaignId, setSelectedKyThiId] = useState<number | null>(null)
  
  const [results, setResults] = useState<ExamResultDetailDto[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [detailexamSubmissionId, setDetailexamSubmissionId] = useState<number | null>(null)

  const [togglingVisibility, setTogglingVisibility] = useState(false)
  


  const [successMsg, setSuccessMsg] = useState<string | null>(null)
  const [errorMsg, setErrorMsg] = useState<string | null>(null)

  // Pending essay state
  const [activeTab, setActiveTab] = useState<'results' | 'pending-essay'>('results')
  const [pendingEssayFilter, setPendingEssayFilter] = useState<'ungraded' | 'graded'>('ungraded')
  const [pendingEssays, setPendingEssays] = useState<PendingEssayDto[]>([])
  const [pendingLoading, setPendingLoading] = useState(false)

  async function loadPendingEssay(isGraded: boolean = pendingEssayFilter === 'graded') {
    setPendingLoading(true)
    try {
      const res = await gradingApi.getPendingEssay(isGraded)
      if (res.data.success && res.data.data) {
        setPendingEssays(res.data.data)
      }
    } catch { /* silent */ } finally {
      setPendingLoading(false)
    }
  }

  // Load ExamCampaign list
  async function loadKyThiList() {
    try {
      const response = await examCampaignApi.getAll()
      if (response.data.success && response.data.data) {
        setKyThiList(response.data.data)
      }
    } catch { /* silent */ }
  }

  useEffect(() => {
    dispatch(fetchActiveExams())
    loadKyThiList()
    loadPendingEssay()
  }, [dispatch])

  // Sync selectedExamCampaignId from preselectedExamId if provided
  useEffect(() => {
    if (preselectedExamId && exams.length > 0) {
      const exam = exams.find(e => e.id === preselectedExamId)
      if (exam?.examCampaignId) {
        setSelectedKyThiId(exam.examCampaignId)
      }
    }
  }, [preselectedExamId, exams])

  // Fetch results when selectedExamCampaignId changes
  useEffect(() => {
    if (selectedExamCampaignId) {
      loadKyThiResults(selectedExamCampaignId)
    } else {
      setResults([])
    }
  }, [selectedExamCampaignId])

  async function loadKyThiResults(examCampaignId: number) {
    setIsLoading(true)
    try {
      const response = await gradingApi.getByKyThi(examCampaignId)
      if (response.data.success && response.data.data) {
        setResults(response.data.data)
      }
    } catch { /* silent */ } finally {
      setIsLoading(false)
    }
  }

  // Departments list from ExamCampaign (1 kỳ thi giờ có thể gán cho 1-n khoa)
  const khoaList = Array.from(new Set(kyThiList.flatMap(k => k.departmentNames.length > 0 ? k.departmentNames : (k.organizedBy ? [k.organizedBy] : [])))).sort()
  const hasUnassigned = kyThiList.some(k => k.departmentNames.length === 0 && !k.organizedBy)

  // Filter ExamCampaign list by department
  const scopedKyThis = isDeptManager && myKhoa
    ? kyThiList.filter(k => k.departmentNames.includes(myKhoa) || k.organizedBy === myKhoa)
    : selectedKhoa === '__unassigned__'
      ? kyThiList.filter(k => k.departmentNames.length === 0 && !k.organizedBy)
      : selectedKhoa
        ? kyThiList.filter(k => k.departmentNames.includes(selectedKhoa) || k.organizedBy === selectedKhoa)
        : kyThiList

  // All exams associated with the selected ExamCampaign
  const examsInSelectedKyThi = useMemo(() => {
    if (!selectedExamCampaignId) return []
    return exams.filter(e => e.examCampaignId === selectedExamCampaignId)
  }, [selectedExamCampaignId, exams])

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
    return examsInSelectedKyThi.every(e => e.isResultPublished)
  }, [examsInSelectedKyThi])

  // Toggle publishing grades for the entire ExamCampaign
  async function handleToggleKyThiVisibility() {
    if (!selectedExamCampaignId || examsInSelectedKyThi.length === 0) return
    setTogglingVisibility(true)
    setErrorMsg(null)
    const nextState = !isAllPublished
    try {
      // Toggle visibility for each exam paper belonging to this ExamCampaign
      await Promise.all(
        examsInSelectedKyThi.map(async (exam) => {
          if (exam.isResultPublished !== nextState) {
            await departmentApi.toggleExamVisibility(exam.id, { isResultPublished: nextState })
          }
        })
      )
      // Reload results and sync exams
      await Promise.all([
        loadKyThiResults(selectedExamCampaignId),
        dispatch(fetchActiveExams())
      ])
      setSuccessMsg(nextState ? 'Đã bật công bố điểm cho toàn bộ kỳ thi' : 'Đã tắt công bố điểm cho toàn bộ kỳ thi')
      setTimeout(() => setSuccessMsg(null), 3000)
    } catch (err) {
      const msg = (err as any)?.response?.data?.message || 'Không thể công bố điểm'
      setErrorMsg(msg)
    } finally {
      setTogglingVisibility(false)
    }
  }

  async function handleRegrade(examSubmissionId: number) {
    if (!window.confirm('Chấm lại bài thi này?')) return
    try {
      await gradingApi.regrade(examSubmissionId)
      if (selectedExamCampaignId) {
        loadKyThiResults(selectedExamCampaignId)
      }
    } catch { /* silent */ }
  }


  async function handleExportResults(examId: number, examPaperName: string) {
    try {
      const response = await gradingApi.exportResults(examId)
      const url = window.URL.createObjectURL(new Blob([response.data]))
      const link = document.createElement('a')
      link.href = url
      link.download = `KetQua_${examPaperName}_${new Date().toLocaleDateString('vi-VN').replace(/\//g, '-')}.xlsx`
      link.click()
      window.URL.revokeObjectURL(url)
    } catch { /* silent */ }
  }


  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between mb-2">
        <h1 className="text-2xl font-bold text-gray-900">Kết quả & Chấm điểm</h1>
      </div>

      {/* Tabs */}
      <div className="flex items-center gap-2 border-b">
        <button
          onClick={() => setActiveTab('results')}
          className={`px-4 py-2.5 text-sm font-semibold border-b-2 transition-colors ${
            activeTab === 'results'
              ? 'border-blue-600 text-blue-700'
              : 'border-transparent text-gray-500 hover:text-gray-700'
          }`}
        >
          📊 Xem kết quả theo kỳ thi
        </button>
        <button
          onClick={() => { setActiveTab('pending-essay'); loadPendingEssay() }}
          className={`flex items-center gap-2 px-4 py-2.5 text-sm font-semibold border-b-2 transition-colors ${
            activeTab === 'pending-essay'
              ? 'border-orange-500 text-orange-700'
              : 'border-transparent text-gray-500 hover:text-gray-700'
          }`}
        >
          <PenLine className="h-4 w-4" />
          Tự luận chờ chấm
          {pendingEssays.length > 0 && (
            <span className="inline-flex items-center justify-center min-w-[20px] h-5 px-1.5 rounded-full text-xs font-bold bg-orange-500 text-white">
              {pendingEssays.length}
            </span>
          )}
        </button>
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
          value={selectedExamCampaignId ?? ''}
          onChange={(e) => setSelectedKyThiId(e.target.value ? Number(e.target.value) : null)}
        >
          <option value="">— Chọn kỳ thi —</option>
          {scopedKyThis.map((kt) => (
            <option key={kt.id} value={kt.id}>
              {kt.campaignCode} — {kt.campaignName}{kt.departmentNames.length > 0 ? ` [${kt.departmentNames.join(', ')}]` : kt.organizedBy ? ` [${kt.organizedBy}]` : ''}
            </option>
          ))}
        </select>

        {selectedExamCampaignId && examsInSelectedKyThi.length > 0 && (
          <div className="flex gap-2 ml-auto flex-wrap">
            <Button variant="outline" size="sm" onClick={() => loadKyThiResults(selectedExamCampaignId)}>
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
      {selectedExamCampaignId && examsInSelectedKyThi.length > 0 && (
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
      {activeTab === 'pending-essay' ? (
        <div className="space-y-4">
          <div className="flex items-center gap-4 bg-orange-50/50 p-1 rounded-lg w-fit border border-orange-100">
            <button
              onClick={() => { setPendingEssayFilter('ungraded'); loadPendingEssay(false) }}
              className={`px-4 py-1.5 text-sm font-medium rounded-md transition-colors ${
                pendingEssayFilter === 'ungraded'
                  ? 'bg-white text-orange-700 shadow-sm border border-orange-200'
                  : 'text-gray-500 hover:text-gray-700'
              }`}
            >
              Chưa chấm
            </button>
            <button
              onClick={() => { setPendingEssayFilter('graded'); loadPendingEssay(true) }}
              className={`px-4 py-1.5 text-sm font-medium rounded-md transition-colors ${
                pendingEssayFilter === 'graded'
                  ? 'bg-white text-orange-700 shadow-sm border border-orange-200'
                  : 'text-gray-500 hover:text-gray-700'
              }`}
            >
              Đã chấm
            </button>
          </div>

          <div className="flex items-center justify-between">
            <p className="text-sm text-orange-700 font-medium">
              {pendingEssays.length > 0
                ? `Có ${pendingEssays.length} bài thi tự luận ${pendingEssayFilter === 'ungraded' ? 'đang chờ chấm' : 'đã chấm'}`
                : `Không có bài thi nào ${pendingEssayFilter === 'ungraded' ? 'chờ chấm' : 'đã chấm'}`}
            </p>
            <div className="flex gap-2">
              {selectedExamCampaignId && pendingEssays.length > 0 && (
                <Button 
                  variant="default" 
                  size="sm" 
                  className="bg-blue-600 hover:bg-blue-700"
                  onClick={() => {
                    const url = isDeptManager ? '/dept-manager/grading/bulk' : '/admin/grading/bulk'
                    navigate(`${url}?examCampaignId=${selectedExamCampaignId}`)
                  }}
                >
                  <PenLine className="h-4 w-4 mr-2" />
                  Chấm hàng loạt
                </Button>
              )}
              <Button variant="outline" size="sm" onClick={() => loadPendingEssay()} disabled={pendingLoading}>
                <RefreshCw className={`h-4 w-4 mr-1 ${pendingLoading ? 'animate-spin' : ''}`} />
                Làm mới
              </Button>
            </div>
          </div>
          <PendingEssayTable
            items={pendingEssays}
            isLoading={pendingLoading}
            onViewDetail={(id) => setDetailexamSubmissionId(id)}
            isGraded={pendingEssayFilter === 'graded'}
          />
        </div>
      ) : !selectedExamCampaignId ? (
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
            const allGradedCount = resultsForExam.filter(r => r.questionsGraded === r.totalQuestions && (r.totalQuestions ?? 0) > 0).length
            const isFullyGraded = hasSubmissions && allGradedCount === resultsForExam.length

            return (
              <div key={exam.id} className="bg-white rounded-xl border shadow-sm overflow-hidden transition-all duration-300 hover:shadow-md">
                {/* ExamPaper Header */}
                <div className="bg-gray-50 border-b px-5 py-4 flex flex-wrap items-center justify-between gap-3">
                  <div className="space-y-1">
                    <h3 className="text-lg font-bold text-gray-900">
                      {exam.examPaperName}
                    </h3>
                    <div className="flex flex-wrap items-center gap-2 text-xs">
                      <span className="px-2 py-0.5 rounded bg-gray-200 text-gray-700 font-mono font-medium">
                        {exam.examPaperCode}
                      </span>
                      {exam.department && (
                        <span className="px-2 py-0.5 rounded bg-blue-50 text-blue-700 font-medium">
                          🏢 {exam.department}
                        </span>
                      )}
                      <span className={`px-2 py-0.5 rounded font-semibold ${
                        exam.isResultPublished ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'
                      }`}>
                        {exam.isResultPublished ? '👁 Đã công bố điểm' : '🔒 Chưa công bố điểm'}
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
                    <Button variant="outline" size="sm" onClick={() => handleExportResults(exam.id, exam.examPaperName || '')}>
                      <Download className="h-3.5 w-3.5 mr-1" />
                      Kết quả
                    </Button>
                  </div>
                </div>

                {/* ExamPaper Content */}
                <div className="p-5">
                  {!hasSubmissions ? (
                    <div className="text-center py-8 text-gray-400 text-sm">
                      Đề thi này chưa có bài làm nào nộp
                    </div>
                  ) : (
                    <ExamResultsTable
                      results={resultsForExam}
                      isLoading={isLoading}
                      onViewDetail={(r) => setDetailexamSubmissionId(r.examSubmissionId)}
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
        open={!!detailexamSubmissionId}
        examSubmissionId={detailexamSubmissionId}
        onClose={() => {
          setDetailexamSubmissionId(null)
          // Reload kết quả sau khi chấm xong để cập nhật điểm mới
          if (selectedExamCampaignId) {
            loadKyThiResults(selectedExamCampaignId)
          }
          // Cũng refresh lại danh sách tự luận chờ chấm
          loadPendingEssay()
        }}
        isAdmin={true}
      />
    </div>
  )
}
