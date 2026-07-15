import { useState, useMemo } from 'react'
import { Button } from '@/components/ui/button'
import { Eye, RotateCcw, ChevronUp, ChevronDown, Search } from 'lucide-react'
import type { ExamResultDetailDto } from '../types'
import { formatDate } from '@/lib/utils'
import { MAX_CHEATING_WARNINGS } from '@/lib/constants'

type SortField = 'correctAnswers' | 'durationMinutes' | 'soCanhBao'
type SortDir = 'asc' | 'desc'

interface ExamResultsTableProps {
  results: ExamResultDetailDto[]
  isLoading: boolean
  onViewDetail: (result: ExamResultDetailDto) => void
  onRegrade: (baiThiId: number) => void
}

export function ExamResultsTable({
  results,
  isLoading,
  onViewDetail,
  onRegrade,
}: ExamResultsTableProps) {
  const [sortField, setSortField] = useState<SortField>('correctAnswers')
  const [sortDir, setSortDir] = useState<SortDir>('desc')
  const [search, setSearch] = useState('')
  const [selectedBaiThiIdByUser, setSelectedBaiThiIdByUser] = useState<Record<string, number>>({})

  const handleSort = (field: SortField) => {
    if (sortField === field) setSortDir((d) => (d === 'asc' ? 'desc' : 'asc'))
    else { setSortField(field); setSortDir('desc') }
  }

  const SortIcon = ({ field }: { field: SortField }) =>
    sortField === field ? (
      sortDir === 'asc' ? <ChevronUp className="h-3.5 w-3.5 inline ml-0.5" /> : <ChevronDown className="h-3.5 w-3.5 inline ml-0.5" />
    ) : <ChevronDown className="h-3.5 w-3.5 inline ml-0.5 opacity-30" />

  // Group attempts by candidate
  const groupedCandidates = useMemo(() => {
    const map: Record<string, ExamResultDetailDto[]> = {}
    results.forEach((r) => {
      const key = r.username || r.userId?.toString() || ''
      if (!map[key]) {
        map[key] = []
      }
      map[key].push(r)
    })

    return Object.entries(map).map(([key, attempts]) => {
      // Sort attempts ascending by baiThiId so attempts[0] is the 1st attempt
      const sortedAttempts = [...attempts].sort((a, b) => a.baiThiId - b.baiThiId)
      return {
        userKey: key,
        attempts: sortedAttempts,
      }
    })
  }, [results])

  // Get currently selected attempt for each candidate
  const candidatesWithSelectedAttempt = useMemo(() => {
    return groupedCandidates.map((c) => {
      let selectedAttempt = c.attempts.find((a) => a.baiThiId === selectedBaiThiIdByUser[c.userKey])
      if (!selectedAttempt) {
        selectedAttempt = c.attempts[0]
      }
      return {
        ...c,
        selectedAttempt,
      }
    })
  }, [groupedCandidates, selectedBaiThiIdByUser])

  // Filter and sort candidates based on their selected attempt
  const filteredAndSortedCandidates = useMemo(() => {
    let data = [...candidatesWithSelectedAttempt]
    if (search) {
      const s = search.toLowerCase()
      data = data.filter(
        (c) =>
          c.selectedAttempt.fullName?.toLowerCase().includes(s) ||
          c.selectedAttempt.username?.toLowerCase().includes(s) ||
          c.selectedAttempt.maNhanVien?.toLowerCase().includes(s) ||
          c.selectedAttempt.department?.toLowerCase().includes(s)
      )
    }
    data.sort((a, b) => {
      const va = (a.selectedAttempt[sortField] ?? 0) as number
      const vb = (b.selectedAttempt[sortField] ?? 0) as number
      return sortDir === 'asc' ? va - vb : vb - va
    })
    return data
  }, [candidatesWithSelectedAttempt, search, sortField, sortDir])

  if (isLoading) {
    return (
      <div className="space-y-1.5">
        {[...Array(5)].map((_, i) => (
          <div key={i} className="h-14 bg-gray-50 rounded-lg animate-pulse border" />
        ))}
      </div>
    )
  }

  if (results.length === 0) {
    return (
      <div className="py-16 text-center border-2 border-dashed rounded-xl text-gray-400">
        <p className="font-medium text-gray-500">Chưa có kết quả bài thi</p>
      </div>
    )
  }

  return (
    <div className="space-y-3">
      {/* Search */}
      <div className="relative max-w-sm">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400" />
        <input
          type="text"
          placeholder="Tìm thí sinh..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="w-full pl-9 pr-4 py-2 border border-gray-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary"
        />
      </div>

      {filteredAndSortedCandidates.length === 0 && search && (
        <p className="text-center py-6 text-gray-400 text-sm">Không tìm thấy "{search}"</p>
      )}

      <div className="overflow-x-auto rounded-xl border shadow-sm">
        <table className="w-full text-sm">
          <thead className="bg-gray-50 border-b">
            <tr>
              <th className="px-4 py-3 text-left font-medium text-gray-600 w-10 whitespace-nowrap">#</th>
              <th className="px-4 py-3 text-left font-medium text-gray-600 whitespace-nowrap">Thí sinh</th>
              <th className="px-4 py-3 text-left font-medium text-gray-600 whitespace-nowrap">Mã nhân viên</th>
              <th className="px-4 py-3 text-left font-medium text-gray-600 hidden md:table-cell whitespace-nowrap">Khoa/Phòng</th>
              <th
                className="px-4 py-3 text-center font-medium text-gray-600 cursor-pointer hover:text-primary whitespace-nowrap select-none"
                onClick={() => handleSort('correctAnswers')}
              >
                Số câu đúng <SortIcon field="correctAnswers" />
              </th>
              <th
                className="px-4 py-3 text-center font-medium text-gray-600 cursor-pointer hover:text-primary hidden lg:table-cell whitespace-nowrap select-none"
                onClick={() => handleSort('durationMinutes')}
              >
                T.gian <SortIcon field="durationMinutes" />
              </th>

              <th
                className="px-4 py-3 text-center font-medium text-gray-600 cursor-pointer hover:text-primary whitespace-nowrap select-none"
                onClick={() => handleSort('soCanhBao')}
              >
                ⚠ Gian lận <SortIcon field="soCanhBao" />
              </th>
              <th className="px-4 py-3 text-center font-medium text-gray-600 whitespace-nowrap">Lần thi</th>
              <th className="px-4 py-3 text-center font-medium text-gray-600 whitespace-nowrap min-w-[120px]">
                Trạng thái chấm
              </th>
              <th className="px-4 py-3 text-center font-medium text-gray-600 whitespace-nowrap">Kết quả</th>
              <th className="px-4 py-3 text-right font-medium text-gray-600 hidden xl:table-cell whitespace-nowrap">Nộp lúc</th>
              <th className="px-4 py-3 text-right font-medium text-gray-600 whitespace-nowrap">Thao tác</th>
            </tr>
          </thead>
          <tbody className="divide-y">
            {filteredAndSortedCandidates.map(({ userKey, attempts, selectedAttempt: r }, idx) => {
              return (
                <tr key={userKey} className="hover:bg-gray-50 transition-colors">
                  <td className="px-4 py-3 text-gray-400 text-xs">{idx + 1}</td>
                  <td className="px-4 py-3">
                    <p className="font-semibold text-gray-900">{r.fullName || r.username}</p>
                    <p className="text-xs text-gray-400">{r.username}</p>
                  </td>
                  <td className="px-4 py-3 text-gray-600 text-sm">
                    {r.maNhanVien || '—'}
                  </td>
                  <td className="px-4 py-3 text-gray-500 text-xs hidden md:table-cell">{r.department || '—'}</td>
                  <td className="px-4 py-3 text-center">
                    <span className="text-lg font-bold text-primary">{r.correctAnswers ?? 0}</span>
                    <span className="text-gray-400 text-sm">/{r.tongSoCau ?? 0}</span>
                  </td>
                  <td className="px-4 py-3 text-center text-gray-500 text-xs hidden lg:table-cell whitespace-nowrap">
                    {r.durationSeconds != null
                      ? r.durationSeconds >= 60
                        ? `${Math.floor(r.durationSeconds / 60)}p ${r.durationSeconds % 60}s`
                        : `${r.durationSeconds} giây`
                      : r.durationMinutes
                        ? `${r.durationMinutes} phút`
                        : '—'}
                  </td>

                  <td className="px-4 py-3 text-center">
                    {(() => {
                      const total = r.soCanhBao ?? 0
                      const max = MAX_CHEATING_WARNINGS
                      const color = total === 0
                        ? 'text-gray-400'
                        : total >= max
                          ? 'text-red-600 font-semibold'
                          : 'text-orange-600 font-medium'
                      return (
                        <span className={`text-sm ${color}`}>
                          {total}/{max}
                        </span>
                      )
                    })()}
                  </td>
                  <td className="px-4 py-3 text-center">
                    {attempts.length > 1 ? (
                      <select
                        className="h-8 rounded-lg border border-gray-200 bg-gray-50/50 hover:bg-white hover:border-blue-400 px-2.5 py-1 text-xs font-semibold text-gray-700 shadow-sm transition-all duration-200 cursor-pointer focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
                        value={r.baiThiId}
                        onChange={(e) => {
                          const baiThiId = Number(e.target.value)
                          setSelectedBaiThiIdByUser((prev) => ({
                            ...prev,
                            [userKey]: baiThiId,
                          }))
                        }}
                      >
                        {attempts.map((attempt, index) => (
                          <option key={attempt.baiThiId} value={attempt.baiThiId}>
                            Lần {index + 1}
                          </option>
                        ))}
                      </select>
                    ) : (
                      <span className="inline-flex items-center px-2 py-0.5 rounded bg-gray-100 text-xs font-semibold text-gray-600 border border-gray-200">
                        Lần 1
                      </span>
                    )}
                  </td>

                  <td className="px-4 py-3 text-center">
                    <div className="flex flex-col items-center gap-1">
                      {/* Trắc nghiệm */}
                      <span className={`inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-medium ${
                        r.soCauTracNghiemDaCham === r.tongSoCauTracNghiem && (r.tongSoCauTracNghiem ?? 0) > 0
                          ? 'bg-green-50 border border-green-200 text-green-700'
                          : 'bg-yellow-50 border border-yellow-200 text-yellow-700'
                      }`}>
                        TN: {r.soCauTracNghiemDaCham ?? 0}/{r.tongSoCauTracNghiem ?? 0}
                      </span>
                      {/* Tự luận */}
                      {(r.tongSoCauTuLuan ?? 0) > 0 && (
                        <span className={`inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-medium ${
                          r.soCauTuLuanDaCham === r.tongSoCauTuLuan
                            ? 'bg-green-50 border border-green-200 text-green-700'
                            : 'bg-orange-100 border border-orange-400 text-orange-700 font-bold animate-pulse'
                        }`}>
                          ✏️ TL: {r.soCauTuLuanDaCham ?? 0}/{r.tongSoCauTuLuan ?? 0}
                        </span>
                      )}
                    </div>
                  </td>

                  <td className="px-4 py-3 text-center">
                    {(() => {
                      const fraudFail = (r.soCanhBao ?? 0) >= MAX_CHEATING_WARNINGS
                      const minCorrect = r.soCauDungToiThieu ?? null
                      const scoreFail = minCorrect !== null && (r.correctAnswers ?? 0) < minCorrect
                      const passed = !fraudFail && !scoreFail
                      return passed ? (
                        <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold bg-green-100 text-green-700 border border-green-200">
                          ✓ Đạt
                        </span>
                      ) : (
                        <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold bg-red-100 text-red-600 border border-red-200" title={fraudFail ? `Gian lận ${r.soCanhBao}/${MAX_CHEATING_WARNINGS} lần` : `Câu đúng ${r.correctAnswers ?? 0}/${minCorrect} (tối thiểu)`}>
                          ✗ Không đạt
                        </span>
                      )
                    })()}
                  </td>

                  <td className="px-4 py-3 text-right text-xs text-gray-400 hidden xl:table-cell whitespace-nowrap">
                    {r.submitTime ? formatDate(r.submitTime) : '—'}
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex items-center justify-end gap-0.5">
                      <Button
                        variant="ghost"
                        size="icon"
                        title="Xem chi tiết"
                        onClick={() => onViewDetail(r)}
                        className="h-8 w-8 text-gray-400 hover:text-blue-600"
                      >
                        <Eye className="h-4 w-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="icon"
                        title="Chấm lại"
                        onClick={() => onRegrade(r.baiThiId)}
                        className="h-8 w-8 text-gray-400 hover:text-green-600"
                      >
                        <RotateCcw className="h-4 w-4" />
                      </Button>
                    </div>
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      </div>

      <p className="text-xs text-gray-400 text-right">
        {filteredAndSortedCandidates.length !== groupedCandidates.length
          ? `Hiển thị ${filteredAndSortedCandidates.length}/${groupedCandidates.length} học viên (${results.length} bài thi)`
          : `${groupedCandidates.length} học viên (${results.length} bài thi)`}
      </p>
    </div>
  )
}
