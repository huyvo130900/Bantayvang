import { Navigate, Link } from 'react-router-dom'
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
    <div className="min-h-screen flex items-center justify-center bg-green-700 p-4">
      <div className="absolute inset-0 overflow-hidden pointer-events-none">
        <div className="absolute -top-40 -right-40 w-96 h-96 bg-white/5 rounded-full" />
        <div className="absolute -bottom-20 -left-20 w-72 h-72 bg-white/5 rounded-full" />
      </div>

      <div className="relative w-full max-w-md space-y-4">
        <div className="text-center mb-2">
          <div className="inline-flex items-center justify-center w-20 h-20 bg-white rounded-2xl shadow-md mb-3 p-2">
            <img src="/logoBVND2.png" alt="Logo BVND2" className="w-full h-full object-contain" />
          </div>
          <h1 className="text-3xl font-bold text-white drop-shadow-md">Kiểm tra nội bộ</h1>
          <p className="text-green-50 text-sm mt-1 font-medium drop-shadow-sm">Hệ thống thi trực tuyến - Bệnh viện Nhi Đồng 2</p>
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

        <div className="flex items-start gap-2.5 bg-black/20 rounded-xl px-4 py-3 text-sm text-green-50 shadow-inner">
          <ShieldCheck className="h-4 w-4 mt-0.5 shrink-0 text-green-300" />
          <p>
            Tài khoản do <strong>quản trị viên</strong> cấp phát theo mã số cán bộ/học viên.
            Nếu chưa có tài khoản, vui lòng liên hệ bộ phận quản lý hoặc <Link to="/dang-ky" className="font-bold text-white underline hover:text-green-200">Đăng ký dự thi</Link> (dành cho người ngoài).
          </p>
        </div>
      </div>
    </div>
  )
}
