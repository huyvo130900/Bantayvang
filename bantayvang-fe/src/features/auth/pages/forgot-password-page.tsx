import { useEffect, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { authApi } from '../api'
import { ShieldCheck } from 'lucide-react'

type Step = 'email' | 'code' | 'password' | 'done'

export function ForgotPasswordPage() {
  const navigate = useNavigate()
  const [step, setStep] = useState<Step>('email')
  const [email, setEmail] = useState('')
  const [code, setCode] = useState('')
  const [resetToken, setResetToken] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [loading, setLoading] = useState(false)
  const [cooldown, setCooldown] = useState(0)
  const [error, setError] = useState('')

  useEffect(() => {
    if (cooldown <= 0) return
    const timer = setInterval(() => setCooldown((s) => Math.max(0, s - 1)), 1000)
    return () => clearInterval(timer)
  }, [cooldown])

  async function handleSendCode(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    if (!email) {
      setError('Vui lòng nhập email')
      return
    }
    setLoading(true)
    try {
      await authApi.forgotPassword(email)
      setStep('code')
      setCooldown(60)
    } catch (err: any) {
      const errorObj = err as { response?: { data?: { message?: string } } }
      // Backend luôn trả thành công để tránh lộ thông tin tài khoản, nhưng vẫn xử lý lỗi mạng/hệ thống
      setError(errorObj.response?.data?.message || 'Có lỗi xảy ra, vui lòng thử lại')
    } finally {
      setLoading(false)
    }
  }

  async function handleResend() {
    setError('')
    setLoading(true)
    try {
      await authApi.forgotPassword(email)
      setCooldown(60)
    } catch (err: any) {
      const errorObj = err as { response?: { data?: { message?: string } } }
      setError(errorObj.response?.data?.message || 'Có lỗi xảy ra, vui lòng thử lại')
    } finally {
      setLoading(false)
    }
  }

  async function handleVerifyCode(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    if (code.length !== 6) {
      setError('Vui lòng nhập đủ 6 số của mã xác nhận')
      return
    }
    setLoading(true)
    try {
      const res = await authApi.verifyResetCode(email, code)
      const token = res.data?.data
      if (!token) {
        setError('Mã xác nhận không đúng hoặc đã hết hạn')
        return
      }
      setResetToken(token)
      setStep('password')
    } catch (err: any) {
      const errorObj = err as { response?: { data?: { message?: string } } }
      setError(errorObj.response?.data?.message || 'Mã xác nhận không hợp lệ')
    } finally {
      setLoading(false)
    }
  }

  async function handleResetPassword(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    if (newPassword.length < 8) {
      setError('Mật khẩu mới phải từ 8 ký tự trở lên')
      return
    }

    const passwordRegex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]/
    if (!passwordRegex.test(newPassword)) {
      setError('Mật khẩu phải chứa ít nhất 1 chữ thường, 1 chữ hoa, 1 số và 1 ký tự đặc biệt')
      return
    }

    if (newPassword !== confirmPassword) {
      setError('Xác nhận mật khẩu không khớp')
      return
    }
    setLoading(true)
    try {
      await authApi.resetPassword(resetToken, newPassword)
      setStep('done')
    } catch (err: any) {
      const errorObj = err as { response?: { data?: { message?: string, title?: string, errors?: Record<string, string[]> } } }
      const data = errorObj.response?.data
      let msg = data?.message || data?.title || 'Có lỗi xảy ra khi đặt lại mật khẩu'
      if (data?.errors) {
        if (Array.isArray(data.errors)) {
          msg = data.errors.join(', ')
        } else if (typeof data.errors === 'object') {
          msg = Object.values(data.errors).flat().join(', ')
        }
      }
      setError(msg)
    } finally {
      setLoading(false)
    }
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
          <h1 className="text-3xl font-bold text-white drop-shadow-md">Quên mật khẩu</h1>
        </div>

        <Card className="shadow-2xl border-0 backdrop-blur-sm bg-white/95">
          <CardHeader className="text-center pb-2">
            <CardTitle className="text-xl text-gray-800">
              {step === 'email' && 'Nhập email của bạn'}
              {step === 'code' && 'Nhập mã xác nhận'}
              {step === 'password' && 'Đặt mật khẩu mới'}
              {step === 'done' && 'Hoàn tất'}
            </CardTitle>
            <CardDescription className="text-gray-500 text-sm">
              {step === 'email' && 'Chúng tôi sẽ gửi mã xác nhận 6 số tới email này'}
              {step === 'code' && `Mã xác nhận đã được gửi tới ${email}`}
              {step === 'password' && 'Đặt mật khẩu mới cho tài khoản của bạn'}
              {step === 'done' && 'Mật khẩu của bạn đã được đặt lại thành công'}
            </CardDescription>
          </CardHeader>
          <CardContent className="pt-2 space-y-4">
            {error && (
              <div className="p-3 text-sm text-red-600 bg-red-50 border border-red-200 rounded-md">
                {error}
              </div>
            )}

            {step === 'email' && (
              <form onSubmit={handleSendCode} className="space-y-4">
                <div className="space-y-2">
                  <label htmlFor="email" className="text-sm font-medium text-gray-700">Email</label>
                  <Input
                    id="email"
                    type="email"
                    placeholder="example@gmail.com"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    autoFocus
                  />
                </div>
                <Button type="submit" className="w-full" disabled={loading}>
                  {loading ? 'Đang gửi...' : 'Gửi mã xác nhận'}
                </Button>
              </form>
            )}

            {step === 'code' && (
              <form onSubmit={handleVerifyCode} className="space-y-4">
                <div className="space-y-2">
                  <label htmlFor="code" className="text-sm font-medium text-gray-700">Mã xác nhận (6 số)</label>
                  <Input
                    id="code"
                    placeholder="000000"
                    maxLength={6}
                    inputMode="numeric"
                    value={code}
                    onChange={(e) => setCode(e.target.value.replace(/\D/g, ''))}
                    autoFocus
                  />
                </div>
                <Button type="submit" className="w-full" disabled={loading || code.length !== 6}>
                  {loading ? 'Đang xác thực...' : 'Xác nhận'}
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  className="w-full"
                  onClick={handleResend}
                  disabled={loading || cooldown > 0}
                >
                  {cooldown > 0 ? `Gửi lại mã (${cooldown}s)` : 'Gửi lại mã'}
                </Button>
              </form>
            )}

            {step === 'password' && (
              <form onSubmit={handleResetPassword} className="space-y-4">
                <div className="space-y-2">
                  <label htmlFor="newPassword" className="text-sm font-medium text-gray-700">Mật khẩu mới</label>
                  <Input
                    id="newPassword"
                    type="password"
                    placeholder="Ít nhất 8 ký tự, có hoa/thường/số/ký tự đặc biệt"
                    value={newPassword}
                    onChange={(e) => setNewPassword(e.target.value)}
                    autoFocus
                  />
                </div>
                <div className="space-y-2">
                  <label htmlFor="confirmPassword" className="text-sm font-medium text-gray-700">Xác nhận mật khẩu mới</label>
                  <Input
                    id="confirmPassword"
                    type="password"
                    placeholder="Nhập lại mật khẩu mới"
                    value={confirmPassword}
                    onChange={(e) => setConfirmPassword(e.target.value)}
                  />
                </div>
                <Button type="submit" className="w-full" disabled={loading}>
                  {loading ? 'Đang xử lý...' : 'Đặt lại mật khẩu'}
                </Button>
              </form>
            )}

            {step === 'done' && (
              <div className="space-y-4 text-center">
                <p className="text-sm text-gray-600">
                  Bạn có thể đăng nhập bằng mật khẩu mới ngay bây giờ.
                </p>
                <Button className="w-full" onClick={() => navigate('/login')}>
                  Về trang đăng nhập
                </Button>
              </div>
            )}

            {step !== 'done' && (
              <p className="text-center text-sm text-gray-500 pt-2">
                <Link to="/login" className="text-primary font-medium hover:underline">
                  Quay lại đăng nhập
                </Link>
              </p>
            )}
          </CardContent>
        </Card>

        <div className="flex items-start gap-2.5 bg-black/20 rounded-xl px-4 py-3 text-sm text-green-50 shadow-inner">
          <ShieldCheck className="h-4 w-4 mt-0.5 shrink-0 text-green-300" />
          <p>Mã xác nhận có hiệu lực trong 10 phút. Không chia sẻ mã cho bất kỳ ai.</p>
        </div>
      </div>
    </div>
  )
}
