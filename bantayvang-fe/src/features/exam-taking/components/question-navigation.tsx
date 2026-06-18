import { cn } from '@/lib/utils'
import type { ExamQuestionDto } from '../types'

interface QuestionNavigationProps {
  questions: ExamQuestionDto[]
  answers: Record<number, { choiceId: number | null; choiceIds?: number[]; essay: string }>
  currentIndex: number
  onNavigate: (index: number) => void
}

export function QuestionNavigation({
  questions,
  answers,
  currentIndex,
  onNavigate,
}: QuestionNavigationProps) {
  return (
    <div className="bg-white rounded-lg border p-4">
      <h3 className="text-sm font-medium text-gray-700 mb-3">Danh sách câu hỏi</h3>
      <div className="grid grid-cols-5 gap-2">
        {questions.map((q, idx) => {
          const userAns = answers[q.id]
          const hasChoices = q.danhSachLuaChon && q.danhSachLuaChon.length > 0
          
          let isAnswered = false
          if (userAns) {
            if (hasChoices) {
              isAnswered = (userAns.choiceId !== null && userAns.choiceId !== undefined && userAns.choiceId !== 0) ||
                           (userAns.choiceIds !== undefined && userAns.choiceIds.length > 0 && userAns.choiceIds.some(id => id !== 0))
            } else {
              isAnswered = !!userAns.essay && userAns.essay.trim().length > 0
            }
          } else {
            if (hasChoices) {
              isAnswered = (q.idLuaChonDaChon !== null && q.idLuaChonDaChon !== undefined && q.idLuaChonDaChon !== 0) ||
                           (q.idLuaChonDaChonList !== undefined && q.idLuaChonDaChonList.length > 0 && q.idLuaChonDaChonList.some(id => id !== 0))
            } else {
              isAnswered = !!q.cauTraLoiTuLuan && q.cauTraLoiTuLuan.trim().length > 0
            }
          }

          const isCurrent = idx === currentIndex

          return (
            <button
              key={q.id}
              onClick={() => onNavigate(idx)}
              className={cn(
                'h-9 w-9 rounded-md text-sm font-medium transition-colors',
                isCurrent && 'ring-2 ring-primary ring-offset-1',
                isAnswered ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
              )}
            >
              {idx + 1}
            </button>
          )
        })}
      </div>
      <div className="flex items-center gap-4 mt-3 text-xs text-gray-500">
        <span className="flex items-center gap-1">
          <span className="h-3 w-3 rounded bg-green-100 border border-green-300" /> Đã trả lời
        </span>
        <span className="flex items-center gap-1">
          <span className="h-3 w-3 rounded bg-gray-100 border border-gray-300" /> Chưa trả lời
        </span>
      </div>
    </div>
  )
}
