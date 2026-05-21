import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Search, Plus } from 'lucide-react'
import type { UserFilterDto } from '../types'

interface UserFilterProps {
  filter: UserFilterDto
  onFilterChange: (filter: Partial<UserFilterDto>) => void
  onCreateClick: () => void
}

const ROLES = [
  { value: '', label: 'Tất cả vai trò' },
  { value: '1', label: 'Admin' },
  { value: '2', label: 'Teacher' },
  { value: '3', label: 'Student' },
  { value: '4', label: 'Supervisor' },
]

const STATUS = [
  { value: '', label: 'Tất cả trạng thái' },
  { value: 'true', label: 'Đang hoạt động' },
  { value: 'false', label: 'Đã vô hiệu' },
]

export function UserFilter({ filter, onFilterChange, onCreateClick }: UserFilterProps) {
  return (
    <div className="flex flex-wrap items-center gap-3 mb-4">
      <div className="relative flex-1 min-w-[200px] max-w-sm">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400" />
        <Input
          placeholder="Tìm kiếm theo tên, email..."
          className="pl-9"
          value={filter.searchKeyword || ''}
          onChange={(e) => onFilterChange({ searchKeyword: e.target.value, pageNumber: 1 })}
        />
      </div>

      <select
        className="h-10 rounded-md border border-input bg-background px-3 text-sm"
        value={filter.idVaiTro ?? ''}
        onChange={(e) =>
          onFilterChange({
            idVaiTro: e.target.value ? Number(e.target.value) : undefined,
            pageNumber: 1,
          })
        }
      >
        {ROLES.map((r) => (
          <option key={r.value} value={r.value}>
            {r.label}
          </option>
        ))}
      </select>

      <select
        className="h-10 rounded-md border border-input bg-background px-3 text-sm"
        value={filter.trangThai === undefined ? '' : String(filter.trangThai)}
        onChange={(e) =>
          onFilterChange({
            trangThai: e.target.value === '' ? undefined : e.target.value === 'true',
            pageNumber: 1,
          })
        }
      >
        {STATUS.map((s) => (
          <option key={s.value} value={s.value}>
            {s.label}
          </option>
        ))}
      </select>

      <Button onClick={onCreateClick} className="ml-auto">
        <Plus className="h-4 w-4 mr-1" />
        Thêm người dùng
      </Button>
    </div>
  )
}
