import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Search, Plus } from 'lucide-react'
import { useEffect, useState } from 'react'
import { departmentApi } from '@/features/departments/api'
import type { DepartmentDto } from '@/features/departments/types'
import type { UserFilterDto } from '../types'

interface UserFilterProps {
  filter: UserFilterDto
  onFilterChange: (filter: Partial<UserFilterDto>) => void
  onCreateClick: () => void
  hideRoleFilter?: boolean
}

const ROLES = [
  { value: '', label: 'Tất cả vai trò' },
  { value: '1', label: 'Admin' },
  { value: '5', label: 'Quản lý Khoa' },
  { value: '3', label: 'Thí sinh' },
  { value: '6', label: 'Thí sinh ngoài' },
]

const STATUS = [
  { value: '', label: 'Tất cả trạng thái' },
  { value: 'true', label: 'Đang hoạt động' },
  { value: 'false', label: 'Đã vô hiệu' },
]

export function UserFilter({ filter, onFilterChange, onCreateClick, hideRoleFilter }: UserFilterProps) {
  const [departments, setDepartments] = useState<DepartmentDto[]>([])

  useEffect(() => {
    departmentApi.getAll({ status: true, pageSize: 200 })
      .then((res: { data: { data?: DepartmentDto[] } }) => {
        const data = res.data?.data
        if (Array.isArray(data)) setDepartments(data)
      })
      .catch(() => {/* silent */})
  }, [])

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

      {/* Lọc theo Khoa/Phòng */}
      {!hideRoleFilter && departments.length > 0 && (
        <select
          className="h-10 rounded-md border border-input bg-background px-3 text-sm"
          value={filter.department ?? ''}
          onChange={(e) =>
            onFilterChange({
              department: e.target.value || undefined,
              pageNumber: 1,
            })
          }
        >
          <option value="">Tất cả Khoa/Phòng</option>
          {departments.map((d) => (
            <option key={d.id} value={d.departmentName}>
              {d.departmentName}
            </option>
          ))}
        </select>
      )}

      {!hideRoleFilter && (
        <select
          className="h-10 rounded-md border border-input bg-background px-3 text-sm"
          value={filter.roleId ?? ''}
          onChange={(e) =>
            onFilterChange({
              roleId: e.target.value ? Number(e.target.value) : undefined,
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
      )}

      <select
        className="h-10 rounded-md border border-input bg-background px-3 text-sm"
        value={filter.status === undefined ? '' : String(filter.status)}
        onChange={(e) =>
          onFilterChange({
            status: e.target.value === '' ? undefined : e.target.value === 'true',
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
