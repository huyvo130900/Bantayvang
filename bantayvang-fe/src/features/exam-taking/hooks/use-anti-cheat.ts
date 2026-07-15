import { useEffect, useRef, useState, useCallback } from 'react'
import { examTakingApi } from '../api'
import { MAX_CHEATING_WARNINGS } from '@/lib/constants'

interface UseAntiCheatOptions {
  examSubmissionId: number
  enabled: boolean
  onForceSubmit?: (reason: string) => void
}

export function useAntiCheat({ examSubmissionId, enabled, onForceSubmit }: UseAntiCheatOptions) {
  const [warningCount, setWarningCount] = useState(0)
  const [lastWarningType, setLastWarningType] = useState<string | null>(null)
  const [isFullscreen, setIsFullscreen] = useState(false)
  const [blockedKeyMessage, setBlockedKeyMessage] = useState<string | null>(null)
  const blockedKeyTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null)
  const baithiIdRef = useRef(examSubmissionId)
  const warningCountRef = useRef(0)
  const onForceSubmitRef = useRef(onForceSubmit)
  const fullscreenTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null)
  const hasEnteredFullscreenRef = useRef(false)

  baithiIdRef.current = examSubmissionId
  onForceSubmitRef.current = onForceSubmit

  const logWarning = useCallback(async (type: string, description: string) => {
    const newCount = warningCountRef.current + 1
    warningCountRef.current = newCount
    setWarningCount(newCount)
    setLastWarningType(type)

    try {
      await examTakingApi.logWarning({
        examSubmissionId: baithiIdRef.current,
        warningType: type,
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
    if (!enabled || !examSubmissionId) return

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

    // Helper: hiện thông báo bị chặn nhưng không phạt
    const showBlockedNotice = (msg: string) => {
      if (blockedKeyTimeoutRef.current) clearTimeout(blockedKeyTimeoutRef.current)
      setBlockedKeyMessage(msg)
      blockedKeyTimeoutRef.current = setTimeout(() => setBlockedKeyMessage(null), 2500)
    }

    // 4. Keyboard anti-cheat handler
    const handleKeyDown = (e: KeyboardEvent) => {
      // === NHÓM 1: CHẶN + THÔNG BÁO NHẸ (không phạt, tránh bấm nhầm) ===
      const silentBlockedKeys: Record<string, string> = {
        'F1': 'F1 (Trợ giúp)',
        'F2': 'F2',
        'F3': 'F3 (Tìm kiếm)',
        'F4': 'F4',
        'F5': 'F5 (Tải lại trang)',
        'F6': 'F6',
        'F7': 'F7',
        'F8': 'F8',
        'F9': 'F9',
        'F10': 'F10',
        'F11': 'F11 (Toàn màn hình)',
      }
      if (e.key in silentBlockedKeys) {
        e.preventDefault()
        showBlockedNotice(`🔒 Phím ${silentBlockedKeys[e.key]} bị vô hiệu hóa trong khi thi`)
        return
      }

      // Ctrl+R / Ctrl+Shift+R: tải lại trang
      if ((e.ctrlKey || e.metaKey) && (e.key === 'r' || e.key === 'R')) {
        e.preventDefault()
        showBlockedNotice('🔒 Phím tải lại trang bị vô hiệu hóa trong khi thi')
        return
      }

      // Alt+Left/Right: điều hướng trình duyệt (back/forward)
      if (e.altKey && (e.key === 'ArrowLeft' || e.key === 'ArrowRight')) {
        e.preventDefault()
        showBlockedNotice('🔒 Phím điều hướng trình duyệt bị vô hiệu hóa trong khi thi')
        return
      }

      // === NHÓM 2: CẢNH BÁO + PHẠT (hành vi có dấu hiệu gian lận) ===

      // Copy / Paste / Cut
      if (e.ctrlKey || e.metaKey) {
        if (e.key === 'c' || e.key === 'v' || e.key === 'x') {
          e.preventDefault()
          logWarning('COPY_PASTE', `Thí sinh sử dụng Ctrl+${e.key.toUpperCase()}`)
          return
        }
        // Ctrl+U: xem source code
        if (e.key === 'u' || e.key === 'U') {
          e.preventDefault()
          logWarning('SUSPICIOUS_KEYBOARD', 'Thí sinh nhấn Ctrl+U (View Source)')
          return
        }
      }

      // Ctrl+Shift+I hoặc Ctrl+Shift+J: mở DevTools
      if ((e.ctrlKey || e.metaKey) && e.shiftKey && (e.key === 'i' || e.key === 'I' || e.key === 'j' || e.key === 'J')) {
        e.preventDefault()
        logWarning('SUSPICIOUS_KEYBOARD', `Thí sinh nhấn Ctrl+Shift+${e.key.toUpperCase()} (DevTools)`)
        return
      }

      // F12: mở DevTools (không chặn được hoàn toàn nhưng vẫn ghi nhận)
      if (e.key === 'F12') {
        e.preventDefault()
        logWarning('SUSPICIOUS_KEYBOARD', 'Thí sinh nhấn F12 (Developer Tools)')
        return
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
  }, [enabled, examSubmissionId, logWarning])

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
    blockedKeyMessage,
  }
}
