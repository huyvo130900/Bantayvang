import { useEffect } from 'react'
import { useAppDispatch } from '@/app/hooks'
import { forceLogout } from '@/features/auth/slice'
import { useNavigate } from 'react-router-dom'

export function useAuthListener() {
  const dispatch = useAppDispatch()
  const navigate = useNavigate()

  useEffect(() => {
    const handleLogout = () => {
      dispatch(forceLogout())
      navigate('/login')
    }

    window.addEventListener('auth:logout', handleLogout)
    return () => window.removeEventListener('auth:logout', handleLogout)
  }, [dispatch, navigate])
}
