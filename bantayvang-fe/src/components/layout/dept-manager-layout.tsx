import { useState } from 'react'
import { Outlet, NavLink, useNavigate } from 'react-router-dom'
import {
  LayoutDashboard, FileQuestion, CalendarDays, Award, ChevronLeft, LogOut, Building2, Bell, ClipboardCheck
} from 'lucide-react'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { useAppDispatch, useAppSelector } from '@/app/hooks'
import { logout } from '@/features/auth/slice'

const menuItems = [
  { path: '/dept-manager/dashboard', label: 'Tổng quan', icon: LayoutDashboard },
  { path: '/dept-manager/questions', label: 'Ngân hàng câu hỏi', icon: FileQuestion },
  { path: '/dept-manager/ky-thi', label: 'Kỳ thi', icon: CalendarDays },
  { path: '/dept-manager/results', label: 'Kết quả thi', icon: Award },
  { path: '/dept-manager/grading', label: 'Chấm điểm', icon: ClipboardCheck },
  { path: '/dept-manager/notifications', label: 'Thông báo', icon: Bell },
]

export function DeptManagerLayout() {
  const [collapsed, setCollapsed] = useState(false)
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const { user } = useAppSelector((state) => state.auth)

  const displayName = user?.hoTen || user?.fullName || user?.tenDangNhap || user?.username || ''
  const khoaPhong = user?.khoaPhong || ''

  const handleLogout = async () => {
    await dispatch(logout())
    navigate('/login')
  }

  return (
    <div className="flex h-screen bg-gray-50">
      {/* Sidebar */}
      <aside className={cn(
        'fixed left-0 top-0 z-40 h-screen border-r bg-white transition-all duration-300 flex flex-col',
        collapsed ? 'w-16' : 'w-64'
      )}>
        {/* Logo */}
        <div className="flex h-16 items-center justify-between border-b px-4 shrink-0">
          {!collapsed && (
            <div className="flex items-center gap-2 min-w-0">
              <img
                src="/logoBVND2.png"
                alt="BVND2"
                className="h-9 w-9 object-contain shrink-0"
              />
              <div className="min-w-0">
                <span className="text-xs font-bold text-primary block leading-tight truncate">BỆNH VIỆN NHI ĐỒNG 2</span>
                <span className="text-[10px] text-gray-400 leading-tight">Quản lý Khoa</span>
              </div>
            </div>
          )}
          {collapsed && (
            <img
              src="/logoBVND2.png"
              alt="BVND2"
              className="h-8 w-8 object-contain mx-auto"
            />
          )}
          <Button variant="ghost" size="icon" onClick={() => setCollapsed(c => !c)} className="ml-auto">
            <ChevronLeft className={cn('h-4 w-4 transition-transform duration-300', collapsed && 'rotate-180')} />
          </Button>
        </div>

        {/* Khoa info */}
        {!collapsed && khoaPhong && (
          <div className="px-4 py-3 border-b bg-blue-50">
            <div className="flex items-center gap-2">
              <Building2 className="h-3.5 w-3.5 text-blue-500 shrink-0" />
              <span className="text-xs text-blue-700 font-medium truncate">{khoaPhong}</span>
            </div>
          </div>
        )}

        {/* Nav */}
        <nav className="flex flex-col gap-0.5 p-2 overflow-y-auto flex-1">
          {menuItems.map(item => (
            <NavLink key={item.path} to={item.path}
              className={({ isActive }) => cn(
                'flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition-colors',
                isActive ? 'bg-primary/10 text-primary' : 'text-gray-600 hover:bg-gray-100 hover:text-gray-900'
              )}
              title={collapsed ? item.label : undefined}
            >
              <item.icon className="h-4 w-4 shrink-0" />
              {!collapsed && <span className="truncate">{item.label}</span>}
            </NavLink>
          ))}
        </nav>

        {/* User info */}
        <div className={cn('border-t p-3 shrink-0', collapsed ? 'flex justify-center' : '')}>
          {!collapsed ? (
            <div className="space-y-2">
              <div className="text-xs truncate">
                <p className="font-semibold text-gray-800 truncate">{displayName}</p>
                <p className="text-gray-400 truncate">Quản lý Khoa</p>
              </div>
              <Button variant="ghost" size="sm" onClick={handleLogout}
                className="w-full justify-start text-gray-500 hover:text-red-600 gap-2 h-8">
                <LogOut className="h-4 w-4" /> Đăng xuất
              </Button>
            </div>
          ) : (
            <Button variant="ghost" size="icon" onClick={handleLogout} title="Đăng xuất" className="h-8 w-8 text-gray-500 hover:text-red-600">
              <LogOut className="h-4 w-4" />
            </Button>
          )}
        </div>
      </aside>

      {/* Main content */}
      <main className={cn('flex-1 overflow-auto transition-all duration-300', collapsed ? 'ml-16' : 'ml-64')}>
        <Outlet />
      </main>
    </div>
  )
}
