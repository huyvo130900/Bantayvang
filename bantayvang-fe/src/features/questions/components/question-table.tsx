import { Button } from '@/components/ui/button'
import { Edit, Trash2, ChevronLeft, ChevronRight } from 'lucide-react'
import type { QuestionDto } from '../types'
import type { PaginationDto } from '@/types'

interface QuestionTableProps {
  questions: QuestionDto[]
  pagination: PaginationDto | null
  isLoading: boolean
  onEdit: (question: QuestionDto) => void
  onDelete: (question: QuestionDto) => void
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
    return (
      <div className="space-y-2 rounded-xl border overflow-hidden">
        <div className="h-11 bg-gray-50 border-b" />
        {[...Array(8)].map((_, i) => (
          <div key={i} className="h-14 bg-gray-50 animate-pulse border-b" />
        ))}
      </div>
    )
  }

  if (questions.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-16 border-2 border-dashed rounded-xl text-gray-400">
        <span className="text-4xl mb-3">❓</span>
        <p className="font-medium text-gray-500">Không có câu hỏi nào</p>
        <p className="text-sm mt-1">Thử thay đổi bộ lọc hoặc thêm câu hỏi mới</p>
      </div>
    )
  }

  // Helper to normalize question content for comparison (ignoring HTML, spacing, case)
  const normalizeText = (text: string | null | undefined): string => {
    if (!text) return ''
    const cleanHtml = text.replace(/<[^>]*>/g, '')
    return cleanHtml.replace(/\s+/g, ' ').trim().toLowerCase()
  }

  // Define unique group keys by combining department and normalized content
  const getGroupKey = (q: QuestionDto): string => {
    const dept = (q.department || '').trim().toLowerCase()
    const norm = normalizeText(q.content)
    return `${dept}|||${norm}`
  }

  // 1. Group questions by department and content
  const groups: { [key: string]: QuestionDto[] } = {}
  questions.forEach((q) => {
    const key = getGroupKey(q)
    if (!groups[key]) {
      groups[key] = []
    }
    groups[key].push(q)
  })

  // 2. Separate duplicate questions and single questions
  const duplicateGroups: { [key: string]: QuestionDto[] } = {}
  const nonDuplicateQuestions: QuestionDto[] = []

  Object.keys(groups).forEach((key) => {
    if (groups[key].length > 1) {
      duplicateGroups[key] = groups[key]
    } else {
      nonDuplicateQuestions.push(groups[key][0])
    }
  })

  // 3. Sort duplicate groups alphabetically by content (A-Z)
  const duplicateKeysSorted = Object.keys(duplicateGroups).sort((a, b) => {
    const textA = a.split('|||')[1] || ''
    const textB = b.split('|||')[1] || ''
    return textA.localeCompare(textB, 'vi', { sensitivity: 'base' })
  })

  // 4. Assign beautiful pastel colors to groups
  const PASTEL_COLORS = [
    { bg: 'bg-blue-50/70', border: 'border-l-blue-400' },
    { bg: 'bg-emerald-50/70', border: 'border-l-emerald-400' },
    { bg: 'bg-purple-50/70', border: 'border-l-purple-400' },
    { bg: 'bg-amber-50/70', border: 'border-l-amber-400' },
    { bg: 'bg-rose-50/70', border: 'border-l-rose-400' },
    { bg: 'bg-indigo-50/70', border: 'border-l-indigo-400' },
    { bg: 'bg-teal-50/70', border: 'border-l-teal-400' },
    { bg: 'bg-orange-50/70', border: 'border-l-orange-400' },
    { bg: 'bg-pink-50/70', border: 'border-l-pink-400' },
    { bg: 'bg-cyan-50/70', border: 'border-l-cyan-400' },
  ]

  const colorMap: { [key: string]: typeof PASTEL_COLORS[0] } = {}
  duplicateKeysSorted.forEach((key, idx) => {
    colorMap[key] = PASTEL_COLORS[idx % PASTEL_COLORS.length]
  })

  // 5. Place duplicate questions at the top of the table
  const sortedDuplicates: QuestionDto[] = []
  duplicateKeysSorted.forEach((key) => {
    sortedDuplicates.push(...duplicateGroups[key])
  })

  const processedQuestions = [...sortedDuplicates, ...nonDuplicateQuestions]

  return (
    <div>
      <div className="overflow-x-auto rounded-xl border shadow-sm">
        <table className="w-full text-sm">
          <thead className="bg-gray-50 border-b">
            <tr>
              <th className="px-4 py-3 text-left font-medium text-gray-600 w-12 whitespace-nowrap">#</th>
              <th className="px-4 py-3 text-left font-medium text-gray-600 whitespace-nowrap">Nội dung câu hỏi</th>
              <th className="px-4 py-3 text-left font-medium text-gray-600 whitespace-nowrap">Loại</th>
              <th className="px-4 py-3 text-left font-medium text-gray-600 whitespace-nowrap">Mức độ</th>
              <th className="px-4 py-3 text-left font-medium text-gray-600 whitespace-nowrap">Khoa/Phòng</th>
              <th className="px-4 py-3 text-left font-medium text-gray-600 whitespace-nowrap">Kỳ thi</th>
              <th className="px-4 py-3 text-left font-medium text-gray-600 whitespace-nowrap">Đề thi</th>
              <th className="px-4 py-3 text-center font-medium text-gray-600 whitespace-nowrap">Đáp án</th>
              <th className="px-4 py-3 text-right font-medium text-gray-600 whitespace-nowrap">Thao tác</th>
            </tr>
          </thead>
          <tbody className="divide-y">
            {processedQuestions.map((q, idx) => {
              const key = getGroupKey(q)
              const isDuplicate = duplicateGroups[key] !== undefined
              const color = isDuplicate ? colorMap[key] : null

              return (
                <tr 
                  key={q.id} 
                  className={`transition-colors border-b ${
                    color ? `${color.bg} hover:bg-opacity-90` : 'hover:bg-gray-50/80'
                  }`}
                >
                  <td className={`px-4 py-3 text-gray-400 text-xs ${color ? `border-l-4 ${color.border}` : ''}`}>
                    {pagination ? (pagination.pageNumber - 1) * pagination.pageSize + idx + 1 : idx + 1}
                  </td>
                  <td className="px-4 py-3 max-w-xs">
                    <p className="font-medium text-gray-900 truncate" title={q.content || ''}>
                      {q.content || '—'}
                    </p>
                    <div className="flex items-center gap-2 mt-0.5">
                      {q.department && (
                        <span className="text-xs text-gray-400 shrink-0">{q.department}</span>
                      )}
                      {isDuplicate && (
                        <span className="text-[10px] font-semibold px-1 py-0.25 rounded bg-amber-50 text-amber-700 border border-amber-200 uppercase scale-90 origin-left shrink-0">
                          Trùng lặp
                        </span>
                      )}
                    </div>
                  </td>
                  <td className="px-4 py-3 text-gray-600 text-xs whitespace-nowrap">{q.categoryName || '—'}</td>
                  <td className="px-4 py-3 whitespace-nowrap">
                    {renderDifficultyBadge(q.difficulty)}
                  </td>
                  <td className="px-4 py-3 text-left">
                    <span className="text-xs text-gray-600">{q.department || '—'}</span>
                  </td>
                  <td className="px-4 py-3 max-w-[180px]">
                    {q.campaigns && q.campaigns.length > 0 ? (
                      <div className="flex flex-wrap gap-1">
                        <span 
                          className="inline-flex items-center px-2 py-0.5 rounded bg-blue-50 text-blue-700 border border-blue-100 text-[11px] font-semibold truncate max-w-full shadow-sm"
                          title={q.campaigns.join(', ')}
                        >
                          {q.campaigns[0]}
                          {q.campaigns.length > 1 && ` (+${q.campaigns.length - 1})`}
                        </span>
                      </div>
                    ) : (
                      <span className="text-gray-300">—</span>
                    )}
                  </td>
                  <td className="px-4 py-3 max-w-[180px]">
                    {q.examPapers && q.examPapers.length > 0 ? (
                      <div className="flex flex-wrap gap-1">
                        <span 
                          className="inline-flex items-center px-2 py-0.5 rounded bg-violet-50 text-violet-700 border border-violet-100 text-[11px] font-semibold truncate max-w-full shadow-sm"
                          title={q.examPapers.join(', ')}
                        >
                          {q.examPapers[0]}
                          {q.examPapers.length > 1 && ` (+${q.examPapers.length - 1})`}
                        </span>
                      </div>
                    ) : (
                      <span className="text-gray-300">—</span>
                    )}
                  </td>
                  <td className="px-4 py-3 text-center text-xs text-gray-500">
                    {q.options.length > 0 ? (
                      <span>{q.options.filter((l) => l.isCorrect).length}/{q.options.length}</span>
                    ) : (
                      <span className="text-gray-300">Tự luận</span>
                    )}
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex items-center justify-end gap-0.5">
                      <Button
                        variant="ghost"
                        size="icon"
                        title="Sửa câu hỏi"
                        onClick={() => onEdit(q)}
                        className="h-8 w-8 text-gray-400 hover:text-yellow-600"
                      >
                        <Edit className="h-4 w-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="icon"
                        title="Xóa câu hỏi"
                        onClick={() => onDelete(q)}
                        className="h-8 w-8 text-gray-400 hover:text-red-600"
                      >
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </div>
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>

      {/* Smart Pagination */}
      {pagination && pagination.totalPages > 1 && (
        <div className="flex items-center justify-between mt-4">
          <p className="text-sm text-gray-500">
            {(pagination.pageNumber - 1) * pagination.pageSize + 1}–
            {Math.min(pagination.pageNumber * pagination.pageSize, pagination.totalRecords)}
            {' '}/ {pagination.totalRecords} câu hỏi
          </p>
          <SmartPagination
            current={pagination.pageNumber}
            total={pagination.totalPages}
            onPageChange={onPageChange}
          />
        </div>
      )}
    </div>
  )
}

function SmartPagination({ current, total, onPageChange }: { current: number; total: number; onPageChange: (p: number) => void }) {
  const pages: (number | '...')[] = []

  if (total <= 7) {
    for (let i = 1; i <= total; i++) pages.push(i)
  } else {
    pages.push(1)
    if (current > 3) pages.push('...')
    for (let i = Math.max(2, current - 1); i <= Math.min(total - 1, current + 1); i++) pages.push(i)
    if (current < total - 2) pages.push('...')
    pages.push(total)
  }

  return (
    <div className="flex items-center gap-1">
      <Button variant="outline" size="icon" className="h-8 w-8" onClick={() => onPageChange(current - 1)} disabled={current <= 1}>
        <ChevronLeft className="h-4 w-4" />
      </Button>
      {pages.map((p, i) =>
        p === '...' ? (
          <span key={`dots-${i}`} className="px-1 text-gray-400 text-sm">…</span>
        ) : (
          <Button
            key={p}
            variant={p === current ? 'default' : 'outline'}
            size="sm"
            onClick={() => onPageChange(p as number)}
            className="h-8 w-8 p-0"
          >
            {p}
          </Button>
        )
      )}
      <Button variant="outline" size="icon" className="h-8 w-8" onClick={() => onPageChange(current + 1)} disabled={current >= total}>
        <ChevronRight className="h-4 w-4" />
      </Button>
    </div>
  )
}

function renderDifficultyBadge(difficulty: string | null | undefined) {
  const normalized = (difficulty || 'Dễ').trim().toLowerCase()
  if (normalized === 'khó') {
    return (
      <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-semibold bg-red-50 text-red-700 border border-red-100 shadow-sm">
        🔴 Khó
      </span>
    )
  }
  if (normalized === 'trung bình') {
    return (
      <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-semibold bg-amber-50 text-amber-700 border border-amber-100 shadow-sm">
        🟡 Trung bình
      </span>
    )
  }
  return (
    <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-semibold bg-emerald-50 text-emerald-700 border border-emerald-100 shadow-sm">
      🟢 Dễ
    </span>
  )
}

