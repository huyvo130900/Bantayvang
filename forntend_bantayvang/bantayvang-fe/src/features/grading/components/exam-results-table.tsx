import { Button } from '@/components/ui/button'
import { Eye, RotateCcw } from 'lucide-react'
import type { ExamResultDetailDto } from '../types'

interface ExamResultsTableProps {
  results: ExamResultDetailDto[]
  isLoading: boolean
  onViewDetail: (result: ExamResultDetailDto) => void
  onRegrade: (baiThiId: number) => void
}

export function ExamResultsTable({ results, isLoading, onViewDetail, onRegrade }: ExamResultsTableProps) {
  if (isLoading) {
    return <div className="flex items-center justify-center py-12 text-gray-500">Đang tải...</div>
  }

  if (results.length === 0) {
    return <div className="flex items-center justify-center py-12 text-gray-500">Chưa có kết quả</div>
  }

  return (
    <div className="overflow-x-auto rounded-lg border">
      <table className="w-full text-sm">
        <thead className="bg-gray-50 border-b">
          <tr>
            <th className="px-4 py-3 text-left font-medium text-gray-600">#</th>
            <th className="px-4 py-3 text-left font-medium text-gray-600">Thí sinh</th>
            <th className="px-4 py-3 text-left font-medium text-gray-600">Khoa/Phòng</th>
            <th className="px-4 py-3 text-left font-medium text-gray-600">Điểm</th>
            <th className="px-4 py-3 text-left font-medium text-gray-600">Đúng/Tổng</th>
            <th className="px-4 py-3 text-left font-medium text-gray-600">Thời gian</th>
            <th className="px-4 py-3 text-left font-medium text-gray-600">Kết quả</th>
            <th className="px-4 py-3 text-left font-medium text-gray-600">Cảnh báo</th>
            <th className="px-4 py-3 text-right font-medium text-gray-600">Thao tác</th>
          </tr>
        </thead>
        <tbody className="divide-y">
          {results.map((r, idx) => (
            <tr key={r.baiThiId} className="hover:bg-gray-50">
              <td className="px-4 py-3 text-gray-500">{idx + 1}</td>
              <td className="px-4 py-3">
                <p className="font-medium">{r.fullName || r.username}</p>
                <p className="text-xs text-gray-400">{r.username}</p>
              </td>
              <td className="px-4 py-3 text-gray-600">{r.khoaPhong || '—'}</td>
              <td className="px-4 py-3 font-bold">{r.tongDiem ?? '—'}</td>
              <td className="px-4 py-3 text-gray-600">{r.soCauDung ?? 0}/{r.tongSoCau ?? 0}</td>
              <td className="px-4 py-3 text-gray-600">{r.durationMinutes ? `${r.durationMinutes} phút` : '—'}</td>
              <td className="px-4 py-3">
                {r.pass ? (
                  <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-700">Đạt</span>
                ) : (
                  <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-red-100 text-red-700">Không đạt</span>
                )}
              </td>
              <td className="px-4 py-3">
                {(r.soCanhBao ?? 0) > 0 ? (
                  <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-orange-100 text-orange-700">
                    {r.soCanhBao}
                  </span>
                ) : (
                  <span className="text-gray-400">0</span>
                )}
              </td>
              <td className="px-4 py-3">
                <div className="flex items-center justify-end gap-1">
                  <Button variant="ghost" size="icon" title="Chi tiết" onClick={() => onViewDetail(r)}>
                    <Eye className="h-4 w-4" />
                  </Button>
                  <Button variant="ghost" size="icon" title="Chấm lại" onClick={() => onRegrade(r.baiThiId)}>
                    <RotateCcw className="h-4 w-4 text-blue-500" />
                  </Button>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
