import { cn } from '@/lib/utils'
import type { ExamQuestionDto } from '../types'
import { useRef, useState } from 'react'
import { ZoomIn, ZoomOut, ImagePlus, Trash2, Loader2 } from 'lucide-react'
import apiClient from '@/lib/axios'
import type { ApiResponse } from '@/types'

interface QuestionDisplayProps {
  question: ExamQuestionDto
  selectedChoiceId: number | null
  selectedChoiceIds?: number[]
  essayAnswer: string
  essayImageUrl?: string | null
  onSelectChoice: (choiceId: number) => void
  onToggleChoiceMultiple?: (choiceId: number) => void
  onEssayChange: (text: string) => void
  onEssayImageChange?: (url: string | null) => void
}

export function QuestionDisplay({
  question,
  selectedChoiceId,
  selectedChoiceIds = [],
  essayAnswer,
  essayImageUrl,
  onSelectChoice,
  onToggleChoiceMultiple,
  onEssayChange,
  onEssayImageChange,
}: QuestionDisplayProps) {
  const hasChoices = question.options.length > 0
  const [imgExpanded, setImgExpanded] = useState(false)
  const [uploadingEssayImg, setUploadingEssayImg] = useState(false)
  const essayImgRef = useRef<HTMLInputElement>(null)

  async function handleEssayImageUpload(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    if (!file) return
    setUploadingEssayImg(true)
    try {
      const formData = new FormData()
      formData.append('file', file)
      const res = await apiClient.post<ApiResponse<{ fileUrl: string }>>(
        '/upload/essay-image',
        formData,
        // BUG FIX: literal 'multipart/form-data' (no boundary) makes the browser send that exact
        // Content-Type instead of auto-generating one with a boundary - `undefined` deletes
        // apiClient's default 'application/json' header so the browser computes it correctly.
        { headers: { 'Content-Type': undefined } }
      )
      if (res.data.success && res.data.data?.fileUrl) {
        onEssayImageChange?.(res.data.data.fileUrl)
      } else {
        alert('Upload ảnh thất bại')
      }
    } catch {
      alert('Upload ảnh thất bại, vui lòng thử lại')
    } finally {
      setUploadingEssayImg(false)
      if (essayImgRef.current) essayImgRef.current.value = ''
    }
  }

  return (
    <div className="bg-white rounded-lg border p-6">
      {/* Question header */}
      <div className="flex items-start gap-3 mb-4">
        <span className="flex items-center justify-center h-8 w-8 rounded-full bg-primary text-white text-sm font-bold shrink-0">
          {question.questionOrder}
        </span>
        <div className="flex-1">
          <p className="text-base font-medium text-gray-900 whitespace-pre-wrap">
            {question.content}
          </p>
        </div>
      </div>

      {/* Image — click để phóng to/thu nhỏ ngay trong card */}
      {question.imageUrl && (
        <div className="mb-4">
          <div
            className={cn(
              'relative group cursor-pointer transition-all duration-300 ease-in-out',
              imgExpanded ? 'w-full' : 'inline-block'
            )}
            onClick={() => setImgExpanded((v) => !v)}
            title={imgExpanded ? 'Click để thu nhỏ' : 'Click để phóng to'}
          >
            <img
              src={question.imageUrl}
              alt="Hình ảnh câu hỏi"
              className={cn(
                'rounded-lg border object-contain transition-all duration-300 ease-in-out',
                imgExpanded
                  ? 'w-full max-h-[420px]'   // Phóng to: chiều rộng đầy card
                  : 'max-h-36 max-w-[200px]'  // Ảnh nhỏ mặc định
              )}
            />

            {/* Hint icon góc trên phải */}
            <div className="absolute top-1.5 right-1.5 opacity-0 group-hover:opacity-100 transition-opacity">
              <div className="flex items-center gap-1 bg-black/60 text-white text-[11px] px-2 py-1 rounded-full shadow">
                {imgExpanded
                  ? <><ZoomOut className="h-3 w-3" /> Thu nhỏ</>
                  : <><ZoomIn className="h-3 w-3" /> Phóng to</>
                }
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Choices */}
      {hasChoices && (
        <div className="space-y-2">
          {question.options.map((choice) => {
            const isSelected = question.allowMultipleSelection
              ? selectedChoiceIds.includes(choice.id)
              : selectedChoiceId === choice.id

            return (
              <label
                key={choice.id}
                className={cn(
                  'flex items-center gap-3 p-3 rounded-lg border cursor-pointer transition-colors',
                  isSelected
                    ? 'border-primary bg-primary/5'
                    : 'border-gray-200 hover:border-gray-300 hover:bg-gray-50'
                )}
              >
                <input
                  type={question.allowMultipleSelection ? 'checkbox' : 'radio'}
                  name={`question-${question.id}`}
                  checked={isSelected}
                  onChange={() => {
                    if (question.allowMultipleSelection) {
                      onToggleChoiceMultiple?.(choice.id)
                    } else {
                      onSelectChoice(choice.id)
                    }
                  }}
                  className={cn(
                    'h-4 w-4 text-primary focus:ring-primary',
                    question.allowMultipleSelection ? 'rounded border-gray-300' : ''
                  )}
                />
                <span className="text-sm text-gray-700">{choice.content}</span>
              </label>
            )
          })}
        </div>
      )}

      {/* Essay input */}
      {!hasChoices && (
        <div className="mt-4 space-y-3">
          <textarea
            value={essayAnswer}
            onChange={(e) => onEssayChange(e.target.value)}
            rows={5}
            className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
            placeholder="Nhập câu trả lời..."
          />

          {/* Essay image upload */}
          <div className="space-y-2">
            {essayImageUrl ? (
              <div className="relative inline-block">
                <img
                  src={essayImageUrl}
                  alt="Ảnh đính kèm"
                  className="max-h-48 rounded-lg border object-contain"
                />
                <button
                  type="button"
                  onClick={() => onEssayImageChange?.(null)}
                  className="absolute top-1.5 right-1.5 p-1.5 rounded-lg bg-white/90 text-red-500 hover:bg-red-50 shadow-sm border transition-colors"
                  title="Xóa ảnh"
                >
                  <Trash2 className="h-3.5 w-3.5" />
                </button>
              </div>
            ) : (
              <button
                type="button"
                onClick={() => essayImgRef.current?.click()}
                disabled={uploadingEssayImg}
                className="flex items-center gap-2 px-3 py-2 rounded-lg border border-dashed border-gray-300 text-sm text-gray-500 hover:border-primary/50 hover:text-primary hover:bg-primary/5 transition-all disabled:opacity-50"
              >
                {uploadingEssayImg
                  ? <><Loader2 className="h-4 w-4 animate-spin" /> Đang tải ảnh lên...</>
                  : <><ImagePlus className="h-4 w-4" /> Đính kèm hình ảnh</>
                }
              </button>
            )}
            <input
              ref={essayImgRef}
              type="file"
              accept="image/*"
              className="hidden"
              onChange={handleEssayImageUpload}
            />
          </div>
        </div>
      )}
    </div>
  )
}

