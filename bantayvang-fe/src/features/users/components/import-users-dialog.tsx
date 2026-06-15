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

  const handleImport = async () => {
    if (!file) return
    setImporting(true)
    setError(null)
    setResult(null)
    try {
      const res = await usersApi.importExcel(file)
      if (res.data.success && res.data.data) {
        setResult(res.data.data as ImportResult)
        if ((res.data.data as ImportResult).success > 0) {
          onSuccess()
        }
      } else {
        setError(res.data.message || 'Nhập tài khoản thất bại')
      }
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message
        || 'Có lỗi khi nhập file'
      setError(msg)
    } finally {
      setImporting(false)
    }
  }

  const handleDownloadTemplate = async () => {
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
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="bg-white rounded-xl shadow-2xl w-full max-w-md mx-4">
        {/* Header */}
        <div className="flex items-center justify-between p-4 border-b">
          <div className="flex items-center gap-2">
            <FileSpreadsheet className="h-5 w-5 text-green-600" />
            <h2 className="text-lg font-semibold">Nhập tài khoản từ file Excel</h2>
          </div>
          <Button variant="ghost" size="icon" onClick={handleClose}>
            <X className="h-4 w-4" />
          </Button>
        </div>

        <div className="p-5 space-y-4">
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
                  <p className="text-sm text-gray-600">Nhấn để chọn file Excel (.xlsx)</p>
                  <p className="text-xs text-gray-400 mt-1">Chỉ hỗ trợ định dạng .xlsx</p>
                </>
              )}
              <input
                ref={fileRef}
                type="file"
                accept=".xlsx,.xls"
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
                'maNhanVien *', 'matKhau *', 'hoTen *',
                'chucDanh', 'khoaPhong', 'vai trò (1-3)',
              ].map((col) => (
                <div key={col} className="flex items-center gap-1.5">
                  <span className="w-1.5 h-1.5 rounded-full bg-primary shrink-0" />
                  <span className="text-xs text-gray-600">{col}</span>
                </div>
              ))}
            </div>
            <p className="text-xs text-gray-400 mt-2">* Bắt buộc. Vai trò mặc định là 3 (thí sinh) nếu để trống. Vai trò: 1 = quản trị viên, 2 = quản lý khoa, 3 = thí sinh</p>
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
            <div className={`rounded-xl p-4 border ${result.failed === 0 ? 'bg-green-50 border-green-200' : 'bg-yellow-50 border-yellow-200'}`}>
              <div className="flex items-center gap-2 mb-2">
                <CheckCircle className="h-4 w-4 text-green-600" />
                <p className="text-sm font-semibold text-gray-800">Kết quả nhập dữ liệu</p>
              </div>
              <div className="grid grid-cols-2 gap-2 text-sm mb-2">
                <div className="bg-white rounded-lg p-2.5 text-center border">
                  <p className="text-xl font-bold text-green-600">{result.success}</p>
                  <p className="text-xs text-gray-500">Thành công</p>
                </div>
                <div className="bg-white rounded-lg p-2.5 text-center border">
                  <p className="text-xl font-bold text-red-500">{result.failed}</p>
                  <p className="text-xs text-gray-500">Thất bại</p>
                </div>
              </div>
              {result.errors.length > 0 && (
                <div className="bg-white border border-red-100 rounded-lg p-2 max-h-32 overflow-y-auto">
                  {result.errors.map((e, i) => (
                    <p key={i} className="text-xs text-red-600 py-0.5">{e}</p>
                  ))}
                </div>
              )}
            </div>
          )}

          {/* Footer */}
          <div className="flex justify-end gap-2 pt-2 border-t">
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
    </div>
  )
}
