import { useEffect, useRef, useState, useCallback } from 'react'
import { examTakingApi } from '../api'
import { MAX_CHEATING_WARNINGS } from '@/lib/constants'

interface UseAntiCheatOptions {
  baithiId: number
  enabled: boolean
  onForceSubmit?: (reason: string) => void
}

export function useAntiCheat({ baithiId, enabled, onForceSubmit }: UseAntiCheatOptions) {
  const [warningCount, setWarningCount] = useState(0)
  const [lastWarningType, setLastWarningType] = useState<string | null>(null)
  const [isFullscreen, setIsFullscreen] = useState(false)
  const baithiIdRef = useRef(baithiId)
  const warningCountRef = useRef(0)
  const onForceSubmitRef = useRef(onForceSubmit)
  const fullscreenTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null)
  const hasEnteredFullscreenRef = useRef(false)

  baithiIdRef.current = baithiId
  onForceSubmitRef.current = onForceSubmit

  const logWarning = useCallback(async (type: string, description: string) => {
    const newCount = warningCountRef.current + 1
    warningCountRef.current = newCount
    setWarningCount(newCount)
    setLastWarningType(type)

    try {
      await examTakingApi.logWarning({
        idBaiThi: baithiIdRef.current,
        loaiCanhBao: type,
        moTa: description,
      })
    } catch {
      // Silent fail - don't disrupt exam
    }

    // Enforce termination after MAX_CHEATING_WARNINGS or immediately for FULLSCREEN_EXIT
    if (type === 'FULLSCREEN_EXIT') {
      onForceSubmitRef.current?.('Thí sinh thoát chế độ toàn màn hình')
    } else if (newCount > MAX_CHEATING_WARNINGS) {
      onForceSubmitRef.current?.(`Bài thi kết thúc do gian lận quá ${MAX_CHEATING_WARNINGS} lần`)
    }
  }, [])

  const requestFullscreen = useCallback(() => {
    document.documentElement.requestFullscreen()
      .then(() => {
        setIsFullscreen(true)
        hasEnteredFullscreenRef.current = true
        if (fullscreenTimeoutRef.current) {
          clearTimeout(fullscreenTimeoutRef.current)
          fullscreenTimeoutRef.current = null
        }
      })
      .catch(() => {
        // Silent fail (browser block)
      })
  }, [])

  useEffect(() => {
    if (!enabled || !baithiId) return

    // Initialize fullscreen state
    const currentFull = !!document.fullscreenElement
    setIsFullscreen(currentFull)
    if (currentFull) {
      hasEnteredFullscreenRef.current = true
    }

    // 1. Tab visibility change
    const handleVisibilityChange = () => {
      if (document.hidden) {
        logWarning('TAB_SWITCH', 'Thí sinh chuyển tab hoặc minimize cửa sổ')
      }
    }

    // 2. Window blur/focus
    const handleBlur = () => {
      logWarning('BROWSER_FOCUS_LOST', 'Cửa sổ trình duyệt mất focus')
    }

    // 3. Right-click prevention
    const handleContextMenu = (e: MouseEvent) => {
      e.preventDefault()
      // Chỉ chặn chuột phải, không ghi nhận cảnh báo
    }

    // 4. Copy/Paste prevention
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.ctrlKey || e.metaKey) {
        if (e.key === 'c' || e.key === 'v' || e.key === 'x') {
          e.preventDefault()
          logWarning('COPY_PASTE', `Thí sinh sử dụng Ctrl+${e.key.toUpperCase()}`)
        }
      }
      if (e.key === 'F12') {
        e.preventDefault()
        logWarning('SUSPICIOUS_KEYBOARD', 'Thí sinh nhấn F12')
      }
    }

    // 5. Prevent text selection copy
    const handleCopy = (e: ClipboardEvent) => {
      e.preventDefault()
      logWarning('COPY_PASTE', 'Thí sinh cố gắng copy nội dung')
    }

    // 6. Fullscreen change listener
    const handleFullscreenChange = () => {
      const isFull = !!document.fullscreenElement
      setIsFullscreen(isFull)

      if (isFull) {
        hasEnteredFullscreenRef.current = true
        if (fullscreenTimeoutRef.current) {
          clearTimeout(fullscreenTimeoutRef.current)
          fullscreenTimeoutRef.current = null
        }
      } else {
        if (hasEnteredFullscreenRef.current) {
          logWarning('FULLSCREEN_EXIT', 'Thí sinh thoát chế độ toàn màn hình')
        }
      }
    }

    document.addEventListener('visibilitychange', handleVisibilityChange)
    window.addEventListener('blur', handleBlur)
    document.addEventListener('contextmenu', handleContextMenu)
    document.addEventListener('keydown', handleKeyDown)
    document.addEventListener('copy', handleCopy)
    document.addEventListener('fullscreenchange', handleFullscreenChange)

    return () => {
      document.removeEventListener('visibilitychange', handleVisibilityChange)
      window.removeEventListener('blur', handleBlur)
      document.removeEventListener('contextmenu', handleContextMenu)
      document.removeEventListener('keydown', handleKeyDown)
      document.removeEventListener('copy', handleCopy)
      document.removeEventListener('fullscreenchange', handleFullscreenChange)

      if (fullscreenTimeoutRef.current) {
        clearTimeout(fullscreenTimeoutRef.current)
      }
    }
  }, [enabled, baithiId, logWarning])

  const remainingWarnings = Math.max(0, MAX_CHEATING_WARNINGS - warningCount)
  const isTerminated = warningCount > MAX_CHEATING_WARNINGS

  return {
    warningCount,
    lastWarningType,
    remainingWarnings,
    isTerminated,
    maxWarnings: MAX_CHEATING_WARNINGS,
    isFullscreen,
    requestFullscreen,
  }
}
