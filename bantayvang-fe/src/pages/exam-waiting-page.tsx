import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { kyThiApi } from '@/features/ky-thi/api'
import { examTakingApi } from '@/features/exam-taking/api'
import { useAppSelector } from '@/app/hooks'
import type { ExamCampaignDto } from '@/features/ky-thi/types'
import type { ExamSubmissionDto } from '@/features/exam-taking/types'
import { formatDate } from '@/lib/utils'
import { Clock, Play, RefreshCw, ClipboardList } from 'lucide-react'

export function ExamWaitingPage() {
  const navigate = useNavigate()
  const { user } = useAppSelector((state) => state.auth)
  const [examCampaigns, setKyThis] = useState<ExamCampaignDto[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [startingId, setStartingId] = useState<number | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [myResults, setMyResults] = useState<Map<string, ExamSubmissionDto>>(new Map())

  const loadData = async () => {
    setIsLoading(true)
    setError(null)
    try {
      const [kyThiRes, myResultsRes] = await Promise.all([
        kyThiApi.getAll(),
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

      const list: ExamCampaignDto[] = (kyThiRes.data.success && kyThiRes.data.data)
        ? kyThiRes.data.data : []
      setKyThis(list)
    } catch {
      setError('Không thể tải danh sách kỳ thi. Vui lòng thử lại.')
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => {
    loadData()
    const interval = setInterval(loadData, 30_000)
    return () => clearInterval(interval)
  }, []) // eslint-disable-line

  const handleStartExam = async (examPaperCode: string | null | undefined, examCampaignId: number) => {
    setStartingId(examCampaignId)
    setError(null)
    try {
      const res = await examTakingApi.start({ examPaperCode: examPaperCode || undefined, examCampaignId })
      if (res.data.success && res.data.data) {
        navigate(`/exam/${res.data.data.id}`)
      } else {
        setError(res.data.message || 'Không thể bắt đầu bài thi')
      }
    } catch (err: unknown) {
      const msg =
        (err as { response?: { data?: { message?: string } } })?.response?.data?.message ||
        'Có lỗi khi bắt đầu bài thi'
      setError(msg)
    } finally {
      setStartingId(null)
    }
  }

  const now = new Date()

  const hasTakenKyThi = (ky: ExamCampaignDto) => {
    const resultsArray = Array.from(myResults.values())
    const hasByKyThiId = resultsArray.some((r) => r.examCampaignId === ky.id)
    if (hasByKyThiId) return true

    if (ky.danhSachMaDeThi && ky.danhSachMaDeThi.length > 0) {
      return ky.danhSachMaDeThi.some((code) => myResults.has(code))
    }

    return ky.examPaperCode ? myResults.has(ky.examPaperCode) : false
  }



  const available = examCampaigns.filter((ky) => {
    const hasExams = ky.examPaperCode || (ky.soLuongDeThi && ky.soLuongDeThi > 0) || (ky.danhSachMaDeThi && ky.danhSachMaDeThi.length > 0)
    if (!hasExams) return false
    
    // Cho phép thi lại nếu còn hạn của kỳ thi
    const start = ky.thoiGianBatDau ? new Date(ky.thoiGianBatDau) : null
    const end = ky.thoiGianKetThuc ? new Date(ky.thoiGianKetThuc) : null
    if (start && start > now) return false
    if (end && end < now && !hasTakenKyThi(ky)) return false
    return true
  })

  const upcoming = examCampaigns.filter((ky) => {
    const hasExams = ky.examPaperCode || (ky.soLuongDeThi && ky.soLuongDeThi > 0) || (ky.danhSachMaDeThi && ky.danhSachMaDeThi.length > 0)
    if (!hasExams) return false
    if (hasTakenKyThi(ky)) return false
    const start = ky.thoiGianBatDau ? new Date(ky.thoiGianBatDau) : null
    const end = ky.thoiGianKetThuc ? new Date(ky.thoiGianKetThuc) : null
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
          onClick={loadData}
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
                {available.map((ky) => (
                  <div key={ky.id} className={`bg-white border rounded-xl p-4 flex flex-col sm:flex-row sm:items-center justify-between gap-4 shadow-sm ${
                    hasTakenKyThi(ky) ? 'border-amber-200' : 'border-green-200'
                  }`}>
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 flex-wrap">
                        <p className="font-semibold text-gray-900">{ky.campaignName}</p>
                        {hasTakenKyThi(ky) && (
                          <span className="inline-flex items-center text-[10px] font-medium bg-amber-50 text-amber-700 border border-amber-200 px-1.5 py-0.5 rounded">
                            {ky.thoiGianKetThuc && new Date(ky.thoiGianKetThuc) < now ? 'Kỳ thi đã kết thúc' : 'Đã thi (Cho phép thi lại)'}
                          </span>
                        )}
                      </div>
                      <p className="text-xs text-primary mt-0.5">{ky.campaignCode}</p>
                      <div className="flex flex-wrap gap-3 mt-1">
                        {ky.thoiGianBatDau && (
                          <span className="text-xs text-gray-400 flex items-center gap-1">
                            <Clock className="h-3 w-3" />
                            {formatDate(ky.thoiGianBatDau)}
                            {ky.thoiGianKetThuc && ` → ${formatDate(ky.thoiGianKetThuc)}`}
                          </span>
                        )}
                        {ky.description && (
                          <span className="text-xs text-gray-400 italic">{ky.description}</span>
                        )}
                      </div>
                    </div>
                    <button
                      onClick={() => handleStartExam(ky.examPaperCode, ky.id)}
                      disabled={startingId === ky.id || !(ky.examPaperCode || (ky.soLuongDeThi && ky.soLuongDeThi > 0)) || !!(ky.thoiGianKetThuc && new Date(ky.thoiGianKetThuc) < now)}
                      className={`shrink-0 text-white text-sm font-semibold px-4 py-2.5 rounded-lg transition-colors w-full sm:w-auto text-center ${
                        ky.thoiGianKetThuc && new Date(ky.thoiGianKetThuc) < now
                          ? 'bg-gray-300 text-gray-500 cursor-not-allowed border'
                          : hasTakenKyThi(ky)
                            ? 'bg-amber-600 hover:bg-amber-700 disabled:bg-amber-300'
                            : 'bg-green-600 hover:bg-green-700 disabled:bg-green-300'
                      }`}
                    >
                      {ky.thoiGianKetThuc && new Date(ky.thoiGianKetThuc) < now ? 'Kỳ thi đã kết thúc' : startingId === ky.id ? 'Đang mở...' : hasTakenKyThi(ky) ? 'Thi lại →' : 'Vào thi →'}
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
                {upcoming.map((ky) => (
                  <div key={ky.id} className="bg-blue-50 border border-blue-200 rounded-xl p-4 flex flex-col sm:flex-row sm:items-center justify-between gap-4">
                    <div>
                      <p className="font-semibold text-gray-900">{ky.campaignName}</p>
                      <p className="text-xs text-primary mt-0.5">{ky.campaignCode}</p>
                      <p className="text-xs text-blue-600 mt-1">
                        🕐 Bắt đầu: {formatDate(ky.thoiGianBatDau)}
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

          {/* Empty state */}
          {available.length === 0 && upcoming.length === 0 && !isLoading && (
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
