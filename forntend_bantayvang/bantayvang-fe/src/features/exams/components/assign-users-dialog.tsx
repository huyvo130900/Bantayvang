import { useState, useEffect } from 'react'
import { usersApi } from '@/features/users/api'
import type { UserDto } from '@/features/users/types'
import type { DethiDto } from '../types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { X, Search, Check } from 'lucide-react'

interface AssignUsersDialogProps {
  open: boolean
  exam: DethiDto | null
  onClose: () => void
  onSubmit: (examId: number, userIds: number[], note?: string) => void
  isLoading: boolean
}

export function AssignUsersDialog({ open, exam, onClose, onSubmit, isLoading }: AssignUsersDialogProps) {
  const [users, setUsers] = useState<UserDto[]>([])
  const [selectedIds, setSelectedIds] = useState<number[]>([])
  const [searchKeyword, setSearchKeyword] = useState('')
  const [note, setNote] = useState('')
  const [loadingUsers, setLoadingUsers] = useState(false)

  useEffect(() => {
    if (open) {
      loadUsers()
      setSelectedIds([])
      setNote('')
    }
  }, [open])

  const loadUsers = async () => {
    setLoadingUsers(true)
    try {
      const response = await usersApi.list({
        pageNumber: 1,
        pageSize: 100,
        idVaiTro: 3, // Students only
        trangThai: true,
        searchKeyword: searchKeyword || undefined,
      })
      if (response.data.success && response.data.data) {
        setUsers(response.data.data)
      }
    } catch {
      // handle error
    } finally {
      setLoadingUsers(false)
    }
  }

  const toggleUser = (id: number) => {
    setSelectedIds((prev) =>
      prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id]
    )
  }

  const selectAll = () => {
    setSelectedIds(users.map((u) => u.id))
  }

  if (!open || !exam) return null

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-lg max-h-[80vh] overflow-y-auto">
        <div className="flex items-center justify-between p-4 border-b sticky top-0 bg-white z-10">
          <div>
            <h2 className="text-lg font-semibold">Phân công thí sinh</h2>
            <p className="text-sm text-gray-500">{exam.tenDeThi}</p>
          </div>
          <Button variant="ghost" size="icon" onClick={onClose}>
            <X className="h-4 w-4" />
          </Button>
        </div>

        <div className="p-4 space-y-3">
          <div className="flex gap-2">
            <div className="relative flex-1">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400" />
              <Input
                placeholder="Tìm thí sinh..."
                className="pl-9"
                value={searchKeyword}
                onChange={(e) => setSearchKeyword(e.target.value)}
                onKeyDown={(e) => e.key === 'Enter' && loadUsers()}
              />
            </div>
            <Button variant="outline" size="sm" onClick={selectAll}>
              Chọn tất cả
            </Button>
          </div>

          <p className="text-sm text-gray-500">{selectedIds.length} thí sinh đã chọn</p>

          <div className="max-h-60 overflow-y-auto border rounded divide-y">
            {loadingUsers ? (
              <p className="p-3 text-sm text-gray-500">Đang tải...</p>
            ) : users.length === 0 ? (
              <p className="p-3 text-sm text-gray-500">Không tìm thấy thí sinh</p>
            ) : (
              users.map((user) => (
                <div
                  key={user.id}
                  className="flex items-center gap-3 px-3 py-2 hover:bg-gray-50 cursor-pointer"
                  onClick={() => toggleUser(user.id)}
                >
                  <div
                    className={`h-5 w-5 rounded border flex items-center justify-center shrink-0 ${
                      selectedIds.includes(user.id)
                        ? 'bg-primary border-primary text-white'
                        : 'border-gray-300'
                    }`}
                  >
                    {selectedIds.includes(user.id) && <Check className="h-3 w-3" />}
                  </div>
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium">{user.hoTen || user.tenDangNhap}</p>
                    <p className="text-xs text-gray-400">{user.khoaPhong} • {user.maNhanVien}</p>
                  </div>
                </div>
              ))
            )}
          </div>

          <div className="space-y-1">
            <label className="text-sm font-medium text-gray-700">Ghi chú (tùy chọn)</label>
            <Input
              value={note}
              onChange={(e) => setNote(e.target.value)}
              placeholder="Ghi chú phân công..."
            />
          </div>

          <div className="flex justify-end gap-2 pt-2 border-t">
            <Button variant="outline" onClick={onClose}>Hủy</Button>
            <Button
              onClick={() => onSubmit(exam.id, selectedIds, note || undefined)}
              disabled={selectedIds.length === 0 || isLoading}
            >
              {isLoading ? 'Đang phân công...' : `Phân công (${selectedIds.length})`}
            </Button>
          </div>
        </div>
      </div>
    </div>
  )
}
