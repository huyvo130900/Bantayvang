import { useState } from 'react'
import { Outlet, NavLink, useNavigate } from 'react-router-dom'
import {
  LayoutDashboard, FileQuestion, CalendarDays, Award, ChevronLeft, LogOut, Building2, Bell, ClipboardCheck, Menu, UserPlus
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
  { path: '/dept-manager/dang-ky-thi', label: 'Duyệt đăng ký', icon: UserPlus },
  { path: '/dept-manager/notifications', label: 'Thông báo', icon: Bell },
]

export function DeptManagerLayout() {
  const [collapsed, setCollapsed] = useState(false)
  const [mobileOpen, setMobileOpen] = useState(false)
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const { user } = useAppSelector((state) => state.auth)

  const displayName = user?.fullName || user?.fullName || user?.username || user?.username || ''
  const department = user?.department || ''

  const handleLogout = async () => {
    await dispatch(logout())
    navigate('/login')
  }

  return (
    <div className="min-h-screen bg-gray-50 flex flex-col md:flex-row">
      {/* Backdrop for mobile */}
      {mobileOpen && (
        <div
          className="fixed inset-0 bg-black/40 z-30 md:hidden backdrop-blur-xs transition-opacity duration-300"
          onClick={() => setMobileOpen(false)}
        />
      )}

      {/* Sidebar */}
      <aside className={cn(
        'fixed left-0 top-0 z-40 h-screen border-r border-white/10 bg-primary text-white transition-all duration-300 flex flex-col',
        collapsed ? 'md:w-16' : 'md:w-64',
        'w-64',
        mobileOpen ? 'translate-x-0' : 'max-md:-translate-x-full'
      )}>
        {/* Logo */}
        <div className="flex h-16 items-center justify-between border-b border-white/10 px-4 shrink-0">
          {!collapsed && (
            <div className="flex items-center gap-2 min-w-0">
              <img
                src="/logoBVND2.png"
                alt="BVND2"
                className="h-9 w-9 object-contain shrink-0"
              />
              <div className="min-w-0">
                <span className="text-xs font-bold text-white block leading-tight truncate">BỆNH VIỆN NHI ĐỒNG 2</span>
                <span className="text-[10px] text-white/75 leading-tight">Quản lý Khoa</span>
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
          <Button variant="ghost" size="icon" onClick={() => (mobileOpen ? setMobileOpen(false) : setCollapsed(c => !c))} className="ml-auto text-white hover:bg-white/10 hover:text-white">
            <ChevronLeft className={cn('h-4 w-4 transition-transform duration-300', collapsed && 'rotate-180', mobileOpen && 'rotate-0')} />
          </Button>
        </div>

        {/* Khoa info */}
        {!collapsed && department && (
          <div className="px-4 py-3 border-b border-white/10 bg-white/5">
            <div className="flex items-center gap-2">
              <Building2 className="h-4 w-4 text-white/90 shrink-0" />
              <span className="text-sm text-white font-medium truncate">{department}</span>
            </div>
          </div>
        )}

        {/* Nav */}
        <nav className="flex flex-col gap-0.5 p-2 overflow-y-auto flex-1">
          {menuItems.map(item => (
            <NavLink
              key={item.path}
              to={item.path}
              onClick={() => setMobileOpen(false)}
              className={({ isActive }) => cn(
                'flex items-center gap-3 rounded-lg px-3 py-2.5 text-base font-semibold transition-colors',
                isActive ? 'bg-white/20 text-white' : 'text-white/80 hover:bg-white/10 hover:text-white'
              )}
              title={collapsed ? item.label : undefined}
            >
              <item.icon className="h-5 w-5 shrink-0" />
              {!collapsed && <span className="truncate">{item.label}</span>}
            </NavLink>
          ))}
        </nav>

        {/* User info */}
        <div className={cn('border-t border-white/10 p-3 shrink-0', collapsed ? 'flex justify-center' : '')}>
          {!collapsed ? (
            <div className="space-y-2">
              <div className="text-xs truncate">
                <p className="font-semibold text-white truncate">{displayName}</p>
                <p className="text-white/60 truncate">Quản lý Khoa</p>
              </div>
              <Button variant="ghost" size="sm" onClick={handleLogout}
                className="w-full justify-start text-white/85 hover:text-red-200 hover:bg-white/10 gap-2 h-9 text-base">
                <LogOut className="h-4 w-4" /> Đăng xuất
              </Button>
            </div>
          ) : (
            <Button variant="ghost" size="icon" onClick={handleLogout} title="Đăng xuất" className="h-8 w-8 text-white/85 hover:text-red-200 hover:bg-white/10">
              <LogOut className="h-4 w-4" />
            </Button>
          )}
        </div>
      </aside>

      {/* Main content wrapper */}
      <div className={cn(
        'flex-1 flex flex-col min-w-0 transition-all duration-300',
        collapsed ? 'md:pl-16' : 'md:pl-64'
      )}>
        {/* Mobile Header */}
        <div className="md:hidden flex h-16 items-center justify-between border-b bg-white px-4 shadow-sm shrink-0 z-10">
          <div className="flex items-center gap-3 min-w-0">
            <Button
              variant="ghost"
              size="icon"
              onClick={() => setMobileOpen(true)}
              className="text-gray-600 hover:text-primary hover:bg-gray-100"
              title="Mở menu"
            >
              <Menu className="h-5 w-5" />
            </Button>
            <img
              src="/logoBVND2.png"
              alt="Logo"
              className="h-8 w-8 object-contain shrink-0"
            />
            <span className="text-xs font-bold text-gray-800 truncate">{department || 'Quản lý Khoa'}</span>
          </div>
          <Button variant="ghost" size="sm" onClick={handleLogout} className="text-gray-600 hover:text-red-600 gap-1 h-8 px-2">
            <LogOut className="h-4 w-4" />
            <span className="hidden sm:inline">Đăng xuất</span>
          </Button>
        </div>

        <main className="p-4 sm:p-6 flex-1 overflow-auto">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
