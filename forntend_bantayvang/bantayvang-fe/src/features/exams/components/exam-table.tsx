import { Button } from '@/components/ui/button'
import { Users, Eye } from 'lucide-react'
import type { DethiDto } from '../types'

interface ExamTableProps {
  exams: DethiDto[]
  isLoading: boolean
  onViewAssignments: (exam: DethiDto) => void
}

export function ExamTable({ exams, isLoading, onViewAssignments }: ExamTableProps) {
  if (isLoading) {
    return <div className="flex items-center justify-center py-12 text-gray-500">Đang tải...</div>
  }

  if (exams.length === 0) {
    return <div className="flex items-center justify-center py-12 text-gray-500">Chưa có đề thi nào</div>
  }

  return (
    <div className="overflow-x-auto rounded-lg border">
      <table className="w-full text-sm">
        <thead className="bg-gray-50 border-b">
          <tr>
            <th className="px-4 py-3 text-left font-medium text-gray-600">Mã đề</th>
            <th className="px-4 py-3 text-left font-medium text-gray-600">Tên đề thi</th>
            <th className="px-4 py-3 text-left font-medium text-gray-600">Thời gian</th>
            <th className="px-4 py-3 text-left font-medium text-gray-600">Bắt đầu</th>
            <th className="px-4 py-3 text-left font-medium text-gray-600">Số câu</th>
            <th className="px-4 py-3 text-left font-medium text-gray-600">Trạng thái</th>
            <th className="px-4 py-3 text-right font-medium text-gray-600">Thao tác</th>
          </tr>
        </thead>
        <tbody className="divide-y">
          {exams.map((exam) => (
            <tr key={exam.id} className="hover:bg-gray-50">
              <td className="px-4 py-3 font-mono text-xs">{exam.maDeThi}</td>
              <td className="px-4 py-3 font-medium">{exam.tenDeThi}</td>
              <td className="px-4 py-3 text-gray-600">{exam.thoiGianLamBai} phút</td>
              <td className="px-4 py-3 text-gray-600">
                {exam.thoiGianBatDau
                  ? new Date(exam.thoiGianBatDau).toLocaleString('vi-VN')
                  : '—'}
              </td>
              <td className="px-4 py-3 text-gray-600">{exam.soCauHoi}</td>
              <td className="px-4 py-3">
                <StatusBadge status={exam.trangThai} />
              </td>
              <td className="px-4 py-3">
                <div className="flex items-center justify-end gap-1">
                  <Button
                    variant="ghost"
                    size="icon"
                    title="Xem phân công"
                    onClick={() => onViewAssignments(exam)}
                  >
                    <Users className="h-4 w-4" />
                  </Button>
                  <Button variant="ghost" size="icon" title="Chi tiết">
                    <Eye className="h-4 w-4" />
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

function StatusBadge({ status }: { status: string | null }) {
  const colors: Record<string, string> = {
    Active: 'bg-green-100 text-green-700',
    Draft: 'bg-yellow-100 text-yellow-700',
    Inactive: 'bg-gray-100 text-gray-500',
  }
  const colorClass = colors[status || ''] || 'bg-gray-100 text-gray-600'

  return (
    <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${colorClass}`}>
      {status || '—'}
    </span>
  )
}
