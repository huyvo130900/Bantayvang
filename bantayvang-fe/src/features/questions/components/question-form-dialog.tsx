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
  const isEssay = selectedType?.categoryName?.toLowerCase().includes('tự luận') || selectedType?.categoryName?.toLowerCase().includes('tu luan') || selectedType?.description?.toLowerCase().includes('tự luận') || selectedType?.description?.toLowerCase().includes('tu luan')

  useEffect(() => {
    if (isEssay) {
      const currentChoices = form.getValues('options')
      if (!currentChoices || currentChoices.length !== 1) {
        form.setValue('options', [
          { content: currentChoices?.[0]?.content || '', orderIndex: 1, isCorrect: true },
        ])
      }
      form.setValue('imageUrl', undefined)
      setImagePreview(null)
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

  const handleImageUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return
    setUploadingImage(true)
    try {
      const formData = new FormData()
      formData.append('file', file)
      const res = await apiClient.post<ApiResponse<{ url: string }>>(
        '/upload/image?folder=questions',
        formData,
        { headers: { 'Content-Type': undefined } }
      )
      if (res.data.success && res.data.data?.url) {
        const url = res.data.data.url
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
      if (!data.options.some(c => c.isCorrect)) {
        form.setError('options', {
          type: 'manual',
          message: 'Phải có ít nhất 1 đáp án đúng',
        })
        return
      }
    } else {
      data.options = []
      data.imageUrl = undefined
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
          {!isEssay && (
            <div className="space-y-2">
              <label className="text-sm font-medium text-gray-700">Hình ảnh (tùy chọn)</label>
              {imagePreview ? (
                <div className="relative inline-block">
                  <img
                    src={imagePreview}
                    alt="Preview"
                    className="max-h-48 max-w-full rounded-lg border object-contain"
                  />
                  <button
                    type="button"
                    onClick={handleRemoveImage}
                    className="absolute -top-2 -right-2 h-6 w-6 flex items-center justify-center rounded-full bg-red-500 text-white hover:bg-red-600 shadow"
                  >
                    <Trash2 className="h-3 w-3" />
                  </button>
                </div>
              ) : (
                <button
                  type="button"
                  onClick={() => imgInputRef.current?.click()}
                  disabled={uploadingImage}
                  className="flex items-center gap-2 px-4 py-2 border-2 border-dashed border-gray-200 rounded-lg text-sm text-gray-500 hover:border-primary/40 hover:text-primary transition-colors disabled:opacity-50"
                >
                  <ImagePlus className="h-4 w-4" />
                  {uploadingImage ? 'Đang upload...' : 'Thêm hình ảnh'}
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
          )}

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
    return {
      content: question.content || '',
      questionCategoryId: question.questionCategoryId || undefined,
      difficulty: question.difficulty || 'Dễ',
      department: question.department || undefined,
      imageUrl: question.imageUrl || undefined,
      options: question.options.map((l) => ({
        content: l.content || '',
        orderIndex: l.orderIndex || 1,
        isCorrect: l.isCorrect || false,
      })),
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
