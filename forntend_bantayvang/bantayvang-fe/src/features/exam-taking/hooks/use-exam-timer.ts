import { useState, useEffect, useCallback, useRef } from 'react'

interface UseExamTimerOptions {
  initialSeconds: number
  onTimeUp: () => void
}

export function useExamTimer({ initialSeconds, onTimeUp }: UseExamTimerOptions) {
  const [remainingSeconds, setRemainingSeconds] = useState(initialSeconds)
  const onTimeUpRef = useRef(onTimeUp)
  onTimeUpRef.current = onTimeUp

  useEffect(() => {
    setRemainingSeconds(initialSeconds)
  }, [initialSeconds])

  useEffect(() => {
    if (remainingSeconds <= 0) {
      onTimeUpRef.current()
      return
    }

    const interval = setInterval(() => {
      setRemainingSeconds((prev) => {
        if (prev <= 1) {
          clearInterval(interval)
          onTimeUpRef.current()
          return 0
        }
        return prev - 1
      })
    }, 1000)

    return () => clearInterval(interval)
  }, [remainingSeconds > 0]) // eslint-disable-line react-hooks/exhaustive-deps

  const formatTime = useCallback((seconds: number) => {
    const h = Math.floor(seconds / 3600)
    const m = Math.floor((seconds % 3600) / 60)
    const s = seconds % 60
    if (h > 0) {
      return `${h}:${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`
    }
    return `${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`
  }, [])

  const isWarning = remainingSeconds <= 300 && remainingSeconds > 60 // < 5 min
  const isCritical = remainingSeconds <= 60 // < 1 min

  return {
    remainingSeconds,
    formattedTime: formatTime(remainingSeconds),
    isWarning,
    isCritical,
  }
}
