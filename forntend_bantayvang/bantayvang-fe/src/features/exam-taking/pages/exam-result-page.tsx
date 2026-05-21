import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import apiClient from '@/lib/axios'
import type { ApiResponse } from '@/types'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { CheckCircle2, XCircle, Trophy } from 'lucide-react'

interface ExamResult {
  baiThiId: number
  tongDiem: number | null
  soCauDung: number | null
  tongSoCau: number | null
  trangThai: string | null
  pass: boolean
  thoiGianBatDau: string | null
  thoiGianNop: string | null
  durationMinutes: number | null
  tenDeThi: string | null
  maDeThi: string | null
}

export function ExamResultPage() {
  const { baithiId } = useParams<{ baithiId: string }>()
  const navigate = useNavigate()
  const [result, setResult] = useState<ExamResult | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    if (!baithiId) return
    loadResult()
  }, [baithiId]) // eslint-disable-line react-hooks/exhaustive-deps

  const loadResult = async () => {
    try {
      const response = await apiClient.get<ApiResponse<ExamResult>>(
        `/grading/result/${baithiId}`
      )
      if (response.data.success && response.data.data) {
        setResult(response.data.data)
      }
    } catch {
      // handle error
    } finally {
      setLoading(false)
    }
  }

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <p className="text-gray-500">Đang tải kết quả...</p>
      </div>
    )
  }

  if (!result) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <p className="text-red-500">Không thể tải kết quả.</p>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-gray-50 flex items-center justify-center p-6">
      <Card className="w-full max-w-md">
        <CardHeader className="text-center">
          <div className="mx-auto mb-3">
            {result.pass ? (
              <Trophy className="h-16 w-16 text-yellow-500" />
            ) : (
              <XCircle className="h-16 w-16 text-red-400" />
            )}
          </div>
          <CardTitle className="text-xl">
            {result.pass ? 'Chúc mừng! Bạn đã đạt!' : 'Chưa đạt'}
          </CardTitle>
          <p className="text-sm text-gray-500">{result.tenDeThi}</p>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-2 gap-4 text-center">
            <div className="p-3 bg-blue-50 rounded-lg">
              <p className="text-2xl font-bold text-blue-700">{result.tongDiem ?? 0}</p>
              <p className="text-xs text-blue-600">Tổng điểm</p>
            </div>
            <div className="p-3 bg-green-50 rounded-lg">
              <p className="text-2xl font-bold text-green-700">
                {result.soCauDung ?? 0}/{result.tongSoCau ?? 0}
              </p>
              <p className="text-xs text-green-600">Câu đúng</p>
            </div>
          </div>

          {result.durationMinutes && (
            <p className="text-sm text-gray-500 text-center">
              Thời gian làm bài: {result.durationMinutes} phút
            </p>
          )}

          <div className="flex items-center justify-center gap-2 text-sm">
            {result.pass ? (
              <span className="flex items-center gap-1 text-green-600">
                <CheckCircle2 className="h-4 w-4" /> Đạt
              </span>
            ) : (
              <span className="flex items-center gap-1 text-red-600">
                <XCircle className="h-4 w-4" /> Không đạt
              </span>
            )}
          </div>

          <Button className="w-full" onClick={() => navigate('/exam-waiting')}>
            Quay lại
          </Button>
        </CardContent>
      </Card>
    </div>
  )
}
