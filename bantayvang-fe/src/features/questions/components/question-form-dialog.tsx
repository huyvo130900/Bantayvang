import { useEffect, useRef, useState } from 'react'
import { useForm, Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { createQuestionSchema, type CreateQuestionFormData } from '../schemas'
import type { QuestionDto, LoaicauhoiDto } from '../types'
import { Button } from '@/components/ui/button'
import { ChoiceEditor } from './choice-editor'
import { X, ImagePlus, Trash2 } from 'lucide-react'
import apiClient from '@/lib/axios'
import type { ApiResponse } from '@/types'
import { questionsApi } from '../api'

interface QuestionFormDialogProps {
  open: boolean
  question: QuestionDto | null
  questionTypes: LoaicauhoiDto[]
  onClose: () => void
  onSubmit: (data: CreateQuestionFormData) => void
  isLoading: boolean
  defaultKhoaPhong?: string
  khoaList?: string[]
}

export function QuestionFormDialog({
  open,
  question,
  questionTypes,
  onClose,
  onSubmit,
  isLoading,
  defaultKhoaPhong,
  khoaList = [],
}: QuestionFormDialogProps) {
  const isEdit = !!question
  const [imagePreview, setImagePreview] = useState<string | null>(null)
  const [uploadingImage, setUploadingImage] = useState(false)
  const [isDuplicate, setIsDuplicate] = useState(false)
  const [checkingDuplicate, setCheckingDuplicate] = useState(false)
  const imgInputRef = useRef<HTMLInputElement>(null)

  const form = useForm<CreateQuestionFormData>({
    resolver: zodResolver(createQuestionSchema),
    defaultValues: getDefaults(null),
  })

  const noiDungValue = form.watch('content')
  const khoaPhongValue = form.watch('department')
  const questionCategoryId = form.watch('questionCategoryId')
  const selectedType = questionTypes.find((t) => t.id === questionCategoryId)
  // BUG FIX: previously only matched 'tự luận'/'tu luan' as a substring of the category
  // name/description, which missed short category codes like "TL" (the abbreviation this
  // app actually uses in several seeded/real categories - see the matching backend fix in
  // EssayQuestionHelper / CreateQuestionAsync). Use the same canonical essay-code list so the
  // frontend and backend agree on which categories are Tự luận.
  const ESSAY_CATEGORY_CODES = ['tự luận', 'tuluan', 'tu luan', 'tu_luan', 'tl', 'essay']
  const matchesEssayCode = (value?: string | null) => {
    if (!value) return false
    const normalized = value.trim().toLowerCase()
    return ESSAY_CATEGORY_CODES.includes(normalized) || normalized.includes('tự luận') || normalized.includes('tu luan')
  }
  const isEssay = matchesEssayCode(selectedType?.categoryName) || matchesEssayCode(selectedType?.description)

  useEffect(() => {
    if (isEssay) {
      const currentChoices = form.getValues('options')
      const first = currentChoices?.[0]
      // A single-element array isn't necessarily complete: react-hook-form's register()
      // on 'options.0.content' can auto-vivify options[0] with only `content` set (no
      // orderIndex/isCorrect) before this effect runs, which happens whenever the question
      // started with an empty options array (essay questions imported via Excel/Word only
      // populate SuggestedAnswer, not QuestionOptions). Require orderIndex/isCorrect to
      // actually be present before treating the array as already synced.
      const isComplete = !!currentChoices && currentChoices.length === 1
        && first?.orderIndex !== undefined && first?.isCorrect !== undefined
      if (!isComplete) {
        form.setValue('options', [
          { content: first?.content || '', orderIndex: 1, isCorrect: true },
        ])
      }
    } else {
      const currentChoices = form.getValues('options')
      if (!currentChoices || currentChoices.length < 2) {
        form.setValue('options', [
          { content: currentChoices?.[0]?.content || '', orderIndex: 1, isCorrect: true },
          { content: '', orderIndex: 2, isCorrect: false },
          { content: '', orderIndex: 3, isCorrect: false },
          { content: '', orderIndex: 4, isCorrect: false },
        ])
      }
    }
  }, [isEssay, form])

  useEffect(() => {
    if (!noiDungValue || noiDungValue.trim().length < 5) {
      setIsDuplicate(false)
      return
    }

    const timer = setTimeout(async () => {
      setCheckingDuplicate(true)
      try {
        const res = await questionsApi.checkDuplicate(
          noiDungValue.trim(),
          khoaPhongValue || undefined,
          question?.id || undefined
        )
        if (res.data.success) {
          setIsDuplicate(!!res.data.data)
        }
      } catch (err) {
        console.error('Lỗi check trùng câu hỏi:', err)
      } finally {
        setCheckingDuplicate(false)
      }
    }, 600)

    return () => clearTimeout(timer)
  }, [noiDungValue, khoaPhongValue, question])

  useEffect(() => {
    if (open) {
      const defaults = getDefaults(question)
      if (defaultKhoaPhong) {
        defaults.department = defaultKhoaPhong
      }
      form.reset(defaults)
      setImagePreview(question?.imageUrl || null)
      setIsDuplicate(false)
      setCheckingDuplicate(false)
    }
  }, [open, question, form, defaultKhoaPhong])

  if (!open) return null

  async function handleImageUpload(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    if (!file) return
    setUploadingImage(true)
    try {
      const formData = new FormData()
      formData.append('file', file)
      
      const dept = form.getValues('department') || 'khac'
      const safeDept = dept.trim().toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/(^-|-$)/g, '') || 'khac'
      const folderPath = `questions/${safeDept}`

      const res = await apiClient.post<ApiResponse<{ fileUrl: string }>>(
        `/upload/image?folder=${encodeURIComponent(folderPath)}`,
        formData,
        // BUG FIX: literal 'multipart/form-data' (no boundary) makes the browser send that exact
        // Content-Type instead of auto-generating one with a boundary - `undefined` deletes
        // apiClient's default 'application/json' header so the browser computes it correctly.
        { headers: { 'Content-Type': undefined } }
      )
      if (res.data.success && res.data.data?.fileUrl) {
        const url = res.data.data.fileUrl
        form.setValue('imageUrl', url)
        setImagePreview(url)
      }
    } catch {
      alert('Upload ảnh thất bại')
    } finally {
      setUploadingImage(false)
      if (imgInputRef.current) imgInputRef.current.value = ''
    }
  }

  const handleRemoveImage = () => {
    form.setValue('imageUrl', undefined)
    setImagePreview(null)
  }

  const handleFormSubmit = form.handleSubmit((data) => {
    if (!isEssay) {
      if (!data.options || data.options.length < 2) {
        form.setError('options', {
          type: 'manual',
          message: 'Phải có ít nhất 2 lựa chọn',
        })
        return
      }
      const hasEmptyChoice = data.options.some(c => !c.content?.trim())
      if (hasEmptyChoice) {
        form.setError('options', {
          type: 'manual',
          message: 'Nội dung lựa chọn không được trống',
        })
        return
      }
      const hasCorrect = data.options.some(c => c.isCorrect)
      if (!hasCorrect) {
        form.setError('options', {
          type: 'manual',
          message: 'Phải chọn ít nhất 1 đáp án đúng',
        })
        return
      }
    }
    onSubmit(data)
  })

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="bg-white rounded-xl shadow-2xl w-full max-w-2xl max-h-[90vh] overflow-y-auto">
        <div className="flex items-center justify-between p-4 border-b sticky top-0 bg-white z-10">
          <h2 className="text-lg font-semibold">
            {isEdit ? 'Sửa câu hỏi' : 'Thêm câu hỏi mới'}
          </h2>
          <Button variant="ghost" size="icon" onClick={onClose}>
            <X className="h-4 w-4" />
          </Button>
        </div>

        <form onSubmit={handleFormSubmit} className="p-5 space-y-4">
          {/* Nội dung */}
          <div className="space-y-1">
            <label className="text-sm font-medium text-gray-700">Nội dung câu hỏi *</label>
            <textarea
              {...form.register('content')}
              rows={3}
              className="flex w-full rounded-lg border border-input bg-background px-3 py-2 text-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring resize-none"
              placeholder="Nhập nội dung câu hỏi..."
            />
            {form.formState.errors.content && (
              <p className="text-xs text-red-500">{form.formState.errors.content.message}</p>
            )}
            {isDuplicate && (
              <p className="text-xs text-red-500 font-medium mt-1">
                ⚠️ Câu hỏi này đã tồn tại trong ngân hàng câu hỏi.
              </p>
            )}
            {checkingDuplicate && (
              <p className="text-xs text-blue-500 mt-1">
                🔄 Đang kiểm tra trùng lặp...
              </p>
            )}
          </div>

          {/* Metadata row */}
          <div className="grid grid-cols-3 gap-4">
            <div className="space-y-1">
              <label className="text-sm font-medium text-gray-700">Loại câu hỏi</label>
              <select
                {...form.register('questionCategoryId', { valueAsNumber: true })}
                className="h-10 w-full rounded-lg border border-input bg-background px-3 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
              >
                <option value="">-- Chọn loại --</option>
                {questionTypes.map((t) => (
                  <option key={t.id} value={t.id}>{t.categoryName}</option>
                ))}
              </select>
            </div>
            <div className="space-y-1">
              <label className="text-sm font-medium text-gray-700">Khoa/Phòng</label>
              <select
                {...form.register('department')}
                disabled={!!defaultKhoaPhong}
                className="h-10 w-full rounded-lg border border-input bg-background px-3 text-sm focus:outline-none focus:ring-2 focus:ring-ring disabled:opacity-75 disabled:bg-gray-100"
              >
                {defaultKhoaPhong ? (
                  <option value={defaultKhoaPhong}>{defaultKhoaPhong}</option>
                ) : (
                  <>
                    <option value="">-- Chọn khoa/phòng --</option>
                    {khoaList.map((k) => (
                      <option key={k} value={k}>{k}</option>
                    ))}
                  </>
                )}
              </select>
            </div>
            <div className="space-y-1">
              <label className="text-sm font-medium text-gray-700">Mức độ khó *</label>
              <select
                {...form.register('difficulty')}
                className="h-10 w-full rounded-lg border border-input bg-background px-3 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
              >
                <option value="">-- Chọn độ khó --</option>
                <option value="Dễ">Dễ</option>
                <option value="Trung bình">Trung bình</option>
                <option value="Khó">Khó</option>
              </select>
            </div>
          </div>

          {/* Image upload */}
          <div className="space-y-2">
            <label className="text-sm font-medium text-gray-700">Hình ảnh (tùy chọn)</label>
            <div className="mt-1">
              {imagePreview ? (
                <div className="relative flex items-center justify-center p-4 border rounded-xl bg-gray-50/50 w-full">
                  <img
                    src={imagePreview}
                    alt="Preview"
                    className="max-h-56 rounded-md object-contain"
                  />
                  <button
                    type="button"
                    onClick={handleRemoveImage}
                    className="absolute top-3 right-3 p-2 rounded-lg bg-white/90 text-red-500 hover:bg-red-50 hover:text-red-600 shadow-sm border transition-colors backdrop-blur-sm"
                  >
                    <Trash2 className="h-4 w-4" />
                  </button>
                </div>
              ) : (
                <button
                  type="button"
                  onClick={() => imgInputRef.current?.click()}
                  disabled={uploadingImage}
                  className="flex flex-col items-center justify-center gap-2 w-full py-8 border-2 border-dashed border-gray-200 rounded-xl text-sm text-gray-500 hover:border-primary/50 hover:bg-primary/5 transition-all disabled:opacity-50"
                >
                  <ImagePlus className="h-8 w-8 text-gray-400 mb-1" />
                  <span className="font-medium text-gray-600">{uploadingImage ? 'Đang tải ảnh lên...' : 'Nhấn để chọn hình ảnh'}</span>
                  <span className="text-xs text-gray-400">Hỗ trợ JPG, PNG, GIF, BMP</span>
                </button>
              )}
              <input
                ref={imgInputRef}
                type="file"
                accept="image/*"
                className="hidden"
                onChange={handleImageUpload}
              />
            </div>
          </div>

          {/* Choices / Standard Answer */}
          {isEssay ? (
            <div className="space-y-1">
              <label className="text-sm font-medium text-gray-700">Đáp án chuẩn / Hướng dẫn chấm</label>
              <textarea
                {...form.register('options.0.content')}
                rows={4}
                placeholder="Nhập đáp án chuẩn hoặc các ý chính cần có..."
                className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring resize-y"
              />
              {form.formState.errors.options?.[0]?.content && (
                <p className="text-xs text-red-500">{form.formState.errors.options[0].content.message}</p>
              )}
            </div>
          ) : (
            <Controller
              name="options"
              control={form.control}
              render={({ field, fieldState }) => (
                <ChoiceEditor
                  choices={field.value || []}
                  onChange={field.onChange}
                  error={fieldState.error?.message || fieldState.error?.root?.message}
                />
              )}
            />
          )}

          {/* Actions */}
          <div className="flex justify-end gap-2 pt-4 border-t">
            <Button type="button" variant="outline" onClick={onClose} disabled={isLoading}>
              Hủy
            </Button>
            <Button type="submit" disabled={isLoading || uploadingImage || isDuplicate || checkingDuplicate}>
              {isLoading ? 'Đang lưu...' : isEdit ? 'Cập nhật' : 'Tạo câu hỏi'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  )
}

function getDefaults(question: QuestionDto | null): CreateQuestionFormData {
  if (question) {
    // BUG FIX: the "Đáp án chuẩn" textarea for Tự luận questions is registered on
    // `options.0.content` (see the render below), so editing a question always reads its
    // existing answer from `options[0]` - but a question whose SuggestedAnswer was set via
    // direct API/import (Excel/Word, or an admin script) legitimately has an EMPTY options
    // array, so the field showed blank even though a real answer existed in the DB. Fall back
    // to `question.suggestedAnswer` whenever there's no options[0] to read from, so opening
    // the edit dialog doesn't look like the answer never made it in - and, combined with the
    // matching backend fix, doesn't risk quietly blanking it out on save either.
    const options = question.options.length > 0
      ? question.options.map((l) => ({
          content: l.content || '',
          orderIndex: l.orderIndex || 1,
          isCorrect: l.isCorrect || false,
        }))
      : question.suggestedAnswer
        ? [{ content: question.suggestedAnswer, orderIndex: 1, isCorrect: true }]
        : []

    return {
      content: question.content || '',
      questionCategoryId: question.questionCategoryId || undefined,
      difficulty: question.difficulty || 'Dễ',
      department: question.department || undefined,
      imageUrl: question.imageUrl || undefined,
      options,
    }
  }
  return {
    content: '',
    questionCategoryId: undefined,
    difficulty: '',
    department: undefined,
    imageUrl: undefined,
    options: [
      { content: '', orderIndex: 1, isCorrect: true },
      { content: '', orderIndex: 2, isCorrect: false },
      { content: '', orderIndex: 3, isCorrect: false },
      { content: '', orderIndex: 4, isCorrect: false },
    ],
  }
}
