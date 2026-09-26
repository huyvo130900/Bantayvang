import { Shield, User, FileText, Settings, Download, Eye, LayoutDashboard, Key, LogIn, Activity, Edit, Plus, Trash2 } from 'lucide-react'

export interface ActionTranslation {
  name: string
  icon: React.ElementType
  color: string
}

/**
 * Hàm phân tích Path (và Method) để trả về tên hành động tiếng Việt thân thiện, 
 * kèm theo Icon và màu sắc tương ứng.
 */
export function translateAction(method: string, path: string): ActionTranslation {
  const methodUpper = (method || '').toUpperCase()
  const pathLower = (path || '').toLowerCase()

  // 1. Nhóm Đăng nhập / Xác thực
  if (pathLower.includes('/api/auth/login')) {
    return { name: 'Đăng nhập hệ thống', icon: LogIn, color: 'text-emerald-500 bg-emerald-50' }
  }
  if (pathLower.includes('/api/auth/change-password')) {
    return { name: 'Đổi mật khẩu', icon: Key, color: 'text-amber-500 bg-amber-50' }
  }

  // 2. Nhóm Câu hỏi (Questions)
  if (pathLower.includes('/api/question')) {
    if (pathLower.includes('import')) return { name: 'Nhập câu hỏi (Import)', icon: Download, color: 'text-blue-500 bg-blue-50' }
    if (pathLower.includes('preview')) return { name: 'Xem trước câu hỏi (Preview)', icon: Eye, color: 'text-indigo-500 bg-indigo-50' }
    if (methodUpper === 'POST') return { name: 'Thêm mới câu hỏi', icon: Plus, color: 'text-blue-500 bg-blue-50' }
    if (methodUpper === 'PUT' || methodUpper === 'PATCH') return { name: 'Cập nhật câu hỏi', icon: Edit, color: 'text-orange-500 bg-orange-50' }
    if (methodUpper === 'DELETE') return { name: 'Xóa câu hỏi', icon: Trash2, color: 'text-red-500 bg-red-50' }
    return { name: 'Truy cập ngân hàng câu hỏi', icon: FileText, color: 'text-gray-500 bg-gray-50' }
  }

  // 3. Nhóm Đề thi (Exam Papers)
  if (pathLower.includes('/api/exam')) {
    if (pathLower.includes('/generate')) return { name: 'Sinh đề thi tự động', icon: Settings, color: 'text-fuchsia-500 bg-fuchsia-50' }
    if (methodUpper === 'POST') return { name: 'Tạo đề thi mới', icon: FileText, color: 'text-fuchsia-500 bg-fuchsia-50' }
    return { name: 'Thao tác trên đề thi', icon: FileText, color: 'text-fuchsia-500 bg-fuchsia-50' }
  }

  // 4. Nhóm Kỳ thi (Exam Campaigns)
  if (pathLower.includes('/api/examcampaign')) {
    if (methodUpper === 'POST') return { name: 'Tạo đợt thi mới', icon: Shield, color: 'text-purple-500 bg-purple-50' }
    if (methodUpper === 'PUT') return { name: 'Cập nhật đợt thi', icon: Edit, color: 'text-purple-500 bg-purple-50' }
    if (methodUpper === 'DELETE') return { name: 'Xóa đợt thi', icon: Trash2, color: 'text-red-500 bg-red-50' }
    return { name: 'Truy cập danh sách đợt thi', icon: LayoutDashboard, color: 'text-purple-500 bg-purple-50' }
  }

  // 5. Nhóm User / Thí sinh
  if (pathLower.includes('/api/user')) {
    if (pathLower.includes('import')) return { name: 'Nhập danh sách người dùng', icon: Download, color: 'text-sky-500 bg-sky-50' }
    if (methodUpper === 'POST') return { name: 'Thêm mới người dùng', icon: User, color: 'text-sky-500 bg-sky-50' }
    if (methodUpper === 'PUT') return { name: 'Cập nhật thông tin người dùng', icon: Edit, color: 'text-sky-500 bg-sky-50' }
    if (methodUpper === 'DELETE') return { name: 'Xóa người dùng', icon: Trash2, color: 'text-red-500 bg-red-50' }
    return { name: 'Thao tác người dùng', icon: User, color: 'text-sky-500 bg-sky-50' }
  }

  // 6. Nhóm Chấm điểm (Grading)
  if (pathLower.includes('/api/grading')) {
    if (pathLower.includes('manual-grade')) return { name: 'Chấm thi thủ công (Tự luận)', icon: Edit, color: 'text-green-600 bg-green-50' }
    if (pathLower.includes('ai-grade')) return { name: 'Chấm thi AI', icon: Activity, color: 'text-indigo-600 bg-indigo-50' }
    return { name: 'Thao tác chấm điểm', icon: FileText, color: 'text-green-600 bg-green-50' }
  }

  // 7. Nhóm Department (Khoa / Phòng)
  if (pathLower.includes('/api/department')) {
    return { name: 'Cấu hình Khoa / Phòng', icon: Building2, color: 'text-teal-600 bg-teal-50' }
  }

  // Default fallback
  let defaultName = 'Truy cập hệ thống'
  let defaultIcon = Activity
  let defaultColor = 'text-gray-500 bg-gray-100'

  if (methodUpper === 'POST') { defaultName = 'Tạo mới dữ liệu'; defaultIcon = Plus; defaultColor = 'text-blue-500 bg-blue-50' }
  else if (methodUpper === 'PUT' || methodUpper === 'PATCH') { defaultName = 'Cập nhật dữ liệu'; defaultIcon = Edit; defaultColor = 'text-orange-500 bg-orange-50' }
  else if (methodUpper === 'DELETE') { defaultName = 'Xóa dữ liệu'; defaultIcon = Trash2; defaultColor = 'text-red-500 bg-red-50' }
  else if (methodUpper === 'GET') { defaultName = 'Truy vấn dữ liệu'; defaultIcon = Eye; defaultColor = 'text-slate-500 bg-slate-50' }

  return { name: defaultName, icon: defaultIcon, color: defaultColor }
}

// Bổ sung Building2 cho fallback
import { Building2 } from 'lucide-react'
