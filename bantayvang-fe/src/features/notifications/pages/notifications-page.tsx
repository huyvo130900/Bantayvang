import { useEffect, useState } from 'react'
import { notificationsApi, type NotificationDto, type CreateNotificationDto } from '../api'
import { usersApi } from '@/features/users/api'
import type { UserDto } from '@/features/users/types'
import { departmentApi } from '@/features/departments/api'
import type { DepartmentDto } from '@/features/departments/types'
import { useAppSelector } from '@/app/hooks'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Bell, BellOff, CheckCheck, Trash2, Send, X, RefreshCw, Filter } from 'lucide-react'
import { formatDate } from '@/lib/utils'

const TYPE_OPTIONS = [
  { value: 'Info', label: 'Thông tin', cls: 'bg-blue-100 text-blue-700' },
  { value: 'Warning', label: 'Cảnh báo', cls: 'bg-yellow-100 text-yellow-700' },
  { value: 'Success', label: 'Thành công', cls: 'bg-green-100 text-green-700' },
  { value: 'Error', label: 'Lỗi', cls: 'bg-red-100 text-red-700' },
]

export function NotificationsPage() {
  const [notifications, setNotifications] = useState<NotificationDto[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [showCreate, setShowCreate] = useState(false)
  const [unreadOnly, setUnreadOnly] = useState(false)
  const [toast, setToast] = useState<string | null>(null)

  useEffect(() => {
    loadNotifications()
  }, [unreadOnly]) // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    const handleNotificationReceived = () => {
      loadNotifications()
    }
    window.addEventListener('notification:received', handleNotificationReceived)
    return () => {
      window.removeEventListener('notification:received', handleNotificationReceived)
    }
  }, [])

  const loadNotifications = async () => {
    setIsLoading(true)
    try {
      const response = await notificationsApi.getMyNotifications(unreadOnly || undefined)
      if (response.data.success && response.data.data) {
        setNotifications(response.data.data)
      }
    } catch {
      // silent
    } finally {
      setIsLoading(false)
    }
  }

  const showToastMsg = (msg: string) => {
    setToast(msg)
    setTimeout(() => setToast(null), 3000)
  }

  const handleMarkAllRead = async () => {
    try {
      await notificationsApi.markAllAsRead()
      showToastMsg('Đã đánh dấu tất cả là đã đọc')
      loadNotifications()
    } catch { /* silent */ }
  }

  const handleDelete = async (id: number) => {
    try {
      await notificationsApi.delete(id)
      setNotifications((prev) => prev.filter((n) => n.id !== id))
    } catch { /* silent */ }
  }

  const handleMarkRead = async (id: number) => {
    try {
      await notificationsApi.markAsRead(id)
      setNotifications((prev) =>
        prev.map((n) => (n.id === id ? { ...n, isRead: true } : n))
      )
    } catch { /* silent */ }
  }

  const handleCreateNotification = async (data: CreateNotificationDto) => {
    try {
      if (data.userId || data.khoaPhong) {
        await notificationsApi.create(data)
      } else {
        await notificationsApi.broadcast(data)
      }
      showToastMsg('Đã gửi thông báo thành công')
      setShowCreate(false)
      loadNotifications()
    } catch {
      showToastMsg('Gửi thông báo thất bại')
    }
  }

  const unreadCount = notifications.filter((n) => !n.isRead).length

  return (
    <div>
      {/* Toast */}
      {toast && (
        <div className="fixed top-4 right-4 z-50 bg-green-600 text-white px-4 py-3 rounded-xl shadow-lg text-sm font-medium">
          ✓ {toast}
        </div>
      )}

      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Thông báo</h1>
          {unreadCount > 0 && (
            <p className="text-sm text-gray-500 mt-0.5">
              <span className="text-primary font-semibold">{unreadCount}</span> chưa đọc
            </p>
          )}
        </div>
        <div className="flex gap-2 flex-wrap">
          <Button variant="outline" size="sm" onClick={loadNotifications} disabled={isLoading}>
            <RefreshCw className={`h-4 w-4 mr-1 ${isLoading ? 'animate-spin' : ''}`} />
            Làm mới
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={() => setUnreadOnly(!unreadOnly)}
            className={unreadOnly ? 'border-primary text-primary bg-primary/5' : ''}
          >
            <Filter className="h-4 w-4 mr-1" />
            {unreadOnly ? 'Tất cả' : 'Chưa đọc'}
          </Button>
          {unreadCount > 0 && (
            <Button variant="outline" size="sm" onClick={handleMarkAllRead}>
              <CheckCheck className="h-4 w-4 mr-1" />
              Đọc tất cả
            </Button>
          )}
          <Button size="sm" onClick={() => setShowCreate(true)}>
            <Send className="h-4 w-4 mr-1" />
            Gửi thông báo
          </Button>
        </div>
      </div>

      {isLoading ? (
        <div className="space-y-2">
          {[1, 2, 3].map((i) => (
            <div key={i} className="h-20 bg-gray-50 rounded-xl animate-pulse border" />
          ))}
        </div>
      ) : notifications.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-16 text-gray-400 border-2 border-dashed rounded-xl">
          <BellOff className="h-12 w-12 mb-3 opacity-30" />
          <p className="font-medium text-gray-500">
            {unreadOnly ? 'Không có thông báo chưa đọc' : 'Không có thông báo nào'}
          </p>
        </div>
      ) : (
        <div className="space-y-2">
          {notifications.map((n) => {
            const typeInfo = TYPE_OPTIONS.find((t) => t.value === n.type)
            return (
              <div
                key={n.id}
                className={`flex items-start gap-3 p-4 rounded-xl border transition-colors ${
                  n.isRead
                    ? 'bg-white border-gray-200'
                    : 'bg-blue-50 border-blue-200'
                }`}
              >
                <div className={`shrink-0 p-1.5 rounded-lg ${n.isRead ? 'bg-gray-100' : 'bg-blue-100'}`}>
                  <Bell className={`h-4 w-4 ${n.isRead ? 'text-gray-400' : 'text-blue-500'}`} />
                </div>
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 flex-wrap">
                    <p className={`text-sm font-semibold ${n.isRead ? 'text-gray-700' : 'text-gray-900'}`}>
                      {n.title}
                    </p>
                    {typeInfo && (
                      <span className={`text-xs px-1.5 py-0.5 rounded-full font-medium ${typeInfo.cls}`}>
                        {typeInfo.label}
                      </span>
                    )}
                    {!n.isRead && (
                      <span className="h-2 w-2 rounded-full bg-blue-500 shrink-0" />
                    )}
                  </div>
                  <p className="text-sm text-gray-600 mt-0.5 whitespace-pre-wrap">{n.message}</p>
                  <p className="text-xs text-gray-400 mt-1">{formatDate(n.createdAt)}</p>
                </div>
                <div className="flex gap-1 shrink-0">
                  {!n.isRead && (
                    <Button
                      variant="ghost"
                      size="icon"
                      title="Đánh dấu đã đọc"
                      onClick={() => handleMarkRead(n.id)}
                      className="h-8 w-8 text-blue-400 hover:text-blue-600"
                    >
                      <CheckCheck className="h-4 w-4" />
                    </Button>
                  )}
                  <Button
                    variant="ghost"
                    size="icon"
                    title="Xóa thông báo"
                    onClick={() => handleDelete(n.id)}
                    className="h-8 w-8 text-gray-300 hover:text-red-500"
                  >
                    <Trash2 className="h-4 w-4" />
                  </Button>
                </div>
              </div>
            )
          })}
        </div>
      )}

      {showCreate && (
        <BroadcastDialog onClose={() => setShowCreate(false)} onSubmit={handleCreateNotification} />
      )}
    </div>
  )
}

function BroadcastDialog({
  onClose,
  onSubmit,
}: {
  onClose: () => void
  onSubmit: (data: CreateNotificationDto) => void
}) {
  const { user } = useAppSelector((state) => state.auth)
  const isDeptManager = user?.role === 'DeptManager' || user?.tenVaiTro === 'DeptManager' || user?.tenVaiTro === 'Quản lý Khoa'
  
  const [title, setTitle] = useState('')
  const [message, setMessage] = useState('')
  const [type, setType] = useState('Info')
  const [targetType, setTargetType] = useState<'all' | 'user' | 'dept'>(isDeptManager ? 'dept' : 'all')
  const [users, setUsers] = useState<UserDto[]>([])
  const [selectedUserId, setSelectedUserId] = useState<number | ''>('')
  const [loadingUsers, setLoadingUsers] = useState(false)
  
  const [departments, setDepartments] = useState<DepartmentDto[]>([])
  const [selectedKhoaPhong, setSelectedKhoaPhong] = useState<string>('')
  const [loadingDepts, setLoadingDepts] = useState(false)

  useEffect(() => {
    if (targetType === 'user') {
      setLoadingUsers(true)
      const params: any = { pageNumber: 1, pageSize: 500, trangThai: true }
      if (isDeptManager && user?.khoaPhong) {
        params.khoaPhong = user.khoaPhong
        params.idVaiTro = 3
      }
      usersApi.list(params)
        .then((res) => {
          if (res.data.success && res.data.data) setUsers(res.data.data)
        })
        .finally(() => setLoadingUsers(false))
    }
  }, [targetType, isDeptManager, user?.khoaPhong])

  useEffect(() => {
    if (targetType === 'dept') {
      if (isDeptManager) {
        setSelectedKhoaPhong(user?.khoaPhong || '')
      } else {
        setLoadingDepts(true)
        departmentApi.getAll({ trangThai: true, pageSize: 100 })
          .then((res) => {
            if (res.data.success && res.data.data) {
              setDepartments(res.data.data)
            }
          })
          .finally(() => setLoadingDepts(false))
      }
    }
  }, [targetType, isDeptManager, user?.khoaPhong])

  const handleSubmit = () => {
    if (!title.trim() || !message.trim()) return
    const data: CreateNotificationDto = {
      title: title.trim(),
      message: message.trim(),
      type,
      userId: targetType === 'user' && selectedUserId ? selectedUserId : null,
      khoaPhong: targetType === 'dept' ? selectedKhoaPhong : null,
    }
    onSubmit(data)
  }

  const valid = title.trim().length > 0 && message.trim().length > 0
    && (targetType === 'all' 
        || (targetType === 'user' && selectedUserId !== '')
        || (targetType === 'dept' && selectedKhoaPhong !== ''))

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="bg-white rounded-xl shadow-2xl w-full max-w-md mx-4">
        <div className="flex items-center justify-between p-4 border-b">
          <h3 className="text-lg font-semibold">Gửi thông báo</h3>
          <Button variant="ghost" size="icon" onClick={onClose}><X className="h-4 w-4" /></Button>
        </div>
        <div className="p-4 space-y-4">
          {/* Target */}
          <div className="grid grid-cols-3 gap-2">
            {!isDeptManager && (
              <button
                onClick={() => setTargetType('all')}
                className={`border rounded-lg px-3 py-2 text-xs font-medium transition-colors ${
                  targetType === 'all' ? 'border-primary bg-primary/5 text-primary' : 'border-gray-200 text-gray-600 hover:border-gray-300'
                }`}
              >
                📢 Tất cả
              </button>
            )}
            <button
              onClick={() => setTargetType('dept')}
              className={`border rounded-lg px-3 py-2 text-xs font-medium transition-colors ${
                targetType === 'dept' ? 'border-primary bg-primary/5 text-primary' : 'border-gray-200 text-gray-600 hover:border-gray-300'
              } ${isDeptManager ? 'col-span-1.5' : ''}`}
            >
              🏢 Theo khoa
            </button>
            <button
              onClick={() => setTargetType('user')}
              className={`border rounded-lg px-3 py-2 text-xs font-medium transition-colors ${
                targetType === 'user' ? 'border-primary bg-primary/5 text-primary' : 'border-gray-200 text-gray-600 hover:border-gray-300'
              } ${isDeptManager ? 'col-span-1.5' : ''}`}
            >
              👤 Người dùng
            </button>
          </div>

          {targetType === 'user' && (
            <div className="space-y-1">
              <label className="text-sm font-medium text-gray-700">Chọn người nhận</label>
              <select
                value={selectedUserId}
                onChange={(e) => setSelectedUserId(e.target.value ? Number(e.target.value) : '')}
                className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm focus:outline-none focus:ring-2 focus:ring-primary"
                disabled={loadingUsers}
              >
                <option value="">-- Chọn người dùng --</option>
                {users.map((u) => (
                  <option key={u.id} value={u.id}>
                    {u.hoTen || u.tenDangNhap} ({u.khoaPhong || 'N/A'})
                  </option>
                ))}
              </select>
            </div>
          )}

          {targetType === 'dept' && (
            <div className="space-y-1">
              <label className="text-sm font-medium text-gray-700">Chọn khoa nhận</label>
              {isDeptManager ? (
                <div className="h-10 w-full rounded-md border border-gray-200 bg-gray-50 px-3 flex items-center text-sm font-medium text-gray-700">
                  {user?.khoaPhong || 'Chưa gán khoa'}
                </div>
              ) : (
                <select
                  value={selectedKhoaPhong}
                  onChange={(e) => setSelectedKhoaPhong(e.target.value)}
                  className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm focus:outline-none focus:ring-2 focus:ring-primary"
                  disabled={loadingDepts}
                >
                  <option value="">-- Chọn khoa/phòng ban --</option>
                  {departments.map((d) => (
                    <option key={d.id} value={d.tenKhoa}>
                      {d.tenKhoa}
                    </option>
                  ))}
                </select>
              )}
            </div>
          )}

          {/* Type */}
          <div className="space-y-1">
            <label className="text-sm font-medium text-gray-700">Loại thông báo</label>
            <div className="flex gap-2 flex-wrap">
              {TYPE_OPTIONS.map((t) => (
                <button
                  key={t.value}
                  onClick={() => setType(t.value)}
                  className={`text-xs px-3 py-1.5 rounded-full font-medium border transition-colors ${
                    type === t.value ? t.cls + ' border-transparent' : 'bg-white border-gray-200 text-gray-600'
                  }`}
                >
                  {t.label}
                </button>
              ))}
            </div>
          </div>

          <div className="space-y-1">
            <label className="text-sm font-medium text-gray-700">Tiêu đề *</label>
            <Input value={title} onChange={(e) => setTitle(e.target.value)} placeholder="Tiêu đề thông báo" />
          </div>

          <div className="space-y-1">
            <label className="text-sm font-medium text-gray-700">Nội dung *</label>
            <textarea
              value={message}
              onChange={(e) => setMessage(e.target.value)}
              rows={3}
              className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring resize-none"
              placeholder="Nội dung thông báo..."
            />
          </div>

          <div className="flex justify-end gap-2 pt-2 border-t">
            <Button variant="outline" onClick={onClose}>Hủy</Button>
            <Button onClick={handleSubmit} disabled={!valid}>
              <Send className="h-4 w-4 mr-1" />
              Gửi
            </Button>
          </div>
        </div>
      </div>
    </div>
  )
}
