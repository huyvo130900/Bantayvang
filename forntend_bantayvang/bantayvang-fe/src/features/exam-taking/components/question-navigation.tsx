import { cn } from '@/lib/utils'
import type { ExamQuestionDto } from '../types'

interface QuestionNavigationProps {
  questions: ExamQuestionDto[]
  currentIndex: number
  onNavigate: (index: number) => void
}

export function QuestionNavigation({
  questions,
  currentIndex,
  onNavigate,
}: QuestionNavigationProps) {
  return (
    <div className="bg-white rounded-lg border p-4">
      <h3 className="text-sm font-medium text-gray-700 mb-3">Danh sách câu hỏi</h3>
      <div className="grid grid-cols-5 gap-2">
        {questions.map((q, idx) => {
          const isAnswered = q.idLuaChonDaChon !== null || (q.cauTraLoiTuLuan && q.cauTraLoiTuLuan.length > 0)
          const isCurrent = idx === currentIndex

          return (
            <button
              key={q.id}
              onClick={() => onNavigate(idx)}
              className={cn(
                'h-9 w-9 rounded-md text-sm font-medium transition-colors',
                isCurrent && 'ring-2 ring-primary ring-offset-1',
                isAnswered && !isCurrent && 'bg-green-100 text-green-700',
                !isAnswered && !isCurrent && 'bg-gray-100 text-gray-600 hover:bg-gray-200'
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
