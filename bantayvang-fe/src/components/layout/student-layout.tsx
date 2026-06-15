import { Outlet, useNavigate, NavLink } from 'react-router-dom'
import { useAppDispatch, useAppSelector } from '@/app/hooks'
import { logout } from '@/features/auth/slice'
import { Button } from '@/components/ui/button'
import { LogOut, Home, ClipboardList, Bell, X, CheckCheck } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import { notificationsApi, type NotificationDto } from '@/features/notifications/api'
import { formatDate } from '@/lib/utils'

const TYPE_STYLE: Record<string, string> = {
  Info: 'bg-blue-100 text-blue-700',
  Warning: 'bg-yellow-100 text-yellow-700',
  Success: 'bg-green-100 text-green-700',
  Error: 'bg-red-100 text-red-700',
}
const TYPE_LABEL: Record<string, string> = {
  Info: 'Thông tin',
  Warning: 'Cảnh báo',
  Success: 'Thành công',
  Error: 'Lỗi',
}

function StudentNotificationBell() {
  const [open, setOpen] = useState(false)
  const [notifications, setNotifications] = useState<NotificationDto[]>([])
  const [unreadCount, setUnreadCount] = useState(0)
  const [loading, setLoading] = useState(false)
  const panelRef = useRef<HTMLDivElement>(null)

  // Load unread count periodically
  const loadUnreadCount = async () => {
    try {
      const res = await notificationsApi.getUnreadCount()
      if (res.data.success && res.data.data != null) {
        setUnreadCount(res.data.data)
      }
    } catch { /* silent */ }
  }

  // Load full notification list
  const loadNotifications = async () => {
    setLoading(true)
    try {
      const res = await notificationsApi.getMyNotifications()
      if (res.data.success && res.data.data) {
        setNotifications(res.data.data)
        setUnreadCount(res.data.data.filter((n) => !n.isRead).length)
      }
    } catch { /* silent */ }
    finally { setLoading(false) }
  }

  useEffect(() => {
    loadUnreadCount()
    const interval = setInterval(loadUnreadCount, 60_000)
    return () => clearInterval(interval)
  }, [])

  // Open panel → load full list
  useEffect(() => {
    if (open) loadNotifications()
  }, [open])

  // Close on outside click
  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (panelRef.current && !panelRef.current.contains(e.target as Node)) {
        setOpen(false)
      }
    }
    if (open) document.addEventListener('mousedown', handler)
    return () => document.removeEventListener('mousedown', handler)
  }, [open])

  const handleMarkRead = async (id: number) => {
    try {
      await notificationsApi.markAsRead(id)
      setNotifications((prev) => prev.map((n) => (n.id === id ? { ...n, isRead: true } : n)))
      setUnreadCount((c) => Math.max(0, c - 1))
    } catch { /* silent */ }
  }

  const handleMarkAllRead = async () => {
    try {
      await notificationsApi.markAllAsRead()
      setNotifications((prev) => prev.map((n) => ({ ...n, isRead: true })))
      setUnreadCount(0)
    } catch { /* silent */ }
  }

  return (
    <div className="relative" ref={panelRef}>
      <button
        onClick={() => setOpen((v) => !v)}
        className="relative flex items-center justify-center h-9 w-9 rounded-lg text-white/85 hover:text-white hover:bg-white/15 transition-colors"
        title="Thông báo"
      >
        <Bell className="h-5 w-5" />
        {unreadCount > 0 && (
          <span className="absolute -top-0.5 -right-0.5 h-4 w-4 flex items-center justify-center rounded-full bg-red-500 text-white text-[10px] font-bold">
            {unreadCount > 9 ? '9+' : unreadCount}
          </span>
        )}
      </button>

      {open && (
        <div className="absolute right-0 top-11 w-80 bg-white border border-gray-200 rounded-xl shadow-xl z-50 flex flex-col max-h-[480px]">
          {/* Header */}
          <div className="flex items-center justify-between px-4 py-3 border-b">
            <span className="font-semibold text-gray-900 text-sm">Thông báo</span>
            <div className="flex items-center gap-1">
              {unreadCount > 0 && (
                <button
                  onClick={handleMarkAllRead}
                  className="flex items-center gap-1 text-xs text-primary hover:underline px-2 py-1 rounded"
                  title="Đánh dấu tất cả đã đọc"
                >
                  <CheckCheck className="h-3.5 w-3.5" />
                  Đọc tất cả
                </button>
              )}
              <button
                onClick={() => setOpen(false)}
                className="text-gray-400 hover:text-gray-600 p-1 rounded"
              >
                <X className="h-4 w-4" />
              </button>
            </div>
          </div>

          {/* Body */}
          <div className="overflow-y-auto flex-1">
            {loading ? (
              <div className="space-y-2 p-3">
                {[1, 2, 3].map((i) => (
                  <div key={i} className="h-14 bg-gray-100 rounded-lg animate-pulse" />
                ))}
              </div>
            ) : notifications.length === 0 ? (
              <div className="flex flex-col items-center justify-center py-10 text-gray-400">
                <Bell className="h-8 w-8 mb-2 opacity-30" />
                <p className="text-sm">Không có thông báo nào</p>
              </div>
            ) : (
              <ul className="divide-y divide-gray-100">
                {notifications.map((n) => (
                  <li
                    key={n.id}
                    className={`flex items-start gap-3 px-4 py-3 transition-colors ${
                      n.isRead ? 'bg-white' : 'bg-blue-50'
                    }`}
                  >
                    <div className={`shrink-0 h-2 w-2 rounded-full mt-1.5 ${n.isRead ? 'bg-gray-200' : 'bg-blue-500'}`} />
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-1.5 flex-wrap">
                        <p className={`text-sm font-semibold truncate ${n.isRead ? 'text-gray-600' : 'text-gray-900'}`}>
                          {n.title}
                        </p>
                        {n.type && TYPE_LABEL[n.type] && (
                          <span className={`text-[10px] px-1.5 py-0.5 rounded-full font-medium shrink-0 ${TYPE_STYLE[n.type] ?? ''}`}>
                            {TYPE_LABEL[n.type]}
                          </span>
                        )}
                      </div>
                      <p className="text-xs text-gray-500 mt-0.5 whitespace-pre-wrap line-clamp-2">{n.message}</p>
                      <p className="text-[10px] text-gray-400 mt-1">{formatDate(n.createdAt)}</p>
                    </div>
                    {!n.isRead && (
                      <button
                        onClick={() => handleMarkRead(n.id)}
                        className="shrink-0 text-blue-400 hover:text-blue-600 p-1 rounded"
                        title="Đánh dấu đã đọc"
                      >
                        <CheckCheck className="h-3.5 w-3.5" />
                      </button>
                    )}
                  </li>
                ))}
              </ul>
            )}
          </div>
        </div>
      )}
    </div>
  )
}

export function StudentLayout() {
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const { user } = useAppSelector((state) => state.auth)

  const handleLogout = async () => {
    await dispatch(logout())
    navigate('/login')
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <header className="sticky top-0 z-20 flex h-16 items-center justify-between border-b border-primary/20 bg-primary px-6 shadow-md text-white">
        <div className="flex items-center gap-3">
          <img
            src="/logoBVND2.png"
            alt="Bệnh Viện Nhi Đồng 2"
            className="h-9 w-9 object-contain bg-white rounded-md p-0.5"
          />
          <span className="text-sm font-bold text-white leading-tight">BỆNH VIỆN NHI ĐỒNG 2</span>
          <nav className="flex items-center gap-1">
            <NavLink
              to="/dashboard"
              className={({ isActive }) =>
                `flex items-center gap-1.5 text-sm px-3 py-1.5 rounded-lg transition-colors ${
                  isActive ? 'bg-white/25 text-white font-medium' : 'text-white/85 hover:bg-white/15 hover:text-white'
                }`
              }
            >
              <Home className="h-4 w-4" />
              Trang chủ
            </NavLink>
            <NavLink
              to="/exam-waiting"
              className={({ isActive }) =>
                `flex items-center gap-1.5 text-sm px-3 py-1.5 rounded-lg transition-colors ${
                  isActive ? 'bg-white/25 text-white font-medium' : 'text-white/85 hover:bg-white/15 hover:text-white'
                }`
              }
            >
              <ClipboardList className="h-4 w-4" />
              Phòng chờ thi
            </NavLink>
          </nav>
        </div>
        <div className="flex items-center gap-3">
          {/* Notification Bell */}
          <StudentNotificationBell />

          <div className="text-right">
            <p className="text-sm font-medium text-white">{user?.hoTen || user?.tenDangNhap}</p>
            {user?.khoaPhong && (
              <p className="text-xs text-white/75">{user.khoaPhong}</p>
            )}
          </div>
          <Button variant="ghost" size="sm" onClick={handleLogout} className="text-white/85 hover:text-red-200 hover:bg-white/15">
            <LogOut className="h-4 w-4 mr-1" />
            Đăng xuất
          </Button>
        </div>
      </header>
      <main className="max-w-3xl mx-auto px-4 py-6">
        <Outlet />
      </main>
    </div>
  )
}
