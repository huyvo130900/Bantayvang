import { useEffect, useState, useCallback, useRef } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { examTakingApi } from '../api'
import { useExamTimer } from '../hooks/use-exam-timer'
import { useAntiCheat } from '../hooks/use-anti-cheat'
import { ExamTimer } from '../components/exam-timer'
import { QuestionNavigation } from '../components/question-navigation'
import { QuestionDisplay } from '../components/question-display'
import { Button } from '@/components/ui/button'
import { AlertTriangle, ChevronLeft, ChevronRight, Send, XCircle } from 'lucide-react'
import { MAX_CHEATING_WARNINGS } from '@/lib/constants'
import type { BaithiDto, ExamQuestionDto } from '../types'

export function ExamTakingPage() {
  const { baithiId } = useParams<{ baithiId: string }>()
  const navigate = useNavigate()
  const id = Number(baithiId)

  const [examInfo, setExamInfo] = useState<BaithiDto | null>(null)
  const [questions, setQuestions] = useState<ExamQuestionDto[]>([])
  const [currentIndex, setCurrentIndex] = useState(0)
  const [answers, setAnswers] = useState<Record<number, { choiceId: number | null; essay: string }>>({})
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [showConfirm, setShowConfirm] = useState(false)
  const [loading, setLoading] = useState(true)
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [cheatingAlert, setCheatingAlert] = useState<string | null>(null)
  const [isForceTerminated, setIsForceTerminated] = useState(false)
  const [terminationReason, setTerminationReason] = useState<string | null>(null)

  const answersRef = useRef(answers)
  const questionsRef = useRef(questions)
  const isSubmittingRef = useRef(false)
  const isForceTerminatedRef = useRef(false)
  answersRef.current = answers
  questionsRef.current = questions

  const handleSubmitExam = useCallback(async (reason?: string) => {
    if (isSubmittingRef.current) return
    isSubmittingRef.current = true
    setIsSubmitting(true)
    setSubmitError(null)
    try {
      const currentAnswers = answersRef.current
      const currentQuestions = questionsRef.current
      const danhSachCauTraLoi = currentQuestions.map((q) => ({
        idBaiThi: id,
        idCauHoi: q.id,
        idLuaChonDaChon: currentAnswers[q.id]?.choiceId ?? null,
        cauTraLoiTuLuan: currentAnswers[q.id]?.essay || undefined,
        daLuu: true,
      }))
      const response = await examTakingApi.submit({ idBaiThi: id, danhSachCauTraLoi })
      if (response.data.success) {
        if (document.fullscreenElement) {
          await document.exitFullscreen().catch(() => {})
        }
        navigate(`/exam-result/${id}`, { replace: true, state: { forcedReason: reason } })
      } else {
        setSubmitError(response.data.message || 'Nộp bài thất bại')
        isSubmittingRef.current = false
        setIsSubmitting(false)
      }
    } catch {
      setSubmitError('Có lỗi kết nối. Vui lòng thử lại.')
      isSubmittingRef.current = false
      setIsSubmitting(false)
    } finally {
      setShowConfirm(false)
    }
  }, [id, navigate])

  const handleTimeUp = useCallback(() => {
    handleSubmitExam('Hết giờ làm bài')
  }, [handleSubmitExam])

  // Anti-cheat with force submit callback
  const handleForceSubmit = useCallback((reason: string) => {
    if (isForceTerminatedRef.current) return
    isForceTerminatedRef.current = true
    setIsForceTerminated(true)
    setTerminationReason(reason)
    setTimeout(() => handleSubmitExam(reason), 3000)
  }, [handleSubmitExam])

  const { warningCount, remainingWarnings, isTerminated, isFullscreen, requestFullscreen } = useAntiCheat({
    baithiId: id,
    enabled: !!examInfo && !isSubmitting,
    onForceSubmit: handleForceSubmit,
  })

  // Show cheating warning when warningCount increases (but not terminated yet)
  useEffect(() => {
    if (warningCount > 0 && !isTerminated) {
      setCheatingAlert(`⚠️ Cảnh báo gian lận lần ${warningCount}/${MAX_CHEATING_WARNINGS} — còn ${remainingWarnings} lần nữa bài thi sẽ bị kết thúc!`)
      const t = setTimeout(() => setCheatingAlert(null), 4000)
      return () => clearTimeout(t)
    }
  }, [warningCount]) // eslint-disable-line

  const { formattedTime, isWarning, isCritical } = useExamTimer({
    initialSeconds: examInfo?.thoiGianConLai ?? 0,
    onTimeUp: handleTimeUp,
  })

  useEffect(() => {
    if (!id) return
    loadExamData()
  }, [id]) // eslint-disable-line

  const loadExamData = async () => {
    setLoading(true)
    try {
      const [progressRes, questionsRes] = await Promise.all([
        examTakingApi.getProgress(id),
        examTakingApi.getQuestions(id),
      ])
      if (progressRes.data.success && progressRes.data.data) {
        setExamInfo(progressRes.data.data)
      }
      if (questionsRes.data.success && questionsRes.data.data) {
        const qs = questionsRes.data.data
        setQuestions(qs)
        const initial: Record<number, { choiceId: number | null; essay: string }> = {}
        qs.forEach((q: ExamQuestionDto) => { initial[q.id] = { choiceId: null, essay: '' } })
        setAnswers(initial)
      }
    } catch {
      setSubmitError('Không thể tải đề thi')
    } finally {
      setLoading(false)
    }
  }

  if (loading) {
    return (
      <div className="flex items-center justify-center h-screen">
        <div className="text-center text-gray-500">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary mx-auto mb-3" />
          Đang tải đề thi...
        </div>
      </div>
    )
  }

  const currentQuestion = questions[currentIndex]

  return (
    <div className="flex h-screen bg-gray-50 overflow-hidden">
      {/* Force terminated overlay */}
      {isForceTerminated && (
        <div className="fixed inset-0 bg-red-900/90 z-50 flex items-center justify-center">
          <div className="bg-white rounded-2xl p-8 max-w-md text-center space-y-4">
            <XCircle className="h-16 w-16 text-red-500 mx-auto" />
            <h2 className="text-2xl font-bold text-red-600">Bài thi bị kết thúc</h2>
            <p className="text-gray-600">
              {terminationReason || `Bạn đã vi phạm quy định gian lận quá ${MAX_CHEATING_WARNINGS} lần.`}
            </p>
            <p className="text-sm text-gray-400">Bài thi đang được nộp tự động...</p>
          </div>
        </div>
      )}

      {/* Yêu cầu Fullscreen overlay */}
      {!isFullscreen && !isForceTerminated && examInfo && (
        <div className="fixed inset-0 bg-slate-900/95 z-40 flex items-center justify-center backdrop-blur-sm">
          <div className="bg-white rounded-2xl p-8 max-w-md text-center space-y-6 shadow-2xl border border-gray-100">
            <div className="w-16 h-16 bg-amber-50 rounded-full flex items-center justify-center mx-auto text-amber-500 animate-pulse">
              <AlertTriangle className="h-8 w-8" />
            </div>
            <div className="space-y-2">
              <h2 className="text-xl font-bold text-slate-800">Chế độ toàn màn hình bắt buộc</h2>
              <p className="text-sm text-gray-500">
                Để đảm bảo tính công bằng, bài thi yêu cầu màn hình phải hiển thị toàn màn hình (Fullscreen) trong suốt quá trình làm bài.
              </p>
            </div>
            <div className="bg-amber-50 rounded-lg p-3 text-xs text-amber-700 text-left border border-amber-100">
              💡 <strong>Lưu ý:</strong>
              <ul className="list-disc pl-4 mt-1 space-y-1">
                <li>Nếu bạn thoát chế độ này, hệ thống sẽ ghi nhận vi phạm.</li>
                <li>Không thoát toàn màn hình trong suốt thời gian thi.</li>
              </ul>
            </div>
            <Button className="w-full py-6 font-semibold" onClick={requestFullscreen}>
              Bật chế độ Toàn màn hình
            </Button>
          </div>
        </div>
      )}

      {/* Cheating warning toast */}
      {cheatingAlert && !isForceTerminated && (
        <div className="fixed top-4 left-1/2 -translate-x-1/2 z-50 bg-red-600 text-white px-6 py-3 rounded-xl shadow-xl flex items-center gap-3 animate-pulse max-w-lg">
          <AlertTriangle className="h-5 w-5 shrink-0" />
          <span className="text-sm font-medium">{cheatingAlert}</span>
        </div>
      )}

      {/* Left: Question Navigation */}
      <div className="w-56 bg-white border-r flex flex-col">
        <div className="p-4 border-b">
          <ExamTimer formattedTime={formattedTime} isWarning={isWarning} isCritical={isCritical} />
        </div>
        {warningCount > 0 && (
          <div className="px-3 py-2 bg-red-50 border-b border-red-100">
            <p className="text-xs text-red-600 font-medium flex items-center gap-1">
              <AlertTriangle className="h-3 w-3" /> {warningCount}/{MAX_CHEATING_WARNINGS} lần vi phạm
            </p>
          </div>
        )}
        <div className="flex-1 overflow-y-auto p-3">
          <QuestionNavigation
            questions={questions}
            answers={answers}
            currentIndex={currentIndex}
            onNavigate={setCurrentIndex}
          />
        </div>
        <div className="p-3 border-t">
          <Button className="w-full" size="sm" onClick={() => setShowConfirm(true)} disabled={isSubmitting}>
            <Send className="h-4 w-4 mr-2" /> Nộp bài
          </Button>
        </div>
      </div>

      {/* Main: Question */}
      <div className="flex-1 flex flex-col overflow-hidden">
        <div className="flex-1 overflow-y-auto p-6">
          {currentQuestion && (
            <QuestionDisplay
              question={currentQuestion}
              selectedChoiceId={answers[currentQuestion.id]?.choiceId ?? null}
              essayAnswer={answers[currentQuestion.id]?.essay ?? ''}
              onSelectChoice={(choiceId) => {
                setAnswers(prev => ({ ...prev, [currentQuestion.id]: { ...prev[currentQuestion.id], choiceId } }))
                const activeIndex = currentIndex
                if (activeIndex < questions.length - 1) {
                  setTimeout(() => {
                    setCurrentIndex(current => (current === activeIndex ? current + 1 : current))
                  }, 200)
                }
              }}
              onEssayChange={(essay) =>
                setAnswers(prev => ({ ...prev, [currentQuestion.id]: { ...prev[currentQuestion.id], essay } }))
              }
            />
          )}
        </div>

        {/* Navigation footer */}
        <div className="border-t bg-white px-6 py-3 flex items-center justify-between">
          <Button variant="outline" onClick={() => setCurrentIndex(i => Math.max(0, i - 1))} disabled={currentIndex === 0}>
            <ChevronLeft className="h-4 w-4 mr-1" /> Câu trước
          </Button>
          <span className="text-sm text-gray-500">Câu {currentIndex + 1} / {questions.length}</span>
          <Button variant="outline" onClick={() => setCurrentIndex(i => Math.min(questions.length - 1, i + 1))} disabled={currentIndex === questions.length - 1}>
            Câu tiếp <ChevronRight className="h-4 w-4 ml-1" />
          </Button>
        </div>
      </div>

      {/* Confirm submit modal */}
      {showConfirm && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
          <div className="bg-white rounded-xl shadow-xl p-6 max-w-sm w-full space-y-4">
            <h3 className="font-semibold text-gray-900">Xác nhận nộp bài</h3>
            {submitError && <p className="text-sm text-red-600">{submitError}</p>}
            <div className="flex gap-3 justify-end">
              <Button variant="outline" onClick={() => setShowConfirm(false)}>Hủy</Button>
              <Button onClick={() => handleSubmitExam()} disabled={isSubmitting}>
                {isSubmitting ? 'Đang nộp...' : 'Xác nhận nộp'}
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
