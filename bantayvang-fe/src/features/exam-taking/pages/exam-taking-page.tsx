import { useEffect, useState, useCallback, useRef } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { examTakingApi } from '../api'
import { useExamTimer } from '../hooks/use-exam-timer'
import { useAntiCheat } from '../hooks/use-anti-cheat'
import { ExamTimer } from '../components/exam-timer'
import { QuestionNavigation } from '../components/question-navigation'
import { QuestionDisplay } from '../components/question-display'
import { Button } from '@/components/ui/button'
import { AlertTriangle, ChevronLeft, ChevronRight, Send, XCircle, ClipboardList } from 'lucide-react'
import { MAX_CHEATING_WARNINGS } from '@/lib/constants'
import { cn } from '@/lib/utils'
import type { BaithiDto, ExamQuestionDto } from '../types'

export function ExamTakingPage() {
  const { baithiId } = useParams<{ baithiId: string }>()
  const navigate = useNavigate()
  const id = Number(baithiId)

  const [examInfo, setExamInfo] = useState<BaithiDto | null>(null)
  const [questions, setQuestions] = useState<ExamQuestionDto[]>([])
  const [currentIndex, setCurrentIndex] = useState(0)
  const [answers, setAnswers] = useState<Record<number, { choiceId: number | null; choiceIds: number[]; essay: string }>>({})
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [showConfirm, setShowConfirm] = useState(false)
  const [loading, setLoading] = useState(true)
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [cheatingAlert, setCheatingAlert] = useState<string | null>(null)
  const [isForceTerminated, setIsForceTerminated] = useState(false)
  const [terminationReason, setTerminationReason] = useState<string | null>(null)
  const [isSidebarOpen, setIsSidebarOpen] = useState(false)

  const answersRef = useRef(answers)
  const questionsRef = useRef(questions)
  const isSubmittingRef = useRef(false)
  const isForceTerminatedRef = useRef(false)
  answersRef.current = answers
  questionsRef.current = questions

  const unansweredQuestions = questions.filter((q) => {
    const userAns = answers[q.id]
    if (!userAns) return true
    const hasChoices = q.danhSachLuaChon && q.danhSachLuaChon.length > 0
    if (hasChoices) {
      const noChoiceId = userAns.choiceId === null || userAns.choiceId === undefined || userAns.choiceId === 0
      const noChoiceIds = !userAns.choiceIds || userAns.choiceIds.length === 0 || userAns.choiceIds.every(id => id === 0)
      return noChoiceId && noChoiceIds
    } else {
      return !userAns.essay || userAns.essay.trim() === ''
    }
  })

  const handleSubmitExam = useCallback(async (reason?: string) => {
    if (isSubmittingRef.current) return
    isSubmittingRef.current = true
    setIsSubmitting(true)
    setSubmitError(null)
    try {
      const currentAnswers = answersRef.current
      const currentQuestions = questionsRef.current
      const danhSachCauTraLoi: any[] = []

      currentQuestions.forEach((q) => {
        const userAns = currentAnswers[q.id]
        const hasChoices = q.danhSachLuaChon && q.danhSachLuaChon.length > 0
        if (hasChoices) {
          const selectedIds = userAns?.choiceIds || []
          if (selectedIds.length > 0) {
            selectedIds.forEach((choiceId) => {
              danhSachCauTraLoi.push({
                idBaiThi: id,
                idCauHoi: q.id,
                idLuaChonDaChon: choiceId,
                daLuu: true,
              })
            })
          } else {
            danhSachCauTraLoi.push({
              idBaiThi: id,
              idCauHoi: q.id,
              idLuaChonDaChon: null,
              daLuu: true,
            })
          }
        } else {
          danhSachCauTraLoi.push({
            idBaiThi: id,
            idCauHoi: q.id,
            idLuaChonDaChon: null,
            cauTraLoiTuLuan: userAns?.essay || undefined,
            daLuu: true,
          })
        }
      })

      const response = await examTakingApi.submit({ idBaiThi: id, danhSachCauTraLoi })
      if (response.data.success) {
        if (document.fullscreenElement) {
          await document.exitFullscreen().catch(() => {})
        }
        navigate(`/exam-result/${id}`, { replace: true, state: { forcedReason: reason } })
      } else {
        const msg = response.data.message || 'Nộp bài thất bại'
        if (msg.includes('đã được nộp') || msg.includes('Completed')) {
          if (document.fullscreenElement) {
            await document.exitFullscreen().catch(() => {})
          }
          navigate(`/exam-result/${id}`, { replace: true, state: { forcedReason: reason } })
          return
        }
        setSubmitError(msg)
        if (!showConfirm) {
          alert(msg)
        }
        isSubmittingRef.current = false
        setIsSubmitting(false)
      }
    } catch (err: any) {
      const msg = err?.response?.data?.message || 'Có lỗi kết nối. Vui lòng thử lại.'
      if (msg.includes('đã được nộp') || msg.includes('Completed')) {
        if (document.fullscreenElement) {
          await document.exitFullscreen().catch(() => {})
        }
        navigate(`/exam-result/${id}`, { replace: true, state: { forcedReason: reason } })
        return
      }
      setSubmitError(msg)
      if (!showConfirm) {
        alert(msg)
      }
      isSubmittingRef.current = false
      setIsSubmitting(false)
    } finally {
      if (showConfirm) {
        setShowConfirm(false)
      }
    }
  }, [id, navigate, showConfirm])

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

  const { warningCount, remainingWarnings, isTerminated, isFullscreen, requestFullscreen, blockedKeyMessage } = useAntiCheat({
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
    isLoaded: !!examInfo,
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
        const initial: Record<number, { choiceId: number | null; choiceIds: number[]; essay: string }> = {}
        qs.forEach((q: ExamQuestionDto) => {
          const choiceId = q.idLuaChonDaChon && q.idLuaChonDaChon !== 0 ? q.idLuaChonDaChon : null
          const rawChoiceIds = q.idLuaChonDaChonList || (q.idLuaChonDaChon ? [q.idLuaChonDaChon] : [])
          const choiceIds = rawChoiceIds.filter(id => id !== 0)
          initial[q.id] = {
            choiceId,
            choiceIds,
            essay: q.cauTraLoiTuLuan || '',
          }
        })
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
    <div className="flex h-screen bg-gray-50 overflow-hidden relative">
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

      {/* Toast: phím bị chặn (không phạt) */}
      {blockedKeyMessage && !isForceTerminated && (
        <div className="fixed top-4 left-1/2 -translate-x-1/2 z-50 bg-amber-500 text-white px-6 py-3 rounded-xl shadow-xl flex items-center gap-3 max-w-lg transition-all">
          <span className="text-lg">🔒</span>
          <span className="text-sm font-medium">{blockedKeyMessage.replace('🔒 ', '')}</span>
        </div>
      )}

      {/* Mobile sidebar backdrop */}
      {isSidebarOpen && (
        <div
          className="fixed inset-0 bg-black/40 z-20 md:hidden backdrop-blur-xs transition-opacity duration-300"
          onClick={() => setIsSidebarOpen(false)}
        />
      )}

      {/* Left: Question Navigation */}
      <div className={cn(
        "fixed inset-y-0 left-0 z-30 w-64 bg-white border-r flex flex-col transition-transform duration-300 ease-in-out md:relative md:w-56 shrink-0",
        isSidebarOpen ? "translate-x-0" : "max-md:-translate-x-full"
      )}>
        <div className="p-4 border-b flex items-center justify-between">
          <ExamTimer formattedTime={formattedTime} isWarning={isWarning} isCritical={isCritical} />
          <button
            onClick={() => setIsSidebarOpen(false)}
            className="md:hidden text-gray-400 hover:text-gray-600 p-1"
            title="Đóng bảng câu hỏi"
          >
            <XCircle className="h-5 w-5" />
          </button>
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
            onNavigate={(index) => {
              setCurrentIndex(index)
              setIsSidebarOpen(false)
            }}
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
        {/* Mobile top bar */}
        <div className="md:hidden border-b bg-white px-4 py-2.5 flex items-center justify-between shadow-sm shrink-0 z-10">
          <button
            onClick={() => setIsSidebarOpen(true)}
            className="flex items-center gap-1.5 text-xs font-semibold text-gray-700 bg-gray-100 px-3 py-2 rounded-lg hover:bg-gray-200 transition-colors"
          >
            <ClipboardList className="h-4 w-4 text-primary" />
            <span>Câu {currentIndex + 1}/{questions.length}</span>
          </button>
          <div className="scale-90 origin-center select-none">
            <ExamTimer formattedTime={formattedTime} isWarning={isWarning} isCritical={isCritical} />
          </div>
          <Button size="sm" className="h-8 text-xs font-semibold" onClick={() => setShowConfirm(true)} disabled={isSubmitting}>
            Nộp bài
          </Button>
        </div>

        <div className="flex-1 overflow-y-auto p-4 sm:p-6">
          {currentQuestion && (
            <QuestionDisplay
              question={currentQuestion}
              selectedChoiceId={answers[currentQuestion.id]?.choiceId ?? null}
              selectedChoiceIds={answers[currentQuestion.id]?.choiceIds ?? []}
              essayAnswer={answers[currentQuestion.id]?.essay ?? ''}
              onSelectChoice={(choiceId) => {
                setAnswers(prev => ({
                  ...prev,
                  [currentQuestion.id]: {
                    ...prev[currentQuestion.id],
                    choiceId,
                    choiceIds: [choiceId]
                  }
                }))
                const activeIndex = currentIndex
                if (activeIndex < questions.length - 1) {
                  setTimeout(() => {
                    setCurrentIndex(current => (current === activeIndex ? current + 1 : current))
                  }, 200)
                }
              }}
              onToggleChoiceMultiple={(choiceId) => {
                setAnswers(prev => {
                  const currentIds = prev[currentQuestion.id]?.choiceIds || []
                  const updatedIds = currentIds.includes(choiceId)
                    ? currentIds.filter(id => id !== choiceId)
                    : [...currentIds, choiceId]
                  return {
                    ...prev,
                    [currentQuestion.id]: {
                      ...prev[currentQuestion.id],
                      choiceIds: updatedIds,
                      choiceId: updatedIds[0] ?? null
                    }
                  }
                })
              }}
              onEssayChange={(essay) =>
                setAnswers(prev => ({ ...prev, [currentQuestion.id]: { ...prev[currentQuestion.id], essay } }))
              }
            />
          )}
        </div>

        {/* Navigation footer */}
        <div className="border-t bg-white px-4 sm:px-6 py-3 flex items-center justify-between">
          <Button variant="outline" size="sm" onClick={() => setCurrentIndex(i => Math.max(0, i - 1))} disabled={currentIndex === 0}>
            <ChevronLeft className="h-4 w-4 mr-1" /> <span className="hidden sm:inline">Câu trước</span>
          </Button>
          <span className="text-xs sm:text-sm text-gray-500 font-medium">Câu {currentIndex + 1} / {questions.length}</span>
          <Button variant="outline" size="sm" onClick={() => setCurrentIndex(i => Math.min(questions.length - 1, i + 1))} disabled={currentIndex === questions.length - 1}>
            <span className="hidden sm:inline">Câu tiếp</span> <ChevronRight className="h-4 w-4 ml-1" />
          </Button>
        </div>
      </div>

      {/* Confirm submit modal */}
      {showConfirm && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 backdrop-blur-sm">
          <div className="bg-white rounded-xl shadow-2xl p-6 max-w-md w-full space-y-4 border border-gray-100">
            <div className="flex items-center gap-3">
              <div className={cn(
                "p-2 rounded-full",
                unansweredQuestions.length > 0 ? "bg-amber-50 text-amber-600" : "bg-green-50 text-green-600"
              )}>
                <AlertTriangle className="h-6 w-6" />
              </div>
              <h3 className="font-semibold text-lg text-gray-900">Xác nhận nộp bài</h3>
            </div>

            {unansweredQuestions.length > 0 ? (
              <div className="space-y-3">
                <div className="p-3 bg-amber-50/80 border border-amber-100 rounded-lg text-sm text-amber-800">
                  <p className="font-semibold text-amber-900">
                    Cảnh báo: Bạn còn {unansweredQuestions.length} câu hỏi chưa hoàn thành!
                  </p>
                  <p className="text-xs text-amber-700 mt-1">
                    Nhập vào số câu dưới đây để di chuyển nhanh tới câu hỏi chưa làm và bổ sung câu trả lời.
                  </p>
                </div>

                {/* Grid of unanswered questions */}
                <div className="max-h-36 overflow-y-auto border border-amber-100 rounded-lg p-2.5 bg-amber-50/20">
                  <div className="flex flex-wrap gap-2">
                    {unansweredQuestions.map((q) => {
                      const idx = questions.findIndex((item) => item.id === q.id)
                      return (
                        <button
                          key={q.id}
                          onClick={() => {
                            setCurrentIndex(idx)
                            setShowConfirm(false)
                          }}
                          className="h-8 min-w-[3.5rem] px-2 bg-white border border-amber-200 hover:border-amber-400 hover:bg-amber-100 rounded text-xs font-semibold text-amber-800 transition-colors shadow-sm cursor-pointer flex items-center justify-center gap-0.5"
                          title={`Chuyển tới câu ${idx + 1}`}
                        >
                          Câu {idx + 1}
                        </button>
                      )
                    })}
                  </div>
                </div>

                <p className="text-sm text-gray-500">
                  Bạn có chắc chắn vẫn muốn nộp bài thi ngay bây giờ không?
                </p>
              </div>
            ) : (
              <p className="text-sm text-gray-500">
                Chúc mừng! Bạn đã hoàn thành tất cả các câu hỏi. Bạn có chắc chắn muốn nộp bài thi ngay bây giờ?
              </p>
            )}

            {submitError && <p className="text-sm text-red-600 font-medium">{submitError}</p>}
            
            <div className="flex gap-3 justify-end pt-2 border-t border-gray-100">
              <Button variant="outline" onClick={() => setShowConfirm(false)}>
                Hủy
              </Button>
              <Button 
                onClick={() => handleSubmitExam()} 
                disabled={isSubmitting}
                className={cn(
                  unansweredQuestions.length > 0 && "bg-amber-600 hover:bg-amber-700 text-white"
                )}
              >
                {isSubmitting ? 'Đang nộp...' : 'Xác nhận nộp'}
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
