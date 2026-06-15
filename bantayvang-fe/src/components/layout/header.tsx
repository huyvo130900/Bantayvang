import { useEffect, useState } from 'react'
import { useAppDispatch, useAppSelector } from '@/app/hooks'
import { logout } from '@/features/auth/slice'
import { useNavigate, useLocation } from 'react-router-dom'
import { notificationsApi } from '@/features/notifications/api'
import { LogOut, User, Bell } from 'lucide-react'
import { Button } from '@/components/ui/button'

export function Header() {
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const location = useLocation()
  const { user } = useAppSelector((state) => state.auth)
  const [unreadCount, setUnreadCount] = useState(0)

  useEffect(() => {
    loadUnreadCount()
    const interval = setInterval(loadUnreadCount, 60_000)

    const handleNotificationReceived = () => {
      loadUnreadCount()
    }

    const handleNotificationClick = () => {
      const role = user?.tenVaiTro || user?.role || ''
      if (role === 'DeptManager' || role === 'Quản lý Khoa') {
        navigate('/dept-manager/notifications')
      } else {
        navigate('/admin/notifications')
      }
    }

    window.addEventListener('notification:received', handleNotificationReceived)
    window.addEventListener('notification:click', handleNotificationClick)

    return () => {
      clearInterval(interval)
      window.removeEventListener('notification:received', handleNotificationReceived)
      window.removeEventListener('notification:click', handleNotificationClick)
    }
  }, [user, navigate])

  // Reload unread when navigating away from notifications
  useEffect(() => {
    if (!location.pathname.includes('notifications')) {
      loadUnreadCount()
    }
  }, [location.pathname])

  const loadUnreadCount = async () => {
    try {
      const res = await notificationsApi.getUnreadCount()
      if (res.data.success && res.data.data != null) {
        setUnreadCount(res.data.data)
      }
    } catch {
      // silent
    }
  }

  const handleLogout = async () => {
    await dispatch(logout())
    navigate('/login')
  }

  // Support both Vietnamese and English field names from backend
  const displayName = user?.hoTen || user?.fullName || user?.tenDangNhap || user?.username || 'User'
  const displayRole = user?.tenVaiTro || user?.role || ''

  return (
    <header className="sticky top-0 z-30 flex h-16 items-center justify-between border-b bg-white px-6 shadow-sm">
      {/* Logo + greeting */}
      <div className="flex items-center gap-4">
        <img
          src="/logoBVND2.png"
          alt="Bệnh Viện Nhi Đồng 2"
          className="h-10 w-10 object-contain"
        />
        <div className="text-sm text-gray-500">
          Xin chào,{' '}
          <span className="font-semibold text-gray-900">{displayName}</span>
        </div>
      </div>

      <div className="flex items-center gap-2">
        {displayRole && (
          <div className="flex items-center gap-1.5 text-xs bg-gray-100 text-gray-600 px-2.5 py-1 rounded-full">
            <User className="h-3.5 w-3.5" />
            <span>{displayRole}</span>
          </div>
        )}

        {/* Notification bell */}
        <Button
          variant="ghost"
          size="icon"
          title="Thông báo"
          className="relative text-gray-600 hover:text-primary"
          onClick={() => {
            const role = user?.tenVaiTro || user?.role || ''
            if (role === 'DeptManager' || role === 'Quản lý Khoa') {
              navigate('/dept-manager/notifications')
            } else {
              navigate('/admin/notifications')
            }
          }}
        >
          <Bell className="h-5 w-5" />
          {unreadCount > 0 && (
            <span className="absolute -top-0.5 -right-0.5 h-4 w-4 flex items-center justify-center rounded-full bg-red-500 text-white text-[10px] font-bold">
              {unreadCount > 9 ? '9+' : unreadCount}
            </span>
          )}
        </Button>

        <Button
          variant="ghost"
          size="sm"
          onClick={handleLogout}
          className="text-gray-600 hover:text-red-600 gap-1.5"
        >
          <LogOut className="h-4 w-4" />
          Đăng xuất
        </Button>
      </div>
    </header>
  )
}
