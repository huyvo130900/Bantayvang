// ============================================================
// FILE: src/features/exam-taking/hooks/use-exam-timer.ts
// FIX: Bài thi kết thúc ngay khi vào
//
// LỖI: initialSeconds = examInfo?.remainingTimeSeconds ?? 0
//   → Khi component mount, examInfo chưa load xong → remainingTimeSeconds = undefined
//   → initialSeconds = 0 → useEffect thấy remainingSeconds <= 0 → gọi onTimeUp() ngay
//   → nộp bài luôn dù chưa làm gì!
//
// FIX: Chỉ start timer sau khi initialSeconds > 0 lần đầu tiên.
//   Dùng flag "hasStarted" để không gọi onTimeUp khi initialSeconds vẫn là 0.
// ============================================================

import { useState, useEffect, useCallback, useRef } from 'react'

interface UseExamTimerOptions {
  initialSeconds: number
  isLoaded: boolean
  onTimeUp: () => void
}

export function useExamTimer({ initialSeconds, isLoaded, onTimeUp }: UseExamTimerOptions) {
  const [remainingSeconds, setRemainingSeconds] = useState(initialSeconds)
  const hasStartedRef = useRef(false)
  const onTimeUpRef = useRef(onTimeUp)
  useEffect(() => {
    onTimeUpRef.current = onTimeUp
  }, [onTimeUp])

  useEffect(() => {
    if (isLoaded) {
      hasStartedRef.current = true
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setRemainingSeconds(initialSeconds)
      if (initialSeconds <= 0) {
        onTimeUpRef.current()
      }
    }
  }, [initialSeconds, isLoaded])

  useEffect(() => {
    if (!hasStartedRef.current) return

    if (remainingSeconds <= 0) {
      onTimeUpRef.current()
      return
    }

    const interval = setInterval(() => {
      setRemainingSeconds((prev) => {
        if (prev <= 1) {
          clearInterval(interval)
          return 0
        }
        return prev - 1
      })
    }, 1000)

    return () => clearInterval(interval)
  }, [remainingSeconds])

  const formatTime = useCallback((seconds: number) => {
    const h = Math.floor(seconds / 3600)
    const m = Math.floor((seconds % 3600) / 60)
    const s = seconds % 60
    if (h > 0) {
      return `${h}:${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`
    }
    return `${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`
  }, [])

  const isWarning = remainingSeconds <= 300 && remainingSeconds > 60
  const isCritical = remainingSeconds <= 60

  return {
    remainingSeconds,
    formattedTime: formatTime(remainingSeconds),
    isWarning,
    isCritical,
  }
}
