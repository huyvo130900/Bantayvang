import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { loginSchema, type LoginFormData } from '../schemas'
import { login, clearError } from '../slice'
import { useAppDispatch, useAppSelector } from '@/app/hooks'
import { useNavigate } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { ROLES } from '@/lib/constants'

export function LoginForm() {
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const { isLoading, error } = useAppSelector((state) => state.auth)

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
    defaultValues: { username: '', password: '', rememberMe: false },
  })

  const onSubmit = async (data: LoginFormData) => {
    dispatch(clearError())
    const result = await dispatch(login(data))
    if (login.fulfilled.match(result)) {
      const user = result.payload.user
      if (user.role === ROLES.STUDENT) {
        navigate('/exam-waiting')
      } else {
        navigate('/admin/dashboard')
      }
    }
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      {error && (
        <div className="p-3 text-sm text-red-600 bg-red-50 border border-red-200 rounded-md">
          {error}
        </div>
      )}

      <div className="space-y-2">
        <label htmlFor="username" className="text-sm font-medium text-gray-700">
          Tên đăng nhập
        </label>
        <Input
          id="username"
          {...register('username')}
          placeholder="Nhập tên đăng nhập"
          autoComplete="username"
          aria-invalid={!!errors.username}
        />
        {errors.username && (
          <p className="text-sm text-red-500">{errors.username.message}</p>
        )}
      </div>

      <div className="space-y-2">
        <label htmlFor="password" className="text-sm font-medium text-gray-700">
          Mật khẩu
        </label>
        <Input
          id="password"
          {...register('password')}
          type="password"
          placeholder="Nhập mật khẩu"
          autoComplete="current-password"
          aria-invalid={!!errors.password}
        />
        {errors.password && (
          <p className="text-sm text-red-500">{errors.password.message}</p>
        )}
      </div>

      <div className="flex items-center space-x-2">
        <input
          {...register('rememberMe')}
          type="checkbox"
          id="rememberMe"
          className="h-4 w-4 rounded border-gray-300 text-primary focus:ring-primary"
        />
        <label htmlFor="rememberMe" className="text-sm text-gray-600">
          Ghi nhớ đăng nhập
        </label>
      </div>

      <Button type="submit" className="w-full" disabled={isLoading}>
        {isLoading ? 'Đang đăng nhập...' : 'Đăng nhập'}
      </Button>
    </form>
  )
}
