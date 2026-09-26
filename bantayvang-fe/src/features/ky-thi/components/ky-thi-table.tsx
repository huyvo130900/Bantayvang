import { Button } from '@/components/ui/button'
import { Trash2, Pencil, Building2, ClipboardList, MonitorPlay } from 'lucide-react'
import type { ExamCampaignDto } from '../types'
import { formatDate } from '@/lib/utils'

interface KyThiTableProps {
  examCampaigns: ExamCampaignDto[]
  isLoading: boolean
  showKhoa?: boolean   // Admin bật để thấy cột Khoa/Đơn vị
  onView: (examCampaign: ExamCampaignDto) => void
  onEdit: (examCampaign: ExamCampaignDto) => void
  onDelete: (examCampaign: ExamCampaignDto) => void
  onChangeStatus: (examCampaign: ExamCampaignDto, status: string) => void
  onMonitor?: (examCampaign: ExamCampaignDto) => void
}

const STATUS_OPTIONS = [
  { value: 'DangChuanBi', label: 'Đang chuẩn bị' },
  { value: 'DangDienRa', label: 'Đang diễn ra' },
  { value: 'TamDung', label: 'Tạm dừng' },
  { value: 'DaKetThuc', label: 'Đã kết thúc' },
]

const statusColors: Record<string, string> = {
  DangChuanBi: 'bg-yellow-100 text-yellow-700',
  DangDienRa: 'bg-green-100 text-green-700',
  TamDung: 'bg-orange-100 text-orange-700',
  DaKetThuc: 'bg-gray-100 text-gray-500',
}

export function KyThiTable({ examCampaigns, isLoading, showKhoa = false, onView, onEdit, onDelete, onChangeStatus, onMonitor }: KyThiTableProps) {
  if (isLoading) {
    return (
      <div className="space-y-2">
        {[1, 2, 3].map((i) => (
          <div key={i} className="h-16 bg-gray-100 rounded-lg animate-pulse" />
        ))}
      </div>
    )
  }

  if (examCampaigns.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-16 text-gray-400 border-2 border-dashed rounded-xl">
        <span className="text-4xl mb-3">🏆</span>
        <p className="font-medium text-gray-500">Chưa có kỳ thi nào</p>
        <p className="text-sm mt-1">Nhấn "Tạo kỳ thi" để bắt đầu</p>
      </div>
    )
  }

  return (
    <div className="overflow-x-auto rounded-xl border shadow-sm">
      <table className="w-full text-sm">
        <thead className="bg-gray-50 border-b">
          <tr>
            <th className="px-4 py-3 text-center font-medium text-gray-600 whitespace-nowrap">Quản lý</th>
            <th className="px-4 py-3 text-left font-medium text-gray-600 whitespace-nowrap">Mã kỳ thi</th>
            <th className="px-4 py-3 text-left font-medium text-gray-600 whitespace-nowrap">Tên kỳ thi</th>
            {showKhoa && (
              <th className="px-4 py-3 text-left font-medium text-gray-600 whitespace-nowrap">
                <span className="flex items-center gap-1"><Building2 className="h-3.5 w-3.5" />Khoa / Đơn vị</span>
              </th>
            )}
            <th className="px-4 py-3 text-left font-medium text-gray-600 whitespace-nowrap">Thời gian</th>
            <th className="px-4 py-3 text-center font-medium text-gray-600 whitespace-nowrap">Đề / Thí sinh</th>
            <th className="px-4 py-3 text-center font-medium text-gray-600 whitespace-nowrap">Đúng tối thiểu</th>
            <th className="px-4 py-3 text-left font-medium text-gray-600 whitespace-nowrap">Trạng thái</th>
            <th className="px-4 py-3 text-right font-medium text-gray-600 whitespace-nowrap">Thao tác</th>
          </tr>
        </thead>
        <tbody className="divide-y">
          {examCampaigns.map((k) => (
            <tr key={k.id} className="hover:bg-gray-50 transition-colors">
              <td className="px-4 py-3 text-center flex flex-col gap-1 items-center">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => onView(k)}
                  className="h-8 w-full justify-start gap-1.5 text-xs text-blue-600 border-blue-200 hover:bg-blue-50 hover:text-blue-700 font-medium whitespace-nowrap"
                >
                  <ClipboardList className="h-3.5 w-3.5" />
                  Quản lý đề
                </Button>
                {onMonitor && (k.status === 'DangDienRa' || k.status === 'DangChuanBi') && (
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => onMonitor(k)}
                    className="h-8 w-full justify-start gap-1.5 text-xs text-green-600 border-green-200 hover:bg-green-50 hover:text-green-700 font-medium whitespace-nowrap"
                  >
                    <MonitorPlay className="h-3.5 w-3.5" />
                    Giám sát thi
                  </Button>
                )}
              </td>
              <td className="px-4 py-3 font-mono text-xs text-gray-600">{k.campaignCode}</td>
              <td className="px-4 py-3">
                <button
                  onClick={() => onView(k)}
                  className="font-medium text-primary hover:underline text-left focus:outline-none transition-colors"
                >
                  {k.campaignName}
                </button>
                {k.isPracticeMode && (
                  <span className="inline-flex items-center gap-1 px-1.5 py-0.5 rounded text-[10px] font-medium bg-purple-50 text-purple-700 border border-purple-200 mt-0.5 ml-1">
                    Luyện tập
                  </span>
                )}
                {/* Chỉ hiện tên khoa dưới tên khi KHÔNG hiện cột Khoa riêng */}
                {!showKhoa && k.departmentNames.length > 0 && (
                  <p className="text-xs text-gray-400 mt-0.5">{k.departmentNames.join(', ')}</p>
                )}
                {k.accessMode === 'AssignedList' && (
                  <span className="inline-flex items-center gap-1 px-1.5 py-0.5 rounded text-[10px] font-medium bg-amber-50 text-amber-700 border border-amber-200 mt-0.5">
                    Danh sách chỉ định
                  </span>
                )}
              </td>
              {showKhoa && (
                <td className="px-4 py-3">
                  {k.accessMode === 'AssignedList' ? (
                    <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-amber-50 text-amber-700 border border-amber-200 whitespace-nowrap">
                      Danh sách chỉ định
                    </span>
                  ) : k.departmentNames.length > 0 ? (
                    <div className="flex flex-wrap gap-1">
                      {k.departmentNames.map((name) => (
                        <span key={name} className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-blue-50 text-blue-700 border border-blue-200 whitespace-nowrap">
                          <Building2 className="h-3 w-3" />
                          {name}
                        </span>
                      ))}
                    </div>
                  ) : (
                    <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-gray-50 text-gray-600 border border-gray-200 whitespace-nowrap">
                      Tất cả các khoa
                    </span>
                  )}
                </td>
              )}
              <td className="px-4 py-3 text-gray-500 text-xs">
                <div className="font-medium text-gray-700">{formatDate(k.startTime)}</div>
                {k.endTime && (
                  <div className="text-gray-400 mt-0.5">đến {formatDate(k.endTime)}</div>
                )}
              </td>
              <td className="px-4 py-3 text-center text-gray-600">
                <span className="font-medium">{k.totalExamPapers}</span>
                <span className="text-gray-400 text-xs"> đề</span>
                {k.totalCandidates > 0 && (
                  <span className="text-xs text-gray-400 block">{k.totalCandidates} người</span>
                )}
              </td>
              <td className="px-4 py-3 text-center text-gray-600">
                {k.minPassQuestions !== undefined && k.minPassQuestions !== null ? (
                  <span className="font-medium text-gray-900">{k.minPassQuestions} câu</span>
                ) : (
                  <span className="text-gray-400">—</span>
                )}
              </td>
              <td className="px-4 py-3">
                <select
                  value={k.status || ''}
                  onChange={(e) => onChangeStatus(k, e.target.value)}
                  className={`text-xs font-medium px-2 py-1 rounded-full border-0 cursor-pointer focus:outline-none focus:ring-1 focus:ring-primary ${statusColors[k.status || ''] || 'bg-gray-100 text-gray-500'}`}
                >
                  {STATUS_OPTIONS.map((s) => (
                    <option key={s.value} value={s.value}>{s.label}</option>
                  ))}
                </select>
              </td>
              <td className="px-4 py-3">
                <div className="flex items-center justify-end gap-1.5">
                  <Button variant="ghost" size="icon" title="Sửa kỳ thi" onClick={() => onEdit(k)} className="h-8 w-8 text-gray-400 hover:text-yellow-600">
                    <Pencil className="h-4 w-4" />
                  </Button>
                  <Button variant="ghost" size="icon" title="Xóa kỳ thi" onClick={() => onDelete(k)} className="h-8 w-8 text-gray-400 hover:text-red-600">
                    <Trash2 className="h-4 w-4" />
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
