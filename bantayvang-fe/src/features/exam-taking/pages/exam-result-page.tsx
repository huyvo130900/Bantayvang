import { useEffect, useState } from 'react'
import { useParams, useNavigate, useLocation } from 'react-router-dom'
import { gradingApi } from '@/features/grading/api'
import type { ExamResultDetailDto } from '@/features/grading/types'
import { Button } from '@/components/ui/button'
import { CheckCircle2, XCircle, Trophy, AlertTriangle, Clock, ChevronDown, ChevronUp, EyeOff, ArrowLeft } from 'lucide-react'
import { formatDate } from '@/lib/utils'

export function ExamResultPage() {
  const { examSubmissionId } = useParams<{ examSubmissionId: string }>()
  const navigate = useNavigate()
  const location = useLocation()
  const [result, setResult] = useState<ExamResultDetailDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [showAnswers, setShowAnswers] = useState(false)

  // Forced termination reason passed from exam-taking-page
  const forcedReason = (location.state as Record<string, unknown> | null)?.forcedReason as string | undefined

  useEffect(() => {
    if (!examSubmissionId) return
    loadResult()
  }, [examSubmissionId]) // eslint-disable-line react-hooks/exhaustive-deps

  async function loadResult() {
    try {
      const response = await gradingApi.getResultDetail(Number(examSubmissionId))
      if (response.data.success && response.data.data) {
        setResult(response.data.data)
      }
    } catch {
      // silent
    } finally {
      setLoading(false)
    }
  }

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50">
        <div className="text-center">
          <div className="animate-spin h-10 w-10 border-4 border-primary border-t-transparent rounded-full mx-auto mb-4" />
          <p className="text-gray-600">Đang tải kết quả...</p>
        </div>
      </div>
    )
  }

  if (!result) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50">
        <div className="text-center">
          <XCircle className="h-16 w-16 text-red-300 mx-auto mb-4" />
          <p className="text-red-500 font-medium">Không thể tải kết quả</p>
          <Button className="mt-4" variant="outline" onClick={() => navigate(-1)}>
            Quay lại
          </Button>
        </div>
      </div>
    )
  }

  const percent = result.totalQuestions
    ? Math.round(((result.correctAnswers ?? 0) / result.totalQuestions) * 100)
    : 0

  const isTimeout = forcedReason === 'Hết giờ làm bài'
  const isCheating = (!!forcedReason && forcedReason !== 'Hết giờ làm bài') || result.status === 'BiBHuyGianLan'
  const isResultPublished = result.isResultPublished ?? false
  
  return (
    <div className="min-h-screen bg-gradient-to-b from-gray-50 to-gray-100 py-4 sm:py-10 px-4">
      <div className="max-w-xl mx-auto space-y-4 sm:space-y-5">

        {/* Timeout banner */}
        {isTimeout && (
          <div className="bg-amber-500 text-white rounded-2xl p-4 flex items-center gap-3">
            <Clock className="h-6 w-6 shrink-0" />
            <div>
              <p className="font-semibold text-sm sm:text-base">Hết thời gian làm bài</p>
              <p className="text-xs sm:text-sm text-amber-100">Bài thi đã được hệ thống tự động nộp.</p>
            </div>
          </div>
        )}

        {/* Forced termination banner */}
        {isCheating && (
          <div className="bg-red-600 text-white rounded-2xl p-4 flex items-center gap-3">
            <AlertTriangle className="h-6 w-6 shrink-0" />
            <div>
              <p className="font-semibold text-sm sm:text-base">Bài thi bị kết thúc cưỡng bức</p>
              <p className="text-xs sm:text-sm text-red-200">{forcedReason || 'Vi phạm quy định gian lận'}</p>
            </div>
          </div>
        )}

        {/* Result card */}
        <div className="bg-white rounded-2xl shadow-lg overflow-hidden">
          {/* Header band */}
          <div className={`p-4 sm:p-6 text-center ${isCheating ? 'bg-red-600' : !isResultPublished ? 'bg-gray-500' : 'bg-primary'}`}>
            {isCheating ? (
              <XCircle className="h-12 w-12 sm:h-16 sm:w-16 text-white mx-auto mb-2" />
            ) : !isResultPublished ? (
              <EyeOff className="h-12 w-12 sm:h-16 sm:w-16 text-white mx-auto mb-2" />
            ) : (
              <Trophy className="h-12 w-12 sm:h-16 sm:w-16 text-white mx-auto mb-2" />
            )}
            <h1 className="text-xl sm:text-2xl font-bold text-white">
              {isCheating ? 'Bài thi bị hủy' : !isResultPublished ? 'Đã nộp bài' : 'Kết quả bài thi'}
            </h1>
            <p className="text-white/80 text-xs sm:text-sm mt-1">{result.examPaperName || 'Bài thi'}</p>
          </div>

          {/* Score — chỉ hiển thị nếu isResultPublished = true */}
          <div className="p-4 sm:p-6">
            {!isResultPublished ? (
              <div className="flex flex-col items-center gap-3 py-6 text-gray-400">
                <EyeOff className="h-12 w-12 opacity-40" />
                <p className="font-medium">Điểm chưa được công bố</p>
                <p className="text-sm text-center">Quản lý khoa chưa bật hiển thị kết quả cho đề thi này.</p>
              </div>
            ) : (
              <>
                {/* Score display */}
                <div className="flex items-center justify-center mb-6">
                  <div className="relative h-36 w-36">
                    <svg className="h-36 w-36 -rotate-90" viewBox="0 0 120 120">
                      <circle cx="60" cy="60" r="50" fill="none" stroke="#e5e7eb" strokeWidth="12" />
                      <circle
                        cx="60" cy="60" r="50" fill="none"
                        stroke={forcedReason ? '#ef4444' : '#3b82f6'}
                        strokeWidth="12"
                        strokeDasharray={`${2 * Math.PI * 50}`}
                        strokeDashoffset={`${2 * Math.PI * 50 * (1 - percent / 100)}`}
                        strokeLinecap="round"
                        className="transition-all duration-1000"
                      />
                    </svg>
                    <div className="absolute inset-0 flex flex-col items-center justify-center">
                      <span className="text-3xl font-bold text-gray-900">
                        {result.totalScore != null ? result.totalScore.toFixed(1) : '0.0'}
                      </span>
                      <span className="text-xs text-gray-500 font-medium">Điểm</span>
                      <span className="text-[11px] text-gray-400 mt-0.5">
                        {result.correctAnswers ?? 0}/{result.totalQuestions ?? 0} câu
                      </span>
                    </div>
                  </div>
                </div>

                {result.minPassQuestions !== undefined && result.minPassQuestions !== null && (
                  <div className={`mb-5 p-4 rounded-xl text-center border font-semibold flex flex-col items-center justify-center gap-1.5 transition-all ${
                    result.pass
                      ? 'bg-emerald-50 text-emerald-800 border-emerald-200'
                      : 'bg-rose-50 text-rose-800 border-rose-200'
                  }`}>
                    <div className="flex items-center gap-2 text-lg">
                      {result.pass ? (
                        <>
                          <CheckCircle2 className="h-5 w-5 text-emerald-600" />
                          <span>Chúc mừng! Bạn đã ĐẠT kỳ thi</span>
                        </>
                      ) : (
                        <>
                          <XCircle className="h-5 w-5 text-rose-600" />
                          <span>Rất tiếc! Bạn KHÔNG ĐẠT kỳ thi</span>
                        </>
                      )}
                    </div>
                    <p className="text-xs font-normal opacity-85">
                      Yêu cầu tối thiểu để đạt: {result.minPassQuestions} câu đúng
                      (Kết quả của bạn: {result.correctAnswers} câu đúng)
                    </p>
                  </div>
                )}

                <div className="grid grid-cols-3 gap-3 text-center">
                  <div className="bg-green-50 rounded-xl p-3">
                    <p className="text-xl font-bold text-green-600">{result.correctAnswers ?? 0}</p>
                    <p className="text-xs text-green-700">Câu đúng</p>
                  </div>
                  <div className="bg-red-50 rounded-xl p-3">
                    <p className="text-xl font-bold text-red-500">
                      {(result.totalQuestions ?? 0) - (result.correctAnswers ?? 0)}
                    </p>
                    <p className="text-xs text-red-600">Câu sai</p>
                  </div>
                  <div className="bg-gray-50 rounded-xl p-3">
                    <p className="text-xl font-bold text-gray-700">{result.totalQuestions ?? 0}</p>
                    <p className="text-xs text-gray-500">Tổng câu</p>
                  </div>
                </div>

                <div className="mt-4 space-y-2 text-sm text-gray-500">
                  {(result.durationSeconds != null || result.durationMinutes != null) && (
                    <div className="flex items-center gap-2">
                      <Clock className="h-4 w-4 shrink-0" />
                      <span>Thời gian làm bài: <strong className="text-gray-700">
                        {result.durationSeconds != null
                          ? result.durationSeconds >= 60
                            ? `${Math.floor(result.durationSeconds / 60)} phút ${result.durationSeconds % 60} giây`
                            : `${result.durationSeconds} giây`
                          : `${result.durationMinutes} phút`}
                      </strong></span>
                    </div>
                  )}
                  {((result.warningCount ?? 0) > 0 || (result.cheatingCount ?? 0) > 0) && (
                    <div className="flex items-center gap-2 text-orange-600">
                      <AlertTriangle className="h-4 w-4 shrink-0" />
                      <span>
                        Cảnh báo vi phạm: <strong>{result.warningCount ?? 0}</strong> lần
                        {(result.cheatingCount ?? 0) > (result.warningCount ?? 0) && (
                          <> (Tổng tích lũy: <strong>{result.cheatingCount}</strong> lần)</>
                        )}
                      </span>
                    </div>
                  )}
                  {result.submitTime && (
                    <p>Nộp lúc: <strong className="text-gray-700">{formatDate(result.submitTime)}</strong></p>
                  )}
                </div>
              </>
            )}
          </div>
        </div>

        {/* Answer review — only when isResultPublished */}
        {isResultPublished && result.answers && result.answers.length > 0 && (
          <div className="bg-white rounded-2xl shadow-lg overflow-hidden">
            <button
              onClick={() => setShowAnswers(!showAnswers)}
              className="w-full flex items-center justify-between p-4 hover:bg-gray-50 transition-colors"
            >
              <span className="font-semibold text-gray-800">Xem lại đáp án ({result.answers.length} câu)</span>
              {showAnswers ? <ChevronUp className="h-5 w-5 text-gray-400" /> : <ChevronDown className="h-5 w-5 text-gray-400" />}
            </button>

            {showAnswers && (
              <div className="divide-y border-t">
                {result.answers.map((a, idx) => (
                  <div key={idx} className="p-4">
                    <div className="flex items-start gap-2.5">
                      {a.isCorrect
                        ? <CheckCircle2 className="h-5 w-5 text-green-500 shrink-0 mt-0.5" />
                        : <XCircle className="h-5 w-5 text-red-400 shrink-0 mt-0.5" />
                      }
                      <div className="flex-1 min-w-0">
                        <p className="text-sm font-medium text-gray-800">
                          Câu {idx + 1}: {a.questionContent}
                        </p>
                        <p className={`text-xs mt-1.5 ${a.isCorrect ? 'text-green-600' : 'text-red-500'}`}>
                          Câu trả lời của bạn: {a.answerContent || a.essayAnswer || <em>Chưa trả lời</em>}
                        </p>
                        {!a.isCorrect && a.correctAnswerContent && (
                          <p className="text-xs mt-0.5 text-green-600">
                            ✓ Đáp án đúng: {a.correctAnswerContent}
                          </p>
                        )}
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}

        {/* Actions */}
        <div className="space-y-3">
          <Button
            className="w-full"
            variant="outline"
            onClick={() => {
              // Go back to previous page (dashboard or wherever they came from)
              // If there's history, go back; otherwise go to dashboard
              if (window.history.length > 1) {
                navigate(-1)
              } else {
                navigate('/dashboard')
              }
            }}
          >
            <ArrowLeft className="h-4 w-4 mr-2" />
            Quay lại
          </Button>
        </div>
      </div>
    </div>
  )
}

