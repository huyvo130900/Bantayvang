import { Clock } from 'lucide-react'
import { cn } from '@/lib/utils'

interface ExamTimerProps {
  formattedTime: string
  isWarning: boolean
  isCritical: boolean
}

export function ExamTimer({ formattedTime, isWarning, isCritical }: ExamTimerProps) {
  return (
    <div
      className={cn(
        'flex items-center gap-2 px-4 py-2 rounded-lg font-mono text-lg font-bold',
        isCritical && 'bg-red-100 text-red-700 animate-pulse',
        isWarning && !isCritical && 'bg-yellow-100 text-yellow-700',
        !isWarning && !isCritical && 'bg-gray-100 text-gray-700'
      )}
    >
      <Clock className="h-5 w-5" />
      <span>{formattedTime}</span>
    </div>
  )
}
