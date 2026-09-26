import { useState, useEffect } from 'react'
import { usersApi } from '@/features/users/api'
import type { UserDto } from '@/features/users/types'
import type { ExamPaperDto } from '../types'
import { examsApi } from '../api'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { X, Search, Check } from 'lucide-react'

interface AssignUsersDialogProps {
  open: boolean
  exam: ExamPaperDto | null
  onClose: () => void
  onSubmit: (examId: number, userIds: number[], note?: string) => void
  isLoading: boolean
}

export function AssignUsersDialog({ open, exam, onClose, onSubmit, isLoading }: AssignUsersDialogProps) {
  const [users, setUsers] = useState<UserDto[]>([])
  const [selectedIds, setSelectedIds] = useState<number[]>([])
  // BUG FIX: this dialog used to always start with every checkbox unticked, with no idea who was
  // already assigned to this exam - the API (getAssignmentsByExam) and a Redux thunk for it existed
  // but nothing ever called them. Admins had no way to see existing assignments and no way to
  // unassign anyone from the UI at all (removeAssignment was likewise defined but never called).
  // Now fetched on open, pre-ticked, and unticking a previously-assigned student on Save unassigns them.
  const [assignmentIdByUserId, setAssignmentIdByUserId] = useState<Record<number, number>>({})
  const [initialUserIds, setInitialUserIds] = useState<number[]>([])
  const [searchKeyword, setSearchKeyword] = useState('')
  const [note, setNote] = useState('')
  const [loadingUsers, setLoadingUsers] = useState(false)
  const [removing, setRemoving] = useState(false)
  const [removeError, setRemoveError] = useState<string | null>(null)

  useEffect(() => {
    if (open && exam) {
      loadUsers()
      loadExistingAssignments(exam.id)
      setNote('')
      setRemoveError(null)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, exam?.id])

  async function loadUsers() {
    setLoadingUsers(true)
    try {
      const response = await usersApi.list({
        pageNumber: 1,
        pageSize: 100,
        roleId: 3, // Students only
        status: true,
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

  async function loadExistingAssignments(examId: number) {
    try {
      const response = await examsApi.getAssignmentsByExam(examId)
      if (response.data.success && response.data.data) {
        const active = response.data.data.filter((a) => a.isActive)
        const map: Record<number, number> = {}
        active.forEach((a) => { map[a.userId] = a.id })
        setAssignmentIdByUserId(map)
        const userIds = active.map((a) => a.userId)
        setInitialUserIds(userIds)
        setSelectedIds(userIds)
      } else {
        setAssignmentIdByUserId({})
        setInitialUserIds([])
        setSelectedIds([])
      }
    } catch {
      setAssignmentIdByUserId({})
      setInitialUserIds([])
      setSelectedIds([])
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

  const toAdd = selectedIds.filter((id) => !initialUserIds.includes(id))
  const toRemove = initialUserIds.filter((id) => !selectedIds.includes(id))

  async function handleSubmit() {
    if (!exam) return
    setRemoveError(null)
    let failedCount = 0
    if (toRemove.length > 0) {
      setRemoving(true)
      try {
        // BUG FIX: was Promise.all() in an empty catch - a single failed removal rejected the
        // whole batch (leaving successfully-removed users unknown to the admin) and the dialog
        // still closed as if everything succeeded, silently leaving those students assigned.
        // allSettled + counting failures lets us report exactly what happened.
        const results = await Promise.allSettled(
          toRemove.map((userId) => {
            const assignmentId = assignmentIdByUserId[userId]
            return assignmentId ? examsApi.removeAssignment(assignmentId) : Promise.resolve()
          })
        )
        failedCount = results.filter((r) => r.status === 'rejected').length
      } finally {
        setRemoving(false)
      }
    }

    if (failedCount > 0) {
      setRemoveError(`Hủy phân công thất bại cho ${failedCount} thí sinh. Vui lòng thử lại.`)
      return
    }

    if (toAdd.length > 0) {
      onSubmit(exam.id, toAdd, note || undefined)
    } else if (toRemove.length > 0) {
      onClose()
    }
  }

  if (!open || !exam) return null

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-lg max-h-[80vh] overflow-y-auto">
        <div className="flex items-center justify-between p-4 border-b sticky top-0 bg-white z-10">
          <div>
            <h2 className="text-lg font-semibold">Phân công thí sinh</h2>
            <p className="text-sm text-gray-500">{exam.examPaperName}</p>
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

          <p className="text-sm text-gray-500">
            {selectedIds.length} thí sinh đã chọn
            {initialUserIds.length > 0 && ` (${initialUserIds.length} đã được phân công từ trước)`}
          </p>

          <div className="max-h-60 overflow-y-auto border rounded divide-y">
            {loadingUsers ? (
              <p className="p-3 text-sm text-gray-500">Đang tải...</p>
            ) : users.length === 0 ? (
              <p className="p-3 text-sm text-gray-500">Không tìm thấy thí sinh</p>
            ) : (
              users.map((user) => {
                const isAlreadyAssigned = initialUserIds.includes(user.id)
                return (
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
                      <p className="text-sm font-medium">{user.fullName || user.username}</p>
                      <p className="text-xs text-gray-400">{user.department} • {user.employeeCode}</p>
                    </div>
                    {isAlreadyAssigned && (
                      <span className="text-xs text-green-700 bg-green-50 border border-green-200 rounded px-1.5 py-0.5 shrink-0">
                        Đã phân công
                      </span>
                    )}
                  </div>
                )
              })
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

          {removeError && (
            <p className="text-xs text-red-600 bg-red-50 border border-red-200 rounded px-2 py-1.5">{removeError}</p>
          )}

          <div className="flex justify-end gap-2 pt-2 border-t">
            <Button variant="outline" onClick={onClose}>Hủy</Button>
            <Button
              onClick={handleSubmit}
              disabled={(toAdd.length === 0 && toRemove.length === 0) || isLoading || removing}
            >
              {removing
                ? 'Đang hủy phân công...'
                : isLoading
                  ? 'Đang phân công...'
                  : toAdd.length > 0 && toRemove.length > 0
                    ? `Lưu (+${toAdd.length} / -${toRemove.length})`
                    : toRemove.length > 0
                      ? `Hủy phân công (${toRemove.length})`
                      : `Phân công (${toAdd.length})`}
            </Button>
          </div>
        </div>
      </div>
    </div>
  )
}
