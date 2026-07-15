import { Navigate, useLocation } from 'react-router-dom'
import { useAppSelector } from '@/app/hooks'
import { ROLES } from '@/lib/constants'

interface ProtectedRouteProps {
  children: React.ReactNode
  allowedRoles?: string[]
}

function getDefaultRedirect(role: string): string {
  switch (role) {
    case ROLES.STUDENT:    return '/exam-waiting'
    case ROLES.DEPT_MANAGER: return '/dept-manager/dashboard'
    case ROLES.ADMIN:      return '/admin/dashboard'
    default:               return '/unauthorized'
  }
}

export function ProtectedRoute({ children, allowedRoles }: ProtectedRouteProps) {
  const { isAuthenticated, user } = useAppSelector((state) => state.auth)
  const location = useLocation()

  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />
  }

  if (allowedRoles && user) {
    const userRole = user.role || user.roleName || ''
    if (!allowedRoles.includes(userRole)) {
      return <Navigate to={getDefaultRedirect(userRole)} replace />
    }
  }

  return <>{children}</>
}
