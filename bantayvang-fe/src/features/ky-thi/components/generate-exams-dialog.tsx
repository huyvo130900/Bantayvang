import { useState, useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { X, Sparkles, AlertTriangle, CheckCircle, HelpCircle, Loader2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { kyThiApi } from '../api'
import type { KyThiDto, ExamGenerationConfig } from '../types'

const schema = z.object({
  soLuongDe: z.number().min(1, 'Số lượng đề tối thiểu là 1').max(100, 'Số lượng đề tối đa là 100'),
  tongSoCau: z.number().min(1, 'Tổng số câu tối thiểu là 1').max(200, 'Tổng số câu tối đa là 200'),
  soCauMC: z.number().nonnegative(),
  soCauEssay: z.number().nonnegative(),
  soCauEasy: z.number().nonnegative(),
  soCauMedium: z.number().nonnegative(),
  soCauHard: z.number().nonnegative(),
  khoaPhong: z.string().optional(),
})

type FormData = z.infer<typeof schema>

interface GenerateExamsDialogProps {
  open: boolean
  kyThi: KyThiDto | null
  onClose: () => void
  onSuccess: (message: string) => void
}


export function GenerateExamsDialog({ open, kyThi, onClose, onSuccess }: GenerateExamsDialogProps) {
  const [checking, setChecking] = useState(false)
  const [generating, setGenerating] = useState(false)
  const [checkWarnings, setCheckWarnings] = useState<string[]>([])
  const [checkPassed, setCheckPassed] = useState<boolean | null>(null)
  const [validationError, setValidationError] = useState<string | null>(null)

  const form = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: {
      soLuongDe: 3,
      tongSoCau: 20,
      soCauMC: 15,
      soCauEssay: 5,
      soCauEasy: 10,
      soCauMedium: 7,
      soCauHard: 3,
      khoaPhong: '',
    },
  })

  // Load initial form values
  useEffect(() => {
    if (open) {
      // Load department from KyThi tenKhoa or donViToChuc if possible as default
      if (kyThi) {
        form.setValue('khoaPhong', kyThi.tenKhoa || kyThi.donViToChuc || '')
        if (kyThi.tongSoCauHoi) {
          form.setValue('tongSoCau', kyThi.tongSoCauHoi)
        }
      }
      setCheckWarnings([])
      setCheckPassed(null)
      setValidationError(null)
    }
  }, [open, kyThi, form])

  // Watch form fields for live validation
  const watchAllFields = form.watch()
  const { tongSoCau, soCauMC, soCauEssay, soCauEasy, soCauMedium, soCauHard } = watchAllFields

  useEffect(() => {
    if (tongSoCau !== (soCauMC + soCauEssay)) {
      setValidationError('Tổng số câu Trắc nghiệm + Tự luận phải bằng Tổng số câu hỏi.')
      setCheckPassed(null)
      return
    }
    if (tongSoCau !== (soCauEasy + soCauMedium + soCauHard)) {
      setValidationError('Tổng số câu Dễ + Trung bình + Khó phải bằng Tổng số câu hỏi.')
      setCheckPassed(null)
      return
    }
    setValidationError(null)
  }, [tongSoCau, soCauMC, soCauEssay, soCauEasy, soCauMedium, soCauHard])

  if (!open || !kyThi) return null

  const handleCheck = async () => {
    if (validationError) return
    setChecking(true)
    setCheckWarnings([])
    setCheckPassed(null)
    try {
      const khoaPhongValue = (kyThi.tenKhoa || kyThi.donViToChuc || '').trim()
      const isAllDepts = !khoaPhongValue || khoaPhongValue === 'Tất cả các khoa' || khoaPhongValue === 'Tất cả khoa phòng'
      const config: ExamGenerationConfig = {
        ...form.getValues(),
        khoaPhong: isAllDepts ? undefined : khoaPhongValue,
      }
      const res = await kyThiApi.checkExamGeneration(kyThi.id, config)
      if (res.data.success && res.data.data) {
        setCheckWarnings(res.data.data.warnings)
        setCheckPassed(res.data.data.canGenerate)
      } else {
        setCheckWarnings([res.data.message || 'Kiểm tra thất bại'])
        setCheckPassed(false)
      }
    } catch (err: any) {
      setCheckWarnings([err?.response?.data?.message || 'Có lỗi xảy ra khi kiểm tra ngân hàng câu hỏi'])
      setCheckPassed(false)
    } finally {
      setChecking(false)
    }
  }

  const handleGenerate = async (data: FormData) => {
    if (validationError || checkPassed === false) return
    setGenerating(true)
    try {
      const khoaPhongValue = (kyThi.tenKhoa || kyThi.donViToChuc || '').trim()
      const isAllDepts = !khoaPhongValue || khoaPhongValue === 'Tất cả các khoa' || khoaPhongValue === 'Tất cả khoa phòng'
      const config: ExamGenerationConfig = {
        ...data,
        khoaPhong: isAllDepts ? undefined : khoaPhongValue,
      }
      const res = await kyThiApi.generateExams(kyThi.id, config)
      if (res.data.success) {
        onSuccess(res.data.message || 'Tạo bộ đề thi thành công!')
        onClose()
      } else {
        setCheckWarnings(res.data.errors || [res.data.message || 'Tạo đề thất bại'])
        setCheckPassed(false)
      }
    } catch (err: any) {
      setCheckWarnings(err?.response?.data?.errors || [err?.response?.data?.message || 'Lỗi hệ thống khi phát sinh đề thi'])
      setCheckPassed(false)
    } finally {
      setGenerating(false)
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm">
      <div className="bg-white rounded-2xl shadow-2xl w-full max-w-lg mx-4 overflow-hidden border border-gray-100 animate-in fade-in zoom-in duration-200">
        {/* Header */}
        <div className="flex items-center justify-between p-5 border-b bg-gradient-to-r from-blue-50 to-indigo-50">
          <div className="flex items-center gap-2">
            <div className="p-2 bg-blue-600 rounded-lg text-white">
              <Sparkles className="h-5 w-5" />
            </div>
            <div>
              <h2 className="text-lg font-bold text-gray-900">Tạo bộ đề thi ngẫu nhiên</h2>
              <p className="text-xs text-gray-500 mt-0.5">Kỳ thi: {kyThi.tenKyThi}</p>
            </div>
          </div>
          <Button variant="ghost" size="icon" onClick={onClose} className="rounded-full hover:bg-gray-200/50">
            <X className="h-4 w-4" />
          </Button>
        </div>

        {/* Content */}
        <form onSubmit={form.handleSubmit(handleGenerate)} className="p-5 space-y-4 max-h-[80vh] overflow-y-auto">
          {/* General Configs */}
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-1">
              <label className="text-xs font-semibold text-gray-700 uppercase tracking-wider">Số lượng đề cần tạo *</label>
              <Input
                type="number"
                min={1}
                max={100}
                {...form.register('soLuongDe', { valueAsNumber: true })}
                className="h-10 rounded-lg border-gray-200 focus:border-blue-500 focus:ring-blue-500"
              />
            </div>
            <div className="space-y-1">
              <label className="text-xs font-semibold text-gray-700 uppercase tracking-wider">Tổng số câu hỏi mỗi đề *</label>
              <Input
                type="number"
                readOnly
                {...form.register('tongSoCau', { valueAsNumber: true })}
                className="h-10 rounded-lg border-gray-200 bg-gray-50 text-gray-500 cursor-not-allowed"
              />
            </div>
          </div>

          {/* Department */}
          <div className="space-y-1">
            <label className="text-xs font-semibold text-gray-700 uppercase tracking-wider">Khoa / Phòng nguồn</label>
            <Input
              type="text"
              readOnly
              disabled
              value={kyThi.tenKhoa || kyThi.donViToChuc || 'Tất cả các khoa'}
              className="h-10 rounded-lg border-gray-200 bg-gray-50 text-gray-500 cursor-not-allowed"
            />
          </div>

          {/* Allocation Details */}
          <div className="border border-gray-100 rounded-xl p-4 bg-gray-50/50 space-y-4">
            <div>
              <h3 className="text-sm font-bold text-gray-800 flex items-center gap-1.5">
                <HelpCircle className="h-4 w-4 text-blue-600" />
                Phân bổ thể loại &amp; Độ khó
              </h3>
              <p className="text-[11px] text-gray-400 mt-0.5">Số câu hỏi thành phần trong một đề thi</p>
            </div>

            {/* Type breakdown */}
            <div className="space-y-2">
              <h4 className="text-xs font-semibold text-gray-500 uppercase tracking-wider">Thể loại câu hỏi</h4>
              <div className="grid grid-cols-2 gap-4">
                <div className="flex items-center justify-between gap-3 bg-white p-2 border border-gray-100 rounded-lg">
                  <span className="text-xs text-gray-600 font-medium pl-1">Trắc nghiệm</span>
                  <Input
                    type="number"
                    min={0}
                    {...form.register('soCauMC', { valueAsNumber: true })}
                    className="w-16 h-8 text-center rounded border-gray-200"
                  />
                </div>
                <div className="flex items-center justify-between gap-3 bg-white p-2 border border-gray-100 rounded-lg">
                  <span className="text-xs text-gray-600 font-medium pl-1">Tự luận</span>
                  <Input
                    type="number"
                    min={0}
                    {...form.register('soCauEssay', { valueAsNumber: true })}
                    className="w-16 h-8 text-center rounded border-gray-200"
                  />
                </div>
              </div>
            </div>

            {/* Difficulty breakdown */}
            <div className="space-y-2">
              <h4 className="text-xs font-semibold text-gray-500 uppercase tracking-wider">Độ khó</h4>
              <div className="grid grid-cols-3 gap-2">
                <div className="flex flex-col items-center justify-between bg-white p-2 border border-gray-100 rounded-lg gap-1.5">
                  <span className="text-xs text-gray-500 font-medium">Dễ</span>
                  <Input
                    type="number"
                    min={0}
                    {...form.register('soCauEasy', { valueAsNumber: true })}
                    className="w-16 h-8 text-center rounded border-gray-200"
                  />
                </div>
                <div className="flex flex-col items-center justify-between bg-white p-2 border border-gray-100 rounded-lg gap-1.5">
                  <span className="text-xs text-gray-500 font-medium">Trung bình</span>
                  <Input
                    type="number"
                    min={0}
                    {...form.register('soCauMedium', { valueAsNumber: true })}
                    className="w-16 h-8 text-center rounded border-gray-200"
                  />
                </div>
                <div className="flex flex-col items-center justify-between bg-white p-2 border border-gray-100 rounded-lg gap-1.5">
                  <span className="text-xs text-gray-500 font-medium">Khó</span>
                  <Input
                    type="number"
                    min={0}
                    {...form.register('soCauHard', { valueAsNumber: true })}
                    className="w-16 h-8 text-center rounded border-gray-200"
                  />
                </div>
              </div>
            </div>
          </div>

          {/* Local validation Error */}
          {validationError && (
            <div className="bg-red-50 border border-red-200 text-red-700 rounded-xl p-3 text-xs flex items-center gap-2">
              <AlertTriangle className="h-4 w-4 shrink-0" />
              <span>{validationError}</span>
            </div>
          )}

          {/* Warnings and messages from check bank API */}
          {checkWarnings.length > 0 && (
            <div className="bg-amber-50 border border-amber-200 text-amber-800 rounded-xl p-3 text-xs space-y-1">
              <p className="font-bold flex items-center gap-1.5 text-amber-700">
                <AlertTriangle className="h-4 w-4 shrink-0" />
                Ngân hàng câu hỏi không đạt yêu cầu:
              </p>
              <ul className="list-disc pl-5 space-y-0.5">
                {checkWarnings.map((w, idx) => (
                  <li key={idx}>{w}</li>
                ))}
              </ul>
            </div>
          )}

          {checkPassed === true && (
            <div className="bg-green-50 border border-green-200 text-green-700 rounded-xl p-3 text-xs flex items-center gap-2">
              <CheckCircle className="h-4 w-4 shrink-0 text-green-600" />
              <span className="font-medium">Ngân hàng câu hỏi đủ điều kiện! Có thể tạo {form.watch('soLuongDe')} đề thi không trùng lặp.</span>
            </div>
          )}

          {/* Action buttons */}
          <div className="flex justify-between items-center gap-2 pt-3 border-t">
            <Button
              type="button"
              variant="outline"
              onClick={handleCheck}
              disabled={checking || !!validationError}
              className="border-blue-200 text-blue-600 hover:bg-blue-50/50 hover:text-blue-700 shrink-0"
            >
              {checking ? (
                <>
                  <Loader2 className="h-4 w-4 mr-1.5 animate-spin" />
                  Đang kiểm tra...
                </>
              ) : (
                'Kiểm tra ngân hàng'
              )}
            </Button>

            <div className="flex gap-2">
              <Button type="button" variant="outline" onClick={onClose} disabled={generating}>Hủy</Button>
              <Button
                type="submit"
                disabled={generating || checking || !!validationError || checkPassed !== true}
                className="bg-blue-600 hover:bg-blue-700 text-white"
              >
                {generating ? (
                  <>
                    <Loader2 className="h-4 w-4 mr-1.5 animate-spin" />
                    Đang tạo...
                  </>
                ) : (
                  'Tạo bộ đề'
                )}
              </Button>
            </div>
          </div>
        </form>
      </div>
    </div>
  )
}
