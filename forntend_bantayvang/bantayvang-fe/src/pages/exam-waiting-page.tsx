import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

export function ExamWaitingPage() {
  return (
    <div className="max-w-2xl mx-auto mt-12">
      <Card>
        <CardHeader className="text-center">
          <CardTitle>Phòng chờ thi</CardTitle>
        </CardHeader>
        <CardContent className="text-center">
          <p className="text-gray-600 mb-4">
            Bạn chưa có đề thi nào được phân công hoặc chưa đến giờ thi.
          </p>
          <p className="text-sm text-gray-500">
            Vui lòng chờ thông báo từ giám thị.
          </p>
        </CardContent>
      </Card>
    </div>
  )
}
