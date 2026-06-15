import { Navigate } from 'react-router-dom'
import { useAppSelector } from '@/app/hooks'
import { LoginForm } from '../components/login-form'
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card'
import { ROLES } from '@/lib/constants'
import { ShieldCheck } from 'lucide-react'

export function LoginPage() {
  const { isAuthenticated, user } = useAppSelector((state) => state.auth)

  if (isAuthenticated && user) {
    const role = user.role || user.tenVaiTro || ''
    if (role === ROLES.STUDENT)       return <Navigate to="/exam-waiting" replace />
    if (role === ROLES.DEPT_MANAGER)  return <Navigate to="/dept-manager/dashboard" replace />
    return <Navigate to="/admin/dashboard" replace />
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-blue-700 via-blue-600 to-purple-700 p-4">
      <div className="absolute inset-0 overflow-hidden pointer-events-none">
        <div className="absolute -top-40 -right-40 w-96 h-96 bg-white/5 rounded-full" />
        <div className="absolute -bottom-20 -left-20 w-72 h-72 bg-white/5 rounded-full" />
      </div>

      <div className="relative w-full max-w-md space-y-4">
        <div className="text-center mb-2">
          <div className="inline-flex items-center justify-center w-16 h-16 bg-white/10 rounded-2xl backdrop-blur-sm mb-3">
            <span className="text-4xl">🏆</span>
          </div>
          <h1 className="text-3xl font-bold text-white">Kiểm tra nội bộ</h1>
          <p className="text-blue-200 text-sm mt-1">Hệ thống thi trực tuyến</p>
        </div>

        <Card className="shadow-2xl border-0 backdrop-blur-sm bg-white/95">
          <CardHeader className="text-center pb-2">
            <CardTitle className="text-xl text-gray-800">Đăng nhập</CardTitle>
            <CardDescription className="text-gray-500 text-sm">
              Nhập thông tin tài khoản để tiếp tục
            </CardDescription>
          </CardHeader>
          <CardContent className="pt-2">
            <LoginForm />
          </CardContent>
        </Card>

        <div className="flex items-start gap-2.5 bg-white/10 backdrop-blur-sm rounded-xl px-4 py-3 text-sm text-blue-100">
          <ShieldCheck className="h-4 w-4 mt-0.5 shrink-0 text-blue-200" />
          <p>
            Tài khoản do <strong>quản trị viên</strong> cấp phát theo mã số cán bộ/học viên.
            Nếu chưa có tài khoản, vui lòng liên hệ bộ phận quản lý.
          </p>
        </div>
      </div>
    </div>
  )
}
