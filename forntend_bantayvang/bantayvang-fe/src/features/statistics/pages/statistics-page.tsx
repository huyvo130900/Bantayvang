import { useEffect, useState } from 'react'
import { statisticsApi, type DashboardDto, type TopPerformerDto } from '../api'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Users, FileQuestion, ClipboardList, Award, AlertTriangle, Activity } from 'lucide-react'

export function StatisticsPage() {
  const [dashboard, setDashboard] = useState<DashboardDto | null>(null)
  const [topPerformers, setTopPerformers] = useState<TopPerformerDto[]>([])
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    loadData()
  }, [])

  const loadData = async () => {
    setIsLoading(true)
    try {
      const [dashRes, topRes] = await Promise.all([
        statisticsApi.getDashboard(),
        statisticsApi.getTopPerformers(10),
      ])
      if (dashRes.data.success && dashRes.data.data) setDashboard(dashRes.data.data)
      if (topRes.data.success && topRes.data.data) setTopPerformers(topRes.data.data)
    } catch {
      // handle error
    } finally {
      setIsLoading(false)
    }
  }

  if (isLoading) {
    return <div className="text-center py-12 text-gray-500">Đang tải thống kê...</div>
  }

  if (!dashboard) {
    return <div className="text-center py-12 text-gray-500">Không thể tải dữ liệu</div>
  }

  return (
    <div>
      <h1 className="text-2xl font-bold text-gray-900 mb-6">Thống kê hệ thống</h1>

      {/* Stats cards */}
      <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-4 mb-8">
        <StatCard icon={Users} label="Người dùng" value={dashboard.totalUsers} sub={`${dashboard.activeUsers} hoạt động`} />
        <StatCard icon={FileQuestion} label="Câu hỏi" value={dashboard.totalQuestions} />
        <StatCard icon={ClipboardList} label="Đề thi" value={dashboard.totalExams} sub={`${dashboard.activeExams} đang mở`} />
        <StatCard icon={Award} label="Bài thi" value={dashboard.totalSubmissions} sub={`${dashboard.completedExams} hoàn thành`} />
        <StatCard icon={AlertTriangle} label="Cảnh báo" value={dashboard.totalCheatingWarnings} color="text-orange-600" />
      </div>

      {/* Average score */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-8">
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Điểm trung bình hệ thống</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-4xl font-bold text-primary">{dashboard.averageScore.toFixed(1)}</p>
            <p className="text-sm text-gray-500 mt-1">
              {dashboard.inProgressExams} bài đang làm • {dashboard.completedExams} đã hoàn thành
            </p>
          </CardContent>
        </Card>

        {/* Top performers */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Top 10 thí sinh xuất sắc</CardTitle>
          </CardHeader>
          <CardContent>
            {topPerformers.length === 0 ? (
              <p className="text-sm text-gray-500">Chưa có dữ liệu</p>
            ) : (
              <div className="space-y-2 max-h-60 overflow-y-auto">
                {topPerformers.map((p, idx) => (
                  <div key={p.userId} className="flex items-center gap-3 text-sm">
                    <span className={`font-bold w-6 text-center ${idx < 3 ? 'text-yellow-600' : 'text-gray-400'}`}>
                      {idx + 1}
                    </span>
                    <div className="flex-1 min-w-0">
                      <p className="truncate font-medium">{p.fullName || p.username}</p>
                      <p className="text-xs text-gray-400">{p.khoaPhong} • {p.examsTaken} lần thi</p>
                    </div>
                    <span className="font-bold text-primary">{p.averageScore.toFixed(1)}</span>
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Recent activities */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base flex items-center gap-2">
            <Activity className="h-4 w-4" />
            Hoạt động gần đây
          </CardTitle>
        </CardHeader>
        <CardContent>
          {dashboard.recentActivities.length === 0 ? (
            <p className="text-sm text-gray-500">Chưa có hoạt động</p>
          ) : (
            <div className="space-y-2">
              {dashboard.recentActivities.map((a, idx) => (
                <div key={idx} className="flex items-center gap-3 text-sm py-1 border-b last:border-0">
                  <span className="text-xs text-gray-400 w-32 shrink-0">
                    {new Date(a.timestamp).toLocaleString('vi-VN')}
                  </span>
                  <span className="text-gray-600 flex-1">{a.description}</span>
                  <span className="text-xs text-gray-400">{a.username}</span>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}

function StatCard({ icon: Icon, label, value, sub, color }: {
  icon: React.ComponentType<{ className?: string }>
  label: string
  value: number
  sub?: string
  color?: string
}) {
  return (
    <Card>
      <CardContent className="p-4">
        <div className="flex items-center gap-3">
          <Icon className={`h-8 w-8 ${color || 'text-primary'}`} />
          <div>
            <p className="text-2xl font-bold">{value}</p>
            <p className="text-xs text-gray-500">{label}</p>
            {sub && <p className="text-xs text-gray-400">{sub}</p>}
          </div>
        </div>
      </CardContent>
    </Card>
  )
}
