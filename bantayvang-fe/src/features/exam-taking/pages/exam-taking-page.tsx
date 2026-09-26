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
import type { ExamSubmissionDto, ExamQuestionDto } from '../types'

export function ExamTakingPage() {
  const { examSubmissionId } = useParams<{ examSubmissionId: string }>()
  const navigate = useNavigate()
  const id = Number(examSubmissionId)

  const [examInfo, setExamInfo] = useState<ExamSubmissionDto | null>(null)
  const [questions, setQuestions] = useState<ExamQuestionDto[]>([])
  const [currentIndex, setCurrentIndex] = useState(0)
  const [answers, setAnswers] = useState<Record<number, { choiceId: number | null; choiceIds: number[]; essay: string; essayImageUrl?: string | null }>>({})
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
  const prevIndexRef = useRef(currentIndex)
  const essayAutosaveTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null)
  useEffect(() => {
    answersRef.current = answers
    questionsRef.current = questions
  }, [answers, questions])

  // BUG FIX: answers used to live only in this component's React state and were sent to the
  // server in a single batch on final "Nộp bài" - a refresh, dropped connection, or closed tab
  // before that click wiped out every answer for the whole exam (the countdown/session resumed
  // fine, but GetExamQuestionsAsync restores selections from SubmissionDetail rows that were
  // never actually written to). Autosave each answer as it changes so those rows stay current.
  const persistAnswer = useCallback((questionId: number, overrideAns?: { choiceId: number | null; choiceIds: number[]; essay: string; essayImageUrl?: string | null }) => {
    const question = questionsRef.current.find(q => q.id === questionId)
    const ans = overrideAns ?? answersRef.current[questionId]
    if (!question) return

    const hasChoices = question.options && question.options.length > 0
    if (hasChoices) {
      const choiceIds = (ans?.choiceIds || []).filter(cid => cid)
      if (question.allowMultipleSelection) {
        examTakingApi
          .saveAnswerMultiple({
            examSubmissionId: id,
            questionId,
            selectedOptionId: choiceIds,
            isSaved: true,
          })
          .catch(() => {})
      } else {
        examTakingApi
          .saveAnswer({
            examSubmissionId: id,
            questionId,
            selectedOptionId: choiceIds[0] ?? null,
            isSaved: true,
          })
          .catch(() => {})
      }
    } else {
      if (!ans?.essay && !ans?.essayImageUrl) return
      examTakingApi
        .saveAnswer({
          examSubmissionId: id,
          questionId,
          selectedOptionId: null,
          essayAnswer: ans?.essay || undefined,
          essayImageUrl: ans?.essayImageUrl || undefined,
          isSaved: true,
        })
        .catch(() => {})
    }
  }, [id])

  // BUG FIX: onEssayChange/onEssayImageChange below only ever updated local React state -
  // persistAnswer was never called until the student navigated to a different question (the
  // effect right below). A student who types an essay answer (especially on the last question,
  // where there's nothing to "navigate away" to) and then crashes/loses connection/closes the tab
  // before clicking "Nộp bài" loses that answer entirely, since the server never received it -
  // exactly the class of bug the persistAnswer mechanism itself was added to prevent. Debounce a
  // save shortly after the student stops typing so it doesn't wait for navigation.
  const scheduleEssayAutosave = useCallback((questionId: number) => {
    if (essayAutosaveTimerRef.current) clearTimeout(essayAutosaveTimerRef.current)
    essayAutosaveTimerRef.current = setTimeout(() => {
      persistAnswer(questionId)
    }, 1500)
  }, [persistAnswer])

  useEffect(() => {
    return () => {
      if (essayAutosaveTimerRef.current) clearTimeout(essayAutosaveTimerRef.current)
    }
  }, [])

  // Save the answer for whichever question is being navigated AWAY from (covers Next/Previous/
  // sidebar jump/keyboard nav - every path that changes currentIndex goes through this one place).
  useEffect(() => {
    const prevIndex = prevIndexRef.current
    if (prevIndex !== currentIndex) {
      if (essayAutosaveTimerRef.current) clearTimeout(essayAutosaveTimerRef.current)
      const leftQuestion = questionsRef.current[prevIndex]
      if (leftQuestion) persistAnswer(leftQuestion.id)
    }
    prevIndexRef.current = currentIndex
  }, [currentIndex, persistAnswer])

  const unansweredQuestions = questions.filter((q) => {
    const userAns = answers[q.id]
    if (!userAns) return true
    const hasChoices = q.options && q.options.length > 0
    if (hasChoices) {
      const noChoiceId = userAns.choiceId === null || userAns.choiceId === undefined || userAns.choiceId === 0
      const noChoiceIds = !userAns.choiceIds || userAns.choiceIds.length === 0 || userAns.choiceIds.every(id => id === 0)
      return noChoiceId && noChoiceIds
    } else {
      // Considered unanswered if no text and no image
      return (!userAns.essay || userAns.essay.trim() === '') && !userAns.essayImageUrl
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
      const answers: any[] = []

      currentQuestions.forEach((q) => {
        const userAns = currentAnswers[q.id]
        const hasChoices = q.options && q.options.length > 0
        if (hasChoices) {
          const selectedIds = userAns?.choiceIds || []
          if (selectedIds.length > 0) {
            selectedIds.forEach((choiceId) => {
              answers.push({
                examSubmissionId: id,
                questionId: q.id,
                selectedOptionId: choiceId,
                isSaved: true,
              })
            })
          } else {
            answers.push({
              examSubmissionId: id,
              questionId: q.id,
              selectedOptionId: null,
              isSaved: true,
            })
          }
        } else {
          answers.push({
            examSubmissionId: id,
            questionId: q.id,
            selectedOptionId: null,
            essayAnswer: userAns?.essay || undefined,
            essayImageUrl: userAns?.essayImageUrl || undefined,
            isSaved: true,
          })
        }
      })

      const response = await examTakingApi.submit({ examSubmissionId: id, answers })
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
    examSubmissionId: id,
    enabled: !!examInfo && !isSubmitting,
    onForceSubmit: handleForceSubmit,
  })

  // Show cheating warning when warningCount increases (but not terminated yet)
  useEffect(() => {
    if (warningCount > 0 && !isTerminated) {
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setCheatingAlert(`⚠️ Cảnh báo gian lận lần ${warningCount}/${MAX_CHEATING_WARNINGS} — còn ${remainingWarnings} lần nữa bài thi sẽ bị kết thúc!`)
      const t = setTimeout(() => setCheatingAlert(null), 4000)
      return () => clearTimeout(t)
    }
  }, [warningCount]) // eslint-disable-line

  const { formattedTime, isWarning, isCritical } = useExamTimer({
    initialSeconds: examInfo?.remainingTimeSeconds ?? 0,
    isLoaded: !!examInfo,
    onTimeUp: handleTimeUp,
  })

  useEffect(() => {
    if (!id) return
    loadExamData()
  }, [id]) // eslint-disable-line

  async function loadExamData() {
    setLoading(true)
    try {
      const [progressRes, questionsRes] = await Promise.all([
        examTakingApi.getProgress(id),
        examTakingApi.getQuestions(id),
      ])
      if (progressRes.data.success && progressRes.data.data) {
        if (progressRes.data.data.status === 'Completed') {
          navigate(`/exam-result/${id}`)
          return
        }
        setExamInfo(progressRes.data.data)
      } else {
        setSubmitError(progressRes.data.message || 'Không thể tải thông tin bài thi')
        setLoading(false)
        return
      }

      if (questionsRes.data.success && questionsRes.data.data) {
        const qs = questionsRes.data.data
        if (qs.length === 0) {
           setSubmitError('Đề thi này chưa có câu hỏi nào. Vui lòng liên hệ quản trị viên.')
           setLoading(false)
           return
        }
        setQuestions(qs)
        const initial: Record<number, { choiceId: number | null; choiceIds: number[]; essay: string; essayImageUrl?: string | null }> = {}
        qs.forEach((q: ExamQuestionDto) => {
          const choiceId = q.selectedOptionId && q.selectedOptionId !== 0 ? q.selectedOptionId : null
          const rawChoiceIds = q.selectedOptionIdList || (q.selectedOptionId ? [q.selectedOptionId] : [])
          const choiceIds = rawChoiceIds.filter((id: number) => id !== 0)
          initial[q.id] = {
            choiceId,
            choiceIds,
            essay: q.essayAnswer || '',
            essayImageUrl: (q as any).essayImageUrl || null
          }
        })
        setAnswers(initial)
      } else {
        setSubmitError(questionsRes.data.message || 'Không thể tải danh sách câu hỏi')
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

  if (submitError && (!examInfo || questions.length === 0)) {
    return (
      <div className="flex items-center justify-center h-screen bg-gray-50">
        <div className="bg-white p-8 rounded-xl shadow-lg max-w-md text-center space-y-4">
          <XCircle className="h-16 w-16 text-red-500 mx-auto" />
          <h2 className="text-2xl font-bold text-red-600">Lỗi Tải Bài Thi</h2>
          <p className="text-gray-600">{submitError}</p>
          <Button onClick={() => navigate('/exam-waiting')} className="w-full mt-4">
            Quay lại phòng chờ
          </Button>
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
              essayImageUrl={answers[currentQuestion.id]?.essayImageUrl ?? null}
              onSelectChoice={(choiceId) => {
                const prevAns = answersRef.current[currentQuestion.id]
                const nextAns = { ...prevAns, choiceId, choiceIds: [choiceId] }
                setAnswers(prev => ({ ...prev, [currentQuestion.id]: nextAns }))
                persistAnswer(currentQuestion.id, nextAns)
                const activeIndex = currentIndex
                if (activeIndex < questions.length - 1) {
                  setTimeout(() => {
                    setCurrentIndex(current => (current === activeIndex ? current + 1 : current))
                  }, 200)
                }
              }}
              onToggleChoiceMultiple={(choiceId) => {
                const currentIds = answersRef.current[currentQuestion.id]?.choiceIds || []
                const updatedIds = currentIds.includes(choiceId)
                  ? currentIds.filter(cid => cid !== choiceId)
                  : [...currentIds, choiceId]
                const nextAns = { ...answersRef.current[currentQuestion.id], choiceIds: updatedIds, choiceId: updatedIds[0] ?? null }
                setAnswers(prev => ({ ...prev, [currentQuestion.id]: nextAns }))
                persistAnswer(currentQuestion.id, nextAns)
              }}
              onEssayChange={(essay) => {
                setAnswers(prev => ({ ...prev, [currentQuestion.id]: { ...prev[currentQuestion.id], essay } }))
                scheduleEssayAutosave(currentQuestion.id)
              }}
              onEssayImageChange={(essayImageUrl) => {
                setAnswers(prev => ({ ...prev, [currentQuestion.id]: { ...prev[currentQuestion.id], essayImageUrl } }))
                scheduleEssayAutosave(currentQuestion.id)
              }}
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
