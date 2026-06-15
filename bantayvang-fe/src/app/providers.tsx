import { Provider } from 'react-redux'
import { store } from './store'
import { AppRouter } from './router'
import { useEffect } from 'react'
import { useAppDispatch, useAppSelector } from './hooks'
import { fetchCurrentUser } from '@/features/auth/slice'

// FIX: Khi reload trang, accessToken còn trong localStorage nhưng user=null
// → gọi fetchCurrentUser để khôi phục user (kể cả role) từ server
function AuthInitializer({ children }: { children: React.ReactNode }) {
  const dispatch = useAppDispatch()
  const { accessToken, user } = useAppSelector((state) => state.auth)

  useEffect(() => {
    if (accessToken && !user) {
      dispatch(fetchCurrentUser())
    }
  }, []) // eslint-disable-line react-hooks/exhaustive-deps

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