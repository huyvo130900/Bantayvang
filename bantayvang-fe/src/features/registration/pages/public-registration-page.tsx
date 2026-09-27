import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { registrationApi } from '../api'
import { authApi } from '@/features/auth/api'
import { departmentApi } from '@/features/departments/api'
import type { DepartmentDto } from '@/features/departments/types'


export function PublicRegistrationPage() {
  const navigate = useNavigate()
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [departments, setDepartments] = useState<DepartmentDto[]>([])
  const [toast, setToast] = useState<{ msg: string; ok: boolean } | null>(null)

  const showToast = (msg: string, ok: boolean) => {
    setToast({ msg, ok })
    setTimeout(() => setToast(null), 3000)
  }

  const [formData, setFormData] = useState({
    fullName: '',
    idCardNumber: '',
    phoneNumber: '',
    email: '',
    password: '',
    workUnit: '',
    major: '',
    departmentId: '',
    examPurpose: '',
  })

  // BUG FIX: the backend has a full OTP email-verification flow (send code / verify code) built
  // specifically for this public registration form, but this page never called either endpoint -
  // anyone could submit with any email (including someone else's), which then silently received
  // the real approval/login-credentials email once an admin approved the application. Wire up the
  // existing authApi.sendEmailVerificationCode/verifyEmailCode calls and require a verified email
  // (for the CURRENT value of the field) before the form can be submitted.
  const [otpSent, setOtpSent] = useState(false)
  const [otpCode, setOtpCode] = useState('')
  const [verifiedEmail, setVerifiedEmail] = useState<string | null>(null)
  const [isSendingCode, setIsSendingCode] = useState(false)
  const [isVerifyingCode, setIsVerifyingCode] = useState(false)
  const isEmailVerified = verifiedEmail !== null && verifiedEmail === formData.email.trim().toLowerCase()

  // BUG FIX: the backend only accepts a verified email for 30 minutes after verification
  // (EmailVerificationService.ConsumeWindowMinutes) - without this, the "✓ Đã xác thực" checkmark
  // and enabled submit button stayed stuck forever in the UI even after that window passed (e.g.
  // an applicant who verifies then spends a while filling out the rest of the form), so submit
  // would suddenly fail with a confusing "please verify your email" error despite the visible
  // checkmark. Proactively expire the client-side flag a little before the real 30-minute window
  // so the UI asks for a fresh code instead of the backend rejecting a stale one.
  useEffect(() => {
    if (!verifiedEmail) return
    const timer = setTimeout(() => {
      setVerifiedEmail(null)
      setOtpSent(false)
      // BUG FIX (round 8): same stale-code class as the email onChange handler above - without
      // clearing this, "Gửi lại mã" after expiry re-shows the OTP row pre-filled with the old,
      // now-invalid code instead of an empty field.
      setOtpCode('')
      showToast('Mã xác thực đã hết hạn, vui lòng xác thực lại email', false)
    }, 25 * 60 * 1000)
    return () => clearTimeout(timer)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [verifiedEmail])

  async function handleSendCode() {
    if (!formData.email.trim()) {
      showToast('Vui lòng nhập email trước', false)
      return
    }
    setIsSendingCode(true)
    try {
      const res = await authApi.sendEmailVerificationCode(formData.email.trim())
      if (res.data.success) {
        setOtpSent(true)
        setVerifiedEmail(null)
        // BUG FIX (round 8): resending after a failed verify attempt left the previous wrong code
        // sitting in the input instead of prompting fresh entry - same stale-code bug as above.
        setOtpCode('')
        showToast(res.data.message || 'Đã gửi mã xác thực, vui lòng kiểm tra email', true)
      } else {
        showToast(res.data.message || 'Không thể gửi mã xác thực', false)
      }
    } catch (error: any) {
      showToast(error.response?.data?.message || 'Có lỗi khi gửi mã xác thực', false)
    } finally {
      setIsSendingCode(false)
    }
  }

  async function handleVerifyCode() {
    if (!otpCode.trim()) {
      showToast('Vui lòng nhập mã xác thực', false)
      return
    }
    setIsVerifyingCode(true)
    try {
      const res = await authApi.verifyEmailCode(formData.email.trim(), otpCode.trim())
      if (res.data.success) {
        setVerifiedEmail(formData.email.trim().toLowerCase())
        showToast('Xác thực email thành công', true)
      } else {
        showToast(res.data.message || 'Mã xác thực không đúng hoặc đã hết hạn', false)
      }
    } catch (error: any) {
      showToast(error.response?.data?.message || 'Có lỗi khi xác thực mã', false)
    } finally {
      setIsVerifyingCode(false)
    }
  }


  useEffect(() => {
    async function fetchDepartments() {
      try {
        const res = await departmentApi.getAll({ status: true })
        if (res.data) {
          const list = Array.isArray(res.data.data) ? res.data.data : (Array.isArray(res.data) ? res.data : [])
          setDepartments(list)
        }
      } catch (error) {
        console.error('Failed to load departments', error)
      }
    }
    fetchDepartments()
  }, [])

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!formData.fullName || !formData.idCardNumber || !formData.phoneNumber || !formData.email || !formData.password) {
      showToast('Vui lòng điền đầy đủ các trường bắt buộc', false)
      return
    }
    // BUG FIX: department used to be optional here - a registration approved with no department
    // produces an account with an empty Department, which the exam-list filter treats as
    // "no restriction" and shows every campaign of every department. Require it up front.
    if (!formData.departmentId) {
      showToast('Vui lòng chọn khoa/phòng muốn thi', false)
      return
    }
    if (!isEmailVerified) {
      showToast('Vui lòng xác thực email trước khi gửi đăng ký', false)
      return
    }

    setIsSubmitting(true)
    try {
      await registrationApi.create({
        ...formData,
        departmentId: formData.departmentId ? parseInt(formData.departmentId) : undefined,
      })
      showToast('Đăng ký thành công! Vui lòng chờ thông báo duyệt qua email/sđt.', true)
      setTimeout(() => navigate('/login'), 2000)
    } catch (error: any) {
      showToast(error.response?.data?.message || 'Có lỗi xảy ra khi đăng ký', false)
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-[#5b8e23] via-[#70aa30] to-[#426b15] p-4">
      {toast && (
        <div className={`fixed top-4 right-4 z-50 flex items-center gap-2 px-4 py-3 rounded-xl shadow-lg text-sm font-medium transition-all ${toast.ok ? 'bg-green-600 text-white' : 'bg-red-600 text-white'}`}>
          {toast.ok ? '✓' : '⚠'} {toast.msg}
        </div>
      )}
      <Card className="w-full max-w-2xl shadow-2xl border-0 backdrop-blur-sm bg-white/95 my-8">
        <CardHeader className="text-center pb-4">
          <div className="flex justify-center mb-4">
            <img src="/logoBVND2.png" alt="Logo BVND2" className="w-16 h-16 object-contain" />
          </div>
          <CardTitle className="text-2xl text-gray-800">Đăng ký dự thi</CardTitle>
          <CardDescription className="text-gray-500 text-sm">
            Dành cho thí sinh ngoài bệnh viện
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-6">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div className="space-y-2">
                <label htmlFor="fullName" className="text-sm font-medium leading-none">Họ và tên *</label>
                <Input
                  id="fullName"
                  placeholder="Nguyễn Văn A"
                  value={formData.fullName}
                  onChange={(e) => setFormData({ ...formData, fullName: e.target.value })}
                  required
                />
              </div>
              <div className="space-y-2">
                <label htmlFor="idCardNumber" className="text-sm font-medium leading-none">Số CCCD *</label>
                <Input
                  id="idCardNumber"
                  placeholder="Căn cước công dân"
                  value={formData.idCardNumber}
                  onChange={(e) => setFormData({ ...formData, idCardNumber: e.target.value })}
                  required
                />
                <p className="text-xs text-gray-500">Được dùng làm Tên đăng nhập</p>
              </div>
              
              <div className="space-y-2">
                <label htmlFor="phoneNumber" className="text-sm font-medium leading-none">Số điện thoại *</label>
                <Input
                  id="phoneNumber"
                  placeholder="0912345678"
                  value={formData.phoneNumber}
                  onChange={(e) => setFormData({ ...formData, phoneNumber: e.target.value })}
                  required
                />
              </div>
              <div className="space-y-2 md:col-span-2">
                <label htmlFor="email" className="text-sm font-medium leading-none">Email *</label>
                <div className="flex gap-2">
                  <Input
                    id="email"
                    type="email"
                    placeholder="example@gmail.com"
                    value={formData.email}
                    onChange={(e) => {
                      setFormData({ ...formData, email: e.target.value })
                      // Đổi email thì mã xác thực cũ (nếu có) không còn hợp lệ cho email mới nữa.
                      // BUG FIX (round 7): editing after verification is now possible (round 6 fix) -
                      // otpSent/otpCode also need resetting here, otherwise the OTP row reappears
                      // pre-filled with the OLD code for the NEW address (stale "Gửi lại mã" state
                      // instead of prompting a fresh send), and a leftover non-empty otpCode lets
                      // "Xác thực" be clicked against the new/empty email before a code was ever sent.
                      setVerifiedEmail(null)
                      setOtpSent(false)
                      setOtpCode('')
                    }}
                    required
                    className="flex-1"
                  />
                  {/* BUG FIX (round 6): the field used to become permanently disabled once verified,
                      with no way to fix a typo short of reloading the page and losing the whole form.
                      The onChange handler above already invalidates verification the moment the value
                      changes, so simply leaving the field editable lets the applicant correct it and
                      re-verify without losing anything else they've filled in. */}
                  {isEmailVerified ? (
                    <span className="flex items-center gap-1 px-3 text-sm text-green-700 bg-green-50 border border-green-200 rounded-md whitespace-nowrap">
                      ✓ Đã xác thực
                    </span>
                  ) : (
                    <Button
                      type="button"
                      variant="outline"
                      className="whitespace-nowrap"
                      onClick={handleSendCode}
                      disabled={isSendingCode || !formData.email.trim()}
                    >
                      {isSendingCode ? 'Đang gửi...' : otpSent ? 'Gửi lại mã' : 'Gửi mã xác thực'}
                    </Button>
                  )}
                </div>
                {otpSent && !isEmailVerified && (
                  <div className="flex gap-2 pt-1">
                    <Input
                      placeholder="Nhập mã 6 số từ email"
                      value={otpCode}
                      onChange={(e) => setOtpCode(e.target.value)}
                      maxLength={6}
                      className="flex-1"
                    />
                    <Button
                      type="button"
                      variant="outline"
                      className="whitespace-nowrap"
                      onClick={handleVerifyCode}
                      disabled={isVerifyingCode || !otpCode.trim()}
                    >
                      {isVerifyingCode ? 'Đang xác thực...' : 'Xác thực'}
                    </Button>
                  </div>
                )}
              </div>

              <div className="space-y-2">
                <label htmlFor="password" className="text-sm font-medium leading-none">Mật khẩu *</label>
                <Input
                  id="password"
                  type="password"
                  placeholder="Tự tạo mật khẩu"
                  value={formData.password}
                  onChange={(e) => setFormData({ ...formData, password: e.target.value })}
                  minLength={8}
                  required
                />
                <p className="text-xs text-gray-500">Ít nhất 8 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt</p>
              </div>
              <div className="space-y-2">
                <label htmlFor="major" className="text-sm font-medium leading-none">Chức danh / Chuyên ngành</label>
                <Input
                  id="major"
                  placeholder="Điều dưỡng, Bác sĩ..."
                  value={formData.major}
                  onChange={(e) => setFormData({ ...formData, major: e.target.value })}
                />
              </div>

              <div className="space-y-2 md:col-span-2">
                <label htmlFor="workUnit" className="text-sm font-medium leading-none">Đơn vị công tác hiện tại</label>
                <Input
                  id="workUnit"
                  placeholder="Sinh viên trường Y, BV Đa khoa Tỉnh..."
                  value={formData.workUnit}
                  onChange={(e) => setFormData({ ...formData, workUnit: e.target.value })}
                />
              </div>

              <div className="space-y-2">
                <label htmlFor="department" className="text-sm font-medium leading-none">Khoa / Phòng muốn thi *</label>
                <select
                  id="department"
                  className="flex h-10 w-full rounded-md border border-gray-300 bg-transparent px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-green-500"
                  value={formData.departmentId}
                  onChange={(e) => setFormData({ ...formData, departmentId: e.target.value })}
                  required
                >
                  <option value="">Chọn khoa / phòng</option>
                  {departments.map((d) => (
                    <option key={d.id} value={d.id.toString()}>
                      {d.departmentName}
                    </option>
                  ))}
                </select>
              </div>

              <div className="space-y-2">
                <label htmlFor="examPurpose" className="text-sm font-medium leading-none">Mục đích dự thi</label>
                <select
                  id="examPurpose"
                  className="flex h-10 w-full rounded-md border border-gray-300 bg-transparent px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-green-500"
                  value={formData.examPurpose}
                  onChange={(e) => setFormData({ ...formData, examPurpose: e.target.value })}
                >
                  <option value="">Chọn mục đích</option>
                  <option value="Tuyển dụng">Tuyển dụng</option>
                  <option value="Lấy chứng chỉ">Lấy chứng chỉ</option>
                  <option value="Thực tập">Thực tập</option>
                  <option value="Khác">Khác</option>
                </select>
              </div>
            </div>

            <div className="flex flex-col gap-3 pt-4">
              <Button
                type="submit"
                className="w-full py-6 text-lg font-bold rounded-xl transition-all hover:scale-[1.02] shadow-xl bg-[#5b8e23] hover:bg-[#4a731c] hover:shadow-2xl disabled:opacity-50 disabled:hover:scale-100"
                disabled={isSubmitting || !isEmailVerified}
              >
                {isSubmitting ? 'Đang xử lý...' : !isEmailVerified ? 'Vui lòng xác thực email trước' : 'Gửi Đăng Ký'}
              </Button>
              <Button type="button" variant="outline" className="w-full" onClick={() => navigate('/login')} disabled={isSubmitting}>
                Quay lại Đăng nhập
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  )
}
