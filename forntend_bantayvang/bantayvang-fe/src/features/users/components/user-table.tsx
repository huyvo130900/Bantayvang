import { Button } from '@/components/ui/button'
import { Edit, ShieldCheck, ShieldOff, KeyRound } from 'lucide-react'
import type { UserDto } from '../types'

interface UserTableProps {
  users: UserDto[]
  isLoading: boolean
  onEdit: (user: UserDto) => void
  onToggleStatus: (user: UserDto) => void
  onResetPassword: (user: UserDto) => void
}

export function UserTable({
  users,
  isLoading,
  onEdit,
  onToggleStatus,
  onResetPassword,
}: UserTableProps) {
  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12 text-gray-500">
        Đang tải...
      </div>
    )
  }

  if (users.length === 0) {
    return (
      <div className="flex items-center justify-center py-12 text-gray-500">
        Không có dữ liệu
      </div>
    )
  }

  return (
    <div className="overflow-x-auto rounded-lg border">
      <table className="w-full text-sm">
        <thead className="bg-gray-50 border-b">
          <tr>
            <th className="px-4 py-3 text-left font-medium text-gray-600">Họ tên</th>
            <th className="px-4 py-3 text-left font-medium text-gray-600">Username</th>
            <th className="px-4 py-3 text-left font-medium text-gray-600">Email</th>
            <th className="px-4 py-3 text-left font-medium text-gray-600">Khoa/Phòng</th>
            <th className="px-4 py-3 text-left font-medium text-gray-600">Vai trò</th>
            <th className="px-4 py-3 text-left font-medium text-gray-600">Trạng thái</th>
            <th className="px-4 py-3 text-right font-medium text-gray-600">Thao tác</th>
          </tr>
        </thead>
        <tbody className="divide-y">
          {users.map((user) => (
            <tr key={user.id} className="hover:bg-gray-50">
              <td className="px-4 py-3 font-medium">{user.hoTen || '—'}</td>
              <td className="px-4 py-3 text-gray-600">{user.tenDangNhap}</td>
              <td className="px-4 py-3 text-gray-600">{user.email || '—'}</td>
              <td className="px-4 py-3 text-gray-600">{user.khoaPhong || '—'}</td>
              <td className="px-4 py-3">
                <RoleBadge role={user.tenVaiTro} />
              </td>
              <td className="px-4 py-3">
                <StatusBadge active={user.trangThai} />
              </td>
              <td className="px-4 py-3">
                <div className="flex items-center justify-end gap-1">
                  <Button
                    variant="ghost"
                    size="icon"
                    title="Sửa"
                    onClick={() => onEdit(user)}
                  >
                    <Edit className="h-4 w-4" />
                  </Button>
                  <Button
                    variant="ghost"
                    size="icon"
                    title={user.trangThai ? 'Vô hiệu hóa' : 'Kích hoạt'}
                    onClick={() => onToggleStatus(user)}
                  >
                    {user.trangThai ? (
                      <ShieldOff className="h-4 w-4 text-orange-500" />
                    ) : (
                      <ShieldCheck className="h-4 w-4 text-green-500" />
                    )}
                  </Button>
                  <Button
                    variant="ghost"
                    size="icon"
                    title="Reset mật khẩu"
                    onClick={() => onResetPassword(user)}
                  >
                    <KeyRound className="h-4 w-4 text-blue-500" />
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

function RoleBadge({ role }: { role: string | null }) {
  const colors: Record<string, string> = {
    Admin: 'bg-red-100 text-red-700',
    Teacher: 'bg-blue-100 text-blue-700',
    Student: 'bg-green-100 text-green-700',
    Supervisor: 'bg-purple-100 text-purple-700',
  }
  const colorClass = colors[role || ''] || 'bg-gray-100 text-gray-700'

  return (
    <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${colorClass}`}>
      {role || 'Unknown'}
    </span>
  )
}

function StatusBadge({ active }: { active: boolean | null }) {
  return active ? (
    <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-700">
      Hoạt động
    </span>
  ) : (
    <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-500">
      Vô hiệu
    </span>
  )
}
