import { useState, useEffect } from 'react'
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Check, X } from 'lucide-react'
import { registrationApi } from '../api'
import type { ExamRegistrationDto } from '../types'

export function AdminRegistrationPage() {
  const [registrations, setRegistrations] = useState<ExamRegistrationDto[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [toast, setToast] = useState<{ msg: string; ok: boolean } | null>(null)

  const showToast = (msg: string, ok: boolean) => {
    setToast({ msg, ok })
    setTimeout(() => setToast(null), 3000)
  }

  const fetchRegistrations = async () => {
    try {
      const res = await registrationApi.getPending()
      setRegistrations(res.data)
    } catch (error) {
      showToast('Không thể tải danh sách đăng ký', false)
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => {
    fetchRegistrations()
  }, [])

  const handleApprove = async (id: number) => {
    try {
      await registrationApi.approve(id)
      showToast('Đã duyệt và tạo tài khoản thành công', true)
      fetchRegistrations()
    } catch (error: any) {
      showToast(error.response?.data?.message || 'Có lỗi khi duyệt', false)
    }
  }

  const handleReject = async (id: number) => {
    const reason = window.prompt('Nhập lý do từ chối (tùy chọn):')
    if (reason === null) return // Canceled

    try {
      await registrationApi.reject(id, { reason })
      showToast('Đã từ chối đơn đăng ký', true)
      fetchRegistrations()
    } catch (error: any) {
      showToast(error.response?.data?.message || 'Có lỗi khi từ chối', false)
    }
  }

  return (
    <div className="space-y-6">
      {toast && (
        <div className={`fixed top-4 right-4 z-50 flex items-center gap-2 px-4 py-3 rounded-xl shadow-lg text-sm font-medium transition-all ${toast.ok ? 'bg-green-600 text-white' : 'bg-red-600 text-white'}`}>
          {toast.ok ? '✓' : '⚠'} {toast.msg}
        </div>
      )}
      <Card>
        <CardHeader>
          <CardTitle>Duyệt đăng ký dự thi</CardTitle>
          <CardDescription>Danh sách thí sinh tự do đang chờ phê duyệt vào hệ thống</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="rounded-md border overflow-hidden">
            <table className="w-full text-sm text-left">
              <thead className="bg-gray-50 border-b">
                <tr>
                  <th className="px-4 py-3 font-medium text-gray-600">Họ tên</th>
                  <th className="px-4 py-3 font-medium text-gray-600">CCCD/Liên hệ</th>
                  <th className="px-4 py-3 font-medium text-gray-600">Chuyên môn</th>
                  <th className="px-4 py-3 font-medium text-gray-600">Đơn vị</th>
                  <th className="px-4 py-3 font-medium text-gray-600">Nguyện vọng Khoa</th>
                  <th className="px-4 py-3 font-medium text-gray-600">Mục đích</th>
                  <th className="px-4 py-3 font-medium text-gray-600">Ngày ĐK</th>
                  <th className="px-4 py-3 font-medium text-gray-600 text-right">Thao tác</th>
                </tr>
              </thead>
              <tbody className="divide-y">
                {isLoading ? (
                  <tr>
                    <td colSpan={8} className="text-center py-10 text-muted-foreground">
                      Đang tải dữ liệu...
                    </td>
                  </tr>
                ) : registrations.length === 0 ? (
                  <tr>
                    <td colSpan={8} className="text-center py-10 text-muted-foreground">
                      Không có đơn đăng ký nào đang chờ duyệt.
                    </td>
                  </tr>
                ) : (
                  registrations.map((reg) => (
                    <tr key={reg.id} className="hover:bg-gray-50 transition-colors">
                      <td className="px-4 py-3 font-medium text-gray-900">{reg.fullName}</td>
                      <td className="px-4 py-3">
                        <div className="text-sm">{reg.cccd}</div>
                        <div className="text-xs text-muted-foreground">{reg.phoneNumber}</div>
                      </td>
                      <td className="px-4 py-3 text-gray-600">{reg.chuyenNganh || '-'}</td>
                      <td className="px-4 py-3 text-gray-600">{reg.workUnit || '-'}</td>
                      <td className="px-4 py-3">
                        {reg.tenKhoaPhong ? (
                          <span className="inline-flex items-center rounded-md border px-2.5 py-0.5 text-xs font-semibold bg-blue-50 text-blue-700">
                            {reg.tenKhoaPhong}
                          </span>
                        ) : '-'}
                      </td>
                      <td className="px-4 py-3 text-gray-600">{reg.mucDichThi || '-'}</td>
                      <td className="px-4 py-3 text-gray-600">{new Date(reg.ngayDangKy).toLocaleDateString('vi-VN')}</td>
                      <td className="px-4 py-3 text-right">
                        <div className="flex justify-end gap-2">
                          <Button size="sm" variant="outline" className="text-green-600 border-green-200 hover:bg-green-50" onClick={() => handleApprove(reg.id)}>
                            <Check className="w-4 h-4 mr-1" /> Duyệt
                          </Button>
                          <Button size="sm" variant="outline" className="text-red-600 border-red-200 hover:bg-red-50" onClick={() => handleReject(reg.id)}>
                            <X className="w-4 h-4" />
                          </Button>
                        </div>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
