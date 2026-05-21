import { Button } from '@/components/ui/button'
import { Eye, Trash2 } from 'lucide-react'
import type { KyThiDto } from '../types'

interface KyThiTableProps {
  kyThis: KyThiDto[]
  isLoading: boolean
  onView: (kyThi: KyThiDto) => void
  onDelete: (kyThi: KyThiDto) => void
}

const statusColors: Record<string, string> = {
  DangChuanBi: 'bg-yellow-100 text-yellow-700',
  DangDienRa: 'bg-green-100 text-green-700',
  TamDung: 'bg-orange-100 text-orange-700',
  DaKetThuc: 'bg-gray-100 text-gray-500',
}

const statusLabels: Record<string, string> = {
  DangChuanBi: 'Đang chuẩn bị',
  DangDienRa: 'Đang diễn ra',
  TamDung: 'Tạm dừng',
  DaKetThuc: 'Đã kết thúc',
}

export function KyThiTable({ kyThis, isLoading, onView, onDelete }: KyThiTableProps) {
  if (isLoading) {
    return <div className="flex items-center justify-center py-12 text-gray-500">Đang tải...</div>
  }

  if (kyThis.length === 0) {
    return <div className="flex items-center justify-center py-12 text-gray-500">Chưa có kỳ thi nào</div>
  }

  return (
    <div className="overflow-x-auto rounded-lg border">
      <table className="w-full text-sm">
        <thead className="bg-gray-50 border-b">
          <tr>
            <th className="px-4 py-3 text-left font-medium text-gray-600">Mã kỳ thi</th>
            <th className="px-4 py-3 text-left font-medium text-gray-600">Tên kỳ thi</th>
            <th className="px-4 py-3 text-left font-medium text-gray-600">Loại</th>
            <th className="px-4 py-3 text-left font-medium text-gray-600">Thời gian</th>
            <th className="px-4 py-3 text-left font-medium text-gray-600">Số ca</th>
            <th className="px-4 py-3 text-left font-medium text-gray-600">Trạng thái</th>
            <th className="px-4 py-3 text-right font-medium text-gray-600">Thao tác</th>
          </tr>
        </thead>
        <tbody className="divide-y">
          {kyThis.map((k) => (
            <tr key={k.id} className="hover:bg-gray-50">
              <td className="px-4 py-3 font-mono text-xs">{k.maKyThi}</td>
              <td className="px-4 py-3 font-medium">{k.tenKyThi}</td>
              <td className="px-4 py-3 text-gray-600">{k.loaiKyThi || '—'}</td>
              <td className="px-4 py-3 text-gray-600 text-xs">
                {k.thoiGianBatDau ? new Date(k.thoiGianBatDau).toLocaleDateString('vi-VN') : '—'}
                {k.thoiGianKetThuc && ` → ${new Date(k.thoiGianKetThuc).toLocaleDateString('vi-VN')}`}
              </td>
              <td className="px-4 py-3 text-gray-600">{k.soCaThi}</td>
              <td className="px-4 py-3">
                <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${statusColors[k.trangThai || ''] || 'bg-gray-100 text-gray-600'}`}>
                  {statusLabels[k.trangThai || ''] || k.trangThai}
                </span>
              </td>
              <td className="px-4 py-3">
                <div className="flex items-center justify-end gap-1">
                  <Button variant="ghost" size="icon" title="Chi tiết" onClick={() => onView(k)}>
                    <Eye className="h-4 w-4" />
                  </Button>
                  <Button variant="ghost" size="icon" title="Xóa" onClick={() => onDelete(k)}>
                    <Trash2 className="h-4 w-4 text-red-500" />
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
