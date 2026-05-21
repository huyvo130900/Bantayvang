import { useAppDispatch, useAppSelector } from '@/app/hooks'
import { logout } from '@/features/auth/slice'
import { useNavigate } from 'react-router-dom'
import { LogOut, User } from 'lucide-react'
import { Button } from '@/components/ui/button'

export function Header() {
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const { user } = useAppSelector((state) => state.auth)

  const handleLogout = async () => {
    await dispatch(logout())
    navigate('/login')
  }

  return (
    <header className="sticky top-0 z-30 flex h-16 items-center justify-between border-b bg-white px-6">
      <div className="text-sm text-gray-500">
        Xin chào, <span className="font-medium text-gray-900">{user?.fullName || user?.username}</span>
      </div>

      <div className="flex items-center gap-3">
        <div className="flex items-center gap-2 text-sm text-gray-600">
          <User className="h-4 w-4" />
          <span>{user?.role}</span>
        </div>
        <Button variant="ghost" size="sm" onClick={handleLogout}>
          <LogOut className="h-4 w-4 mr-1" />
          Đăng xuất
        </Button>
      </div>
    </header>
  )
}
