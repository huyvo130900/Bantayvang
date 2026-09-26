import { useEffect } from 'react'
import { HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr'
import { useAppSelector } from '@/app/hooks'

export function useRealtimeNotifications() {
  const { user, isAuthenticated } = useAppSelector((state) => state.auth)

  useEffect(() => {
    if (!isAuthenticated || !user) return

    const baseUrl = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5293'
    const connection = new HubConnectionBuilder()
      // Backend now derives the user from the authenticated JWT (Hub is [Authorize]-guarded)
      // instead of trusting a client-supplied ?userId= query param, which used to let anyone
      // read anyone else's private notifications by just changing the URL.
      .withUrl(`${baseUrl}/hubs/notifications`, {
        accessTokenFactory: () => localStorage.getItem('accessToken') ?? '',
      })
      .withAutomaticReconnect()
      .build()

    async function startConnection() {
      try {
        if (connection.state === HubConnectionState.Disconnected) {
          await connection.start()
          console.log('Successfully connected to NotificationHub')
        }
      } catch (err) {
        console.error('Error connecting to NotificationHub:', err)
        setTimeout(startConnection, 5000)
      }
    }

    connection.on('ReceiveNotification', (notification: Record<string, string>) => {
      console.log('Received real-time notification:', notification)
      
      // 1. Show beautiful dynamic floating toast notification in DOM
      showGlobalNotificationToast(notification.title, notification.message)

      // 2. Dispatch custom global events for pages to reactively refresh their state
      window.dispatchEvent(new CustomEvent('notification:received', { detail: notification }))
    })

    // AI cham tu luan xong 1 cau -> phat custom event de trang cham hang loat cap nhat ngay,
    // khong can cho polling. Backend ban su kien nay qua Clients.All trong AiGradingWorker.
    connection.on('AiGradingDone', (payload: Record<string, unknown>) => {
      window.dispatchEvent(new CustomEvent('ai-grading:done', { detail: payload }))
    })

    startConnection()

    return () => {
      connection.stop()
    }
  }, [isAuthenticated, user])
}

function showGlobalNotificationToast(title: string, message: string) {
  // Create toast container if not exists
  let container = document.getElementById('global-notification-container')
  if (!container) {
    container = document.createElement('div')
    container.id = 'global-notification-container'
    container.className = 'fixed bottom-4 right-4 z-[9999] flex flex-col gap-2 pointer-events-none'
    document.body.appendChild(container)
  }

  // Create toast element
  const toast = document.createElement('div')
  toast.className = 'bg-white border-2 border-blue-500 rounded-xl shadow-2xl p-4 w-80 pointer-events-auto transform translate-y-2 opacity-0 transition-all duration-300 flex flex-col gap-1 cursor-pointer'
  
  toast.innerHTML = `
    <div class="flex items-center justify-between">
      <span class="text-sm font-bold text-blue-600 flex items-center gap-1.5">
        🔔 Thông báo mới
      </span>
      <button class="close-btn text-gray-400 hover:text-gray-600 text-xs font-semibold p-1">✕</button>
    </div>
    <div class="text-sm font-semibold text-gray-900 mt-1">${title}</div>
    <div class="text-xs text-gray-600 line-clamp-3">${message}</div>
  `

  // Close button functionality
  const closeBtn = toast.querySelector('.close-btn')
  closeBtn?.addEventListener('click', (e) => {
    e.stopPropagation()
    toast.classList.remove('opacity-100', 'translate-y-0')
    toast.classList.add('opacity-0', 'translate-y-2')
    setTimeout(() => toast.remove(), 300)
  })

  // Click toast functionality to navigate to notifications
  toast.addEventListener('click', () => {
    window.dispatchEvent(new CustomEvent('notification:click'))
    toast.classList.remove('opacity-100', 'translate-y-0')
    toast.classList.add('opacity-0', 'translate-y-2')
    setTimeout(() => toast.remove(), 300)
  })

  container.appendChild(toast)

  // Trigger animation
  requestAnimationFrame(() => {
    toast.classList.remove('opacity-0', 'translate-y-2')
    toast.classList.add('opacity-100', 'translate-y-0')
  })

  // Auto remove after 6 seconds
  setTimeout(() => {
    if (toast.parentElement) {
      toast.classList.remove('opacity-100', 'translate-y-0')
      toast.classList.add('opacity-0', 'translate-y-2')
      setTimeout(() => toast.remove(), 300)
    }
  }, 6000)
}
