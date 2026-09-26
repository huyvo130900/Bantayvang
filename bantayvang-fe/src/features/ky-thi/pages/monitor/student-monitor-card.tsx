import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { AlertCircle, Lock, User, FileText, Clock } from 'lucide-react'
import type { ActiveStudentMonitor } from './use-monitor-signalr'
import { format } from 'date-fns'
import { vi } from 'date-fns/locale'

interface StudentMonitorCardProps {
  student: ActiveStudentMonitor
  onForceSubmit: (submissionId: number) => void
}

export function StudentMonitorCard({ student, onForceSubmit }: StudentMonitorCardProps) {
  const isLocked = student.warningCount >= 6
  const isCompleted = student.status === 'Completed'
  
  let borderColor = 'border-gray-200'
  let bgColor = 'bg-white'
  let warningIcon = null

  if (isLocked) {
    borderColor = 'border-red-800'
    bgColor = 'bg-red-100'
    warningIcon = <Lock className="text-red-800 w-5 h-5" />
  } else if (isCompleted) {
    borderColor = 'border-gray-200 opacity-50'
    bgColor = 'bg-gray-50'
  } else if (student.warningCount >= 4) {
    borderColor = 'border-red-500'
    bgColor = 'bg-red-50'
    warningIcon = <AlertCircle className="text-red-500 w-5 h-5 animate-pulse" />
  } else if (student.warningCount >= 1) {
    borderColor = 'border-yellow-400'
    bgColor = 'bg-yellow-50'
    warningIcon = <AlertCircle className="text-yellow-500 w-5 h-5" />
  }

  return (
    <Card className={`transition-all duration-300 border-2 ${borderColor} ${bgColor}`}>
      <CardHeader className="pb-2">
        <div className="flex justify-between items-start">
          <CardTitle className="text-sm font-semibold flex items-center gap-2">
            <User className="w-4 h-4" />
            {student.fullName}
          </CardTitle>
          {warningIcon}
        </div>
        <div className="text-xs text-muted-foreground font-mono mt-1">
          {student.userCode || 'N/A'}
        </div>
      </CardHeader>
      <CardContent className="text-sm space-y-2 pb-2">
        <div className="flex items-center gap-2">
          <FileText className="w-4 h-4 text-gray-500" />
          <span>Mã đề: <strong>{student.examPaperCode || 'N/A'}</strong></span>
        </div>
        <div className="flex items-center gap-2">
          <AlertCircle className="w-4 h-4 text-gray-500" />
          <span>Lỗi vi phạm: <strong>{student.warningCount}/5</strong></span>
        </div>
        {student.startTime && (
          <div className="flex items-center gap-2">
            <Clock className="w-4 h-4 text-gray-500" />
            <span>Bắt đầu: {format(new Date(student.startTime), 'HH:mm', { locale: vi })}</span>
          </div>
        )}
        {isLocked && <div className="text-red-700 font-bold mt-2">Đã bị khóa</div>}
        {isCompleted && !isLocked && <div className="text-gray-500 font-bold mt-2">Đã nộp bài</div>}
      </CardContent>
      <div className="flex items-center p-4 pt-0">
        <Button 
          variant="destructive" 
          size="sm" 
          className="w-full" 
          disabled={isCompleted || isLocked}
          onClick={() => onForceSubmit(student.examSubmissionId)}
        >
          Đuổi thi
        </Button>
      </div>
    </Card>
  )
}
