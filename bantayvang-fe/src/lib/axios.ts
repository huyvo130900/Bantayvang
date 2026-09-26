import axios from 'axios'

const apiClient = axios.create({
  baseURL: '/api',
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 30000,
})

// Request interceptor: attach access token
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('accessToken')
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  (error) => Promise.reject(error)
)

// Response interceptor: handle 401 + refresh token
let isRefreshing = false
let failedQueue: Array<{
  resolve: (value: any) => void
  reject: (reason?: any) => void
}> = []

const processQueue = (error: any, token: string | null = null) => {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error)
    } else {
      prom.resolve(token)
    }
  })
  failedQueue = []
}

// BUG FIX: `isRefreshing` above only prevents concurrent refresh calls WITHIN a single browser
// tab - each tab runs its own copy of this module. Two tabs logged into the same account share
// the same access/refresh token pair via localStorage, so both tokens expire at the exact same
// moment. If both tabs happen to make a request right around that expiry (very plausible over a
// real work session with background polling), both independently see a 401 and both call
// /api/Auth/refresh with the SAME refresh token at nearly the same time. The backend's reuse-
// detection (correct and intentional - it exists to catch a stolen refresh token being replayed)
// cannot tell this apart from an actual attacker: it sees the token used twice and revokes every
// refresh token for that user, including the brand new one the first tab just received - forcing
// BOTH tabs to log out, even though nothing malicious happened. Coordinate across tabs via
// localStorage so only one tab ever calls the refresh endpoint for a given token.
const REFRESH_LOCK_KEY = 'token_refresh_lock'
const REFRESH_LOCK_TTL_MS = 10000 // safety timeout in case the refreshing tab crashed/closed mid-refresh

function acquireCrossTabRefreshLock(): boolean {
  const existing = localStorage.getItem(REFRESH_LOCK_KEY)
  if (existing && Date.now() - Number(existing) < REFRESH_LOCK_TTL_MS) {
    return false
  }
  localStorage.setItem(REFRESH_LOCK_KEY, Date.now().toString())
  return true
}

function releaseCrossTabRefreshLock() {
  localStorage.removeItem(REFRESH_LOCK_KEY)
}

// Poll localStorage for the access token another tab is refreshing to actually change (or for
// that tab's lock to clear without a new token, meaning its refresh failed).
async function waitForOtherTabRefresh(tokenAtFailureTime: string | null): Promise<string | null> {
  const start = Date.now()
  while (Date.now() - start < REFRESH_LOCK_TTL_MS) {
    await new Promise((r) => setTimeout(r, 150))
    const currentToken = localStorage.getItem('accessToken')
    if (currentToken && currentToken !== tokenAtFailureTime) {
      return currentToken
    }
    if (!localStorage.getItem(REFRESH_LOCK_KEY)) {
      return null
    }
  }
  return null
}

apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config

    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true

      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject })
        }).then((token) => {
          originalRequest.headers.Authorization = `Bearer ${token}`
          return apiClient(originalRequest)
        })
      }

      const tokenAtFailureTime = localStorage.getItem('accessToken')

      if (!acquireCrossTabRefreshLock()) {
        // Another browser tab is already refreshing (possibly for this exact same token) -
        // wait for it instead of racing it with our own refresh call.
        const newToken = await waitForOtherTabRefresh(tokenAtFailureTime)
        if (newToken) {
          originalRequest.headers.Authorization = `Bearer ${newToken}`
          return apiClient(originalRequest)
        }
        window.dispatchEvent(new Event('auth:logout'))
        return Promise.reject(error)
      }

      // We hold the lock now - but another tab may have already refreshed and released the
      // lock in the brief moment between our 401 and acquiring it. Check before calling the
      // endpoint ourselves.
      const currentToken = localStorage.getItem('accessToken')
      if (currentToken && currentToken !== tokenAtFailureTime) {
        releaseCrossTabRefreshLock()
        originalRequest.headers.Authorization = `Bearer ${currentToken}`
        return apiClient(originalRequest)
      }

      isRefreshing = true

      try {
        const refreshToken = localStorage.getItem('refreshToken')
        if (!refreshToken) {
          throw new Error('No refresh token')
        }

        const response = await axios.post('/api/auth/refresh', {
          refreshToken,
        })

        if (response.data.success) {
          const { accessToken, refreshToken: newRefreshToken } = response.data.data
          localStorage.setItem('accessToken', accessToken)
          localStorage.setItem('refreshToken', newRefreshToken)

          processQueue(null, accessToken)

          originalRequest.headers.Authorization = `Bearer ${accessToken}`
          return apiClient(originalRequest)
        } else {
          throw new Error('Refresh failed')
        }
      } catch (refreshError) {
        processQueue(refreshError, null)
        localStorage.removeItem('accessToken')
        localStorage.removeItem('refreshToken')
        window.dispatchEvent(new Event('auth:logout'))
        return Promise.reject(refreshError)
      } finally {
        isRefreshing = false
        releaseCrossTabRefreshLock()
      }
    }

    return Promise.reject(error)
  }
)

export default apiClient
