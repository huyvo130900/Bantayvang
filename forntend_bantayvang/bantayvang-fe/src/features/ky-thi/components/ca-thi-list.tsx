import { Button } from '@/components/ui/button'
import { Plus, Trash2 } from 'lucide-react'
import type { CaThiDto } from '../types'

interface CaThiListProps {
  caThis: CaThiDto[]
  onAdd: () => void
  onDelete: (caThi: CaThiDto) => void
}

export function CaThiList({ caThis, onAdd, onDelete }: CaThiListProps) {
  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-medium text-gray-700">Danh sách ca thi ({caThis.length})</h3>
        <Button size="sm" onClick={onAdd}>
          <Plus className="h-3 w-3 mr-1" />
          Thêm ca
        </Button>
      </div>

      {caThis.length === 0 ? (
        <p className="text-sm text-gray-500 py-4 text-center">Chưa có ca thi nào</p>
      ) : (
        <div className="space-y-2">
          {caThis.map((ca) => (
            <div key={ca.id} className="flex items-center justify-between p-3 border rounded-lg">
              <div>
                <p className="text-sm font-medium">{ca.tenCa}</p>
                <p className="text-xs text-gray-500">
                  {ca.thoiGianBatDau ? new Date(ca.thoiGianBatDau).toLocaleString('vi-VN') : '—'}
                  {ca.maDeThi && ` • Đề: ${ca.maDeThi}`}
                  {ca.soLuongToiDa && ` • Tối đa: ${ca.soLuongToiDa} người`}
                </p>
              </div>
              <div className="flex items-center gap-2">
                <span className={`text-xs px-2 py-0.5 rounded-full ${
                  ca.trangThai === 'DangDienRa' ? 'bg-green-100 text-green-700' :
                  ca.trangThai === 'DaKetThuc' ? 'bg-gray-100 text-gray-500' :
                  'bg-yellow-100 text-yellow-700'
                }`}>
                  {ca.trangThai === 'ChuaBatDau' ? 'Chưa bắt đầu' :
                   ca.trangThai === 'DangDienRa' ? 'Đang diễn ra' : 'Đã kết thúc'}
                </span>
                <Button variant="ghost" size="icon" onClick={() => onDelete(ca)}>
                  <Trash2 className="h-4 w-4 text-red-400" />
                </Button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
