import { useEffect, useState } from 'react'
import { notificationsApi, type NotificationDto, type CreateNotificationDto } from '../api'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Bell, CheckCheck, Trash2, Send, X } from 'lucide-react'

export function NotificationsPage() {
  const [notifications, setNotifications] = useState<NotificationDto[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [showCreate, setShowCreate] = useState(false)

  useEffect(() => {
    loadNotifications()
  }, [])

  const loadNotifications = async () => {
    setIsLoading(true)
    try {
      const response = await notificationsApi.getMyNotifications()
      if (response.data.success && response.data.data) {
        setNotifications(response.data.data)
      }
    } catch {
      // handle error
    } finally {
      setIsLoading(false)
    }
  }

  const handleMarkAllRead = async () => {
    await notificationsApi.markAllAsRead()
    loadNotifications()
  }

  const handleDelete = async (id: number) => {
    await notificationsApi.delete(id)
    setNotifications((prev) => prev.filter((n) => n.id !== id))
  }

  const handleMarkRead = async (id: number) => {
    await notificationsApi.markAsRead(id)
    setNotifications((prev) =>
      prev.map((n) => (n.id === id ? { ...n, isRead: true } : n))
    )
  }

  const handleBroadcast = async (data: CreateNotificationDto) => {
    await notificationsApi.broadcast(data)
    setShowCreate(false)
    loadNotifications()
  }

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Thông báo</h1>
        <div className="flex gap-2">
          <Button variant="outline" size="sm" onClick={handleMarkAllRead}>
            <CheckCheck className="h-4 w-4 mr-1" />
            Đánh dấu tất cả đã đọc
          </Button>
          <Button size="sm" onClick={() => setShowCreate(true)}>
            <Send className="h-4 w-4 mr-1" />
            Gửi thông báo
          </Button>
        </div>
      </div>

      {isLoading ? (
        <div className="text-center py-12 text-gray-500">Đang tải...</div>
      ) : notifications.length === 0 ? (
        <div className="text-center py-12 text-gray-500">Không có thông báo</div>
      ) : (
        <div className="space-y-2">
          {notifications.map((n) => (
            <div
              key={n.id}
              className={`flex items-start gap-3 p-4 rounded-lg border ${
                n.isRead ? 'bg-white' : 'bg-blue-50 border-blue-200'
              }`}
            >
              <Bell className={`h-5 w-5 shrink-0 mt-0.5 ${n.isRead ? 'text-gray-400' : 'text-blue-500'}`} />
              <div className="flex-1 min-w-0">
                <p className="text-sm font-medium text-gray-900">{n.title}</p>
                <p className="text-sm text-gray-600 mt-0.5">{n.message}</p>
                <p className="text-xs text-gray-400 mt-1">
                  {new Date(n.createdAt).toLocaleString('vi-VN')}
                </p>
              </div>
              <div className="flex gap-1">
                {!n.isRead && (
                  <Button variant="ghost" size="icon" title="Đánh dấu đã đọc" onClick={() => handleMarkRead(n.id)}>
                    <CheckCheck className="h-4 w-4 text-blue-500" />
                  </Button>
                )}
                <Button variant="ghost" size="icon" title="Xóa" onClick={() => handleDelete(n.id)}>
                  <Trash2 className="h-4 w-4 text-red-400" />
                </Button>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Broadcast dialog */}
      {showCreate && (
        <BroadcastDialog onClose={() => setShowCreate(false)} onSubmit={handleBroadcast} />
      )}
    </div>
  )
}

function BroadcastDialog({ onClose, onSubmit }: { onClose: () => void; onSubmit: (data: CreateNotificationDto) => void }) {
  const [title, setTitle] = useState('')
  const [message, setMessage] = useState('')

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-md p-4">
        <div className="flex items-center justify-between mb-4">
          <h3 className="text-lg font-semibold">Gửi thông báo broadcast</h3>
          <Button variant="ghost" size="icon" onClick={onClose}><X className="h-4 w-4" /></Button>
        </div>
        <div className="space-y-3">
          <Input value={title} onChange={(e) => setTitle(e.target.value)} placeholder="Tiêu đề" />
          <textarea
            value={message}
            onChange={(e) => setMessage(e.target.value)}
            rows={3}
            className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
            placeholder="Nội dung thông báo..."
          />
          <div className="flex justify-end gap-2">
            <Button variant="outline" onClick={onClose}>Hủy</Button>
            <Button onClick={() => onSubmit({ title, message, type: 'Info' })} disabled={!title || !message}>
              Gửi
            </Button>
          </div>
        </div>
      </div>
    </div>
  )
}
