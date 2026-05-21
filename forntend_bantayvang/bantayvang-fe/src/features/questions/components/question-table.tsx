import { Button } from '@/components/ui/button'
import { Edit, Trash2 } from 'lucide-react'
import type { CauhoiDto } from '../types'
import type { PaginationDto } from '@/types'

interface QuestionTableProps {
  questions: CauhoiDto[]
  pagination: PaginationDto | null
  isLoading: boolean
  onEdit: (question: CauhoiDto) => void
  onDelete: (question: CauhoiDto) => void
  onPageChange: (page: number) => void
}

export function QuestionTable({
  questions,
  pagination,
  isLoading,
  onEdit,
  onDelete,
  onPageChange,
}: QuestionTableProps) {
  if (isLoading) {
    return <div className="flex items-center justify-center py-12 text-gray-500">Đang tải...</div>
  }

  if (questions.length === 0) {
    return <div className="flex items-center justify-center py-12 text-gray-500">Không có câu hỏi nào</div>
  }

  return (
    <div>
      <div className="overflow-x-auto rounded-lg border">
        <table className="w-full text-sm">
          <thead className="bg-gray-50 border-b">
            <tr>
              <th className="px-4 py-3 text-left font-medium text-gray-600 w-12">#</th>
              <th className="px-4 py-3 text-left font-medium text-gray-600">Nội dung</th>
              <th className="px-4 py-3 text-left font-medium text-gray-600">Danh mục</th>
              <th className="px-4 py-3 text-left font-medium text-gray-600">Loại</th>
              <th className="px-4 py-3 text-left font-medium text-gray-600">Độ khó</th>
              <th className="px-4 py-3 text-left font-medium text-gray-600">Điểm</th>
              <th className="px-4 py-3 text-right font-medium text-gray-600">Thao tác</th>
            </tr>
          </thead>
          <tbody className="divide-y">
            {questions.map((q, idx) => (
              <tr key={q.id} className="hover:bg-gray-50">
                <td className="px-4 py-3 text-gray-500">
                  {pagination ? (pagination.pageNumber - 1) * pagination.pageSize + idx + 1 : idx + 1}
                </td>
                <td className="px-4 py-3 max-w-md">
                  <p className="truncate font-medium">{q.noiDung || '—'}</p>
                  <p className="text-xs text-gray-400 mt-0.5">
                    {q.danhSachLuaChon.length} lựa chọn
                  </p>
                </td>
                <td className="px-4 py-3 text-gray-600">{q.tenDanhMuc || '—'}</td>
                <td className="px-4 py-3 text-gray-600">{q.tenLoaiCauHoi || '—'}</td>
                <td className="px-4 py-3">
                  <DifficultyBadge level={q.doKho} />
                </td>
                <td className="px-4 py-3 text-gray-600">{q.diem ?? '—'}</td>
                <td className="px-4 py-3">
                  <div className="flex items-center justify-end gap-1">
                    <Button variant="ghost" size="icon" title="Sửa" onClick={() => onEdit(q)}>
                      <Edit className="h-4 w-4" />
                    </Button>
                    <Button variant="ghost" size="icon" title="Xóa" onClick={() => onDelete(q)}>
                      <Trash2 className="h-4 w-4 text-red-500" />
                    </Button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Pagination */}
      {pagination && pagination.totalPages > 1 && (
        <div className="flex items-center justify-between mt-4">
          <p className="text-sm text-gray-500">
            Hiển thị {questions.length} / {pagination.totalRecords} câu hỏi
          </p>
          <div className="flex gap-1">
            {Array.from({ length: pagination.totalPages }, (_, i) => i + 1).map((page) => (
              <Button
                key={page}
                variant={page === pagination.pageNumber ? 'default' : 'outline'}
                size="sm"
                onClick={() => onPageChange(page)}
              >
                {page}
              </Button>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}

function DifficultyBadge({ level }: { level: string | null }) {
  const colors: Record<string, string> = {
    De: 'bg-green-100 text-green-700',
    TrungBinh: 'bg-yellow-100 text-yellow-700',
    Kho: 'bg-red-100 text-red-700',
  }
  const labels: Record<string, string> = {
    De: 'Dễ',
    TrungBinh: 'TB',
    Kho: 'Khó',
  }
  const colorClass = colors[level || ''] || 'bg-gray-100 text-gray-600'
  const label = labels[level || ''] || level || '—'

  return (
    <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${colorClass}`}>
      {label}
    </span>
  )
}
