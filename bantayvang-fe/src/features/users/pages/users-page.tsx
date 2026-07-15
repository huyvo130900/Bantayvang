import { useEffect, useState, useCallback } from 'react'
import { useAppDispatch, useAppSelector } from '@/app/hooks'
import { fetchUsers, createUser, updateUser, toggleUserStatus, setFilter, deleteUser, restoreUser, hardDeleteUser, bulkDeleteUsers } from '../slice'
import { usersApi } from '../api'
import { UserFilter } from '../components/user-filter'
import { UserTable } from '../components/user-table'
import { UserFormDialog } from '../components/user-form-dialog'
import { ResetPasswordDialog } from '../components/reset-password-dialog'
import { ImportUsersDialog } from '../components/import-users-dialog'
import type { UserDto, UserFilterDto } from '../types'
import type { CreateUserFormData, UpdateUserFormData } from '../schemas'
import { Button } from '@/components/ui/button'
import { ChevronLeft, ChevronRight, RefreshCw, FileSpreadsheet } from 'lucide-react'

export function UsersPage() {
  const dispatch = useAppDispatch()
  const { users, isLoading, filter } = useAppSelector((state) => state.users)
  const currentUser = useAppSelector((state) => state.auth.user)

  const isDeptManager = currentUser?.role === 'DeptManager' || currentUser?.tenVaiTro === 'DeptManager'
  const myKhoa = currentUser?.tenKhoaQuanLy || currentUser?.department || null

  const [formOpen, setFormOpen] = useState(false)
  const [importOpen, setImportOpen] = useState(false)
  const [editingUser, setEditingUser] = useState<UserDto | null>(null)
  const [resetPwUser, setResetPwUser] = useState<UserDto | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [successMsg, setSuccessMsg] = useState<string | null>(null)
  const [errorMsg, setErrorMsg] = useState<string | null>(null)
  const [selectedIds, setSelectedIds] = useState<number[]>([])

  useEffect(() => {
    if (isDeptManager && myKhoa) {
      dispatch(setFilter({ department: myKhoa, roleId: 3, pageNumber: 1 }))
    }
  }, [isDeptManager, myKhoa, dispatch])

  useEffect(() => {
    dispatch(fetchUsers(filter))
    setSelectedIds([])
  }, [dispatch, filter])

  const hasNextPage = users.length === filter.pageSize

  const handleFilterChange = useCallback(
    (changes: Partial<UserFilterDto>) => {
      if (isDeptManager && myKhoa) {
        changes = { ...changes, department: myKhoa, roleId: 3 }
      }
      dispatch(setFilter({ ...changes, pageNumber: 1 }))
    },
    [dispatch, isDeptManager, myKhoa]
  )

  const showMsg = (msg: string, isError = false) => {
    if (isError) {
      setErrorMsg(msg)
      setTimeout(() => setErrorMsg(null), 4000)
    } else {
      setSuccessMsg(msg)
      setTimeout(() => setSuccessMsg(null), 3000)
    }
  }

  const handleCreate = () => { setEditingUser(null); setFormOpen(true) }
  const handleEdit = (user: UserDto) => { setEditingUser(user); setFormOpen(true) }

  const handleFormSubmit = async (data: CreateUserFormData | UpdateUserFormData) => {
    setSubmitting(true)
    try {
      const sanitizedData = {
        ...data,
        idKhoaQuanLy: data.idKhoaQuanLy || undefined
      } as any

      if (isDeptManager && myKhoa) {
        sanitizedData.roleId = 3
        sanitizedData.department = myKhoa
      }

      if (editingUser) {
        await dispatch(updateUser({ id: editingUser.id, data: sanitizedData as any })).unwrap()
        showMsg('Cập nhật người dùng thành công')
      } else {
        await dispatch(createUser(sanitizedData as any)).unwrap()
        showMsg('Tạo tài khoản thành công')
      }
      setFormOpen(false)
      dispatch(fetchUsers(filter))
    } catch (err: unknown) {
      const errorMessage = typeof err === 'string' ? err : (err as { message?: string })?.message || 'Có lỗi xảy ra'
      showMsg(errorMessage, true)
    } finally {
      setSubmitting(false)
    }
  }

  const handleDelete = async (user: UserDto) => {
    if (!window.confirm(`Bạn có chắc chắn muốn xóa tài khoản "${user.fullName || user.username}" không?\nTài khoản sẽ được đưa vào Thùng rác và có thể khôi phục lại sau.`)) {
      return
    }
    
    setSubmitting(true)
    try {
      await dispatch(deleteUser(user.id)).unwrap()
      showMsg('Xóa tài khoản thành công')
      dispatch(fetchUsers(filter))
    } catch (err: unknown) {
      const errorMessage = typeof err === 'string' ? err : (err as { message?: string })?.message || 'Có lỗi xảy ra khi xóa'
      showMsg(errorMessage, true)
    } finally {
      setSubmitting(false)
    }
  }

  const handleRestore = async (user: UserDto) => {
    if (!window.confirm(`Bạn có chắc chắn muốn khôi phục tài khoản "${user.fullName || user.username}" không?`)) {
      return
    }
    
    setSubmitting(true)
    try {
      await dispatch(restoreUser(user.id)).unwrap()
      showMsg('Khôi phục tài khoản thành công')
      dispatch(fetchUsers(filter))
    } catch (err: unknown) {
      const errorMessage = typeof err === 'string' ? err : (err as { message?: string })?.message || 'Có lỗi xảy ra khi khôi phục'
      showMsg(errorMessage, true)
    } finally {
      setSubmitting(false)
    }
  }

  const handleHardDelete = async (user: UserDto) => {
    if (!window.confirm(`XÓA VĨNH VIỄN: Bạn có chắc chắn muốn xóa vĩnh viễn tài khoản "${user.fullName || user.username}" không?\nToàn bộ bài thi, điểm số và dữ liệu liên quan sẽ bị xóa sạch và KHÔNG THỂ HOÀN TÁC.`)) {
      return
    }
    
    setSubmitting(true)
    try {
      await dispatch(hardDeleteUser(user.id)).unwrap()
      showMsg('Xóa vĩnh viễn tài khoản thành công')
      dispatch(fetchUsers(filter))
    } catch (err: unknown) {
      const errorMessage = typeof err === 'string' ? err : (err as { message?: string })?.message || 'Có lỗi xảy ra khi xóa vĩnh viễn'
      showMsg(errorMessage, true)
    } finally {
      setSubmitting(false)
    }
  }

  const handleSelectId = useCallback((id: number, selected: boolean) => {
    setSelectedIds(prev => selected ? [...prev, id] : prev.filter(x => x !== id))
  }, [])

  const handleSelectAll = useCallback((selected: boolean) => {
    setSelectedIds(selected ? users.map(u => u.id) : [])
  }, [users])

  const handleBulkDelete = async () => {
    if (!window.confirm(`Bạn có chắc chắn muốn đưa ${selectedIds.length} tài khoản đã chọn vào Thùng rác không?`)) {
      return
    }
    
    setSubmitting(true)
    try {
      await dispatch(bulkDeleteUsers(selectedIds)).unwrap()
      showMsg(`Đã xóa ${selectedIds.length} tài khoản thành công`)
      setSelectedIds([])
      dispatch(fetchUsers(filter))
    } catch (err: unknown) {
      const errorMessage = typeof err === 'string' ? err : (err as { message?: string })?.message || 'Có lỗi xảy ra khi xóa hàng loạt'
      showMsg(errorMessage, true)
    } finally {
      setSubmitting(false)
    }
  }

  const handleToggleStatus = async (user: UserDto) => {
    const activate = !user.status
    if (!window.confirm(
      activate
        ? `Kích hoạt tài khoản "${user.fullName || user.username}"?`
        : `Vô hiệu hóa tài khoản "${user.fullName || user.username}"?`
    )) return
    try {
      await dispatch(toggleUserStatus({ id: user.id, activate })).unwrap()
      showMsg(activate ? 'Đã kích hoạt tài khoản' : 'Đã vô hiệu hóa tài khoản')
    } catch {
      showMsg('Không thể thay đổi trạng thái', true)
    }
  }

  const handleResetPassword = async (userId: number, newPassword: string) => {
    setSubmitting(true)
    try {
      await usersApi.resetPassword(userId, newPassword)
      setResetPwUser(null)
      showMsg('Đã đặt lại mật khẩu thành công')
    } catch {
      showMsg('Không thể đặt lại mật khẩu', true)
    } finally {
      setSubmitting(false)
    }
  }

  const currentPage = filter.pageNumber

  return (
    <div>
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Quản lý người dùng</h1>
        <div className="flex gap-2 flex-wrap">
          <Button variant="outline" size="sm" onClick={() => dispatch(fetchUsers(filter))}>
            <RefreshCw className="h-4 w-4 mr-1" />
            Làm mới
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={() => setImportOpen(true)}
            className="text-green-700 border-green-200 hover:bg-green-50"
          >
            <FileSpreadsheet className="h-4 w-4 mr-1" />
            Nhập từ Excel
          </Button>
        </div>
      </div>

      {/* Toast messages */}
      {successMsg && (
        <div className="mb-4 flex items-center gap-2 bg-green-50 border border-green-200 text-green-700 rounded-xl px-4 py-2.5 text-sm">
          ✓ {successMsg}
        </div>
      )}
      {errorMsg && (
        <div className="mb-4 flex items-center gap-2 bg-red-50 border border-red-200 text-red-700 rounded-xl px-4 py-2.5 text-sm">
          ⚠ {errorMsg}
        </div>
      )}

      <UserFilter
        filter={filter}
        onFilterChange={handleFilterChange}
        onCreateClick={handleCreate}
        hideRoleFilter={isDeptManager}
      />

      <div className="flex items-center justify-between mb-4 min-h-[40px]">
        <div>
          {selectedIds.length > 0 && (
            <div className="flex items-center gap-3 bg-red-50 border border-red-200 px-4 py-1.5 rounded-lg animate-in fade-in slide-in-from-left-4">
              <span className="text-sm font-medium text-red-700">Đã chọn {selectedIds.length} dòng</span>
              <Button variant="destructive" size="sm" onClick={handleBulkDelete} disabled={submitting} className="h-7 text-xs">
                Xóa hàng loạt
              </Button>
            </div>
          )}
        </div>
        <label className="flex items-center gap-2 cursor-pointer">
          <input
            type="checkbox"
            checked={!!filter.includeDeleted}
            onChange={(e) => dispatch(setFilter({ includeDeleted: e.target.checked, pageNumber: 1 }))}
            className="w-4 h-4 text-primary border-gray-300 rounded focus:ring-primary"
          />
          <span className="text-sm font-medium text-gray-700">Hiển thị tài khoản đã xóa (Thùng rác)</span>
        </label>
      </div>

      <UserTable
        users={users}
        isLoading={isLoading}
        onEdit={handleEdit}
        onToggleStatus={handleToggleStatus}
        onResetPassword={(user) => setResetPwUser(user)}
        onDelete={handleDelete}
        onRestore={handleRestore}
        onHardDelete={handleHardDelete}
        selectedIds={selectedIds}
        onSelectId={handleSelectId}
        onSelectAll={handleSelectAll}
      />

      {/* Pagination */}
      <div className="flex items-center justify-between mt-4 flex-wrap gap-3">
        <div className="flex items-center gap-2">
          <span className="text-sm text-gray-500">Hiển thị</span>
          <select
            value={filter.pageSize}
            onChange={(e) => dispatch(setFilter({ pageSize: Number(e.target.value), pageNumber: 1 }))}
            className="text-sm border border-gray-200 rounded-lg px-2 py-1.5 bg-white text-gray-700 focus:outline-none focus:ring-2 focus:ring-primary/30 cursor-pointer"
          >
            {[10, 20, 50, 100].map((size) => (
              <option key={size} value={size}>{size} dòng</option>
            ))}
          </select>
          <span className="text-sm text-gray-500">
            · Trang {currentPage} · {users.length} người dùng
          </span>
        </div>
        {(currentPage > 1 || hasNextPage) && (
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => dispatch(setFilter({ pageNumber: currentPage - 1 }))}
              disabled={currentPage <= 1 || isLoading}
            >
              <ChevronLeft className="h-4 w-4 mr-1" />
              Trước
            </Button>
            <span className="text-sm font-medium w-8 text-center">{currentPage}</span>
            <Button
              variant="outline"
              size="sm"
              onClick={() => dispatch(setFilter({ pageNumber: currentPage + 1 }))}
              disabled={!hasNextPage || isLoading}
            >
              Sau
              <ChevronRight className="h-4 w-4 ml-1" />
            </Button>
          </div>
        )}
      </div>


      <UserFormDialog
        open={formOpen}
        user={editingUser}
        onClose={() => setFormOpen(false)}
        onSubmit={handleFormSubmit}
        isLoading={submitting}
      />

      <ResetPasswordDialog
        open={!!resetPwUser}
        user={resetPwUser}
        onClose={() => setResetPwUser(null)}
        onSubmit={handleResetPassword}
        isLoading={submitting}
      />

      <ImportUsersDialog
        open={importOpen}
        onClose={() => setImportOpen(false)}
        onSuccess={() => {
          dispatch(fetchUsers(filter))
          showMsg('Nhập tài khoản thành công!')
        }}
      />
    </div>
  )
}
