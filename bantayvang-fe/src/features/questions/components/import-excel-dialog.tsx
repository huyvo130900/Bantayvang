import { useState, useRef, useEffect } from 'react'
import { Button } from '@/components/ui/button'
import { X, FileSpreadsheet, Building2, CheckCircle2, AlertTriangle, Download, Eye, ArrowLeft } from 'lucide-react'
import { questionsApi } from '../api'
import { departmentApi } from '@/features/departments/api'
import type { DepartmentDto } from '@/features/departments/types'
import type { QuestionDto } from '../types'
import { useAppSelector } from '@/app/hooks'
import { ROLES } from '@/lib/constants'

interface ImportExcelDialogProps {
  open: boolean
  onClose: () => void
  onSuccess: () => void
}

interface ImportResult {
  success: boolean
  message: string
  errors?: string[]
}

// -------------------------------------------------------
// Bước 2: Màn hình Preview — xem và sửa đáp án mẫu
// -------------------------------------------------------
function PreviewStep({
  questions,
  onBack,
  onConfirm,
  isImporting,
}: {
  questions: QuestionDto[]
  onBack: () => void
  onConfirm: (qs: QuestionDto[]) => void
  isImporting: boolean
}) {
  const [items, setItems] = useState<QuestionDto[]>(questions)

  const updateAnswer = (idx: number, val: string) => {
    setItems((prev) =>
      prev.map((q, i) => (i === idx ? { ...q, suggestedAnswer: val } : q))
    )
  }

  return (
    <>
      <div className="p-6 space-y-4 max-h-[65vh] overflow-y-auto">
        <div className="flex items-center gap-2 text-sm text-gray-600 bg-blue-50 border border-blue-100 rounded-lg px-3 py-2.5">
          <Eye className="h-4 w-4 text-blue-500 shrink-0" />
          <span>Xem trước <strong>{items.length}</strong> câu hỏi. Bạn có thể thêm Đáp án mẫu cho từng câu (tùy chọn, để trống cũng được).</span>
        </div>

        <div className="space-y-3">
          {items.map((q, idx) => (
            <div key={idx} className="border border-gray-200 rounded-xl p-4 space-y-2 bg-white">
              <div className="flex items-start gap-2">
                <span className="flex-shrink-0 inline-flex items-center justify-center h-5 w-5 rounded-full bg-blue-600 text-white text-[10px] font-bold mt-0.5">
                  {idx + 1}
                </span>
                <p className="text-sm text-gray-800 leading-snug">{q.content}</p>
              </div>

              {/* Đáp án mẫu — tùy chọn */}
              <div className="pl-7">
                <label className="block text-[11px] font-semibold text-gray-500 mb-1">
                  Đáp án mẫu <span className="text-gray-400 font-normal">(tùy chọn)</span>
                </label>
                <textarea
                  rows={2}
                  value={q.suggestedAnswer ?? ''}
                  onChange={(e) => updateAnswer(idx, e.target.value)}
                  placeholder="Nhập đáp án mẫu hoặc để trống..."
                  className="w-full text-xs border border-gray-200 rounded-lg px-3 py-2 text-gray-700 placeholder:text-gray-300 focus:outline-none focus:ring-2 focus:ring-blue-300/50 resize-none bg-gray-50"
                />
              </div>
            </div>
          ))}
        </div>
      </div>

      <div className="flex justify-between gap-2 p-4 border-t border-gray-100 bg-gray-50/50">
        <Button variant="outline" onClick={onBack} disabled={isImporting} className="rounded-lg text-xs">
          <ArrowLeft className="h-3.5 w-3.5 mr-1" /> Quay lại
        </Button>
        <Button
          onClick={() => onConfirm(items)}
          disabled={isImporting}
          className="bg-blue-600 hover:bg-blue-700 text-white rounded-lg font-bold text-xs shadow-md disabled:opacity-50"
        >
          {isImporting ? 'Đang import...' : `✓ Xác nhận Import ${items.length} câu`}
        </Button>
      </div>
    </>
  )
}

// -------------------------------------------------------
// Main Dialog
// -------------------------------------------------------
export function ImportExcelDialog({ open, onClose, onSuccess }: ImportExcelDialogProps) {
  const [file, setFile] = useState<File | null>(null)
  const [isUploading, setIsUploading] = useState(false)
  const [isImporting, setIsImporting] = useState(false)
  const [departments, setDepartments] = useState<DepartmentDto[]>([])
  const [selectedKhoa, setSelectedKhoa] = useState<string>('')
  const [selectedLoai, setSelectedLoai] = useState<number | ''>('')
  const [result, setResult] = useState<ImportResult | null>(null)
  const [previewQuestions, setPreviewQuestions] = useState<QuestionDto[] | null>(null)
  const inputRef = useRef<HTMLInputElement>(null)

  const currentUser = useAppSelector((state) => state.auth.user)
  const questionTypes = useAppSelector((state) => state.questions.questionTypes)
  
  const isDeptManager =
    currentUser?.role === ROLES.DEPT_MANAGER ||
    currentUser?.roleName === 'DeptManager'
  const myKhoa = currentUser?.deptManagerDeptName || currentUser?.department || null

  const tracNghiemType = questionTypes.find(
    (t) => {
      const name = (t.categoryName || '').toLowerCase();
      const desc = (t.description || '').toLowerCase();
      return name === 'tn' || name.includes('trắc nghiệm') || name.includes('trac nghiem') || desc.includes('trắc nghiệm') || desc.includes('trac nghiem');
    }
  )
  const tuLuanType = questionTypes.find(
    (t) => {
      const name = (t.categoryName || '').toLowerCase();
      const desc = (t.description || '').toLowerCase();
      return name === 'tl' || name.includes('tự luận') || name.includes('tu luan') || desc.includes('tự luận') || desc.includes('tu luan');
    }
  )

  const tracNghiemId = tracNghiemType?.id || 1
  const tuLuanId = tuLuanType?.id || 2

  useEffect(() => {
    if (open) {
      departmentApi.getAll({ status: true, pageSize: 100 })
        .then((res) => setDepartments(res.data?.data || []))
        .catch(() => setDepartments([]))

      if (isDeptManager && myKhoa) {
        setSelectedKhoa(myKhoa)
      } else {
        setSelectedKhoa('')
      }
      setSelectedLoai('')
      setFile(null)
      setResult(null)
      setPreviewQuestions(null)
    }
  }, [open, isDeptManager, myKhoa])

  if (!open) return null

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const selected = e.target.files?.[0]
    if (selected) {
      setFile(selected)
      setResult(null)
      setPreviewQuestions(null)
    }
  }

  // Bước 1: Preview (chỉ áp dụng cho Tự luận)
  async function handlePreview() {
    if (!file || !selectedKhoa || !selectedLoai) return
    setIsUploading(true)
    setResult(null)
    try {
      const res = await questionsApi.previewExcel(file, selectedKhoa, Number(selectedLoai))
      if (res.data.success && res.data.data) {
        setPreviewQuestions(res.data.data)
      } else {
        // BUG FIX: dropped the per-row `errors` list the API returns (still shown correctly by
        // handleDirectImport below) - without it, the admin only sees a generic failure message
        // and has no way to tell which rows in their file are wrong.
        setResult({ success: false, message: res.data.message || 'Không đọc được file', errors: (res.data as any).errors })
      }
    } catch (err: any) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message || 'Có lỗi khi đọc file'
      const errs = (err as { response?: { data?: { errors?: string[] } } })?.response?.data?.errors
      setResult({ success: false, message: msg, errors: errs })
    } finally {
      setIsUploading(false)
    }
  }

  // Bước 2: Import thật (kèm đáp án mẫu đã chỉnh)
  async function handleConfirmImport(editedQuestions: QuestionDto[]) {
    if (!selectedLoai) return
    setIsImporting(true)
    try {
      const payload = editedQuestions.map((q) => ({
        content: q.content ?? '',
        questionCategoryId: Number(selectedLoai),
        difficulty: q.difficulty === 'Khó' ? '3' : q.difficulty === 'Trung bình' ? '2' : '1',
        department: selectedKhoa,
        suggestedAnswer: q.suggestedAnswer || undefined,
        options: q.options?.map((o) => ({
          content: o.content ?? '',
          isCorrect: o.isCorrect ?? false,
          orderIndex: o.orderIndex ?? 0,
        })) ?? [],
      }))
      const res = await questionsApi.importFromPreview(payload, selectedKhoa, Number(selectedLoai))
      setPreviewQuestions(null)
      setResult({ success: res.data.success, message: res.data.message || res.data.data as any as string, errors: (res.data as any).errors })
      if (res.data.success) onSuccess()
    } catch (err: any) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message || 'Có lỗi khi import'
      const errs = (err as { response?: { data?: { errors?: string[] } } })?.response?.data?.errors
      setResult({ success: false, message: msg, errors: errs })
      setPreviewQuestions(null)
    } finally {
      setIsImporting(false)
    }
  }

  // Import trực tiếp (Trắc nghiệm)
  async function handleDirectImport() {
    if (!file || !selectedKhoa || !selectedLoai) return
    setIsUploading(true)
    setResult(null)
    try {
      const response = await questionsApi.importExcel(file, selectedKhoa, Number(selectedLoai))
      setResult({
        success: response.data.success,
        message: response.data.message,
        errors: (response.data as any).errors,
      })
      if (response.data.success) onSuccess()
    } catch {
      setResult({ success: false, message: 'Lỗi kết nối server khi import file' })
    } finally {
      setIsUploading(false)
    }
  }

  async function handleDownloadTemplate() {
    if (!selectedLoai) return
    try {
      const response = await questionsApi.downloadTemplate(Number(selectedLoai))
      const url = window.URL.createObjectURL(new Blob([response.data]))
      const link = document.createElement('a')
      link.href = url
      const loaiName = selectedLoai === tracNghiemId ? 'trac_nghiem' : 'tu_luan'
      link.download = `template_import_${loaiName}.xlsx`
      link.click()
      window.URL.revokeObjectURL(url)
    } catch { alert('Không thể tải template') }
  }

  const isFormValid = selectedKhoa !== '' && selectedLoai !== ''
  const isTuLuan = selectedLoai === tuLuanId

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm transition-all duration-300">
      <div className="bg-white rounded-2xl shadow-2xl w-full max-w-lg border border-gray-100 overflow-hidden transform scale-100 transition-all duration-300">
        
        {/* Header */}
        <div className="flex items-center justify-between p-5 border-b border-gray-100 bg-gradient-to-r from-blue-50/50 to-indigo-50/50">
          <div className="flex items-center gap-3">
            <div className="p-2 bg-blue-600 text-white rounded-lg">
              <FileSpreadsheet className="h-5 w-5" />
            </div>
            <div>
              <h2 className="text-lg font-bold text-gray-800">Nhập câu hỏi từ file Excel</h2>
              <p className="text-xs text-gray-500">
                {previewQuestions ? 'Bước 2: Xem trước & thêm Đáp án mẫu' : 'Nhập hàng loạt câu hỏi một cách nhanh chóng'}
              </p>
            </div>
          </div>
          <Button variant="ghost" size="icon" onClick={onClose} className="rounded-full hover:bg-white transition-colors">
            <X className="h-5 w-5 text-gray-500" />
          </Button>
        </div>

        {/* STEP 2: Preview */}
        {previewQuestions ? (
          <PreviewStep
            questions={previewQuestions}
            onBack={() => setPreviewQuestions(null)}
            onConfirm={handleConfirmImport}
            isImporting={isImporting}
          />
        ) : (
          <>
            {/* STEP 1: Cấu hình + Upload */}
            <div className="p-6 space-y-6 max-h-[75vh] overflow-y-auto">
              
              {/* Bước 1: Chọn Khoa */}
              <div className="space-y-2.5 bg-gray-50/50 p-4 rounded-xl border border-gray-100">
                <label className="text-sm font-semibold text-gray-700 flex items-center gap-1.5">
                  <div className="flex items-center justify-center h-5 w-5 rounded-full bg-blue-100 text-blue-700 text-xs">1</div>
                  Khoa / Phòng Ban <span className="text-red-500">*</span>
                </label>
                {isDeptManager ? (
                  <div className="flex items-center gap-2 px-3 py-2.5 bg-white border border-blue-200 rounded-lg text-sm text-blue-800 font-medium">
                    <Building2 className="h-4 w-4 text-blue-500" /> {myKhoa}
                  </div>
                ) : (
                  <div className="relative">
                    <Building2 className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400" />
                    <select
                      className="w-full pl-9 pr-3 py-2.5 bg-white border border-gray-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 transition-all appearance-none"
                      value={selectedKhoa}
                      onChange={(e) => { setSelectedKhoa(e.target.value); setFile(null); setResult(null) }}
                    >
                      <option value="">-- Chọn Khoa / Phòng --</option>
                      {departments.map((dept) => (
                        <option key={dept.id} value={dept.departmentName}>
                          {dept.departmentName} ({dept.deptCode})
                        </option>
                      ))}
                    </select>
                  </div>
                )}
              </div>

              {/* Bước 2: Chọn Loại */}
              <div className="space-y-2.5 bg-gray-50/50 p-4 rounded-xl border border-gray-100">
                <label className="text-sm font-semibold text-gray-700 flex items-center gap-1.5">
                  <div className="flex items-center justify-center h-5 w-5 rounded-full bg-indigo-100 text-indigo-700 text-xs">2</div>
                  Loại câu hỏi <span className="text-red-500">*</span>
                </label>
                <div className="grid grid-cols-2 gap-3">
                  {(
                    [
                      {
                        id: tracNghiemId,
                        label: 'Trắc nghiệm',
                        // BUG FIX: Tailwind's build-time scanner only picks up class names that
                        // appear as literal strings in source - a template-built name like
                        // `border-${color}-500` is invisible to it. Full literal strings per
                        // option (not a `color` variable) so both variants actually ship in the
                        // compiled CSS instead of only the one that happens to coincidentally
                        // appear elsewhere in the codebase.
                        selectedClass: 'border-blue-500 bg-blue-50/50 text-blue-700 shadow-sm ring-2 ring-blue-500/20 ring-offset-1',
                        ribbonClass: 'bg-blue-500',
                      },
                      {
                        id: tuLuanId,
                        label: 'Tự luận',
                        selectedClass: 'border-indigo-500 bg-indigo-50/50 text-indigo-700 shadow-sm ring-2 ring-indigo-500/20 ring-offset-1',
                        ribbonClass: 'bg-indigo-500',
                      },
                    ] as const
                  ).map(({ id, label, selectedClass, ribbonClass }) => (
                    <div
                      key={label}
                      onClick={() => { setSelectedLoai(id); setFile(null); setResult(null) }}
                      className={`relative overflow-hidden border-2 rounded-xl p-3 cursor-pointer text-center transition-all duration-200 ${
                        selectedLoai === id
                          ? selectedClass
                          : 'border-gray-200 hover:border-gray-300 hover:bg-gray-50 text-gray-600'
                      }`}
                    >
                      {selectedLoai === id && (
                        <div className={`absolute top-0 right-0 w-8 h-8 ${ribbonClass} flex justify-end items-start p-1`} style={{ clipPath: 'polygon(0 0, 100% 0, 100% 100%)' }}>
                          <CheckCircle2 className="h-3 w-3 text-white absolute top-1 right-1" />
                        </div>
                      )}
                      <span className="font-semibold text-sm relative z-10">{label}</span>
                    </div>
                  ))}
                </div>
              </div>

              {/* Bước 3+4: Template & Upload */}
              {isFormValid ? (
                <div className="space-y-4 animate-in fade-in slide-in-from-bottom-2 duration-300">
                  <div className="bg-emerald-50/50 border border-emerald-200/60 rounded-xl p-4 flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3">
                    <div>
                      <p className="text-sm font-semibold text-emerald-800 flex items-center gap-1.5 mb-1">
                        <div className="flex items-center justify-center h-4 w-4 rounded-full bg-emerald-100 text-emerald-700 text-[10px]">3</div>
                        Tải file mẫu Excel
                      </p>
                      <p className="text-xs text-emerald-600/80 pl-6">
                        {isTuLuan
                          ? 'Dùng file mẫu để nhập liệu đúng cấu trúc (có cột Đáp án mẫu tùy chọn).'
                          : 'Vui lòng sử dụng template chuẩn để đảm bảo dữ liệu được import chính xác.'}
                      </p>
                    </div>
                    <Button variant="outline" size="sm" onClick={handleDownloadTemplate} className="shrink-0 bg-white border-emerald-200 text-emerald-700 hover:bg-emerald-50 hover:text-emerald-800 w-full sm:w-auto">
                      <Download className="h-3.5 w-3.5 mr-1.5" /> Tải mẫu (.xlsx)
                    </Button>
                  </div>

                  <div className="space-y-2">
                    <p className="text-sm font-semibold text-gray-700 flex items-center gap-1.5 mb-2">
                      <div className="flex items-center justify-center h-5 w-5 rounded-full bg-orange-100 text-orange-700 text-xs">4</div>
                      Chọn file Excel để nhập
                    </p>
                    <div
                      className={`group border-2 border-dashed rounded-xl p-8 text-center cursor-pointer transition-all duration-200 ${
                        file ? 'border-blue-500 bg-blue-50/30' : 'border-gray-300 hover:border-blue-400 hover:bg-blue-50/20'
                      }`}
                      onClick={() => inputRef.current?.click()}
                    >
                      <div className={`mx-auto w-12 h-12 rounded-full flex items-center justify-center mb-3 transition-colors ${
                        file ? 'bg-blue-100 text-blue-600' : 'bg-gray-100 text-gray-400 group-hover:bg-blue-50 group-hover:text-blue-500'
                      }`}>
                        <FileSpreadsheet className="h-6 w-6" />
                      </div>
                      {file ? (
                        <div className="space-y-1 animate-in zoom-in-95">
                          <p className="text-sm font-semibold text-blue-900 truncate max-w-[250px] mx-auto">{file.name}</p>
                          <p className="text-xs text-blue-600/70 font-medium">{(file.size / 1024).toFixed(1)} KB — Nhấp để đổi file</p>
                        </div>
                      ) : (
                        <div className="space-y-1">
                          <p className="text-sm font-semibold text-gray-700">Kéo thả hoặc click để chọn file</p>
                          <p className="text-xs text-gray-400">Hỗ trợ .xlsx, .xls</p>
                        </div>
                      )}
                      <input ref={inputRef} type="file" accept=".xlsx, .xls" className="hidden" onChange={handleFileChange} />
                    </div>
                  </div>
                </div>
              ) : (
                <div className="border border-yellow-200/60 rounded-xl p-4 bg-yellow-50/50 text-center flex flex-col items-center justify-center min-h-[140px] border-dashed">
                  <div className="h-10 w-10 rounded-full bg-yellow-100/50 flex items-center justify-center mb-2">
                    <AlertTriangle className="h-5 w-5 text-yellow-600" />
                  </div>
                  <p className="text-sm text-yellow-800 font-medium">Hoàn thành bước 1 & 2</p>
                  <p className="text-xs text-yellow-600/70 mt-1">Chọn khoa/phòng và loại câu hỏi để tải mẫu và upload file</p>
                </div>
              )}

              {/* Result Area */}
              {result && (
                <div className={`p-4 rounded-xl border animate-in slide-in-from-top-2 ${
                  result.success ? 'bg-green-50 border-green-200' : 'bg-red-50 border-red-200'
                }`}>
                  <div className="flex gap-3">
                    {result.success ? (
                      <div className="mt-0.5 rounded-full bg-green-100 p-1"><CheckCircle2 className="h-4 w-4 text-green-600" /></div>
                    ) : (
                      <div className="mt-0.5 rounded-full bg-red-100 p-1"><AlertTriangle className="h-4 w-4 text-red-600" /></div>
                    )}
                    <div className="flex-1">
                      <p className={`text-sm font-bold ${result.success ? 'text-green-800' : 'text-red-800'}`}>{result.message}</p>
                      {result.errors && result.errors.length > 0 && (
                        <div className="mt-2 pl-2 border-l-2 border-red-200">
                          <ul className="text-xs space-y-1.5 text-red-700/80">
                            {result.errors.slice(0, 5).map((err, i) => <li key={i}>{err}</li>)}
                            {result.errors.length > 5 && <li className="font-semibold italic mt-1">... và {result.errors.length - 5} lỗi khác.</li>}
                          </ul>
                        </div>
                      )}
                    </div>
                  </div>
                </div>
              )}
            </div>

            {/* Footer */}
            <div className="flex justify-end gap-2.5 p-4 border-t border-gray-100 bg-gray-50/50">
              <Button variant="outline" onClick={onClose} disabled={isUploading} className="rounded-lg text-sm bg-white hover:bg-gray-50 border-gray-200">
                Hủy bỏ
              </Button>
              {isTuLuan ? (
                <Button
                  onClick={handlePreview}
                  disabled={!file || !isFormValid || isUploading}
                  className="rounded-lg text-sm font-semibold bg-blue-600 hover:bg-blue-700 text-white shadow-sm disabled:opacity-50 min-w-[120px]"
                >
                  <Eye className="h-4 w-4 mr-1.5" />
                  {isUploading ? 'Đang đọc...' : 'Xem trước & Sửa'}
                </Button>
              ) : (
                <Button
                  onClick={handleDirectImport}
                  disabled={!file || !isFormValid || isUploading}
                  className="rounded-lg text-sm font-semibold bg-blue-600 hover:bg-blue-700 text-white shadow-sm disabled:opacity-50 min-w-[120px]"
                >
                  {isUploading ? 'Đang import...' : 'Bắt đầu import'}
                </Button>
              )}
            </div>
          </>
        )}
      </div>
    </div>
  )
}
