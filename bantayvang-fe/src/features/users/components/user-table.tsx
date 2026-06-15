import { Button } from '@/components/ui/button'
import { Edit, ShieldCheck, ShieldOff, KeyRound, Trash2 } from 'lucide-react'
import type { UserDto } from '../types'

interface UserTableProps {
  users: UserDto[]
  isLoading: boolean
  onEdit: (user: UserDto) => void
  onToggleStatus: (user: UserDto) => void
  onResetPassword: (user: UserDto) => void
  onDelete?: (user: UserDto) => void
}

export function UserTable({ users, isLoading, onEdit, onToggleStatus, onResetPassword, onDelete }: UserTableProps) {
  if (isLoading) {
    return (
      <div className="space-y-2 rounded-lg border overflow-hidden">
        {[1, 2, 3, 4, 5].map((i) => (
          <div key={i} className="h-14 bg-gray-50 animate-pulse border-b" />
        ))}
      </div>
    )
  }

  if (users.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-16 text-gray-400 border-2 border-dashed rounded-xl">
        <span className="text-4xl mb-3">👤</span>
        <p className="font-medium text-gray-500">Không tìm thấy người dùng nào</p>
        <p className="text-sm mt-1">Thử thay đổi bộ lọc hoặc tạo tài khoản mới</p>
      </div>
    )
  }

  return (
    <div className="overflow-x-auto rounded-xl border shadow-sm">
      <table className="w-full text-sm">
        <thead className="bg-gray-50 border-b">
          <tr>
            <th className="px-4 py-3 text-left font-medium text-gray-600 whitespace-nowrap">Họ tên</th>
            <th className="px-4 py-3 text-left font-medium text-gray-600 whitespace-nowrap">Username</th>
            <th className="px-4 py-3 text-left font-medium text-gray-600 whitespace-nowrap">Khoa/Phòng</th>
            <th className="px-4 py-3 text-left font-medium text-gray-600 whitespace-nowrap">Vai trò</th>
            <th className="px-4 py-3 text-left font-medium text-gray-600 whitespace-nowrap">Trạng thái</th>
            <th className="px-4 py-3 text-right font-medium text-gray-600 whitespace-nowrap">Thao tác</th>
          </tr>
        </thead>
        <tbody className="divide-y">
          {users.map((user) => (
            <tr key={user.id} className="hover:bg-gray-50 transition-colors">
              <td className="px-4 py-3">
                <p className="font-medium text-gray-900">{user.hoTen || '—'}</p>
                {user.maNhanVien && (
                  <p className="text-xs text-gray-400">{user.maNhanVien}</p>
                )}
              </td>
              <td className="px-4 py-3 text-gray-600">{user.tenDangNhap}</td>
              <td className="px-4 py-3 text-xs">
                {user.idVaiTro === 5 && user.tenKhoaQuanLy ? (
                  <span className="text-blue-700 font-medium">{user.tenKhoaQuanLy}</span>
                ) : (
                  <span className="text-gray-600">{user.khoaPhong || '—'}</span>
                )}
              </td>
              <td className="px-4 py-3">
                <RoleBadge role={user.tenVaiTro} />
              </td>
              <td className="px-4 py-3">
                <StatusBadge active={user.trangThai} />
              </td>
              <td className="px-4 py-3">
                <div className="flex items-center justify-end gap-0.5">
                  <Button
                    variant="ghost"
                    size="icon"
                    title="Sửa thông tin"
                    onClick={() => onEdit(user)}
                    className="h-8 w-8 text-gray-400 hover:text-yellow-600"
                  >
                    <Edit className="h-4 w-4" />
                  </Button>
                  <Button
                    variant="ghost"
                    size="icon"
                    title={user.trangThai ? 'Vô hiệu hóa' : 'Kích hoạt'}
                    onClick={() => onToggleStatus(user)}
                    className="h-8 w-8 text-gray-400 hover:text-blue-600"
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
                    title="Đặt lại mật khẩu"
                    onClick={() => onResetPassword(user)}
                    className="h-8 w-8 text-gray-400 hover:text-blue-600"
                  >
                    <KeyRound className="h-4 w-4" />
                  </Button>
                  {onDelete && (
                    <Button
                      variant="ghost"
                      size="icon"
                      title="Xóa tài khoản"
                      onClick={() => onDelete(user)}
                      className="h-8 w-8 text-gray-400 hover:text-red-600"
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

function RoleBadge({ role }: { role: string | null }) {
  const config: Record<string, { color: string; label: string }> = {
    Admin:        { color: 'bg-red-100 text-red-700',     label: 'Admin' },
    DeptManager:  { color: 'bg-blue-100 text-blue-700',   label: 'Quản lý Khoa' },
    Student:      { color: 'bg-green-100 text-green-700', label: 'Thí sinh' },
    // Obsolete — keep for backward compat display
    Teacher:      { color: 'bg-gray-100 text-gray-500',   label: 'Teacher (cũ)' },
    Supervisor:   { color: 'bg-gray-100 text-gray-500',   label: 'Supervisor (cũ)' },
  }
  const { color, label } = config[role || ''] || { color: 'bg-gray-100 text-gray-700', label: role || 'Unknown' }
  return (
    <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${color}`}>
      {label}
    </span>
  )
}

function StatusBadge({ active }: { active: boolean | null }) {
  return active ? (
    <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-700">
      <span className="w-1.5 h-1.5 rounded-full bg-green-500" />
      Hoạt động
    </span>
  ) : (
    <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-500">
      <span className="w-1.5 h-1.5 rounded-full bg-gray-400" />
      Vô hiệu
    </span>
  )
}
