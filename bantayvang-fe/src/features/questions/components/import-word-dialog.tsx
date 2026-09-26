import { useState, useRef, useEffect } from 'react'
import { Button } from '@/components/ui/button'
import { X, FileText, Building2, HelpCircle, CheckCircle2, AlertTriangle, Download, Eye, ArrowLeft } from 'lucide-react'
import { questionsApi } from '../api'
import { departmentApi } from '@/features/departments/api'
import type { DepartmentDto } from '@/features/departments/types'
import type { QuestionDto } from '../types'
import { useAppSelector } from '@/app/hooks'

interface ImportWordDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
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
export function ImportWordDialog({ open, onOpenChange, onSuccess }: ImportWordDialogProps) {
  const [file, setFile] = useState<File | null>(null)
  const [isUploading, setIsUploading] = useState(false)
  const [isImporting, setIsImporting] = useState(false)
  const [departments, setDepartments] = useState<DepartmentDto[]>([])
  const [selectedKhoa, setSelectedKhoa] = useState('')
  const [selectedLoai, setSelectedLoai] = useState<number | null>(null)
  const [result, setResult] = useState<ImportResult | null>(null)
  const [previewQuestions, setPreviewQuestions] = useState<QuestionDto[] | null>(null)
  const inputRef = useRef<HTMLInputElement>(null)

  const currentUser = useAppSelector((state) => state.auth.user)
  const isDeptManager =
    currentUser?.role === 'DeptManager' || currentUser?.roleName === 'DeptManager'
  const myKhoa = currentUser?.deptManagerDeptName || currentUser?.department || null
  const questionTypes = useAppSelector((state) => state.questions.questionTypes)

  const tracNghiemType = questionTypes.find(
    (t) => (t.categoryName || '').toLowerCase().includes('tn') ||
             (t.description || '').toLowerCase().includes('trắc nghiệm')
  )
  const tuLuanType = questionTypes.find(
    (t) => (t.categoryName || '').toLowerCase().includes('tl') ||
             (t.description || '').toLowerCase().includes('tự luận') ||
             (t.categoryName || '').toLowerCase().includes('tự luận')
  )
  const tracNghiemId = tracNghiemType?.id ?? null
  const tuLuanId = tuLuanType?.id ?? null
  const isFormValid = selectedKhoa !== '' && selectedLoai !== null
  const isTuLuan = selectedLoai === tuLuanId

  useEffect(() => {
    if (!open) return
    setFile(null); setResult(null); setSelectedLoai(null); setPreviewQuestions(null)
    if (isDeptManager && myKhoa) setSelectedKhoa(myKhoa)
    else setSelectedKhoa('')
    if (!isDeptManager) {
      departmentApi.getAll({ status: true, pageSize: 100 })
        .then((res) => setDepartments(res.data?.data || []))
        .catch(() => setDepartments([]))
    }
  }, [open]) // eslint-disable-line react-hooks/exhaustive-deps

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const f = e.target.files?.[0]
    if (!f) return
    setFile(f); setResult(null); setPreviewQuestions(null)
    e.target.value = ''
  }

  // Bước 1: Preview (chỉ áp dụng cho Tự luận)
  async function handlePreview() {
    if (!file || !selectedKhoa || selectedLoai === null) return
    setIsUploading(true)
    setResult(null)
    try {
      const res = await questionsApi.previewWord(file, selectedKhoa, selectedLoai)
      if (res.data.success && res.data.data) {
        setPreviewQuestions(res.data.data)
      } else {
        setResult({ success: false, message: res.data.message || 'Không đọc được file' })
      }
    } catch (err: any) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message || 'Có lỗi khi đọc file'
      setResult({ success: false, message: msg })
    } finally {
      setIsUploading(false)
    }
  }

  // Bước 2: Import thật (kèm đáp án mẫu đã chỉnh)
  async function handleConfirmImport(editedQuestions: QuestionDto[]) {
    if (selectedLoai === null) return
    setIsImporting(true)
    try {
      const payload = editedQuestions.map((q) => ({
        content: q.content ?? '',
        questionCategoryId: selectedLoai,
        difficulty: q.difficulty === 'Khó' ? '3' : q.difficulty === 'Trung bình' ? '2' : '1',
        department: selectedKhoa,
        suggestedAnswer: q.suggestedAnswer || undefined,
        options: q.options?.map((o) => ({
          content: o.content ?? '',
          isCorrect: o.isCorrect ?? false,
          orderIndex: o.orderIndex ?? 0,
        })) ?? [],
      }))
      const res = await questionsApi.importFromPreview(payload, selectedKhoa, selectedLoai)
      setPreviewQuestions(null)
      setResult({ success: res.data.success, message: res.data.message || res.data.data as any as string })
      if (res.data.success) onSuccess()
    } catch (err: any) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message || 'Có lỗi khi import'
      setResult({ success: false, message: msg })
      setPreviewQuestions(null)
    } finally {
      setIsImporting(false)
    }
  }

  // Import trực tiếp (Trắc nghiệm không cần preview)
  async function handleDirectImport() {
    if (!file || !selectedKhoa || selectedLoai === null) return
    setIsUploading(true)
    setResult(null)
    try {
      const response = await questionsApi.importWord(file, selectedKhoa, selectedLoai)
      setResult({ success: response.data.success, message: response.data.message, errors: [] })
      if (response.data.success) onSuccess()
    } catch (err: any) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message || 'Có lỗi khi nhập câu hỏi'
      setResult({ success: false, message: msg })
    } finally {
      setIsUploading(false)
    }
  }

  async function handleDownloadTemplate() {
    try {
      const response = await questionsApi.downloadWordTemplate()
      const url = window.URL.createObjectURL(new Blob([response.data]))
      const link = document.createElement('a')
      link.href = url; link.download = 'Mau_Import_CauHoi_TrucNghiem.docx'; link.click()
      window.URL.revokeObjectURL(url)
    } catch { alert('Không thể tải file mẫu') }
  }

  if (!open) return null

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm">
      <div className="bg-white rounded-2xl shadow-2xl w-full max-w-lg mx-4 flex flex-col overflow-hidden animate-fadeIn">

        {/* Header */}
        <div className="flex items-center justify-between p-5 border-b border-gray-100">
          <div className="flex items-center gap-3">
            <div className="p-2 bg-blue-100 rounded-xl">
              <FileText className="h-5 w-5 text-blue-600" />
            </div>
            <div>
              <h2 className="text-lg font-bold text-gray-800">Nhập câu hỏi từ Word</h2>
              <p className="text-xs text-gray-500">
                {previewQuestions ? 'Bước 2: Xem trước & thêm Đáp án mẫu' : 'Hỗ trợ file .docx theo chuẩn Azota'}
              </p>
            </div>
          </div>
          <Button variant="ghost" size="icon" onClick={() => onOpenChange(false)} className="rounded-full hover:bg-gray-200/50">
            <X className="h-5 w-5 text-gray-400" />
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
            <div className="p-6 space-y-5 max-h-[75vh] overflow-y-auto">
              {/* Chọn Khoa */}
              <div className="space-y-2">
                <label className="text-sm font-semibold text-gray-700 flex items-center gap-1.5">
                  <Building2 className="h-4 w-4 text-blue-600" />
                  1. Chọn Khoa / Phòng Ban <span className="text-red-500">*</span>
                </label>
                {isDeptManager ? (
                  <div className="flex items-center gap-2 px-3 py-2.5 bg-blue-50 border border-blue-200 rounded-lg text-sm text-blue-800 font-medium">
                    <Building2 className="h-4 w-4 text-blue-500" /> {myKhoa}
                  </div>
                ) : (
                  <select
                    className="w-full border border-gray-200 rounded-lg px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-blue-400/40 bg-white"
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
                )}
              </div>

              {/* Chọn loại */}
              <div className="space-y-2">
                <label className="text-sm font-semibold text-gray-700 flex items-center gap-1.5">
                  <HelpCircle className="h-4 w-4 text-indigo-600" />
                  2. Chọn hình thức câu hỏi <span className="text-red-500">*</span>
                </label>
                <div className="grid grid-cols-2 gap-3">
                  {[
                    { id: tracNghiemId, label: 'Trắc nghiệm', desc: 'Nhận diện đáp án A-D bằng Bold/Underline', icon: <HelpCircle className="h-5 w-5" /> },
                    { id: tuLuanId, label: 'Tự luận', desc: 'Có thể thêm Đáp án mẫu trước khi import', icon: <FileText className="h-5 w-5" /> },
                  ].map(({ id, label, desc, icon }) => (
                    <div
                      key={label}
                      onClick={() => { setSelectedLoai(id); setFile(null); setResult(null) }}
                      className={`group border-2 rounded-xl p-4 cursor-pointer text-center transition-all duration-300 ${
                        selectedLoai === id
                          ? 'border-blue-600 bg-blue-50/40 text-blue-900 shadow-sm'
                          : 'border-gray-200 hover:border-blue-300 hover:bg-gray-50/50'
                      }`}
                    >
                      <div className={`mx-auto p-2.5 rounded-lg w-fit transition-colors duration-300 ${
                        selectedLoai === id ? 'bg-blue-600 text-white' : 'bg-gray-100 text-gray-500 group-hover:bg-blue-100 group-hover:text-blue-600'
                      }`}>{icon}</div>
                      <h3 className="font-bold text-sm mt-3">{label}</h3>
                      <p className="text-[11px] text-gray-400 group-hover:text-gray-500 mt-1">{desc}</p>
                    </div>
                  ))}
                </div>
              </div>

              {/* Upload + Template */}
              {isFormValid ? (
                <div className="space-y-4 pt-1">
                  <div className="bg-blue-50 border border-blue-200 rounded-xl p-4 text-left">
                    <p className="text-sm font-semibold text-blue-800 mb-2">Bước 3 — Tải file mẫu Word</p>
                    <p className="text-xs text-blue-600 mb-3">
                      {isTuLuan
                        ? 'Câu tự luận: mỗi câu 1 dòng "Câu N: ...". Bạn cũng có thể thêm dòng "Đáp án mẫu: ..." trong file.'
                        : 'File mẫu định dạng chuẩn Azota. Đánh dấu đáp án đúng bằng Bôi đậm hoặc Gạch chân chữ A/B/C/D.'}
                    </p>
                    <Button variant="outline" size="sm" onClick={handleDownloadTemplate} className="border-blue-300 text-blue-700 hover:bg-blue-100 bg-white">
                      <Download className="h-3.5 w-3.5 mr-1.5" /> Tải file mẫu (.docx)
                    </Button>
                  </div>

                  <div>
                    <p className="text-sm font-semibold text-gray-700 mb-2">Bước 4 — Chọn file Word để nhập</p>
                    <div
                      className={`border-2 border-dashed rounded-xl p-8 text-center cursor-pointer transition-all duration-300 ${
                        file ? 'border-blue-500 bg-blue-50/10' : 'border-gray-300 hover:border-blue-400 hover:bg-blue-50/5'
                      }`}
                      onClick={() => inputRef.current?.click()}
                    >
                      <FileText className={`h-10 w-10 mx-auto mb-3 transition-colors ${file ? 'text-blue-600' : 'text-gray-400'}`} />
                      {file ? (
                        <div className="space-y-1">
                          <p className="text-sm font-semibold text-gray-800 truncate max-w-xs mx-auto">{file.name}</p>
                          <p className="text-xs text-gray-400">{(file.size / 1024).toFixed(1)} KB — Nhấp để chọn file khác</p>
                        </div>
                      ) : (
                        <div>
                          <p className="text-sm font-medium text-gray-700">Chọn file Word (.docx)</p>
                          <p className="text-xs text-gray-400 mt-1">Hỗ trợ định dạng .docx</p>
                        </div>
                      )}
                      <input ref={inputRef} type="file" accept=".docx,application/vnd.openxmlformats-officedocument.wordprocessingml.document" className="hidden" onChange={handleFileChange} />
                    </div>
                  </div>
                </div>
              ) : (
                <div className="border border-yellow-100 rounded-xl p-4 bg-yellow-50/50 text-center flex flex-col items-center justify-center">
                  <AlertTriangle className="h-5 w-5 text-yellow-600 mb-1" />
                  <p className="text-xs text-yellow-800 font-medium">
                    Vui lòng chọn Khoa/Phòng và Loại câu hỏi ở bước trên để mở khóa khu vực tải file mẫu và chọn file.
                  </p>
                </div>
              )}

              {/* Kết quả */}
              {result && (
                <div className={`p-3.5 rounded-xl text-sm border ${result.success ? 'bg-green-50 text-green-800 border-green-200' : 'bg-red-50 text-red-800 border-red-200'}`}>
                  <div className="flex gap-2">
                    {result.success ? <CheckCircle2 className="h-4 w-4 text-green-600 shrink-0 mt-0.5" /> : <AlertTriangle className="h-4 w-4 shrink-0 mt-0.5 text-red-600" />}
                    <div>
                      <p className="font-semibold text-xs">{result.message}</p>
                      {result.errors && result.errors.length > 0 && (
                        <ul className="list-disc list-inside text-[11px] space-y-1 text-gray-600 mt-1.5 pl-1">
                          {result.errors.slice(0, 5).map((err, i) => <li key={i}>{err}</li>)}
                          {result.errors.length > 5 && <li className="text-gray-400 font-medium">... và {result.errors.length - 5} lỗi khác.</li>}
                        </ul>
                      )}
                    </div>
                  </div>
                </div>
              )}
            </div>

            {/* Footer */}
            <div className="flex justify-end gap-2 p-4 border-t border-gray-100 bg-gray-50/50">
              <Button variant="outline" onClick={() => onOpenChange(false)} disabled={isUploading} className="rounded-lg font-medium text-xs">Hủy bỏ</Button>
              {isTuLuan ? (
                <Button
                  onClick={handlePreview}
                  disabled={!file || !isFormValid || isUploading}
                  className="bg-blue-600 hover:bg-blue-700 text-white rounded-lg font-bold text-xs shadow-md disabled:opacity-50"
                >
                  <Eye className="h-3.5 w-3.5 mr-1.5" />
                  {isUploading ? 'Đang đọc file...' : 'Xem trước & thêm Đáp án mẫu'}
                </Button>
              ) : (
                <Button
                  onClick={handleDirectImport}
                  disabled={!file || !isFormValid || isUploading}
                  className="bg-blue-600 hover:bg-blue-700 text-white rounded-lg font-bold text-xs shadow-md disabled:opacity-50"
                >
                  {isUploading ? 'Đang nhập dữ liệu...' : 'Bắt đầu nhập'}
                </Button>
              )}
            </div>
          </>
        )}
      </div>
    </div>
  )
}
