import { NavLink } from 'react-router-dom'
import {
  LayoutDashboard,
  Users,
  FileQuestion,
  ClipboardList,
  CalendarDays,
  UserCheck,
  Award,
  BarChart3,
  ScrollText,
  Bell,
  ChevronLeft,
} from 'lucide-react'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'

interface SidebarProps {
  collapsed: boolean
  onToggle: () => void
}

const menuItems = [
  { path: '/admin/dashboard', label: 'Tổng quan', icon: LayoutDashboard },
  { path: '/admin/users', label: 'Người dùng', icon: Users },
  { path: '/admin/questions', label: 'Ngân hàng câu hỏi', icon: FileQuestion },
  { path: '/admin/exams', label: 'Đề thi', icon: ClipboardList },
  { path: '/admin/ky-thi', label: 'Kỳ thi & Ca thi', icon: CalendarDays },
  { path: '/admin/assignments', label: 'Phân công thí sinh', icon: UserCheck },
  { path: '/admin/grading', label: 'Kết quả & Chấm điểm', icon: Award },
  { path: '/admin/statistics', label: 'Thống kê', icon: BarChart3 },
  { path: '/admin/audit-log', label: 'Audit Log', icon: ScrollText },
  { path: '/admin/notifications', label: 'Thông báo', icon: Bell },
]

export function Sidebar({ collapsed, onToggle }: SidebarProps) {
  return (
    <aside
      className={cn(
        'fixed left-0 top-0 z-40 h-screen border-r bg-white transition-all duration-300',
        collapsed ? 'w-16' : 'w-64'
      )}
    >
      {/* Logo */}
      <div className="flex h-16 items-center justify-between border-b px-4">
        {!collapsed && (
          <span className="text-lg font-bold text-primary">BanTayVang</span>
        )}
        <Button variant="ghost" size="icon" onClick={onToggle}>
          <ChevronLeft
            className={cn('h-4 w-4 transition-transform', collapsed && 'rotate-180')}
          />
        </Button>
      </div>

      {/* Navigation */}
      <nav className="flex flex-col gap-1 p-2 overflow-y-auto h-[calc(100vh-4rem)]">
        {menuItems.map((item) => (
          <NavLink
            key={item.path}
            to={item.path}
            className={({ isActive }) =>
              cn(
                'flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors',
                isActive
                  ? 'bg-primary/10 text-primary'
                  : 'text-gray-600 hover:bg-gray-100 hover:text-gray-900'
              )
            }
            title={collapsed ? item.label : undefined}
          >
            <item.icon className="h-5 w-5 shrink-0" />
            {!collapsed && <span>{item.label}</span>}
          </NavLink>
        ))}
      </nav>
    </aside>
  )
}
