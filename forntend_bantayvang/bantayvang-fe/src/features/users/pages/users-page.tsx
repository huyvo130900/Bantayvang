import { useEffect, useState, useCallback } from 'react'
import { useAppDispatch, useAppSelector } from '@/app/hooks'
import { fetchUsers, createUser, updateUser, toggleUserStatus, setFilter } from '../slice'
import { usersApi } from '../api'
import { UserFilter } from '../components/user-filter'
import { UserTable } from '../components/user-table'
import { UserFormDialog } from '../components/user-form-dialog'
import { ResetPasswordDialog } from '../components/reset-password-dialog'
import type { UserDto, UserFilterDto } from '../types'
import type { CreateUserFormData, UpdateUserFormData } from '../schemas'

export function UsersPage() {
  const dispatch = useAppDispatch()
  const { users, isLoading, filter } = useAppSelector((state) => state.users)

  const [formOpen, setFormOpen] = useState(false)
  const [editingUser, setEditingUser] = useState<UserDto | null>(null)
  const [resetPwUser, setResetPwUser] = useState<UserDto | null>(null)
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    dispatch(fetchUsers(filter))
  }, [dispatch, filter])

  const handleFilterChange = useCallback(
    (changes: Partial<UserFilterDto>) => {
      dispatch(setFilter(changes))
    },
    [dispatch]
  )

  const handleCreate = () => {
    setEditingUser(null)
    setFormOpen(true)
  }

  const handleEdit = (user: UserDto) => {
    setEditingUser(user)
    setFormOpen(true)
  }

  const handleFormSubmit = async (data: CreateUserFormData | UpdateUserFormData) => {
    setSubmitting(true)
    try {
      if (editingUser) {
        await dispatch(updateUser({ id: editingUser.id, data: data as UpdateUserFormData })).unwrap()
      } else {
        await dispatch(createUser(data as CreateUserFormData)).unwrap()
      }
      setFormOpen(false)
      dispatch(fetchUsers(filter))
    } catch {
      // Error handled by slice
    } finally {
      setSubmitting(false)
    }
  }

  const handleToggleStatus = async (user: UserDto) => {
    const activate = !user.trangThai
    const confirmMsg = activate
      ? `Kích hoạt tài khoản "${user.hoTen || user.tenDangNhap}"?`
      : `Vô hiệu hóa tài khoản "${user.hoTen || user.tenDangNhap}"?`

    if (!window.confirm(confirmMsg)) return
    await dispatch(toggleUserStatus({ id: user.id, activate }))
  }

  const handleResetPassword = async (userId: number, newPassword: string) => {
    setSubmitting(true)
    try {
      await usersApi.resetPassword(userId, newPassword)
      setResetPwUser(null)
    } catch {
      // handle error
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div>
      <h1 className="text-2xl font-bold text-gray-900 mb-6">Quản lý người dùng</h1>

      <UserFilter
        filter={filter}
        onFilterChange={handleFilterChange}
        onCreateClick={handleCreate}
      />

      <UserTable
        users={users}
        isLoading={isLoading}
        onEdit={handleEdit}
        onToggleStatus={handleToggleStatus}
        onResetPassword={(user) => setResetPwUser(user)}
      />

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
    </div>
  )
}
