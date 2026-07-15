import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Plus, Trash2 } from 'lucide-react'
import type { CreateQuestionOptionDto } from '../types'

interface ChoiceEditorProps {
  choices: CreateQuestionOptionDto[]
  onChange: (choices: CreateQuestionOptionDto[]) => void
  error?: string
}

export function ChoiceEditor({ choices, onChange, error }: ChoiceEditorProps) {
  const addChoice = () => {
    onChange([
      ...choices,
      { content: '', orderIndex: choices.length + 1, isCorrect: false },
    ])
  }

  const removeChoice = (index: number) => {
    if (choices.length <= 2) return
    const updated = choices.filter((_, i) => i !== index).map((c, i) => ({ ...c, orderIndex: i + 1 }))
    onChange(updated)
  }

  const updateContent = (index: number, content: string) => {
    const updated = [...choices]
    updated[index] = { ...updated[index], content }
    onChange(updated)
  }

  const toggleCorrect = (index: number) => {
    const updated = choices.map((c, i) => ({
      ...c,
      isCorrect: i === index ? !c.isCorrect : c.isCorrect,
    }))
    onChange(updated)
  }

  return (
    <div className="space-y-2">
      <label className="text-sm font-medium text-gray-700">Danh sách lựa chọn *</label>

      {choices.map((choice, index) => (
        <div key={index} className="flex items-center gap-2">
          <input
            type="checkbox"
            checked={choice.isCorrect}
            onChange={() => toggleCorrect(index)}
            className="h-4 w-4 rounded border-gray-300 text-green-600 focus:ring-green-500"
            title="Đáp án đúng"
          />
          <span className="text-sm text-gray-500 w-6">{String.fromCharCode(65 + index)}.</span>
          <Input
            value={choice.content}
            onChange={(e) => updateContent(index, e.target.value)}
            placeholder={`Lựa chọn ${String.fromCharCode(65 + index)}`}
            className="flex-1"
          />
          <Button
            type="button"
            variant="ghost"
            size="icon"
            onClick={() => removeChoice(index)}
            disabled={choices.length <= 2}
          >
            <Trash2 className="h-4 w-4 text-red-400" />
          </Button>
        </div>
      ))}

      <Button type="button" variant="outline" size="sm" onClick={addChoice}>
        <Plus className="h-3 w-3 mr-1" />
        Thêm lựa chọn
      </Button>

      {error && <p className="text-xs text-red-500">{error}</p>}

      <p className="text-xs text-gray-400">
        ✓ Tick checkbox để đánh dấu đáp án đúng. Có thể chọn nhiều đáp án.
      </p>
    </div>
  )
}
