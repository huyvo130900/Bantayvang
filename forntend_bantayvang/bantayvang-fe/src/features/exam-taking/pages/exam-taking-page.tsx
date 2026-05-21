import { useEffect, useState, useCallback } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { examTakingApi } from '../api'
import { useExamTimer } from '../hooks/use-exam-timer'
import { useAntiCheat } from '../hooks/use-anti-cheat'
import { ExamTimer } from '../components/exam-timer'
import { QuestionNavigation } from '../components/question-navigation'
import { QuestionDisplay } from '../components/question-display'
import { Button } from '@/components/ui/button'
import { AlertTriangle, ChevronLeft, ChevronRight, Send } from 'lucide-react'
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

  // Anti-cheat
  const { warningCount } = useAntiCheat({ baithiId: id, enabled: !!examInfo })

  // Timer
  const handleTimeUp = useCallback(async () => {
    await handleSubmitExam()
  }, []) // eslint-disable-line react-hooks/exhaustive-deps

  const { formattedTime, isWarning, isCritical } = useExamTimer({
    initialSeconds: examInfo?.thoiGianConLai ?? 0,
    onTimeUp: handleTimeUp,
  })

  // Load exam data
  useEffect(() => {
    if (!id) return
    loadExamData()
  }, [id]) // eslint-disable-line react-hooks/exhaustive-deps

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

        // Restore saved answers
        const savedAnswers: Record<number, { choiceId: number | null; essay: string }> = {}
        qs.forEach((q) => {
          savedAnswers[q.id] = {
            choiceId: q.idLuaChonDaChon,
            essay: q.cauTraLoiTuLuan || '',
          }
        })
        setAnswers(savedAnswers)
      }
    } catch {
      // handle error
    } finally {
      setLoading(false)
    }
  }

  // Save answer to server (auto-save)
  const saveAnswer = async (questionId: number, choiceId: number | null, essay: string) => {
    try {
      await examTakingApi.saveAnswer({
        idBaiThi: id,
        idCauHoi: questionId,
        idLuaChonDaChon: choiceId,
        cauTraLoiTuLuan: essay || undefined,
        daLuu: true,
      })
    } catch {
      // Silent fail - don't disrupt exam
    }
  }

  const handleSelectChoice = (choiceId: number) => {
    const question = questions[currentIndex]
    setAnswers((prev) => ({
      ...prev,
      [question.id]: { ...prev[question.id], choiceId },
    }))
    // Update local question state
    setQuestions((prev) =>
      prev.map((q, i) => (i === currentIndex ? { ...q, idLuaChonDaChon: choiceId } : q))
    )
    // Auto-save
    saveAnswer(question.id, choiceId, answers[question.id]?.essay || '')
  }

  const handleEssayChange = (text: string) => {
    const question = questions[currentIndex]
    setAnswers((prev) => ({
      ...prev,
      [question.id]: { ...prev[question.id], essay: text },
    }))
  }

  // Save essay on blur/navigate
  const saveCurrentEssay = () => {
    const question = questions[currentIndex]
    const answer = answers[question.id]
    if (answer?.essay) {
      saveAnswer(question.id, answer.choiceId, answer.essay)
    }
  }

  const handleNavigate = (index: number) => {
    saveCurrentEssay()
    setCurrentIndex(index)
  }

  const handlePrev = () => {
    if (currentIndex > 0) handleNavigate(currentIndex - 1)
  }

  const handleNext = () => {
    if (currentIndex < questions.length - 1) handleNavigate(currentIndex + 1)
  }

  const handleSubmitExam = async () => {
    setIsSubmitting(true)
    try {
      const danhSachCauTraLoi = questions.map((q) => ({
        idBaiThi: id,
        idCauHoi: q.id,
        idLuaChonDaChon: answers[q.id]?.choiceId ?? null,
        cauTraLoiTuLuan: answers[q.id]?.essay || undefined,
        daLuu: true,
      }))

      const response = await examTakingApi.submit({ idBaiThi: id, danhSachCauTraLoi })
      if (response.data.success) {
        navigate(`/exam-result/${id}`, { replace: true })
      }
    } catch {
      // handle error
    } finally {
      setIsSubmitting(false)
      setShowConfirm(false)
    }
  }

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <p className="text-gray-500">Đang tải bài thi...</p>
      </div>
    )
  }

  if (!examInfo || questions.length === 0) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <p className="text-red-500">Không thể tải bài thi. Vui lòng thử lại.</p>
      </div>
    )
  }

  const currentQuestion = questions[currentIndex]
  const answeredCount = questions.filter(
    (q) => answers[q.id]?.choiceId !== null || (answers[q.id]?.essay && answers[q.id].essay.length > 0)
  ).length

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <header className="sticky top-0 z-30 bg-white border-b shadow-sm px-6 py-3">
        <div className="flex items-center justify-between max-w-7xl mx-auto">
          <div>
            <h1 className="text-lg font-bold text-gray-900">{examInfo.tenDeThi}</h1>
            <p className="text-sm text-gray-500">
              Đã trả lời: {answeredCount}/{questions.length}
            </p>
          </div>

          <div className="flex items-center gap-4">
            {warningCount > 0 && (
              <div className="flex items-center gap-1 text-orange-600 text-sm">
                <AlertTriangle className="h-4 w-4" />
                <span>{warningCount} cảnh báo</span>
              </div>
            )}
            <ExamTimer
              formattedTime={formattedTime}
              isWarning={isWarning}
              isCritical={isCritical}
            />
            <Button
              variant="destructive"
              size="sm"
              onClick={() => setShowConfirm(true)}
            >
              <Send className="h-4 w-4 mr-1" />
              Nộp bài
            </Button>
          </div>
        </div>
      </header>

      {/* Content */}
      <div className="max-w-7xl mx-auto p-6 grid grid-cols-1 lg:grid-cols-4 gap-6">
        {/* Question panel */}
        <div className="lg:col-span-3 space-y-4">
          <QuestionDisplay
            question={currentQuestion}
            selectedChoiceId={answers[currentQuestion.id]?.choiceId ?? null}
            essayAnswer={answers[currentQuestion.id]?.essay ?? ''}
            onSelectChoice={handleSelectChoice}
            onEssayChange={handleEssayChange}
          />

          {/* Navigation buttons */}
          <div className="flex items-center justify-between">
            <Button
              variant="outline"
              onClick={handlePrev}
              disabled={currentIndex === 0}
            >
              <ChevronLeft className="h-4 w-4 mr-1" />
              Câu trước
            </Button>
            <span className="text-sm text-gray-500">
              Câu {currentIndex + 1} / {questions.length}
            </span>
            <Button
              variant="outline"
              onClick={handleNext}
              disabled={currentIndex === questions.length - 1}
            >
              Câu sau
              <ChevronRight className="h-4 w-4 ml-1" />
            </Button>
          </div>
        </div>

        {/* Sidebar */}
        <div className="lg:col-span-1">
          <QuestionNavigation
            questions={questions}
            currentIndex={currentIndex}
            onNavigate={handleNavigate}
          />
        </div>
      </div>

      {/* Submit confirmation */}
      {showConfirm && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="bg-white rounded-lg shadow-xl w-full max-w-sm p-6">
            <h3 className="text-lg font-semibold mb-2">Xác nhận nộp bài</h3>
            <p className="text-sm text-gray-600 mb-1">
              Đã trả lời: <strong>{answeredCount}/{questions.length}</strong> câu
            </p>
            {answeredCount < questions.length && (
              <p className="text-sm text-orange-600 mb-4">
                ⚠️ Bạn còn {questions.length - answeredCount} câu chưa trả lời!
              </p>
            )}
            <div className="flex justify-end gap-2 mt-4">
              <Button variant="outline" onClick={() => setShowConfirm(false)}>
                Tiếp tục làm
              </Button>
              <Button
                variant="destructive"
                onClick={handleSubmitExam}
                disabled={isSubmitting}
              >
                {isSubmitting ? 'Đang nộp...' : 'Nộp bài'}
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
