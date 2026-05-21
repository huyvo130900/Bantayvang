import { useNavigate } from 'react-router-dom'
import { Button } from '@/components/ui/button'

export function UnauthorizedPage() {
  const navigate = useNavigate()

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50">
      <div className="text-center">
        <h1 className="text-4xl font-bold text-gray-900 mb-2">403</h1>
        <p className="text-gray-600 mb-6">Bạn không có quyền truy cập trang này.</p>
        <Button onClick={() => navigate(-1)}>Quay lại</Button>
      </div>
    </div>
  )
}
