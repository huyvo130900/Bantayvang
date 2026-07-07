import { useEffect, useState, useCallback } from 'react'
import { useAppDispatch, useAppSelector } from '@/app/hooks'
import { fetchUsers, createUser, updateUser, toggleUserStatus, setFilter } from '../slice'
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
  const myKhoa = currentUser?.tenKhoaQuanLy || currentUser?.khoaPhong || null

  const [formOpen, setFormOpen] = useState(false)
  const [importOpen, setImportOpen] = useState(false)
  const [editingUser, setEditingUser] = useState<UserDto | null>(null)
  const [resetPwUser, setResetPwUser] = useState<UserDto | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [successMsg, setSuccessMsg] = useState<string | null>(null)
  const [errorMsg, setErrorMsg] = useState<string | null>(null)

  useEffect(() => {
    if (isDeptManager && myKhoa) {
      dispatch(setFilter({ khoaPhong: myKhoa, idVaiTro: 3, pageNumber: 1 }))
    }
  }, [isDeptManager, myKhoa, dispatch])

  useEffect(() => {
    dispatch(fetchUsers(filter))
  }, [dispatch, filter])

  const hasNextPage = users.length === filter.pageSize

  const handleFilterChange = useCallback(
    (changes: Partial<UserFilterDto>) => {
      if (isDeptManager && myKhoa) {
        changes = { ...changes, khoaPhong: myKhoa, idVaiTro: 3 }
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
        sanitizedData.idVaiTro = 3
        sanitizedData.khoaPhong = myKhoa
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

  const handleToggleStatus = async (user: UserDto) => {
    const activate = !user.trangThai
    if (!window.confirm(
      activate
        ? `Kích hoạt tài khoản "${user.hoTen || user.tenDangNhap}"?`
        : `Vô hiệu hóa tài khoản "${user.hoTen || user.tenDangNhap}"?`
    )) return
    try {
      await dispatch(toggleUserStatus({ id: user.id, activate })).unwrap()
      showMsg(activate ? 'Đã kích hoạt tài khoản' : 'Đã vô hiệu hóa tài khoản')
    } catch {
      showMsg('Không thể thay đổi trạng thái', true)
    }
  }

  const handleDelete = async (user: UserDto) => {
    if (!window.confirm(`Xóa tài khoản "${user.hoTen || user.tenDangNhap}"? Không thể hoàn tác.`)) return
    try {
      await usersApi.delete(user.id)
      showMsg('Đã xóa tài khoản')
      dispatch(fetchUsers(filter))
    } catch {
      showMsg('Không thể xóa tài khoản này', true)
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

      <UserTable
        users={users}
        isLoading={isLoading}
        onEdit={handleEdit}
        onToggleStatus={handleToggleStatus}
        onResetPassword={(user) => setResetPwUser(user)}
        onDelete={handleDelete}
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
