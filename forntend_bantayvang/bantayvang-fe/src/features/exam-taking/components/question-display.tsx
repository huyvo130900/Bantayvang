import { cn } from '@/lib/utils'
import type { ExamQuestionDto } from '../types'

interface QuestionDisplayProps {
  question: ExamQuestionDto
  selectedChoiceId: number | null
  essayAnswer: string
  onSelectChoice: (choiceId: number) => void
  onEssayChange: (text: string) => void
}

export function QuestionDisplay({
  question,
  selectedChoiceId,
  essayAnswer,
  onSelectChoice,
  onEssayChange,
}: QuestionDisplayProps) {
  const hasChoices = question.danhSachLuaChon.length > 0

  return (
    <div className="bg-white rounded-lg border p-6">
      {/* Question header */}
      <div className="flex items-start gap-3 mb-4">
        <span className="flex items-center justify-center h-8 w-8 rounded-full bg-primary text-white text-sm font-bold shrink-0">
          {question.thuTuCau}
        </span>
        <div className="flex-1">
          <p className="text-base font-medium text-gray-900 whitespace-pre-wrap">
            {question.noiDung}
          </p>
          {question.diem && (
            <p className="text-sm text-gray-500 mt-1">({question.diem} điểm)</p>
          )}
        </div>
      </div>

      {/* Image if exists */}
      {question.hinhAnh && (
        <div className="mb-4">
          <img
            src={question.hinhAnh}
            alt="Hình ảnh câu hỏi"
            className="max-w-full max-h-64 rounded-lg border"
          />
        </div>
      )}

      {/* Choices */}
      {hasChoices && (
        <div className="space-y-2">
          {question.danhSachLuaChon.map((choice) => (
            <label
              key={choice.id}
              className={cn(
                'flex items-center gap-3 p-3 rounded-lg border cursor-pointer transition-colors',
                selectedChoiceId === choice.id
                  ? 'border-primary bg-primary/5'
                  : 'border-gray-200 hover:border-gray-300 hover:bg-gray-50'
              )}
            >
              <input
                type="radio"
                name={`question-${question.id}`}
                checked={selectedChoiceId === choice.id}
                onChange={() => onSelectChoice(choice.id)}
                className="h-4 w-4 text-primary focus:ring-primary"
              />
              <span className="text-sm text-gray-700">{choice.noiDung}</span>
            </label>
          ))}
        </div>
      )}

      {/* Essay input (if no choices or additional) */}
      {!hasChoices && (
        <div className="mt-4">
          <textarea
            value={essayAnswer}
            onChange={(e) => onEssayChange(e.target.value)}
            rows={5}
            className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
            placeholder="Nhập câu trả lời..."
          />
        </div>
      )}
    </div>
  )
}
