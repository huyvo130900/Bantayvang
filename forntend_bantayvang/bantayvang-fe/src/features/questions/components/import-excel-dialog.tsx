import { useState, useRef } from 'react'
import { Button } from '@/components/ui/button'
import { X, Download, FileSpreadsheet } from 'lucide-react'
import { questionsApi } from '../api'

interface ImportExcelDialogProps {
  open: boolean
  onClose: () => void
  onSuccess: () => void
}

export function ImportExcelDialog({ open, onClose, onSuccess }: ImportExcelDialogProps) {
  const [file, setFile] = useState<File | null>(null)
  const [isUploading, setIsUploading] = useState(false)
  const [result, setResult] = useState<{ message: string; errors?: string[] } | null>(null)
  const inputRef = useRef<HTMLInputElement>(null)

  if (!open) return null

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const selected = e.target.files?.[0]
    if (selected) {
      setFile(selected)
      setResult(null)
    }
  }

  const handleImport = async () => {
    if (!file) return
    setIsUploading(true)
    setResult(null)
    try {
      const response = await questionsApi.importExcel(file)
      setResult({
        message: response.data.message,
        errors: response.data.errors,
      })
      if (response.data.success) {
        onSuccess()
      }
    } catch {
      setResult({ message: 'Lỗi import file' })
    } finally {
      setIsUploading(false)
    }
  }

  const handleDownloadTemplate = async () => {
    try {
      const response = await questionsApi.downloadTemplate()
      const url = window.URL.createObjectURL(new Blob([response.data]))
      const link = document.createElement('a')
      link.href = url
      link.download = 'import_cau_hoi_template.xlsx'
      link.click()
      window.URL.revokeObjectURL(url)
    } catch {
      // handle error
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-md">
        <div className="flex items-center justify-between p-4 border-b">
          <h2 className="text-lg font-semibold">Import câu hỏi từ Excel</h2>
          <Button variant="ghost" size="icon" onClick={onClose}>
            <X className="h-4 w-4" />
          </Button>
        </div>

        <div className="p-4 space-y-4">
          <Button variant="outline" size="sm" onClick={handleDownloadTemplate} className="w-full">
            <Download className="h-4 w-4 mr-2" />
            Tải template Excel mẫu
          </Button>

          <div
            className="border-2 border-dashed rounded-lg p-6 text-center cursor-pointer hover:border-primary/50 transition-colors"
            onClick={() => inputRef.current?.click()}
          >
            <FileSpreadsheet className="h-8 w-8 mx-auto text-gray-400 mb-2" />
            {file ? (
              <p className="text-sm font-medium text-gray-700">{file.name}</p>
            ) : (
              <p className="text-sm text-gray-500">Click để chọn file Excel (.xlsx)</p>
            )}
            <input
              ref={inputRef}
              type="file"
              accept=".xlsx,.xls"
              className="hidden"
              onChange={handleFileChange}
            />
          </div>

          {result && (
            <div className={`p-3 rounded-md text-sm ${result.errors?.length ? 'bg-yellow-50 text-yellow-800' : 'bg-green-50 text-green-800'}`}>
              <p className="font-medium">{result.message}</p>
              {result.errors && result.errors.length > 0 && (
                <ul className="mt-1 list-disc list-inside text-xs">
                  {result.errors.slice(0, 5).map((err, i) => (
                    <li key={i}>{err}</li>
                  ))}
                  {result.errors.length > 5 && (
                    <li>...và {result.errors.length - 5} lỗi khác</li>
                  )}
                </ul>
              )}
            </div>
          )}

          <div className="flex justify-end gap-2 pt-2">
            <Button variant="outline" onClick={onClose}>Đóng</Button>
            <Button onClick={handleImport} disabled={!file || isUploading}>
              {isUploading ? 'Đang import...' : 'Import'}
            </Button>
          </div>
        </div>
      </div>
    </div>
  )
}
