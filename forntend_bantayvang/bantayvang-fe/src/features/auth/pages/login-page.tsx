import { Navigate } from 'react-router-dom'
import { useAppSelector } from '@/app/hooks'
import { LoginForm } from '../components/login-form'
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card'
import { ROLES } from '@/lib/constants'

export function LoginPage() {
  const { isAuthenticated, user } = useAppSelector((state) => state.auth)

  if (isAuthenticated && user) {
    return user.role === ROLES.STUDENT ? (
      <Navigate to="/exam-waiting" replace />
    ) : (
      <Navigate to="/admin/dashboard" replace />
    )
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-blue-600 to-purple-700 p-4">
      <Card className="w-full max-w-md">
        <CardHeader className="text-center">
          <CardTitle className="text-2xl">Hệ Thống Thi Trực Tuyến</CardTitle>
          <CardDescription>Bàn Tay Vàng</CardDescription>
        </CardHeader>
        <CardContent>
          <LoginForm />
        </CardContent>
      </Card>
    </div>
  )
}
