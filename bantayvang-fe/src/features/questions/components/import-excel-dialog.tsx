import { useState, useRef, useEffect } from 'react'
import { Button } from '@/components/ui/button'
import { X, FileSpreadsheet, Building2, HelpCircle, FileText, CheckCircle2, AlertTriangle, Download } from 'lucide-react'
import { questionsApi } from '../api'
import { departmentApi } from '@/features/departments/api'
import type { DepartmentDto } from '@/features/departments/types'
import { useAppSelector } from '@/app/hooks'
import { ROLES } from '@/lib/constants'

interface ImportExcelDialogProps {
  open: boolean
  onClose: () => void
  onSuccess: () => void
}

export function ImportExcelDialog({ open, onClose, onSuccess }: ImportExcelDialogProps) {
  const [file, setFile] = useState<File | null>(null)
  const [isUploading, setIsUploading] = useState(false)
  const [departments, setDepartments] = useState<DepartmentDto[]>([])
  const [selectedKhoa, setSelectedKhoa] = useState<string>('')
  const [selectedLoai, setSelectedLoai] = useState<number | ''>('')
  const [result, setResult] = useState<{ success: boolean; message: string; errors?: string[] } | null>(null)
  const inputRef = useRef<HTMLInputElement>(null)

  const currentUser = useAppSelector((state) => state.auth.user)
  const questionTypes = useAppSelector((state) => state.questions.questionTypes)
  
  const isDeptManager =
    currentUser?.role === ROLES.DEPT_MANAGER ||
    currentUser?.tenVaiTro === 'DeptManager'
  const myKhoa = currentUser?.tenKhoaQuanLy || currentUser?.department || null

  // Tìm ID động của loại câu hỏi từ Redux (DB đang lưu categoryName là TN/TL, moTa chứa tên đầy đủ)
  const tracNghiemType = questionTypes.find(
    (t) => {
      const name = (t.categoryName || '').toLowerCase();
      const desc = (t.moTa || '').toLowerCase();
      return name === 'tn' || name.includes('trắc nghiệm') || name.includes('trac nghiem') || desc.includes('trắc nghiệm') || desc.includes('trac nghiem');
    }
  )
  const tuLuanType = questionTypes.find(
    (t) => {
      const name = (t.categoryName || '').toLowerCase();
      const desc = (t.moTa || '').toLowerCase();
      return name === 'tl' || name.includes('tự luận') || name.includes('tu luan') || desc.includes('tự luận') || desc.includes('tu luan');
    }
  )

  const tracNghiemId = tracNghiemType?.id || 1
  const tuLuanId = tuLuanType?.id || 2

  useEffect(() => {
    if (open) {
      // Load departments
      departmentApi.getAll({ status: true, pageSize: 100 })
        .then((res) => setDepartments(res.data?.data || []))
        .catch(() => {})

      if (isDeptManager && myKhoa) {
        setSelectedKhoa(myKhoa)
      } else {
        setSelectedKhoa('')
      }
      setSelectedLoai('')
      setFile(null)
      setResult(null)
    }
  }, [open, isDeptManager, myKhoa])

  if (!open) return null

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const selected = e.target.files?.[0]
    if (selected) {
      setFile(selected)
      setResult(null)
    }
  }

  const handleImport = async () => {
    if (!file || !selectedKhoa || !selectedLoai) return
    setIsUploading(true)
    setResult(null)
    try {
      const response = await questionsApi.importExcel(file, selectedKhoa, selectedLoai)
      setResult({
        success: response.data.success,
        message: response.data.message,
        errors: (response.data as any).errors,
      })
      if (response.data.success) {
        onSuccess()
      }
    } catch {
      setResult({ success: false, message: 'Lỗi kết nối server khi import file' })
    } finally {
      setIsUploading(false)
    }
  }

  const handleDownloadTemplate = async () => {
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
    } catch {
      alert('Không thể tải template')
    }
  }

  const isFormValid = selectedKhoa !== '' && selectedLoai !== ''

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
              <p className="text-xs text-gray-500">Nhập danh sách câu hỏi hàng loạt từ file Excel</p>
            </div>
          </div>
          <Button variant="ghost" size="icon" onClick={onClose} className="rounded-full hover:bg-gray-200/50">
            <X className="h-5 w-5 text-gray-400" />
          </Button>
        </div>

        {/* Content */}
        <div className="p-6 space-y-5 max-h-[75vh] overflow-y-auto">
          
          {/* Bước 1: Chọn Khoa/Phòng */}
          <div className="space-y-2">
            <label className="text-sm font-semibold text-gray-700 flex items-center gap-1.5">
              <Building2 className="h-4 w-4 text-blue-600" />
              1. Chọn Khoa / Phòng Ban <span className="text-red-500">*</span>
            </label>
            
            {isDeptManager ? (
              <div className="flex items-center gap-2 px-3 py-2.5 bg-gray-50 border border-gray-200 rounded-lg text-sm text-gray-700 font-medium">
                <CheckCircle2 className="h-4 w-4 text-green-600 shrink-0" />
                <span>Bạn đang quản lý khoa: <strong>{myKhoa}</strong></span>
              </div>
            ) : (
              <select
                className="w-full h-11 rounded-lg border border-gray-200 bg-white px-3 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500/30 focus:border-blue-500 transition-all cursor-pointer"
                value={selectedKhoa}
                onChange={(e) => {
                  setSelectedKhoa(e.target.value)
                  setFile(null)
                  setResult(null)
                }}
              >
                <option value="">-- Click để chọn Khoa / Phòng --</option>
                {departments.map((dept) => (
                  <option key={dept.id} value={dept.departmentName}>
                    {dept.departmentName} ({dept.maKhoa})
                  </option>
                ))}
              </select>
            )}
          </div>

          {/* Bước 2: Chọn Loại Câu Hỏi */}
          <div className="space-y-2">
            <label className="text-sm font-semibold text-gray-700 flex items-center gap-1.5">
              <HelpCircle className="h-4 w-4 text-indigo-600" />
              2. Chọn hình thức câu hỏi <span className="text-red-500">*</span>
            </label>
            
            <div className="grid grid-cols-2 gap-3">
              {/* Thẻ Trắc nghiệm */}
              <div
                onClick={() => {
                  setSelectedLoai(tracNghiemId)
                  setFile(null)
                  setResult(null)
                }}
                className={`group border-2 rounded-xl p-4 cursor-pointer text-center transition-all duration-300 ${
                  selectedLoai === tracNghiemId
                    ? 'border-blue-600 bg-blue-50/40 text-blue-900 shadow-sm'
                    : 'border-gray-200 hover:border-blue-300 hover:bg-gray-50/50'
                }`}
              >
                <div className={`mx-auto p-2.5 rounded-lg w-fit transition-colors duration-300 ${
                  selectedLoai === tracNghiemId ? 'bg-blue-600 text-white' : 'bg-gray-100 text-gray-500 group-hover:bg-blue-100 group-hover:text-blue-600'
                }`}>
                  <HelpCircle className="h-5 w-5" />
                </div>
                <h3 className="font-bold text-sm mt-3">Trắc nghiệm</h3>
                <p className="text-[11px] text-gray-400 group-hover:text-gray-500 mt-1">Mẫu 7 cột, có Đáp án A-D và đáp án đúng</p>
              </div>

              {/* Thẻ Tự luận */}
              <div
                onClick={() => {
                  setSelectedLoai(tuLuanId)
                  setFile(null)
                  setResult(null)
                }}
                className={`group border-2 rounded-xl p-4 cursor-pointer text-center transition-all duration-300 ${
                  selectedLoai === tuLuanId
                    ? 'border-green-600 bg-green-50/40 text-green-900 shadow-sm'
                    : 'border-gray-200 hover:border-green-300 hover:bg-gray-50/50'
                }`}
              >
                <div className={`mx-auto p-2.5 rounded-lg w-fit transition-colors duration-300 ${
                  selectedLoai === tuLuanId ? 'bg-green-600 text-white' : 'bg-gray-100 text-gray-500 group-hover:bg-green-100 group-hover:text-green-600'
                }`}>
                  <FileText className="h-5 w-5" />
                </div>
                <h3 className="font-bold text-sm mt-3">Tự luận</h3>
                <p className="text-[11px] text-gray-400 group-hover:text-gray-500 mt-1">Mẫu 3 cột (Nội dung, Điểm, Độ khó)</p>
              </div>
            </div>
          </div>

          {/* Hộp tải file mẫu và Drag-Drop */}
          {isFormValid ? (
            <div className="space-y-4 pt-1 animate-fadeIn">
              
              {/* Bước 3: Tải file mẫu */}
              <div className="bg-blue-50 border border-blue-200 rounded-xl p-4 text-left">
                <p className="text-sm font-semibold text-blue-800 mb-1">Bước 3 — Tải file mẫu Excel</p>
                <p className="text-xs text-blue-600 mb-3">
                  Tải file mẫu của hình thức đã chọn, điền câu hỏi rồi upload lên.
                </p>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={handleDownloadTemplate}
                  className="border-blue-300 text-blue-700 hover:bg-blue-100 bg-white"
                >
                  <Download className="h-3.5 w-3.5 mr-1.5" />
                  Tải file mẫu Excel (.xlsx)
                </Button>
              </div>

              {/* Vùng chọn file */}
              <div
                className={`border-2 border-dashed rounded-xl p-8 text-center cursor-pointer transition-all duration-300 ${
                  file 
                    ? 'border-blue-500 bg-blue-50/10' 
                    : 'border-gray-300 hover:border-blue-400 hover:bg-blue-50/5'
                }`}
                onClick={() => inputRef.current?.click()}
              >
                <FileSpreadsheet className={`h-10 w-10 mx-auto mb-3 transition-colors ${
                  file ? 'text-blue-600' : 'text-gray-400'
                }`} />
                
                {file ? (
                  <div className="space-y-1">
                    <p className="text-sm font-semibold text-gray-800 truncate max-w-xs mx-auto">{file.name}</p>
                    <p className="text-xs text-gray-400">
                      {(file.size / 1024).toFixed(1)} KB — Nhấp để chọn file khác
                    </p>
                  </div>
                ) : (
                  <div>
                    <p className="text-sm font-medium text-gray-700">Chọn file Excel nhập câu hỏi</p>
                    <p className="text-xs text-gray-400 mt-1">Hỗ trợ định dạng .xlsx, .xls</p>
                  </div>
                )}
                
                <input
                  ref={inputRef}
                  type="file"
                  accept=".xlsx,.xls"
                  className="hidden"
                  onChange={handleFileChange}
                />
              </div>
            </div>
          ) : (
            <div className="border border-yellow-100 rounded-xl p-4 bg-yellow-50/50 text-center flex flex-col items-center justify-center">
              <AlertTriangle className="h-5 w-5 text-yellow-600 mb-1" />
              <p className="text-xs text-yellow-800 font-medium">
                Vui lòng chọn Khoa/Phòng và Loại câu hỏi ở bước trên để mở khóa khu vực tải file mẫu và chọn file Excel.
              </p>
            </div>
          )}

          {/* Kết quả nhập */}
          {result && (
            <div
              className={`p-3.5 rounded-xl text-sm border animate-fadeIn ${
                result.success
                  ? 'bg-green-50 text-green-800 border-green-200'
                  : result.errors?.length
                  ? 'bg-yellow-50 text-yellow-800 border-yellow-200'
                  : 'bg-red-50 text-red-800 border-red-200'
              }`}
            >
              <div className="flex gap-2">
                {result.success ? (
                  <CheckCircle2 className="h-4 w-4 text-green-600 shrink-0 mt-0.5" />
                ) : (
                  <AlertTriangle className="h-4 w-4 shrink-0 mt-0.5 text-orange-600" />
                )}
                <div className="space-y-1">
                  <p className="font-semibold text-xs">{result.message}</p>
                  {result.errors && result.errors.length > 0 && (
                    <ul className="list-disc list-inside text-[11px] space-y-1 text-gray-600 mt-1.5 pl-1">
                      {result.errors.slice(0, 5).map((err, i) => (
                        <li key={i} className="truncate max-w-sm">{err}</li>
                      ))}
                      {result.errors.length > 5 && (
                        <li className="text-gray-400 font-medium">... và {result.errors.length - 5} dòng lỗi khác.</li>
                      )}
                    </ul>
                  )}
                </div>
              </div>
            </div>
          )}
        </div>

        {/* Footer Actions */}
        <div className="flex justify-end gap-2 p-4 border-t border-gray-100 bg-gray-50/50">
          <Button variant="outline" onClick={onClose} disabled={isUploading} className="rounded-lg font-medium text-xs">
            Hủy bỏ
          </Button>
          <Button 
            onClick={handleImport} 
            disabled={!file || !isFormValid || isUploading}
            className="bg-blue-600 hover:bg-blue-700 text-white rounded-lg font-bold text-xs shadow-md shadow-blue-500/10 disabled:opacity-50 transition-all duration-300"
          >
            {isUploading ? 'Đang nhập dữ liệu...' : 'Bắt đầu nhập'}
          </Button>
        </div>
      </div>
    </div>
  )
}
