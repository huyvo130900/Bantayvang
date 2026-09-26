import { Button } from '@/components/ui/button'
import { Pencil, Trash2, ToggleLeft, ToggleRight, BarChart2, Eye, EyeOff, ScanSearch, Building2 } from 'lucide-react'
import type { ExamPaperDto } from '../types'
import { formatDate } from '@/lib/utils'

interface ExamTableProps {
  exams: ExamPaperDto[]
  isLoading: boolean
  showKhoa?: boolean   // Admin bật lên để thấy cột Khoa
  onViewAssignments?: (exam: ExamPaperDto) => void
  onEdit?: (exam: ExamPaperDto) => void
  onDelete?: (exam: ExamPaperDto) => void
  onToggleStatus?: (exam: ExamPaperDto) => void
  onToggleCongBo?: (exam: ExamPaperDto) => void
  onViewStats?: (exam: ExamPaperDto) => void
  onPreview?: (exam: ExamPaperDto) => void
}

export function ExamTable({
  exams,
  isLoading,
  showKhoa = false,
  onEdit,
  onDelete,
  onToggleStatus,
  onToggleCongBo,
  onViewStats,
  onPreview,
}: ExamTableProps) {
  if (isLoading) {
    return (
      <div className="rounded-lg border overflow-hidden">
        {[1, 2, 3].map((i) => (
          <div key={i} className="h-14 bg-gray-50 animate-pulse border-b" />
        ))}
      </div>
    )
  }

  if (exams.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-16 text-gray-400 border rounded-lg">
        <span className="text-4xl mb-3">📋</span>
        <p className="font-medium">Chưa có đề thi nào</p>
        <p className="text-sm mt-1">Nhấn "Tạo đề thi" để bắt đầu</p>
      </div>
    )
  }

  return (
    <div className="overflow-x-auto rounded-lg border shadow-sm">
      <table className="w-full text-sm">
        <thead className="bg-gray-50 border-b">
          <tr>
            <th className="px-4 py-3 text-left font-medium text-gray-600 whitespace-nowrap">Mã đề</th>
            <th className="px-4 py-3 text-left font-medium text-gray-600 whitespace-nowrap">Tên đề thi</th>
            {showKhoa && (
              <th className="px-4 py-3 text-left font-medium text-gray-600 whitespace-nowrap">
                <span className="flex items-center gap-1"><Building2 className="h-3.5 w-3.5" />Khoa</span>
              </th>
            )}
            <th className="px-4 py-3 text-left font-medium text-gray-600 whitespace-nowrap">Thời gian</th>
            <th className="px-4 py-3 text-left font-medium text-gray-600 whitespace-nowrap">Bắt đầu</th>
            <th className="px-4 py-3 text-center font-medium text-gray-600 whitespace-nowrap">Câu</th>

            <th className="px-4 py-3 text-center font-medium text-gray-600 whitespace-nowrap">Trạng thái</th>
            <th className="px-4 py-3 text-center font-medium text-gray-600 whitespace-nowrap">Công bố điểm</th>
            <th className="px-4 py-3 text-right font-medium text-gray-600 whitespace-nowrap">Thao tác</th>
          </tr>
        </thead>
        <tbody className="divide-y">
          {exams.map((exam) => (
            <tr key={exam.id} className="hover:bg-gray-50 transition-colors">
              <td className="px-4 py-3 font-mono text-xs text-gray-600">{exam.examPaperCode}</td>
              <td className="px-4 py-3 font-medium text-gray-900 max-w-[200px] truncate">{exam.examPaperName}</td>
              {showKhoa && (
                <td className="px-4 py-3">
                  {exam.department ? (
                    <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-blue-50 text-blue-700 border border-blue-200 whitespace-nowrap">
                      <Building2 className="h-3 w-3" />
                      {exam.department}
                    </span>
                  ) : (
                    <span className="text-gray-400 text-xs">—</span>
                  )}
                </td>
              )}
              <td className="px-4 py-3 text-gray-600 whitespace-nowrap">{exam.durationMinutes} phút</td>
              <td className="px-4 py-3 text-gray-500 whitespace-nowrap text-xs">
                {formatDate(exam.startTime)}
              </td>
              <td className="px-4 py-3 text-center text-gray-600">{exam.totalQuestions}</td>

              <td className="px-4 py-3 text-center">
                <StatusBadge status={exam.status} />
              </td>
              <td className="px-4 py-3 text-center">
                {onToggleCongBo ? (
                  <button
                    onClick={() => onToggleCongBo(exam)}
                    title={exam.isResultPublished ? 'Tắt công bố điểm' : 'Bật công bố điểm'}
                    className={`inline-flex items-center gap-1 text-xs px-2 py-1 rounded-full border transition-colors ${
                      exam.isResultPublished
                        ? 'bg-green-50 text-green-700 border-green-300 hover:bg-green-100'
                        : 'bg-gray-50 text-gray-500 border-gray-200 hover:bg-gray-100'
                    }`}
                  >
                    {exam.isResultPublished
                      ? <><Eye className="h-3 w-3" /> Đã bật</>
                      : <><EyeOff className="h-3 w-3" /> Chưa bật</>
                    }
                  </button>
                ) : (
                  <span className={`text-xs ${exam.isResultPublished ? 'text-green-600' : 'text-gray-400'}`}>
                    {exam.isResultPublished ? '✓ Đã bật' : '— Chưa bật'}
                  </span>
                )}
              </td>
              <td className="px-4 py-3">
                <div className="flex items-center justify-end gap-0.5">
                  {onPreview && (
                    <Button
                      variant="ghost"
                      size="icon"
                      title="Xem trước đề thi"
                      onClick={() => onPreview(exam)}
                      className="h-8 w-8 text-gray-500 hover:text-indigo-600"
                    >
                      <ScanSearch className="h-4 w-4" />
                    </Button>
                  )}

                  {onViewStats && (
                    <Button
                      variant="ghost"
                      size="icon"
                      title="Thống kê"
                      onClick={() => onViewStats(exam)}
                      className="h-8 w-8 text-gray-500 hover:text-purple-600"
                    >
                      <BarChart2 className="h-4 w-4" />
                    </Button>
                  )}
                  {onToggleStatus && (
                    <Button
                      variant="ghost"
                      size="icon"
                      title={exam.status === 'Active' ? 'Đóng đề thi' : 'Mở đề thi'}
                      onClick={() => onToggleStatus(exam)}
                      className={`h-8 w-8 ${exam.status === 'Active' ? 'text-green-600 hover:text-gray-500' : 'text-gray-400 hover:text-green-600'}`}
                    >
                      {exam.status === 'Active' ? (
                        <ToggleRight className="h-4 w-4" />
                      ) : (
                        <ToggleLeft className="h-4 w-4" />
                      )}
                    </Button>
                  )}
                  {onEdit && (
                    <Button
                      variant="ghost"
                      size="icon"
                      title="Chỉnh sửa"
                      onClick={() => onEdit(exam)}
                      className="h-8 w-8 text-gray-500 hover:text-yellow-600"
                    >
                      <Pencil className="h-4 w-4" />
                    </Button>
                  )}
                  {onDelete && (
                    <Button
                      variant="ghost"
                      size="icon"
                      title="Xóa"
                      onClick={() => onDelete(exam)}
                      className="h-8 w-8 text-gray-500 hover:text-red-600"
                    >
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  )}
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

export function StatusBadge({ status }: { status: string | null }) {
  const map: Record<string, { cls: string; label: string }> = {
    Active: { cls: 'bg-green-100 text-green-700', label: 'Đang mở' },
    Draft: { cls: 'bg-yellow-100 text-yellow-700', label: 'Nháp' },
    Inactive: { cls: 'bg-gray-100 text-gray-500', label: 'Đã đóng' },
    Completed: { cls: 'bg-blue-100 text-blue-700', label: 'Hoàn thành' },
  }
  const { cls, label } = map[status || ''] || { cls: 'bg-gray-100 text-gray-500', label: status || '—' }
  return (
    <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${cls}`}>
      {label}
    </span>
  )
}
