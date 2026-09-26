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


  async function loadDashboard(isRefresh = false) {
    if (isRefresh) setLoading(true)
    setError(null)
    try {
      // Gọi API backend — trả về dữ liệu thực của khoa mà người dùng đang quản lý
      const res = await departmentApi.getMyDashboard()
      if (res.data?.success && res.data.data) {
        setDashboard(res.data.data)
      } else {
        setError(res.data?.message || 'Không thể tải dữ liệu tổng quan')
      }
    } catch (err: any) {
      const e = err as { response?: { data?: { message?: string } } }
      const msg = e.response?.data?.message || 'Không thể tải dữ liệu tổng quan. Vui lòng thử lại.'
      setError(msg)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect
    loadDashboard(false)
  }, [])  


  const stats = [
    {
      label: 'Câu hỏi',
      value: dashboard?.totalQuestions ?? 0,
      icon: FileQuestion,
      color: 'text-blue-500',
      bg: 'bg-blue-50',
      path: '/dept-manager/questions',
    },
    {
      label: 'Kỳ thi',
      value: dashboard?.totalExams ?? 0,
      icon: CalendarDays,
      color: 'text-purple-500',
      bg: 'bg-purple-50',
      path: '/dept-manager/ky-thi',
    },
    {
      label: 'Thí sinh',
      value: dashboard?.totalCandidates ?? 0,
      icon: Users,
      color: 'text-green-500',
      bg: 'bg-green-50',
      path: '/dept-manager/results',
    },
    {
      label: 'Điểm TB',
      value: (dashboard?.averageScore ?? 0) > 0 
        ? dashboard!.averageScore.toFixed(1) 
        : '—',
      icon: BarChart3,
      color: 'text-orange-500',
      bg: 'bg-orange-50',
      path: '/dept-manager/results',
    },
  ]

  return (
    <div className="p-4 sm:p-6 space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Tổng quan</h1>
          <p className="text-sm text-gray-500 mt-1">
            {dashboard?.departmentName
              ? `Khoa: ${dashboard.departmentName}`
              : 'Dashboard Quản lý Khoa'}
          </p>
        </div>
        <Button variant="ghost" size="sm" onClick={() => loadDashboard(true)} disabled={loading} className="w-full sm:w-auto justify-center">
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
                className="bg-white rounded-xl border p-4 sm:p-5 text-left hover:shadow-md transition-shadow"
              >
                <div className={`inline-flex p-2 rounded-lg ${s.bg} mb-3`}>
                  <s.icon className={`h-5 w-5 ${s.color}`} />
                </div>
                <p className="text-xl sm:text-2xl font-bold text-gray-900">{s.value}</p>
                <p className="text-xs sm:text-sm text-gray-500 mt-1">{s.label}</p>
              </button>
            ))}
          </div>

          {/* Kỳ thi gần đây */}
          <div className="bg-white rounded-xl border">
            <div className="flex items-center justify-between px-4 sm:px-6 py-4 border-b">
              <h2 className="font-semibold text-gray-800 flex items-center gap-2">
                <CalendarDays className="h-4 w-4 text-blue-500" />
                Kỳ thi của khoa
              </h2>
              <Button variant="ghost" size="sm" onClick={() => navigate('/dept-manager/ky-thi')}>
                Xem tất cả <ChevronRight className="h-4 w-4 ml-1" />
              </Button>
            </div>

            {!dashboard?.recentCampaigns?.length ? (
              <div className="text-center py-12 text-gray-400">Chưa có kỳ thi nào</div>
            ) : (
              <div className="divide-y">
                {dashboard.recentCampaigns.map(kt => (
                  <div
                    key={kt.id}
                    className="flex flex-col sm:flex-row sm:items-center justify-between px-4 sm:px-6 py-4 hover:bg-gray-50 cursor-pointer gap-2"
                    onClick={() => navigate('/dept-manager/ky-thi')}
                  >
                    <div className="min-w-0 flex-1">
                      <p className="font-medium text-gray-800 truncate">{kt.campaignName}</p>
                      <p className="text-xs text-gray-400 mt-0.5">
                        {kt.examCount} đề thi • {kt.candidateCount} thí sinh
                      </p>
                    </div>
                    <div className="flex items-center justify-between sm:justify-end gap-3 w-full sm:w-auto shrink-0 mt-1 sm:mt-0">
                      <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${
                        kt.status === 'DangDienRa'
                          ? 'bg-green-100 text-green-700'
                          : kt.status === 'DaKetThuc'
                          ? 'bg-gray-100 text-gray-500'
                          : 'bg-yellow-100 text-yellow-700'
                      }`}>
                        {kt.status === 'DangDienRa' ? 'Đang diễn ra'
                          : kt.status === 'DaKetThuc' ? 'Đã kết thúc'
                          : 'Chuẩn bị'}
                      </span>
                      <ChevronRight className="h-4 w-4 text-gray-400 hidden sm:block" />
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
