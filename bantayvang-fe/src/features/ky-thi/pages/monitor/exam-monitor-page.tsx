import { useParams, useNavigate } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { ArrowLeft, Users, AlertTriangle } from 'lucide-react'
import { useMonitorSignalR } from './use-monitor-signalr'
import { StudentMonitorCard } from './student-monitor-card'
import { examMonitorApi } from '../../api'

export function ExamMonitorPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const examCampaignId = parseInt(id || '0')

  const { students, loading } = useMonitorSignalR(examCampaignId)

  async function handleForceSubmit(submissionId: number) {
    if (!window.confirm('Bạn có chắc chắn muốn đuổi thí sinh này? Bài thi sẽ bị nộp ngay lập tức.')) return

    try {
      const res = await examMonitorApi.forceSubmitSession(submissionId)
      if (res.data.success) {
        alert('Đã đình chỉ thí sinh thành công')
      } else {
        alert(res.data.message || 'Lỗi khi đình chỉ thí sinh')
      }
    } catch (error) {
      alert('Lỗi kết nối khi đình chỉ')
    }
  }

  if (loading) {
    return <div className="p-8 text-center">Đang tải danh sách phòng thi...</div>
  }

  const activeCount = students.filter(s => s.status !== 'Completed').length
  const warningCount = students.filter(s => s.warningCount > 0 && s.status !== 'Completed').length

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center gap-4">
        <Button variant="ghost" onClick={() => navigate('/quan-ly-ky-thi')}>
          <ArrowLeft className="w-4 h-4 mr-2" />
          Quay lại
        </Button>
        <h1 className="text-2xl font-bold">Giám sát kỳ thi thời gian thực</h1>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="bg-white p-4 rounded-lg shadow border flex items-center gap-4">
          <div className="p-3 bg-blue-100 rounded-full">
            <Users className="w-6 h-6 text-blue-600" />
          </div>
          <div>
            <div className="text-sm text-gray-500">Tổng thí sinh đang thi</div>
            <div className="text-2xl font-bold">{activeCount}</div>
          </div>
        </div>
        <div className="bg-white p-4 rounded-lg shadow border flex items-center gap-4">
          <div className="p-3 bg-yellow-100 rounded-full">
            <AlertTriangle className="w-6 h-6 text-yellow-600" />
          </div>
          <div>
            <div className="text-sm text-gray-500">Có cảnh báo vi phạm</div>
            <div className="text-2xl font-bold text-yellow-600">{warningCount}</div>
          </div>
        </div>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
        {students.map(student => (
          <StudentMonitorCard 
            key={student.examSubmissionId} 
            student={student} 
            onForceSubmit={handleForceSubmit} 
          />
        ))}
        {students.length === 0 && (
          <div className="col-span-full p-8 text-center text-gray-500 border-2 border-dashed rounded-lg">
            Không có thí sinh nào đang làm bài trong kỳ thi này.
          </div>
        )}
      </div>
    </div>
  )
}
