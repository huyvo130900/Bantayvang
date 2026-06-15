import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import type { DepartmentDashboardDto } from '@/features/departments/types'
import { departmentApi } from '@/features/departments/api'
import { FileQuestion, Users, BarChart3, CalendarDays, ChevronRight, RefreshCw } from 'lucide-react'
import { Button } from '@/components/ui/button'

export function DeptManagerDashboard() {
  const navigate = useNavigate()
  const [dashboard, setDashboard] = useState<DepartmentDashboardDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    loadDashboard()
  }, []) // eslint-disable-line

  const loadDashboard = async () => {
    setLoading(true)
    setError(null)
    try {
      // Gọi API backend — trả về dữ liệu thực của khoa mà người dùng đang quản lý
      const res = await departmentApi.getMyDashboard()
      if (res.data?.success && res.data.data) {
        setDashboard(res.data.data)
      } else {
        setError(res.data?.message || 'Không thể tải dữ liệu tổng quan')
      }
    } catch (err: unknown) {
      const e = err as { response?: { data?: { message?: string } } }
      const msg = e.response?.data?.message || 'Không thể tải dữ liệu tổng quan. Vui lòng thử lại.'
      setError(msg)
    } finally {
      setLoading(false)
    }
  }

  const stats = [
    {
      label: 'Câu hỏi',
      value: dashboard?.tongSoCauHoi ?? 0,
      icon: FileQuestion,
      color: 'text-blue-500',
      bg: 'bg-blue-50',
      path: '/dept-manager/questions',
    },
    {
      label: 'Kỳ thi',
      value: dashboard?.tongSoDeThi ?? 0,
      icon: CalendarDays,
      color: 'text-purple-500',
      bg: 'bg-purple-50',
      path: '/dept-manager/ky-thi',
    },
    {
      label: 'Thí sinh',
      value: dashboard?.tongSoThiSinh ?? 0,
      icon: Users,
      color: 'text-green-500',
      bg: 'bg-green-50',
      path: '/dept-manager/results',
    },
    {
      label: 'Điểm TB',
      value: (dashboard?.diemTrungBinh ?? 0) > 0
        ? dashboard!.diemTrungBinh.toFixed(1)
        : '—',
      icon: BarChart3,
      color: 'text-orange-500',
      bg: 'bg-orange-50',
      path: '/dept-manager/results',
    },
  ]

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Tổng quan</h1>
          <p className="text-sm text-gray-500 mt-1">
            {dashboard?.tenKhoa
              ? `Khoa: ${dashboard.tenKhoa}`
              : 'Dashboard Quản lý Khoa'}
          </p>
        </div>
        <Button variant="ghost" size="sm" onClick={loadDashboard} disabled={loading}>
          <RefreshCw className={`h-4 w-4 mr-1 ${loading ? 'animate-spin' : ''}`} />
          Làm mới
        </Button>
      </div>

      {/* Error banner */}
      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 rounded-lg px-4 py-3 text-sm">
          {error}
        </div>
      )}

      {loading ? (
        <div className="text-center py-12 text-gray-400">Đang tải...</div>
      ) : (
        <>
          {/* Stats grid */}
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            {stats.map(s => (
              <button
                key={s.label}
                onClick={() => navigate(s.path)}
                className="bg-white rounded-xl border p-5 text-left hover:shadow-md transition-shadow"
              >
                <div className={`inline-flex p-2 rounded-lg ${s.bg} mb-3`}>
                  <s.icon className={`h-5 w-5 ${s.color}`} />
                </div>
                <p className="text-2xl font-bold text-gray-900">{s.value}</p>
                <p className="text-sm text-gray-500 mt-1">{s.label}</p>
              </button>
            ))}
          </div>

          {/* Kỳ thi gần đây */}
          <div className="bg-white rounded-xl border">
            <div className="flex items-center justify-between px-6 py-4 border-b">
              <h2 className="font-semibold text-gray-800 flex items-center gap-2">
                <CalendarDays className="h-4 w-4 text-blue-500" />
                Kỳ thi của khoa
              </h2>
              <Button variant="ghost" size="sm" onClick={() => navigate('/dept-manager/ky-thi')}>
                Xem tất cả <ChevronRight className="h-4 w-4 ml-1" />
              </Button>
            </div>

            {!dashboard?.kyThiGanDay?.length ? (
              <div className="text-center py-12 text-gray-400">Chưa có kỳ thi nào</div>
            ) : (
              <div className="divide-y">
                {dashboard.kyThiGanDay.map(kt => (
                  <div
                    key={kt.id}
                    className="flex items-center justify-between px-6 py-4 hover:bg-gray-50 cursor-pointer"
                    onClick={() => navigate('/dept-manager/ky-thi')}
                  >
                    <div>
                      <p className="font-medium text-gray-800">{kt.tenKyThi}</p>
                      <p className="text-xs text-gray-400 mt-0.5">
                        {kt.soDeThi} đề thi · {kt.soThiSinh} thí sinh
                      </p>
                    </div>
                    <div className="flex items-center gap-3">
                      <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${
                        kt.trangThai === 'DangDienRa'
                          ? 'bg-green-100 text-green-700'
                          : kt.trangThai === 'DaKetThuc'
                          ? 'bg-gray-100 text-gray-500'
                          : 'bg-yellow-100 text-yellow-700'
                      }`}>
                        {kt.trangThai === 'DangDienRa' ? 'Đang diễn ra'
                          : kt.trangThai === 'DaKetThuc' ? 'Đã kết thúc'
                          : 'Chuẩn bị'}
                      </span>
                      <ChevronRight className="h-4 w-4 text-gray-400" />
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </>
      )}
    </div>
  )
}
