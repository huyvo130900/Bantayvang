import { useEffect, useRef, useState } from 'react'
import { examTakingApi } from '../api'

interface UseAntiCheatOptions {
  baithiId: number
  enabled: boolean
}

export function useAntiCheat({ baithiId, enabled }: UseAntiCheatOptions) {
  const [warningCount, setWarningCount] = useState(0)
  const baithiIdRef = useRef(baithiId)
  baithiIdRef.current = baithiId

  useEffect(() => {
    if (!enabled || !baithiId) return

    const logWarning = async (type: string, description: string) => {
      setWarningCount((prev) => prev + 1)
      try {
        await examTakingApi.logWarning({
          idBaiThi: baithiIdRef.current,
          loaiCanhBao: type,
          moTa: description,
        })
      } catch {
        // Silent fail - don't disrupt exam
      }
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
      logWarning('RIGHT_CLICK', 'Thí sinh click chuột phải')
    }

    // 4. Copy/Paste prevention
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.ctrlKey || e.metaKey) {
        if (e.key === 'c' || e.key === 'v' || e.key === 'x') {
          e.preventDefault()
          logWarning('COPY_PASTE', `Thí sinh sử dụng Ctrl+${e.key.toUpperCase()}`)
        }
      }
      // Prevent F12 / DevTools
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

    document.addEventListener('visibilitychange', handleVisibilityChange)
    window.addEventListener('blur', handleBlur)
    document.addEventListener('contextmenu', handleContextMenu)
    document.addEventListener('keydown', handleKeyDown)
    document.addEventListener('copy', handleCopy)

    return () => {
      document.removeEventListener('visibilitychange', handleVisibilityChange)
      window.removeEventListener('blur', handleBlur)
      document.removeEventListener('contextmenu', handleContextMenu)
      document.removeEventListener('keydown', handleKeyDown)
      document.removeEventListener('copy', handleCopy)
    }
  }, [enabled, baithiId])

  return { warningCount }
}
