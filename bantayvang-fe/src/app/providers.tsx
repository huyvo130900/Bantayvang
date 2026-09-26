import { Provider } from 'react-redux'
import { store } from './store'
import { AppRouter } from './router'
import { useEffect, useState } from 'react'
import { useAppDispatch, useAppSelector } from './hooks'
import { fetchCurrentUser } from '@/features/auth/slice'

// FIX: Khi reload trang, accessToken còn trong localStorage nhưng user=null
// → gọi fetchCurrentUser để khôi phục user (kể cả role) từ server
function AuthInitializer({ children }: { children: React.ReactNode }) {
  const dispatch = useAppDispatch()
  const { accessToken, user } = useAppSelector((state) => state.auth)
  // BUG FIX: children (the whole router, including every ProtectedRoute) used to render
  // immediately while this fetch was still in flight. During that window isAuthenticated was
  // already true (derived from the token alone) but user was still null, and ProtectedRoute's
  // role check is skipped entirely when user is null (`allowedRoles && user`) - so a Student
  // pasting an /admin/... URL would briefly mount the admin page (and fire its data calls,
  // which the backend correctly rejects) before the role became known and it redirected away.
  // Gate rendering on this fetch actually settling first.
  const [checkingAuth, setCheckingAuth] = useState(!!accessToken && !user)

  useEffect(() => {
    if (accessToken && !user) {
      dispatch(fetchCurrentUser()).finally(() => setCheckingAuth(false))
    } else {
      setCheckingAuth(false)
    }
  }, []) // eslint-disable-line react-hooks/exhaustive-deps

  if (checkingAuth) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-white">
        <div className="h-8 w-8 border-2 border-gray-300 border-t-green-600 rounded-full animate-spin" />
      </div>
    )
  }

  return <>{children}</>
}

export function AppProviders() {
  return (
    <Provider store={store}>
      <AuthInitializer>
        <AppRouter />
      </AuthInitializer>
    </Provider>
  )
}