import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { statisticsApi, type DashboardDto, type TopPerformerDto } from '@/features/statistics/api'
import { examCampaignApi } from '@/features/ky-thi/api'
import { examTakingApi } from '@/features/exam-taking/api'
import type { ExamCampaignDto } from '@/features/ky-thi/types'
import type { ExamSubmissionDto } from '@/features/exam-taking/types'
import {
  Users,
  FileQuestion,
  ClipboardList,
  Award,
  AlertTriangle,
  TrendingUp,
  Activity,
  CheckCircle,
  Clock,
  Play,
  Eye,
  Search,
  RotateCcw,
  RefreshCw,
  Lock,
} from 'lucide-react'
import { useAppSelector } from '@/app/hooks'
import { ROLES } from '@/lib/constants'
import { formatDate } from '@/lib/utils'

interface StatCardProps {
  icon: React.ElementType
  label: string
  value: number | string
  sub?: string
  color?: string
  bgColor?: string
}

function StatCard({ icon: Icon, label, value, sub, color = 'text-green-600', bgColor = 'bg-green-50' }: StatCardProps) {
  return (
    <div className="bg-white rounded-lg border border-gray-200 p-4 flex items-start gap-3 shadow-sm">
      <div className={`p-2 rounded-lg ${bgColor}`}>
        <Icon className={`h-5 w-5 ${color}`} />
      </div>
      <div>
        <p className="text-sm text-gray-500">{label}</p>
        <p className="text-2xl font-bold text-gray-900">{value}</p>
        {sub && <p className="text-xs text-gray-400 mt-0.5">{sub}</p>}
      </div>
    </div>
  )
}

// --- Admin Dashboard ---
function AdminDashboard() {
  const [dashboard, setDashboard] = useState<DashboardDto | null>(null)
  const [topPerformers, setTopPerformers] = useState<TopPerformerDto[]>([])
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    async function load() {
      try {
        const [dashRes, topRes] = await Promise.all([
          statisticsApi.getDashboard(),
          statisticsApi.getTopPerformers(5),
        ])
        if (dashRes.data.success && dashRes.data.data) setDashboard(dashRes.data.data)
        if (topRes.data.success && topRes.data.data) setTopPerformers(topRes.data.data)
      } catch {
        // silent
      } finally {
        setIsLoading(false)
      }
    }
    load()
  }, [])

  if (isLoading) {
    return (
      <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-4 animate-pulse">
        {Array.from({ length: 10 }).map((_, i) => (
          <div key={i} className="bg-gray-100 rounded-lg h-24" />
        ))}
      </div>
    )
  }

  if (!dashboard) {
    return <p className="text-gray-500">Không thể tải dữ liệu dashboard</p>
  }

  return (
    <div className="space-y-6">
      <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-4">
        <StatCard icon={Users} label="Người dùng" value={dashboard.totalUsers} sub={`${dashboard.activeUsers} hoạt động`} />
        <StatCard icon={FileQuestion} label="Câu hỏi" value={dashboard.totalQuestions} bgColor="bg-green-50" color="text-green-600" />
        <StatCard icon={ClipboardList} label="Đề thi" value={dashboard.totalExams} sub={`${dashboard.activeExams} đang mở`} bgColor="bg-blue-50" color="text-blue-600" />
        <StatCard icon={Award} label="Bài thi" value={dashboard.totalSubmissions} sub={`${dashboard.completedExams} hoàn thành`} bgColor="bg-purple-50" color="text-purple-600" />
        <StatCard icon={AlertTriangle} label="Cảnh báo gian lận" value={dashboard.totalCheatingWarnings} bgColor="bg-orange-50" color="text-orange-600" />
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="bg-white rounded-lg border border-gray-200 p-5 shadow-sm">
          <div className="flex items-center gap-2 mb-3">
            <TrendingUp className="h-4 w-4 text-primary" />
            <h3 className="font-semibold text-gray-800">Điểm trung bình</h3>
          </div>
          <p className="text-4xl font-bold text-primary">{dashboard.averageScore.toFixed(1)}</p>
          <p className="text-xs text-gray-400 mt-1">Toàn hệ thống</p>
        </div>
        <div className="bg-white rounded-lg border border-gray-200 p-5 shadow-sm">
          <div className="flex items-center gap-2 mb-3">
            <Activity className="h-4 w-4 text-blue-600" />
            <h3 className="font-semibold text-gray-800">Đang diễn ra</h3>
          </div>
          <p className="text-4xl font-bold text-blue-600">{dashboard.inProgressExams}</p>
          <p className="text-xs text-gray-400 mt-1">Bài thi đang làm</p>
        </div>
        <div className="bg-white rounded-lg border border-gray-200 p-5 shadow-sm">
          <div className="flex items-center gap-2 mb-3">
            <CheckCircle className="h-4 w-4 text-green-600" />
            <h3 className="font-semibold text-gray-800">Hoàn thành</h3>
          </div>
          <p className="text-4xl font-bold text-green-600">{dashboard.completedExams}</p>
          <p className="text-xs text-gray-400 mt-1">Bài thi đã nộp</p>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <div className="bg-white rounded-lg border border-gray-200 shadow-sm">
          <div className="p-4 border-b border-gray-100">
            <h3 className="font-semibold text-gray-800">🏆 Top thí sinh nổi bật</h3>
          </div>
          <div className="divide-y divide-gray-50">
            {topPerformers.length === 0 ? (
              <p className="p-4 text-gray-400 text-sm">Chưa có dữ liệu</p>
            ) : (
              topPerformers.map((p, i) => (
                <div key={p.userId} className="flex items-center gap-3 p-3">
                  <span className={`w-7 h-7 rounded-full flex items-center justify-center text-xs font-bold ${
                    i === 0 ? 'bg-yellow-100 text-yellow-700' : i === 1 ? 'bg-gray-100 text-gray-600' : i === 2 ? 'bg-orange-100 text-orange-700' : 'bg-gray-50 text-gray-500'
                  }`}>{i + 1}</span>
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium text-gray-800 truncate">{p.fullName || p.username}</p>
                    <p className="text-xs text-gray-400">{p.department || '—'} · {p.examsTaken} bài</p>
                  </div>
                  <div className="text-right">
                    <p className="text-sm font-bold text-primary">{p.averageScore.toFixed(1)}</p>
                    <p className="text-xs text-gray-400">TB</p>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>

        <div className="bg-white rounded-lg border border-gray-200 shadow-sm">
          <div className="p-4 border-b border-gray-100">
            <h3 className="font-semibold text-gray-800">📋 Hoạt động gần đây</h3>
          </div>
          <div className="divide-y divide-gray-50 max-h-72 overflow-y-auto">
            {dashboard.recentActivities.length === 0 ? (
              <p className="p-4 text-gray-400 text-sm">Chưa có hoạt động</p>
            ) : (
              dashboard.recentActivities.slice(0, 10).map((a, i) => (
                <div key={i} className="flex items-start gap-3 p-3">
                  <div className="w-1.5 h-1.5 mt-2 rounded-full bg-primary shrink-0" />
                  <div className="flex-1 min-w-0">
                    <p className="text-sm text-gray-700 truncate">{a.description}</p>
                    <p className="text-xs text-gray-400">
                      {a.username && <span className="font-medium">{a.username} · </span>}
                      {formatDate(a.timestamp)}
                    </p>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>
      </div>
    </div>
  )
}

// --- Student Dashboard ---
function StudentDashboard() {
  const navigate = useNavigate()
  const [examCampaigns, setExamCampaigns] = useState<ExamCampaignDto[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [myResults, setMyResults] = useState<Map<string, ExamSubmissionDto>>(new Map())
  const { user } = useAppSelector((state) => state.auth)

  const [searchQuery, setSearchQuery] = useState('')
  const [startDate, setStartDate] = useState('')
  const [endDate, setEndDate] = useState('')

  async function load() {
    try {
      const res = await examCampaignApi.getAll()
      const list = res.data.success && res.data.data ? res.data.data : []
      setExamCampaigns(list)

      const myRes = await examTakingApi.getMyResults()
      if (myRes.data.success && myRes.data.data) {
        const map = new Map<string, ExamSubmissionDto>()
        myRes.data.data.forEach((b) => {
          if (b.examPaperCode) map.set(b.examPaperCode, b)
        })
        setMyResults(map)
      }
    } catch {
      // silent
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect
    load()
  }, [])

  const now = new Date()

  const hasTakenExamCampaign = (examCampaign: ExamCampaignDto) => {
    const resultsArray = Array.from(myResults.values())
    const hasByKyThiId = resultsArray.some((r) => r.examCampaignId === examCampaign.id)
    if (hasByKyThiId) return true

    if (examCampaign.examPaperCodes && examCampaign.examPaperCodes.length > 0) {
      return examCampaign.examPaperCodes.some((code) => myResults.has(code))
    }

    return examCampaign.examPaperCode ? myResults.has(examCampaign.examPaperCode) : false
  }

  const getExamCampaignResult = (examCampaign: ExamCampaignDto) => {
    const resultsArray = Array.from(myResults.values())
    const byKyThiId = resultsArray.find((r) => r.examCampaignId === examCampaign.id)
    if (byKyThiId) return byKyThiId

    if (examCampaign.examPaperCodes && examCampaign.examPaperCodes.length > 0) {
      for (const code of examCampaign.examPaperCodes) {
        const res = myResults.get(code)
        if (res) return res
      }
    }

    return examCampaign.examPaperCode ? myResults.get(examCampaign.examPaperCode) : undefined
  }

  const clearFilters = () => {
    setSearchQuery('')
    setStartDate('')
    setEndDate('')
  }

  const filteredExamCampaigns = examCampaigns.filter((examCampaign) => {
    // Kỳ thi luyện tập tồn tại vĩnh viễn, không có thời hạn thật và không tính đạt/không đạt -
    // không thuộc về các mục "đã xong"/"bỏ lỡ" của trang này (đã có mục Luyện tập riêng ở
    // /exam-waiting, trang mặc định sau đăng nhập).
    if (examCampaign.isPracticeMode) return false

    if (searchQuery.trim()) {
      const term = searchQuery.toLowerCase().trim()
      const matchesName = examCampaign.campaignName?.toLowerCase().includes(term)
      const matchesCode = examCampaign.campaignCode?.toLowerCase().includes(term)
      if (!matchesName && !matchesCode) return false
    }

    if (startDate) {
      const start = new Date(startDate)
      start.setHours(0, 0, 0, 0)
      const examDate = examCampaign.endTime ? new Date(examCampaign.endTime) : (examCampaign.startTime ? new Date(examCampaign.startTime) : null)
      if (examDate && examDate < start) return false
    }

    if (endDate) {
      const end = new Date(endDate)
      end.setHours(23, 59, 59, 999)
      const examDate = examCampaign.startTime ? new Date(examCampaign.startTime) : (examCampaign.endTime ? new Date(examCampaign.endTime) : null)
      if (examDate && examDate > end) return false
    }

    return true
  })

  // Kỳ thi có thể thi: loại bỏ bài đã hết giờ. Cho phép hiển thị cả bài đã thi nếu còn hạn để thi lại.
  const available = filteredExamCampaigns.filter((examCampaign) => {
    const hasExams = examCampaign.examPaperCode || (examCampaign.totalExamPapers && examCampaign.totalExamPapers > 0) || (examCampaign.examPaperCodes && examCampaign.examPaperCodes.length > 0)
    if (!hasExams) return false
    const start = examCampaign.startTime ? new Date(examCampaign.startTime) : null
    const end = examCampaign.endTime ? new Date(examCampaign.endTime) : null
    if (start && start > now) return false
    if (end && end < now) return false
    return true
  })

  const upcoming = filteredExamCampaigns.filter((examCampaign) => {
    const hasExams = examCampaign.examPaperCode || (examCampaign.totalExamPapers && examCampaign.totalExamPapers > 0) || (examCampaign.examPaperCodes && examCampaign.examPaperCodes.length > 0)
    if (!hasExams) return false
    if (hasTakenExamCampaign(examCampaign)) return false
    const start = examCampaign.startTime ? new Date(examCampaign.startTime) : null
    const end = examCampaign.endTime ? new Date(examCampaign.endTime) : null
    if (end && end < now) return false
    return start != null && start > now
  })

  // Bài đã thi xong (có điểm từ server)
  const doneWithResults = filteredExamCampaigns.filter(
    (examCampaign) => hasTakenExamCampaign(examCampaign)
  )

  // Kỳ thi đã hết giờ mà chưa thi → coi như Không đạt, điểm 0
  const missedExams = filteredExamCampaigns.filter((examCampaign) => {
    const hasExams = examCampaign.examPaperCode || (examCampaign.totalExamPapers && examCampaign.totalExamPapers > 0) || (examCampaign.examPaperCodes && examCampaign.examPaperCodes.length > 0)
    if (!hasExams) return false
    if (hasTakenExamCampaign(examCampaign)) return false // đã thi rồi
    const end = examCampaign.endTime ? new Date(examCampaign.endTime) : null
    return end != null && end < now
  })

  // Tổng "đã xong" = đã thi + bỏ lỡ
  const totalDone = doneWithResults.length + missedExams.length

  return (
    <div className="space-y-6">
      <div className="bg-gradient-to-r from-primary/10 to-purple-50 rounded-xl p-6 border border-primary/20">
        <h2 className="text-xl font-bold text-gray-800 mb-1">
          Hello, {user?.fullName || user?.fullName || user?.username || user?.username}! 👋
        </h2>
        <p className="text-gray-600 text-sm">Good luck with your exams today.</p>
      </div>

      {/* Search and Filters */}
      <div className="bg-white rounded-xl border border-gray-200 p-4 shadow-sm flex flex-col md:flex-row md:items-end gap-4">
        <div className="flex-1 space-y-1.5">
          <label htmlFor="searchQuery" className="text-xs font-semibold text-gray-500 uppercase tracking-wider">
            Search exam
          </label>
          <div className="relative">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400" />
            <input
              id="searchQuery"
              type="text"
              placeholder="Enter name or exam code..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="w-full pl-9 pr-4 py-2 border border-gray-200 rounded-lg text-sm placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all"
            />
          </div>
        </div>

        <div className="w-full md:w-44 space-y-1.5">
          <label htmlFor="startDate" className="text-xs font-semibold text-gray-500 uppercase tracking-wider">
            From
          </label>
          <input
            id="startDate"
            type="date"
            value={startDate}
            onChange={(e) => setStartDate(e.target.value)}
            className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-gray-700"
          />
        </div>

        <div className="w-full md:w-44 space-y-1.5">
          <label htmlFor="endDate" className="text-xs font-semibold text-gray-500 uppercase tracking-wider">
            To
          </label>
          <input
            id="endDate"
            type="date"
            value={endDate}
            onChange={(e) => setEndDate(e.target.value)}
            className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-gray-700"
          />
        </div>

        {(searchQuery || startDate || endDate) && (
          <button
            onClick={clearFilters}
            className="flex items-center justify-center gap-1.5 px-4 py-2 text-sm font-medium text-gray-500 hover:text-gray-700 hover:bg-gray-50 border border-gray-200 rounded-lg transition-colors shrink-0 h-[38px] cursor-pointer w-full md:w-auto"
            title="Reset filters"
          >
            <RotateCcw className="h-4 w-4" />
            Reset
          </button>
        )}
      </div>

      {/* Quick stats */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <StatCard icon={Clock} label="Upcoming" value={upcoming.length} bgColor="bg-blue-50" color="text-blue-600" />
        <StatCard icon={Play} label="Available" value={available.length} bgColor="bg-green-50" color="text-green-600" />
        <StatCard icon={CheckCircle} label="Completed" value={totalDone} bgColor="bg-purple-50" color="text-purple-600" />
      </div>

      {/* Đề thi đang mở — chưa làm */}
      {available.length > 0 && (
        <div className="bg-white rounded-lg border border-green-200 shadow-sm">
          <div className="p-4 border-b border-green-100 bg-green-50 rounded-t-lg">
            <h3 className="font-semibold text-green-800 flex items-center gap-2">
              <Play className="h-4 w-4" /> Exams open now — Take it now!
            </h3>
          </div>
          <div className="divide-y">
            {available.map((examCampaign) => (
              <div key={examCampaign.id} className="flex flex-col sm:flex-row sm:items-center justify-between p-4 gap-4">
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 flex-wrap">
                    <p className="font-medium text-gray-800">{examCampaign.campaignName}</p>
                    {hasTakenExamCampaign(examCampaign) && (
                      <span className="inline-flex items-center text-[10px] font-medium bg-amber-50 text-amber-700 border border-amber-200 px-1.5 py-0.5 rounded">
                        Taken (Retake allowed)
                      </span>
                    )}
                  </div>
                  <p className="text-xs text-gray-500 mt-0.5">{examCampaign.campaignCode}</p>
                  {examCampaign.endTime && (
                    <span className="text-[10px] text-gray-500 bg-gray-50 px-2 py-0.5 rounded border border-gray-100 flex items-center gap-1 shrink-0">
                      End: {formatDate(examCampaign.endTime)}
                    </span>
                  )}
                </div>
                <button
                  onClick={() => navigate('/exam-waiting')}
                  className={`text-white text-sm font-medium px-4 py-2 rounded-lg transition-colors w-full sm:w-auto text-center shrink-0 ${
                    hasTakenExamCampaign(examCampaign)
                      ? 'bg-amber-600 hover:bg-amber-700'
                      : 'bg-green-600 hover:bg-green-700'
                  }`}
                >
                  {hasTakenExamCampaign(examCampaign) ? 'Thi lại →' : 'Vào thi →'}
                </button>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Bài đã thi + bị lỡ — đều hiện kết quả/điểm 0 */}
      {totalDone > 0 && (
        <div className="bg-white rounded-lg border border-gray-200 shadow-sm">
          <div className="p-4 border-b border-gray-100">
            <h3 className="font-semibold text-gray-800 flex items-center gap-2">
              <CheckCircle className="h-4 w-4 text-green-600" /> Bài đã hoàn thành
            </h3>
          </div>
          <div className="divide-y">
            {/* Bài đã nộp — có điểm thật */}
            {doneWithResults.map((examCampaign) => {
              const r = getExamCampaignResult(examCampaign)
              const score = r?.totalScore ?? 0
              const isPublished = r?.isResultPublished ?? false
              const end = examCampaign.endTime ? new Date(examCampaign.endTime) : null
              const isExpired = end !== null && end <= now
              const isResultPublished = isExpired || isPublished
              const examSubmissionId = r?.id
              return (
                <div key={examCampaign.id} className="flex flex-col sm:flex-row sm:items-center justify-between p-4 gap-4">
                  <div className="flex-1 min-w-0">
                    <p className="font-medium text-gray-800 truncate">{examCampaign.campaignName}</p>
                    <p className="text-xs text-gray-500">{examCampaign.campaignCode}</p>
                    {isResultPublished ? (
                       <p className="text-xs text-gray-500 mt-0.5">
                        Điểm: <b>{score.toFixed(1)}</b>
                        {r?.correctAnswers != null && ` · ${r.correctAnswers}/${r.totalQuestions} câu`}
                      </p>
                    ) : (
                      <p className="text-xs text-gray-400 mt-0.5 italic">Đã nộp bài — chờ công bố điểm</p>
                    )}
                  </div>
                  <div className="flex flex-wrap items-center gap-2 w-full sm:w-auto shrink-0 justify-start sm:justify-end">
                    {isResultPublished && examSubmissionId ? (
                      <>
                        {examCampaign.minPassQuestions !== undefined && examCampaign.minPassQuestions !== null && r?.correctAnswers != null ? (
                          r.correctAnswers >= examCampaign.minPassQuestions ? (
                            <span className="text-xs font-bold px-2.5 py-1 rounded-full bg-emerald-100 text-emerald-800 border border-emerald-200 shrink-0">
                              ✓ Đạt
                            </span>
                          ) : (
                            <span className="text-xs font-bold px-2.5 py-1 rounded-full bg-rose-100 text-rose-800 border border-rose-200 shrink-0">
                              ✗ Không đạt
                            </span>
                          )
                        ) : (
                          <span className="text-xs font-bold px-2.5 py-1 rounded-full bg-green-100 text-green-700 shrink-0">
                            ✓ Đã hoàn thành
                          </span>
                        )}
                        <button
                          onClick={() => navigate(`/exam-result/${examSubmissionId}`)}
                          className="inline-flex items-center justify-center gap-1.5 text-xs font-medium px-3 py-1.5 rounded-lg bg-green-50 text-green-700 hover:bg-green-100 transition-colors border border-green-200 w-full sm:w-auto"
                        >
                          <Eye className="h-3.5 w-3.5" />
                          Xem kết quả
                        </button>
                        {!isExpired ? (
                          <button
                            onClick={() => navigate('/exam-waiting')}
                            className="inline-flex items-center justify-center gap-1.5 text-xs font-medium px-3 py-1.5 rounded-lg bg-amber-500 text-white hover:bg-amber-600 transition-colors w-full sm:w-auto"
                          >
                            <RefreshCw className="h-3.5 w-3.5" />
                            Thi lại
                          </button>
                        ) : (
                          <button
                            disabled
                            className="inline-flex items-center justify-center gap-1.5 text-xs font-medium px-3 py-1.5 rounded-lg bg-gray-100 text-gray-500 cursor-not-allowed border border-gray-200 w-full sm:w-auto"
                          >
                            <Lock className="h-3.5 w-3.5" />
                            Kỳ thi đã kết thúc
                          </button>
                        )}
                      </>
                    ) : (
                      <span className="text-xs font-medium px-3 py-1.5 rounded-full bg-gray-100 text-gray-500 w-full sm:w-auto text-center border">
                        ⏳ Chờ kết quả
                      </span>
                    )}
                  </div>
                </div>
              )
            })}
            {/* Bài bị lỡ (hết giờ không vào thi) — điểm 0 */}
            {missedExams.map((examCampaign) => (
              <div key={examCampaign.id} className="flex flex-col sm:flex-row sm:items-center justify-between p-4 gap-4">
                <div>
                  <p className="font-medium text-gray-800">{examCampaign.campaignName}</p>
                  <p className="text-xs text-gray-500 mt-0.5">{examCampaign.campaignCode}</p>
                  <p className="text-xs text-gray-500 mt-0.5">Điểm: <b>0.0</b> · Không tham gia</p>
                </div>
                <span className="text-xs font-bold px-3 py-1 rounded-full bg-gray-100 text-gray-500 w-full sm:w-auto text-center shrink-0 border">
                  Không tham gia
                </span>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Sắp diễn ra */}
      {upcoming.length > 0 && (
        <div className="bg-white rounded-lg border border-gray-200 shadow-sm">
          <div className="p-4 border-b border-gray-100">
            <h3 className="font-semibold text-gray-800 flex items-center gap-2">
              <Clock className="h-4 w-4 text-blue-600" /> Đề thi sắp diễn ra
            </h3>
          </div>
          <div className="divide-y">
            {upcoming.map((examCampaign) => (
              <div key={examCampaign.id} className="flex flex-col sm:flex-row sm:items-center justify-between p-4 gap-4">
                <div>
                  <p className="font-medium text-gray-800">{examCampaign.campaignName}</p>
                  <p className="text-xs text-gray-500 mt-0.5">{examCampaign.campaignCode}</p>
                  <p className="text-xs text-blue-600 mt-0.5">
                    Bắt đầu: {examCampaign.startTime ? formatDate(examCampaign.startTime) : '—'}
                  </p>
                </div>
                <span className="text-xs bg-blue-50 text-blue-700 px-2.5 py-1 rounded-full font-medium w-full sm:w-auto text-center shrink-0">Chờ thi</span>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* No search results fallback */}
      {!isLoading && examCampaigns.length > 0 && filteredExamCampaigns.length === 0 && (
        <div className="bg-white rounded-lg border border-gray-200 shadow-sm text-center py-12 text-gray-400">
          <Search className="h-12 w-12 mx-auto mb-3 opacity-30 text-gray-400" />
          <p className="text-gray-500 font-medium">Không tìm thấy kỳ thi nào khớp với bộ lọc</p>
          <button
            onClick={clearFilters}
            className="mt-3 text-primary text-sm font-semibold hover:underline cursor-pointer"
          >
            Xóa bộ lọc
          </button>
        </div>
      )}

      {isLoading && <p className="text-center text-gray-400 py-8">Đang tải...</p>}
      {!isLoading && examCampaigns.length === 0 && (
        <div className="text-center py-12 text-gray-400">
          <ClipboardList className="h-12 w-12 mx-auto mb-3 opacity-30" />
          <p>Chưa có kỳ thi nào</p>
        </div>
      )}
    </div>
  )
}

// --- Main DashboardPage ---
export function DashboardPage() {
  const { user } = useAppSelector((state) => state.auth)
  const role = user?.role || user?.roleName
  // BUG FIX: only matched ROLES.STUDENT, so a ThiSinhNgoai (external candidate) account landing
  // here via the "Trang chủ" link in their own StudentLayout rendered <AdminDashboard/> instead -
  // which calls AdminOnly statistics endpoints. Backend policy already blocks the actual data
  // (confirmed: this was never an actual leak), but the page itself misidentified this role.
  const isStudent = role === ROLES.STUDENT || role === ROLES.THI_SINH_NGOAI

  return (
    <div>
      <h1 className="text-2xl font-bold text-gray-900 mb-6">
        {isStudent ? 'Trang chủ' : 'Tổng quan hệ thống'}
      </h1>
      {isStudent ? <StudentDashboard /> : <AdminDashboard />}
    </div>
  )
}
