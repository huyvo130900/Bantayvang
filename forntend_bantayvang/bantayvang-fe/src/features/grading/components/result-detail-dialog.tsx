import { useEffect, useState } from 'react'
import { gradingApi } from '../api'
import type { ExamResultDetailDto } from '../types'
import { Button } from '@/components/ui/button'
import { X, CheckCircle2, XCircle } from 'lucide-react'

interface ResultDetailDialogProps {
  open: boolean
  baiThiId: number | null
  onClose: () => void
}

export function ResultDetailDialog({ open, baiThiId, onClose }: ResultDetailDialogProps) {
  const [detail, setDetail] = useState<ExamResultDetailDto | null>(null)
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    if (open && baiThiId) {
      loadDetail(baiThiId)
    }
  }, [open, baiThiId])

  const loadDetail = async (id: number) => {
    setLoading(true)
    try {
      const response = await gradingApi.getResultDetail(id)
      if (response.data.success && response.data.data) {
        setDetail(response.data.data)
      }
    } catch {
      // handle error
    } finally {
      setLoading(false)
    }
  }

  if (!open) return null

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-2xl max-h-[85vh] overflow-y-auto">
        <div className="flex items-center justify-between p-4 border-b sticky top-0 bg-white z-10">
          <h2 className="text-lg font-semibold">Chi tiết bài thi</h2>
          <Button variant="ghost" size="icon" onClick={onClose}>
            <X className="h-4 w-4" />
          </Button>
        </div>

        {loading ? (
          <div className="p-8 text-center text-gray-500">Đang tải...</div>
        ) : detail ? (
          <div className="p-4 space-y-4">
            {/* Summary */}
            <div className="grid grid-cols-3 gap-3 text-center">
              <div className="p-3 bg-blue-50 rounded-lg">
                <p className="text-xl font-bold text-blue-700">{detail.tongDiem ?? 0}</p>
                <p className="text-xs text-blue-600">Tổng điểm</p>
              </div>
              <div className="p-3 bg-green-50 rounded-lg">
                <p className="text-xl font-bold text-green-700">{detail.soCauDung ?? 0}/{detail.tongSoCau ?? 0}</p>
                <p className="text-xs text-green-600">Câu đúng</p>
              </div>
              <div className="p-3 bg-gray-50 rounded-lg">
                <p className="text-xl font-bold text-gray-700">{detail.durationMinutes ?? 0} phút</p>
                <p className="text-xs text-gray-600">Thời gian</p>
              </div>
            </div>

            {/* Answers */}
            {detail.answers && detail.answers.length > 0 && (
              <div className="space-y-3">
                <h3 className="text-sm font-medium text-gray-700">Chi tiết từng câu</h3>
                {detail.answers.map((a, idx) => (
                  <div key={idx} className="border rounded-lg p-3">
                    <div className="flex items-start gap-2">
                      {a.isCorrect ? (
                        <CheckCircle2 className="h-5 w-5 text-green-500 shrink-0 mt-0.5" />
                      ) : (
                        <XCircle className="h-5 w-5 text-red-400 shrink-0 mt-0.5" />
                      )}
                      <div className="flex-1 min-w-0">
                        <p className="text-sm font-medium">Câu {idx + 1}: {a.noiDungCauHoi}</p>
                        <p className="text-xs text-gray-500 mt-1">
                          Trả lời: <span className={a.isCorrect ? 'text-green-600' : 'text-red-600'}>{a.noiDungDapAn || a.cauTraLoiTuLuan || '(Không trả lời)'}</span>
                        </p>
                        {!a.isCorrect && a.noiDungDapAnDung && (
                          <p className="text-xs text-green-600 mt-0.5">
                            Đáp án đúng: {a.noiDungDapAnDung}
                          </p>
                        )}
                        <p className="text-xs text-gray-400 mt-0.5">
                          Điểm: {a.diemDatDuoc ?? 0}/{a.diem ?? 0}
                        </p>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        ) : (
          <div className="p-8 text-center text-gray-500">Không có dữ liệu</div>
        )}
      </div>
    </div>
  )
}
