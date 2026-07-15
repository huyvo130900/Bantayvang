import { PenLine, Clock, ChevronRight } from 'lucide-react'
import { Button } from '@/components/ui/button'
import type { PendingEssayDto } from '../types'

interface PendingEssayTableProps {
  items: PendingEssayDto[]
  isLoading: boolean
  onViewDetail: (baiThiId: number) => void
  isGraded?: boolean
}

export function PendingEssayTable({ items, isLoading, onViewDetail, isGraded }: PendingEssayTableProps) {
  if (isLoading) {
    return (
      <div className="space-y-2">
        {[1, 2, 3].map(i => (
          <div key={i} className="h-16 bg-orange-50 animate-pulse rounded-lg border border-orange-100" />
        ))}
      </div>
    )
  }

  if (items.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-16 text-gray-400 border-2 border-dashed border-green-200 rounded-xl bg-green-50">
        <span className="text-5xl mb-3">✅</span>
        <p className="font-semibold text-green-700">
          {isGraded ? 'Chưa có bài thi tự luận nào được chấm' : 'Tuyệt vời! Không còn bài tự luận nào cần chấm'}
        </p>
        <p className="text-sm mt-1 text-green-600">
          {isGraded ? 'Khi bạn chấm điểm bài tự luận, chúng sẽ xuất hiện ở đây' : 'Tất cả câu tự luận đã được chấm xong'}
        </p>
      </div>
    )
  }

  return (
    <div className="overflow-x-auto rounded-xl border border-orange-200 shadow-sm">
      <table className="w-full text-sm">
        <thead className="bg-orange-50 border-b border-orange-200">
          <tr>
            <th className="px-4 py-3 text-left font-semibold text-orange-800 whitespace-nowrap">Thí sinh</th>
            <th className="px-4 py-3 text-left font-semibold text-orange-800 whitespace-nowrap">Khoa/Phòng</th>
            <th className="px-4 py-3 text-left font-semibold text-orange-800 whitespace-nowrap">Đề thi / Kỳ thi</th>
            <th className="px-4 py-3 text-left font-semibold text-orange-800 whitespace-nowrap">Thời gian nộp</th>
            <th className="px-4 py-3 text-center font-semibold text-orange-800 whitespace-nowrap">Câu chờ chấm</th>
            <th className="px-4 py-3 text-right font-semibold text-orange-800 whitespace-nowrap">Thao tác</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-orange-100">
          {items.map(item => (
            <tr key={item.baiThiId} className="hover:bg-orange-50/50 transition-colors">
              <td className="px-4 py-3">
                <p className="font-semibold text-gray-900">{item.fullName || '—'}</p>
                <p className="text-xs text-gray-400">{item.username} {item.maNhanVien ? `· ${item.maNhanVien}` : ''}</p>
              </td>
              <td className="px-4 py-3 text-gray-600 text-xs">{item.khoaPhong || '—'}</td>
              <td className="px-4 py-3">
                <p className="text-gray-800 font-medium">{item.tenDeThi || item.maDeThi || '—'}</p>
                {item.tenKyThi && (
                  <p className="text-xs text-gray-400">{item.tenKyThi}</p>
                )}
              </td>
              <td className="px-4 py-3 text-gray-500 text-xs whitespace-nowrap">
                {item.thoiGianNop
                  ? new Date(item.thoiGianNop).toLocaleString('vi-VN', { hour: '2-digit', minute: '2-digit', day: '2-digit', month: '2-digit', year: 'numeric' })
                  : '—'}
              </td>
              <td className="px-4 py-3 text-center">
                <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-bold bg-orange-100 text-orange-700 border border-orange-300">
                  <PenLine className="h-3 w-3" />
                  {item.soCauTuLuanChuaCham}/{item.tongSoCauTuLuan} câu
                </span>
              </td>
              <td className="px-4 py-3 text-right">
                <Button
                  size="sm"
                  variant="outline"
                  className="border-orange-400 text-orange-700 hover:bg-orange-100 h-8 text-xs"
                  onClick={() => onViewDetail(item.baiThiId)}
                >
                  {isGraded ? 'Xem / Chấm lại' : 'Chấm ngay'}
                  <ChevronRight className="h-3 w-3 ml-1" />
                </Button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
