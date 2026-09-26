import { useState, useEffect, useRef } from 'react'
import { examCampaignApi } from '../api'
import type { ExamCampaignDto, ExamCampaignEligibilityDto, AssignFromExcelResultDto } from '../types'
import { Button } from '@/components/ui/button'
import { X, Search, Upload } from 'lucide-react'

interface EligibilityDialogProps {
  open: boolean
  examCampaign: ExamCampaignDto | null
  onClose: () => void
}

export function EligibilityDialog({ open, examCampaign, onClose }: EligibilityDialogProps) {
  const [rows, setRows] = useState<ExamCampaignEligibilityDto[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [searchKeyword, setSearchKeyword] = useState('')
  const [isUploading, setIsUploading] = useState(false)
  const [uploadResult, setUploadResult] = useState<AssignFromExcelResultDto | null>(null)
  const [uploadError, setUploadError] = useState<string | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)

  useEffect(() => {
    if (open && examCampaign) {
      setSearchKeyword('')
      setUploadResult(null)
      setUploadError(null)
      loadEligibility(examCampaign.id)
    }
  }, [open, examCampaign?.id])

  async function handleUpload(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    if (!file || !examCampaign) return
    setIsUploading(true)
    setUploadError(null)
    setUploadResult(null)
    try {
      const res = await examCampaignApi.assignFromExcel(examCampaign.id, file)
      if (res.data.success && res.data.data) {
        setUploadResult(res.data.data)
        loadEligibility(examCampaign.id)
      } else {
        setUploadError(res.data.message || 'Úp danh sách thất bại')
      }
    } catch (err: any) {
      setUploadError(err.response?.data?.message || 'Có lỗi xảy ra khi úp danh sách')
    } finally {
      setIsUploading(false)
      if (fileInputRef.current) fileInputRef.current.value = ''
    }
  }

  async function loadEligibility(examCampaignId: number) {
    setIsLoading(true)
    try {
      const res = await examCampaignApi.getEligibility(examCampaignId)
      setRows(res.data.success && res.data.data ? res.data.data : [])
    } catch {
      setRows([])
    } finally {
      setIsLoading(false)
    }
  }

  if (!open || !examCampaign) return null

  const filtered = rows.filter((r) =>
    !searchKeyword.trim() ||
    r.fullName?.toLowerCase().includes(searchKeyword.toLowerCase()) ||
    r.employeeCode?.toLowerCase().includes(searchKeyword.toLowerCase())
  )
  const submittedCount = rows.filter((r) => r.hasSubmitted).length

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-2xl max-h-[85vh] overflow-y-auto">
        <div className="flex items-center justify-between p-4 border-b sticky top-0 bg-white z-10">
          <div>
            <h2 className="text-lg font-semibold">Ai được thi / ai đã thi</h2>
            <p className="text-sm text-gray-500">{examCampaign.campaignName}</p>
          </div>
          <Button variant="ghost" size="icon" onClick={onClose}>
            <X className="h-4 w-4" />
          </Button>
        </div>

        <div className="p-4 space-y-3">
          {examCampaign.accessMode === 'AssignedList' && (
            <div className="rounded-md border border-blue-200 bg-blue-50/50 p-3 space-y-2">
              <div className="flex items-center justify-between gap-2">
                <p className="text-sm text-blue-900 font-medium">Kỳ thi ở chế độ Danh sách chỉ định</p>
                <input ref={fileInputRef} type="file" accept=".csv,.xlsx,.xls" className="hidden" onChange={handleUpload} />
                <Button size="sm" variant="outline" onClick={() => fileInputRef.current?.click()} disabled={isUploading} className="gap-1 shrink-0">
                  <Upload className="h-3.5 w-3.5" />
                  {isUploading ? 'Đang úp...' : 'Úp danh sách'}
                </Button>
              </div>
              <p className="text-xs text-blue-700">
                File Excel/CSV, mỗi dòng 1 mã nhân viên hoặc tên đăng nhập (cột đầu tiên, có hoặc không có tiêu đề).
              </p>
              {uploadResult && (
                <p className="text-xs text-green-700">
                  Đã gán quyền thi cho {uploadResult.matchedUserCount}/{uploadResult.totalRows} người.
                  {uploadResult.notFoundCodes.length > 0 && (
                    <> Không tìm thấy tài khoản cho: {uploadResult.notFoundCodes.join(', ')}.</>
                  )}
                </p>
              )}
              {uploadError && <p className="text-xs text-red-600">{uploadError}</p>}
            </div>
          )}

          <div className="relative">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400" />
            <input
              placeholder="Tìm theo tên hoặc mã nhân viên..."
              className="h-10 w-full rounded-md border border-input bg-background pl-9 pr-3 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
              value={searchKeyword}
              onChange={(e) => setSearchKeyword(e.target.value)}
            />
          </div>

          <p className="text-sm text-gray-500">
            {rows.length} người đủ điều kiện thi • {submittedCount} đã thi (ít nhất 1 lần)
          </p>

          <div className="max-h-96 overflow-y-auto border rounded divide-y">
            {isLoading ? (
              <p className="p-3 text-sm text-gray-500">Đang tải...</p>
            ) : filtered.length === 0 ? (
              <p className="p-3 text-sm text-gray-500">Không có ai đủ điều kiện thi kỳ thi này</p>
            ) : (
              filtered.map((r) => (
                <div key={r.userId} className="flex items-center gap-3 px-3 py-2">
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium">{r.fullName}</p>
                    <p className="text-xs text-gray-400">{r.department} • {r.employeeCode}</p>
                  </div>
                  {r.hasSubmitted ? (
                    <div className="text-right shrink-0">
                      <span className="text-xs text-green-700 bg-green-50 border border-green-200 rounded px-1.5 py-0.5">
                        Đã thi{r.submissionStatus ? ` • ${r.submissionStatus}` : ''}
                      </span>
                      {r.totalScore != null && (
                        <p className="text-xs text-gray-400 mt-0.5">{r.totalScore} điểm</p>
                      )}
                    </div>
                  ) : (
                    <span className="text-xs text-gray-500 bg-gray-50 border border-gray-200 rounded px-1.5 py-0.5 shrink-0">
                      Chưa thi
                    </span>
                  )}
                </div>
              ))
            )}
          </div>

          <div className="flex justify-end pt-2 border-t">
            <Button variant="outline" onClick={onClose}>Đóng</Button>
          </div>
        </div>
      </div>
    </div>
  )
}
