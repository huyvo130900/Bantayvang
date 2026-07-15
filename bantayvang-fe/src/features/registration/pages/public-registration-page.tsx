import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { registrationApi } from '../api'
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
    cccd: '',
    soDienThoai: '',
    email: '',
    password: '',
    workUnit: '',
    chuyenNganh: '',
    departmentId: '',
    mucDichThi: '',
  })

  useEffect(() => {
    const fetchDepartments = async () => {
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

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!formData.fullName || !formData.cccd || !formData.soDienThoai || !formData.password) {
      showToast('Vui lòng điền đầy đủ các trường bắt buộc', false)
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
                <label htmlFor="cccd" className="text-sm font-medium leading-none">Số CCCD *</label>
                <Input
                  id="cccd"
                  placeholder="Căn cước công dân"
                  value={formData.cccd}
                  onChange={(e) => setFormData({ ...formData, cccd: e.target.value })}
                  required
                />
                <p className="text-xs text-gray-500">Được dùng làm Tên đăng nhập</p>
              </div>
              
              <div className="space-y-2">
                <label htmlFor="soDienThoai" className="text-sm font-medium leading-none">Số điện thoại *</label>
                <Input
                  id="soDienThoai"
                  placeholder="0912345678"
                  value={formData.soDienThoai}
                  onChange={(e) => setFormData({ ...formData, soDienThoai: e.target.value })}
                  required
                />
              </div>
              <div className="space-y-2">
                <label htmlFor="email" className="text-sm font-medium leading-none">Email</label>
                <Input
                  id="email"
                  type="email"
                  placeholder="example@gmail.com"
                  value={formData.email}
                  onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                />
              </div>

              <div className="space-y-2">
                <label htmlFor="password" className="text-sm font-medium leading-none">Mật khẩu *</label>
                <Input
                  id="password"
                  type="password"
                  placeholder="Tự tạo mật khẩu"
                  value={formData.password}
                  onChange={(e) => setFormData({ ...formData, password: e.target.value })}
                  required
                />
              </div>
              <div className="space-y-2">
                <label htmlFor="chuyenNganh" className="text-sm font-medium leading-none">Chức danh / Chuyên ngành</label>
                <Input
                  id="chuyenNganh"
                  placeholder="Điều dưỡng, Bác sĩ..."
                  value={formData.chuyenNganh}
                  onChange={(e) => setFormData({ ...formData, chuyenNganh: e.target.value })}
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
                <label htmlFor="department" className="text-sm font-medium leading-none">Khoa / Phòng muốn thi</label>
                <select
                  id="department"
                  className="flex h-10 w-full rounded-md border border-gray-300 bg-transparent px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-green-500"
                  value={formData.departmentId}
                  onChange={(e) => setFormData({ ...formData, departmentId: e.target.value })}
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
                <label htmlFor="mucDichThi" className="text-sm font-medium leading-none">Mục đích dự thi</label>
                <select
                  id="mucDichThi"
                  className="flex h-10 w-full rounded-md border border-gray-300 bg-transparent px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-green-500"
                  value={formData.mucDichThi}
                  onChange={(e) => setFormData({ ...formData, mucDichThi: e.target.value })}
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
              <Button type="submit" className="w-full bg-[#5b8e23] hover:bg-[#4a731c]" disabled={isSubmitting}>
                {isSubmitting ? 'Đang xử lý...' : 'Gửi Đăng Ký'}
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
