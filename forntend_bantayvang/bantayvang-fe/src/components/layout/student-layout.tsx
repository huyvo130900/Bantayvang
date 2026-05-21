import { Outlet } from 'react-router-dom'
import { useAppDispatch, useAppSelector } from '@/app/hooks'
import { logout } from '@/features/auth/slice'
import { useNavigate } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { LogOut } from 'lucide-react'

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
      <header className="flex h-16 items-center justify-between border-b bg-white px-6 shadow-sm">
        <span className="text-lg font-bold text-primary">BanTayVang - Thi Trực Tuyến</span>
        <div className="flex items-center gap-4">
          <span className="text-sm text-gray-600">
            {user?.fullName || user?.username}
          </span>
          <Button variant="ghost" size="sm" onClick={handleLogout}>
            <LogOut className="h-4 w-4 mr-1" />
            Đăng xuất
          </Button>
        </div>
      </header>
      <main className="p-6">
        <Outlet />
      </main>
    </div>
  )
}
