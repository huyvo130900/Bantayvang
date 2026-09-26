import { useRef, useState } from 'react'
import { usersApiExtended as usersApi } from '../api'
import { Button } from '@/components/ui/button'
import { X, Upload, Download, CheckCircle, AlertTriangle, FileSpreadsheet } from 'lucide-react'

interface ImportResult {
  success: number
  failed: number
  errors: string[]
}

interface ImportUsersDialogProps {
  open: boolean
  onClose: () => void
  onSuccess: () => void
}

export function ImportUsersDialog({ open, onClose, onSuccess }: ImportUsersDialogProps) {
  const fileRef = useRef<HTMLInputElement>(null)
  const [file, setFile] = useState<File | null>(null)
  const [importing, setImporting] = useState(false)
  const [result, setResult] = useState<ImportResult | null>(null)
  const [error, setError] = useState<string | null>(null)

  if (!open) return null

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const f = e.target.files?.[0] || null
    setFile(f)
    setResult(null)
    setError(null)
  }

  async function handleImport() {
    if (!file) return
    setImporting(true)
    setError(null)
    setResult(null)
    try {
      const res = await usersApi.importExcel(file)
      if (res.data.data) {
        // Luôn hiển thị result nếu có data (dù success=false)
        setResult(res.data.data as ImportResult)
        if ((res.data.data as ImportResult).success > 0) {
          onSuccess()
        }
      } else if (!res.data.success) {
        setError(res.data.message || 'Nhập tài khoản thất bại')
      } else {
        setResult(res.data.data as ImportResult)
      }
    } catch (err: any) {
      const errData = (err as { response?: { data?: { data?: ImportResult; message?: string } } })?.response?.data
      if (errData?.data) {
        // 400 response vẫn có thể kèm chi tiết lỗi từng dòng
        setResult(errData.data)
      } else {
        setError(errData?.message || 'Có lỗi khi nhập file')
      }
    } finally {
      setImporting(false)
    }
  }

  async function handleDownloadTemplate() {
    try {
      const res = await usersApi.downloadTemplate()
      const url = window.URL.createObjectURL(new Blob([res.data]))
      const a = document.createElement('a')
      a.href = url
      a.download = 'MauImportTaiKhoan.xlsx'
      a.click()
      window.URL.revokeObjectURL(url)
    } catch {
      alert('Không thể tải file mẫu')
    }
  }

  const handleClose = () => {
    setFile(null)
    setResult(null)
    setError(null)
    if (fileRef.current) fileRef.current.value = ''
    onClose()
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
      <div className="bg-white rounded-xl shadow-2xl w-full max-w-md flex flex-col max-h-[90vh] overflow-hidden">
        {/* Header */}
        <div className="flex items-center justify-between p-4 border-b shrink-0 bg-gray-50/50">
          <div className="flex items-center gap-2">
            <FileSpreadsheet className="h-5 w-5 text-green-600" />
            <h2 className="text-lg font-semibold text-gray-800">Nhập tài khoản từ file Excel</h2>
          </div>
          <Button variant="ghost" size="icon" onClick={handleClose} className="rounded-full hover:bg-gray-200">
            <X className="h-4 w-4" />
          </Button>
        </div>

        {/* Content body (Scrollable) */}
        <div className="p-5 space-y-4 overflow-y-auto flex-1">
          {/* Step 1: Download template */}
          <div className="bg-blue-50 border border-blue-200 rounded-xl p-4">
            <p className="text-sm font-semibold text-blue-800 mb-1">Bước 1 — Tải file mẫu Excel</p>
            <p className="text-xs text-blue-600 mb-3">
              File mẫu chứa đúng định dạng cột. Điền thông tin vào file mẫu rồi upload lên.
            </p>
            <Button
              variant="outline"
              size="sm"
              onClick={handleDownloadTemplate}
              className="border-blue-300 text-blue-700 hover:bg-blue-100"
            >
              <Download className="h-3.5 w-3.5 mr-1.5" />
              Tải file mẫu (.xlsx)
            </Button>
          </div>

          {/* Step 2: Upload */}
          <div>
            <p className="text-sm font-semibold text-gray-700 mb-2">Bước 2 — Chọn file đã điền thông tin</p>
            <div
              onClick={() => fileRef.current?.click()}
              className={`border-2 border-dashed rounded-xl p-6 text-center cursor-pointer transition-colors ${
                file
                  ? 'border-green-300 bg-green-50'
                  : 'border-gray-200 hover:border-primary/40 hover:bg-gray-50'
              }`}
            >
              <Upload className={`h-8 w-8 mx-auto mb-2 ${file ? 'text-green-500' : 'text-gray-300'}`} />
              {file ? (
                <>
                  <p className="text-sm font-semibold text-green-700">{file.name}</p>
                  <p className="text-xs text-green-500 mt-1">{(file.size / 1024).toFixed(1)} KB — Nhấn để đổi file</p>
                </>
              ) : (
                <>
                  <p className="text-sm text-gray-600">Nhấn để chọn file Excel (.xlsx) hoặc CSV (.csv)</p>
                  <p className="text-xs text-gray-400 mt-1">Hỗ trợ định dạng .xlsx, .csv</p>
                </>
              )}
              <input
                ref={fileRef}
                type="file"
                accept=".xlsx,.xls,.csv"
                className="hidden"
                onChange={handleFileChange}
              />
            </div>
          </div>

          {/* Columns info */}
          <div className="bg-gray-50 rounded-xl p-3 border">
            <p className="text-xs font-semibold text-gray-600 mb-2">Các cột trong file mẫu:</p>
            <div className="grid grid-cols-2 gap-1">
              {[
                'STT', 'Tài khoản *', 'Họ tên *',
                'Email', 'Số điện thoại', 'Khoa/phòng'
              ].map((col) => (
                <div key={col} className="flex items-center gap-1.5">
                  <span className="w-1.5 h-1.5 rounded-full bg-primary shrink-0" />
                  <span className="text-xs text-gray-600">{col}</span>
                </div>
              ))}
            </div>
            <p className="text-xs text-gray-400 mt-2">* Bắt buộc. Tài khoản dùng làm mã nhân viên đăng nhập. Mật khẩu mặc định là "123456". Vai trò mặc định là Thí sinh.</p>
          </div>

          {/* Error */}
          {error && (
            <div className="flex items-start gap-2 bg-red-50 border border-red-200 rounded-xl p-3">
              <AlertTriangle className="h-4 w-4 text-red-500 shrink-0 mt-0.5" />
              <p className="text-sm text-red-700">{error}</p>
            </div>
          )}

          {/* Result */}
          {result && (
            <div className={`rounded-xl p-4 border ${result.failed === 0 ? 'bg-green-50 border-green-200' : 'bg-yellow-50/70 border-yellow-200'}`}>
              <div className="flex items-center gap-2 mb-3">
                <CheckCircle className={`h-5 w-5 ${result.failed === 0 ? 'text-green-600' : 'text-yellow-600'}`} />
                <p className="text-sm font-semibold text-gray-800">Kết quả nhập dữ liệu</p>
              </div>
              <div className="grid grid-cols-2 gap-3 text-sm mb-3">
                <div className="bg-white rounded-lg p-3 text-center border border-green-100 shadow-sm">
                  <p className="text-2xl font-bold text-green-600">{result.success}</p>
                  <p className="text-xs font-medium text-gray-500 mt-0.5">Thành công</p>
                </div>
                <div className="bg-white rounded-lg p-3 text-center border border-red-100 shadow-sm">
                  <p className="text-2xl font-bold text-red-500">{result.failed}</p>
                  <p className="text-xs font-medium text-gray-500 mt-0.5">Thất bại</p>
                </div>
              </div>
              
              {result.failed > 0 && result.errors.length > 0 && (
                <div className="mt-3 space-y-1.5">
                  <p className="text-xs font-semibold text-red-700">Chi tiết lỗi các dòng thất bại:</p>
                  <div className="bg-white border border-red-100 rounded-lg p-2.5 max-h-48 overflow-y-auto space-y-1 shadow-inner">
                    {result.errors.map((e, i) => (
                      <div key={i} className="text-xs flex items-start gap-1.5 text-red-600 py-0.5 border-b border-red-50 last:border-0 last:pb-0">
                        <span className="inline-block w-1.5 h-1.5 rounded-full bg-red-400 mt-1.5 shrink-0" />
                        <span className="leading-relaxed">{e}</span>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="flex justify-end gap-2 p-4 border-t shrink-0 bg-gray-50/50">
          <Button variant="outline" onClick={handleClose} disabled={importing}>
            {result ? 'Đóng' : 'Hủy'}
          </Button>
          {!result && (
            <Button onClick={handleImport} disabled={!file || importing}>
              {importing ? (
                <span className="flex items-center gap-2">
                  <span className="animate-spin h-4 w-4 border-2 border-white border-t-transparent rounded-full" />
                  Đang nhập...
                </span>
              ) : (
                <>
                  <Upload className="h-4 w-4 mr-1.5" />
                  Nhập tài khoản
                </>
              )}
            </Button>
          )}
        </div>
      </div>
    </div>
  )
}
