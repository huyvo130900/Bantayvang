import { useEffect, useState } from 'react'
import { statisticsApi, type DashboardDto, type ExamStatisticsDto } from '../api'
import { Users, FileQuestion, ClipboardList, Award, AlertTriangle, RefreshCw, BarChart2 } from 'lucide-react'
import { formatDate } from '@/lib/utils'
import { kyThiApi } from '@/features/ky-thi/api'
import type { KyThiDto } from '@/features/ky-thi/types'

function StatCard({
  icon: Icon,
  label,
  value,
  sub,
  color = 'text-green-600',
  bgColor = 'bg-green-50',
}: {
  icon: React.ElementType
  label: string
  value: number | string
  sub?: string
  color?: string
  bgColor?: string
}) {
  return (
    <div className="bg-white rounded-xl border p-4 flex items-start gap-3 shadow-sm">
      <div className={`p-2.5 rounded-xl ${bgColor}`}>
        <Icon className={`h-5 w-5 ${color}`} />
      </div>
      <div className="min-w-0">
        <p className="text-xs text-gray-400 font-medium uppercase tracking-wide">{label}</p>
        <p className="text-2xl font-bold text-gray-900 mt-0.5">{value}</p>
        {sub && <p className="text-xs text-gray-400 mt-0.5">{sub}</p>}
      </div>
    </div>
  )
}

export function StatisticsPage() {
  const [dashboard, setDashboard] = useState<DashboardDto | null>(null)
  const [kyThis, setKyThis] = useState<KyThiDto[]>([])
  const [kyThiStats, setKyThiStats] = useState<ExamStatisticsDto | null>(null)
  const [selectedKyThiId, setSelectedKyThiId] = useState<number | ''>('')
  const [isLoading, setIsLoading] = useState(true)
  const [isLoadingKyThiStats, setIsLoadingKyThiStats] = useState(false)

  useEffect(() => {
    loadDashboard()
  }, [])

  useEffect(() => {
    if (selectedKyThiId) {
      loadKyThiStats(Number(selectedKyThiId))
    } else {
      setKyThiStats(null)
    }
  }, [selectedKyThiId])

  const loadDashboard = async () => {
    setIsLoading(true)
    try {
      const [dashRes, kyThiRes] = await Promise.all([
        statisticsApi.getDashboard(),
        kyThiApi.getAll(),
      ])
      if (dashRes.data.success && dashRes.data.data) setDashboard(dashRes.data.data)
      if (kyThiRes.data.success && kyThiRes.data.data) setKyThis(kyThiRes.data.data)
    } catch {
      // silent
    } finally {
      setIsLoading(false)
    }
  }

  const loadKyThiStats = async (id: number) => {
    setIsLoadingKyThiStats(true)
    try {
      const res = await statisticsApi.getKyThiStatistics(id)
      if (res.data.success && res.data.data) setKyThiStats(res.data.data)
    } catch {
      // silent
    } finally {
      setIsLoadingKyThiStats(false)
    }
  }

  if (isLoading) {
    return (
      <div className="space-y-4 animate-pulse">
        <div className="grid grid-cols-2 md:grid-cols-5 gap-4">
          {[...Array(5)].map((_, i) => <div key={i} className="h-24 bg-gray-100 rounded-xl" />)}
        </div>
        <div className="h-64 bg-gray-100 rounded-xl" />
      </div>
    )
  }

  if (!dashboard) {
    return (
      <div className="text-center py-16">
        <p className="text-gray-500">Không thể tải dữ liệu thống kê</p>
        <button onClick={loadDashboard} className="mt-3 text-primary text-sm hover:underline">Thử lại</button>
      </div>
    )
  }

  const maxDistCount = Math.max(
    ...(kyThiStats?.scoreDistribution.map((d) => d.count) || [1]),
    1
  )

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <h1 className="text-2xl font-bold text-gray-900">Thống kê hệ thống</h1>
        <button
          onClick={loadDashboard}
          className="flex items-center justify-center gap-1.5 text-sm text-gray-500 hover:text-gray-900 border rounded-lg px-3 py-1.5 hover:bg-gray-50 transition-colors w-full sm:w-auto"
        >
          <RefreshCw className="h-4 w-4" />
          Làm mới
        </button>
      </div>

      {/* Global stats */}
      <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-4">
        <StatCard icon={Users} label="Người dùng" value={dashboard.totalUsers} sub={`${dashboard.activeUsers} hoạt động`} />
        <StatCard icon={FileQuestion} label="Câu hỏi" value={dashboard.totalQuestions} bgColor="bg-green-50" color="text-green-600" />
        <StatCard icon={ClipboardList} label="Đề thi" value={dashboard.totalExams} sub={`${dashboard.activeExams} đang mở`} bgColor="bg-blue-50" color="text-blue-600" />
        <StatCard icon={Award} label="Bài thi" value={dashboard.totalSubmissions} sub={`${dashboard.completedExams} hoàn thành`} bgColor="bg-purple-50" color="text-purple-600" />
        <StatCard icon={AlertTriangle} label="Cảnh báo GTC" value={dashboard.totalCheatingWarnings} bgColor="bg-orange-50" color="text-orange-600" />
      </div>

      {/* Average + Progress */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="bg-white rounded-xl border p-5 shadow-sm text-center">
          <p className="text-xs text-gray-400 uppercase tracking-wide mb-2">Điểm trung bình hệ thống</p>
          <p className="text-5xl font-bold text-primary">{dashboard.averageScore.toFixed(1)}</p>
          <p className="text-xs text-gray-400 mt-2">/ 10 điểm</p>
        </div>
        <div className="bg-white rounded-xl border p-5 shadow-sm">
          <p className="text-xs text-gray-400 uppercase tracking-wide mb-3">Tình trạng bài thi</p>
          <div className="space-y-2">
            {[
              { label: 'Đang làm', value: dashboard.inProgressExams, color: 'bg-blue-500', max: dashboard.totalSubmissions },
              { label: 'Hoàn thành', value: dashboard.completedExams, color: 'bg-green-500', max: dashboard.totalSubmissions },
            ].map((item) => (
              <div key={item.label}>
                <div className="flex justify-between text-xs mb-1">
                  <span className="text-gray-600">{item.label}</span>
                  <span className="font-semibold text-gray-800">{item.value}</span>
                </div>
                <div className="h-2 bg-gray-100 rounded-full overflow-hidden">
                  <div
                    className={`h-full ${item.color} rounded-full transition-all duration-700`}
                    style={{ width: `${item.max > 0 ? (item.value / item.max) * 100 : 0}%` }}
                  />
                </div>
              </div>
            ))}
          </div>
        </div>
        <div className="bg-white rounded-xl border p-5 shadow-sm">
          <p className="text-xs text-gray-400 uppercase tracking-wide mb-3">Hoạt động gần đây</p>
          <div className="space-y-2 max-h-28 overflow-y-auto">
            {dashboard.recentActivities.slice(0, 5).map((a, i) => (
              <div key={i} className="flex items-start gap-2 text-xs">
                <span className="w-1.5 h-1.5 rounded-full bg-primary shrink-0 mt-1" />
                <div className="min-w-0">
                  <p className="text-gray-700 truncate">{a.description}</p>
                  <p className="text-gray-400">{formatDate(a.timestamp)}</p>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Per-exam statistics */}
      <div className="bg-white rounded-xl border shadow-sm overflow-hidden">
        <div className="p-4 border-b flex items-center justify-between">
          <div className="flex items-center gap-2">
            <BarChart2 className="h-5 w-5 text-primary" />
            <h2 className="font-semibold text-gray-900">Thống kê theo kỳ thi</h2>
          </div>
          <select
            value={selectedKyThiId}
            onChange={(e) => setSelectedKyThiId(e.target.value ? Number(e.target.value) : '')}
            className="h-9 rounded-lg border border-gray-200 px-3 text-sm min-w-[240px] focus:outline-none focus:ring-2 focus:ring-primary/30"
          >
            <option value="">— Chọn kỳ thi —</option>
            {kyThis.map((k) => (
              <option key={k.id} value={k.id}>{k.maKyThi} — {k.tenKyThi}</option>
            ))}
          </select>
        </div>

        {!selectedKyThiId && (
          <div className="py-16 text-center text-gray-400">
            <BarChart2 className="h-12 w-12 mx-auto mb-3 opacity-20" />
            <p>Chọn kỳ thi để xem thống kê chi tiết</p>
          </div>
        )}

        {selectedKyThiId && isLoadingKyThiStats && (
          <div className="py-12 text-center text-gray-400 animate-pulse">Đang tải...</div>
        )}

        {selectedKyThiId && !isLoadingKyThiStats && kyThiStats && (
          <div className="p-5 space-y-5">
            {/* Exam summary stats */}
            <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
              {[
                { label: 'Thí sinh tham gia', value: kyThiStats.totalParticipants, cls: 'text-gray-700' },
                { label: 'Tỷ lệ đạt', value: `${kyThiStats.passRate.toFixed(1)}%`, cls: 'text-green-600' },
                { label: 'Điểm TB', value: kyThiStats.averageScore.toFixed(1), cls: 'text-primary' },
                { label: 'Cao nhất / Thấp nhất', value: `${kyThiStats.highestScore} / ${kyThiStats.lowestScore}`, cls: 'text-gray-600' },
              ].map((s) => (
                <div key={s.label} className="bg-gray-50 rounded-xl p-4 text-center border">
                  <p className={`text-xl font-bold ${s.cls}`}>{s.value}</p>
                  <p className="text-xs text-gray-400 mt-1">{s.label}</p>
                </div>
              ))}
            </div>

            {/* Pass/fail bar */}
            <div className="bg-gray-50 rounded-xl p-4 border">
              <div className="flex justify-between text-sm mb-2">
                <span className="text-green-600 font-medium">Đạt: {kyThiStats.passCount}</span>
                <span className="text-red-500 font-medium">Không đạt: {kyThiStats.failCount}</span>
              </div>
              {kyThiStats.totalParticipants > 0 && (
                <div className="h-4 bg-red-100 rounded-full overflow-hidden">
                  <div
                    className="h-full bg-green-500 rounded-full transition-all duration-700"
                    style={{ width: `${(kyThiStats.passCount / kyThiStats.totalParticipants) * 100}%` }}
                  />
                </div>
              )}
            </div>

            {/* Score distribution chart */}
            {kyThiStats.scoreDistribution.length > 0 && (
              <div>
                <h3 className="text-sm font-semibold text-gray-700 mb-3">Phân phối điểm</h3>
                <div className="space-y-2">
                  {kyThiStats.scoreDistribution.map((dist) => (
                    <div key={dist.range} className="flex items-center gap-3">
                      <span className="text-xs text-gray-500 w-20 text-right shrink-0">{dist.range}</span>
                      <div className="flex-1 h-6 bg-gray-100 rounded-lg overflow-hidden">
                        <div
                          className="h-full bg-primary rounded-lg transition-all duration-500 flex items-center justify-end pr-2"
                          style={{
                            width: `${(dist.count / maxDistCount) * 100}%`,
                            minWidth: dist.count > 0 ? '2rem' : '0',
                          }}
                        >
                          {dist.count > 0 && (
                            <span className="text-[11px] text-white font-semibold">{dist.count}</span>
                          )}
                        </div>
                      </div>
                      <span className="text-xs text-gray-400 w-12 shrink-0">
                        {dist.percentage.toFixed(0)}%
                      </span>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  )
}
