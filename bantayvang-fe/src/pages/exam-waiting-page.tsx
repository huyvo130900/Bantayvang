import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { examCampaignApi } from '@/features/ky-thi/api'
import { examTakingApi } from '@/features/exam-taking/api'
import { useAppSelector } from '@/app/hooks'
import type { ExamCampaignDto } from '@/features/ky-thi/types'
import type { ExamSubmissionDto } from '@/features/exam-taking/types'
import { formatDate } from '@/lib/utils'
import { Clock, Play, RefreshCw, ClipboardList } from 'lucide-react'

export function ExamWaitingPage() {
  const navigate = useNavigate()
  const { user } = useAppSelector((state) => state.auth)
  const [examCampaigns, setExamCampaigns] = useState<ExamCampaignDto[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [startingId, setStartingId] = useState<number | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [myResults, setMyResults] = useState<Map<string, ExamSubmissionDto>>(new Map())

  async function loadData(isRefresh = false) {
    if (isRefresh) setIsLoading(true)
    setError(null)
    try {
      const [examCampaignRes, myResultsRes] = await Promise.all([
        examCampaignApi.getAll(),
        examTakingApi.getMyResults(),
      ])

      if (myResultsRes.data.success && myResultsRes.data.data) {
        const map = new Map<string, ExamSubmissionDto>()
        // Lấy bài thi gần nhất cho mỗi examPaperCode (sort theo submitTime desc)
        const sorted = [...myResultsRes.data.data].sort((a, b) => {
          const ta = a.submitTime ? new Date(a.submitTime).getTime() : 0
          const tb = b.submitTime ? new Date(b.submitTime).getTime() : 0
          return tb - ta
        })
        sorted.forEach((b) => {
          if (b.examPaperCode && !map.has(b.examPaperCode)) map.set(b.examPaperCode, b)
        })
        setMyResults(map)
      }

      const list: ExamCampaignDto[] = (examCampaignRes.data.success && examCampaignRes.data.data)
        ? examCampaignRes.data.data : []
      setExamCampaigns(list)
    } catch {
      setError('Không thể tải danh sách kỳ thi. Vui lòng thử lại.')
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect
    loadData(false)
    const interval = setInterval(() => loadData(false), 30_000)
    return () => clearInterval(interval)
  }, [])  

  async function handleStartExam(examPaperCode: string | null | undefined, examCampaignId: number) {
    setStartingId(examCampaignId)
    setError(null)
    try {
      const res = await examTakingApi.start({ examPaperCode: examPaperCode || undefined, examCampaignId })
      if (res.data.success && res.data.data) {
        navigate(`/exam/${res.data.data.id}`)
      } else {
        setError(res.data.message || 'Không thể bắt đầu bài thi')
      }
    } catch (err: any) {
      const msg =
        (err as { response?: { data?: { message?: string } } })?.response?.data?.message ||
        'Có lỗi khi bắt đầu bài thi'
      setError(msg)
    } finally {
      setStartingId(null)
    }
  }

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



  const hasExamPapers = (examCampaign: ExamCampaignDto) =>
    !!(examCampaign.examPaperCode || (examCampaign.totalExamPapers && examCampaign.totalExamPapers > 0) || (examCampaign.examPaperCodes && examCampaign.examPaperCodes.length > 0))

  // Kỳ thi luyện tập tồn tại vĩnh viễn - tách hẳn khỏi danh sách kỳ thi thật (có/sắp có hạn) để
  // đúng cảm giác "luôn ở đó", không lẫn vào giữa các kỳ thi có ngày giờ cụ thể.
  const practiceExams = examCampaigns.filter((k) => k.isPracticeMode && hasExamPapers(k))

  const available = examCampaigns.filter((examCampaign) => {
    if (examCampaign.isPracticeMode) return false
    if (!hasExamPapers(examCampaign)) return false

    // Cho phép thi lại nếu còn hạn của kỳ thi
    const start = examCampaign.startTime ? new Date(examCampaign.startTime) : null
    const end = examCampaign.endTime ? new Date(examCampaign.endTime) : null
    if (start && start > now) return false
    if (end && end < now && !hasTakenExamCampaign(examCampaign)) return false
    return true
  })

  const upcoming = examCampaigns.filter((examCampaign) => {
    if (examCampaign.isPracticeMode) return false
    if (!hasExamPapers(examCampaign)) return false
    if (hasTakenExamCampaign(examCampaign)) return false
    const start = examCampaign.startTime ? new Date(examCampaign.startTime) : null
    const end = examCampaign.endTime ? new Date(examCampaign.endTime) : null
    if (end && end < now) return false
    return start != null && start > now
  })

  return (
    <div className="max-w-2xl mx-auto py-8 px-4">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Phòng chờ thi</h1>
          <p className="text-sm text-gray-500 mt-0.5">
            Xin chào, <span className="font-medium">{user?.fullName || user?.fullName || user?.username || user?.username}</span>
          </p>
        </div>
        <button
          onClick={() => loadData(true)}
          disabled={isLoading}
          className="flex items-center justify-center gap-1.5 text-sm text-gray-500 hover:text-gray-900 border border-gray-200 rounded-lg px-3 py-1.5 hover:bg-gray-50 disabled:opacity-50 transition-colors w-full sm:w-auto"
        >
          <RefreshCw className={`h-4 w-4 ${isLoading ? 'animate-spin' : ''}`} />
          Làm mới
        </button>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 rounded-lg p-3 text-sm mb-4">
          ⚠️ {error}
        </div>
      )}

      {isLoading && examCampaigns.length === 0 ? (
        <div className="space-y-3">
          {[1, 2].map((i) => (
            <div key={i} className="bg-gray-100 rounded-xl h-24 animate-pulse" />
          ))}
        </div>
      ) : (
        <div className="space-y-6">
          {/* Có thể thi ngay */}
          {available.length > 0 && (
            <section>
              <h2 className="text-sm font-semibold text-green-700 uppercase tracking-wide mb-2 flex items-center gap-1.5">
                <Play className="h-3.5 w-3.5 fill-green-700" /> Có thể vào thi ngay
              </h2>
              <div className="space-y-2">
                {available.map((examCampaign) => (
                  <div key={examCampaign.id} className={`bg-white border rounded-xl p-4 flex flex-col sm:flex-row sm:items-center justify-between gap-4 shadow-sm ${
                    hasTakenExamCampaign(examCampaign) ? 'border-amber-200' : 'border-green-200'
                  }`}>
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 flex-wrap">
                        <p className="font-semibold text-gray-900">{examCampaign.campaignName}</p>
                        {hasTakenExamCampaign(examCampaign) && (
                          <span className="inline-flex items-center text-[10px] font-medium bg-amber-50 text-amber-700 border border-amber-200 px-1.5 py-0.5 rounded">
                            <div className="flex items-center gap-2">
                              {examCampaign.endTime && new Date(examCampaign.endTime) < now ? 'Exam ended' : 'Attempted (Retake allowed)'}
                            </div>
                          </span>
                        )}
                      </div>
                      <p className="text-xs text-primary mt-0.5">{examCampaign.campaignCode}</p>
                      <div className="flex items-center gap-1.5 text-sm text-gray-500 bg-gray-50 px-3 py-1.5 rounded-md border border-gray-100">
                        <Clock className="w-4 h-4 text-gray-400" />
                        {examCampaign.startTime && (
                          <span>
                            {formatDate(examCampaign.startTime)}
                            {examCampaign.endTime && ` → ${formatDate(examCampaign.endTime)}`}
                          </span>
                        )}
                        {examCampaign.description && (
                          <span className="text-xs text-gray-400 italic">{examCampaign.description}</span>
                        )}
                      </div>
                    </div>
                    <button
                      onClick={() => handleStartExam(examCampaign.examPaperCode, examCampaign.id)}
                      disabled={startingId === examCampaign.id || !(examCampaign.examPaperCode || (examCampaign.totalExamPapers && examCampaign.totalExamPapers > 0)) || !!(examCampaign.endTime && new Date(examCampaign.endTime) < now)}
                      className={`whitespace-nowrap px-6 shadow-sm text-white text-sm font-semibold py-2.5 rounded-lg transition-colors w-full sm:w-auto text-center ${
                        examCampaign.endTime && new Date(examCampaign.endTime) < now
                          ? 'bg-gray-300 text-gray-500 cursor-not-allowed border'
                          : hasTakenExamCampaign(examCampaign)
                            ? 'bg-amber-600 hover:bg-amber-700 disabled:bg-amber-300'
                            : 'bg-green-600 hover:bg-green-700 disabled:bg-green-300'
                      }`}
                    >
                      {examCampaign.endTime && new Date(examCampaign.endTime) < now ? 'Kỳ thi đã kết thúc' : startingId === examCampaign.id ? 'Đang mở...' : hasTakenExamCampaign(examCampaign) ? 'Thi lại →' : 'Vào thi →'}
                    </button>
                  </div>
                ))}
              </div>
            </section>
          )}

          {/* Sắp diễn ra */}
          {upcoming.length > 0 && (
            <section>
              <h2 className="text-sm font-semibold text-blue-600 uppercase tracking-wide mb-2 flex items-center gap-1.5">
                <Clock className="h-3.5 w-3.5" /> Sắp diễn ra
              </h2>
              <div className="space-y-2">
                {upcoming.map((examCampaign) => (
                  <div key={examCampaign.id} className="bg-blue-50 border border-blue-200 rounded-xl p-4 flex flex-col sm:flex-row sm:items-center justify-between gap-4">
                    <div>
                      <p className="font-semibold text-gray-900">{examCampaign.campaignName}</p>
                      <p className="text-xs text-primary mt-0.5">{examCampaign.campaignCode}</p>
                      <p className="text-xs text-blue-600 mt-1">
                        🕐 Bắt đầu: {formatDate(examCampaign.startTime)}
                      </p>
                    </div>
                    <span className="text-xs bg-blue-100 text-blue-700 px-3 py-1 rounded-full font-medium w-full sm:w-auto text-center shrink-0">
                      Chờ thi
                    </span>
                  </div>
                ))}
              </div>
            </section>
          )}

          {/* Luyện tập - tồn tại vĩnh viễn, không giới hạn thời gian/số lần */}
          {practiceExams.length > 0 && (
            <section>
              <h2 className="text-sm font-semibold text-purple-700 uppercase tracking-wide mb-2 flex items-center gap-1.5">
                <ClipboardList className="h-3.5 w-3.5" /> Luyện tập
              </h2>
              <div className="space-y-2">
                {practiceExams.map((examCampaign) => (
                  <div key={examCampaign.id} className="bg-purple-50 border border-purple-200 rounded-xl p-4 flex flex-col sm:flex-row sm:items-center justify-between gap-4">
                    <div className="flex-1 min-w-0">
                      <p className="font-semibold text-gray-900">{examCampaign.campaignName}</p>
                      <p className="text-xs text-primary mt-0.5">{examCampaign.campaignCode}</p>
                      {examCampaign.description && (
                        <p className="text-xs text-gray-400 italic mt-1">{examCampaign.description}</p>
                      )}
                    </div>
                    <button
                      onClick={() => handleStartExam(examCampaign.examPaperCode, examCampaign.id)}
                      disabled={startingId === examCampaign.id}
                      className="whitespace-nowrap px-6 shadow-sm text-white text-sm font-semibold py-2.5 rounded-lg transition-colors w-full sm:w-auto text-center bg-purple-600 hover:bg-purple-700 disabled:bg-purple-300"
                    >
                      {startingId === examCampaign.id ? 'Đang mở...' : hasTakenExamCampaign(examCampaign) ? 'Luyện tập lại →' : 'Vào luyện tập →'}
                    </button>
                  </div>
                ))}
              </div>
            </section>
          )}

          {/* Empty state */}
          {available.length === 0 && upcoming.length === 0 && practiceExams.length === 0 && !isLoading && (
            <div className="text-center py-16">
              <ClipboardList className="h-16 w-16 mx-auto text-gray-200 mb-4" />
              <p className="text-gray-600 font-medium text-lg">Không có kỳ thi nào</p>
              <p className="text-sm text-gray-400 mt-2">
                Trang sẽ tự động cập nhật mỗi 30 giây.
              </p>
            </div>
          )}
        </div>
      )}
    </div>
  )
}
