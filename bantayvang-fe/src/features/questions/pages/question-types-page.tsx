import { useEffect, useState } from 'react'
import { questionsApi } from '../api'
import type { LoaicauhoiDto } from '../types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Plus, Search, Layers, Edit, Trash2, RefreshCw } from 'lucide-react'

export function QuestionTypesPage() {
  const [types, setTypes] = useState<LoaicauhoiDto[]>([])
  const [search, setSearch] = useState('')
  const [loading, setLoading] = useState(false)

  // Modal Form State
  const [showForm, setShowForm] = useState(false)
  const [editing, setEditing] = useState<LoaicauhoiDto | null>(null)
  const [form, setForm] = useState({ tenLoai: '', moTa: '' })

  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)

  useEffect(() => {
    load()
  }, [])

  // Auto clear alerts
  useEffect(() => {
    if (success || error) {
      const t = setTimeout(() => {
        setSuccess(null)
        setError(null)
      }, 4000)
      return () => clearTimeout(t)
    }
  }, [success, error])

  const load = async () => {
    setLoading(true)
    try {
      const res = await questionsApi.getQuestionTypes()
      if (res.data.success) {
        setTypes(res.data.data || [])
      } else {
        setError(res.data.message || 'Không thể tải danh sách loại câu hỏi')
      }
    } catch {
      setError('Lỗi kết nối máy chủ khi tải danh sách')
    } finally {
      setLoading(false)
    }
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!form.tenLoai.trim()) {
      setError('Vui lòng nhập tên loại câu hỏi')
      return
    }

    try {
      if (editing) {
        const res = await questionsApi.updateQuestionType(editing.id, {
          tenLoai: form.tenLoai.trim(),
          moTa: form.moTa.trim(),
        })
        if (res.data.success) {
          setSuccess('Cập nhật loại câu hỏi thành công')
          setShowForm(false)
          setEditing(null)
          load()
        } else {
          setError(res.data.message || 'Cập nhật thất bại')
        }
      } else {
        const res = await questionsApi.createQuestionType({
          tenLoai: form.tenLoai.trim(),
          moTa: form.moTa.trim(),
        })
        if (res.data.success) {
          setSuccess('Tạo loại câu hỏi thành công')
          setShowForm(false)
          setForm({ tenLoai: '', moTa: '' })
          load()
        } else {
          setError(res.data.message || 'Tạo mới thất bại')
        }
      }
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message
      setError(msg || 'Có lỗi xảy ra trong quá trình xử lý')
    }
  }

  const handleEdit = (t: LoaicauhoiDto) => {
    setEditing(t)
    setForm({ tenLoai: t.tenLoai || '', moTa: t.moTa || '' })
    setShowForm(true)
  }

  const handleDelete = async (t: LoaicauhoiDto) => {
    if (t.soCauHoi && t.soCauHoi > 0) {
      setError(`Không thể xóa loại câu hỏi "${t.tenLoai}" đang có ${t.soCauHoi} câu hỏi`)
      return
    }

    if (!window.confirm(`Bạn có chắc chắn muốn xóa loại câu hỏi "${t.tenLoai}"?`)) return

    try {
      const res = await questionsApi.deleteQuestionType(t.id)
      if (res.data.success) {
        setSuccess('Xóa loại câu hỏi thành công')
        load()
      } else {
        setError(res.data.message || 'Xóa loại câu hỏi thất bại')
      }
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message
      setError(msg || 'Không thể xóa loại câu hỏi này')
    }
  }

  // Local client side filtering for search
  const filteredTypes = types.filter(
    (t) =>
      t.tenLoai?.toLowerCase().includes(search.toLowerCase()) ||
      t.moTa?.toLowerCase().includes(search.toLowerCase())
  )

  return (
    <div className="p-4 sm:p-6 space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Quản lý Loại câu hỏi</h1>
          <p className="text-sm text-gray-500 mt-1">Quản lý các loại câu hỏi trong hệ thống</p>
        </div>
        <div className="flex gap-2 flex-wrap">
          <Button variant="outline" size="sm" onClick={load}>
            <RefreshCw className="h-4 w-4 mr-1" /> Làm mới
          </Button>
          <Button
            onClick={() => {
              setEditing(null)
              setForm({ tenLoai: '', moTa: '' })
              setShowForm(true)
            }}
          >
            <Plus className="h-4 w-4 mr-2" /> Thêm Loại câu hỏi
          </Button>
        </div>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg text-sm flex items-center gap-2 transition-all">
          ⚠ {error}
        </div>
      )}
      {success && (
        <div className="bg-green-50 border border-green-200 text-green-700 px-4 py-3 rounded-lg text-sm flex items-center gap-2 transition-all">
          ✓ {success}
        </div>
      )}

      {/* Search Bar */}
      <div className="flex gap-3">
        <div className="relative flex-1 max-w-sm">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400" />
          <Input
            placeholder="Tìm kiếm loại câu hỏi..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="pl-9"
          />
        </div>
      </div>

      {/* Table */}
      {loading ? (
        <div className="text-center py-12 text-gray-500">Đang tải...</div>
      ) : (
        <div className="bg-white rounded-xl border overflow-x-auto shadow-sm">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b">
              <tr>
                <th className="text-left px-6 py-3 font-medium text-gray-600 whitespace-nowrap">Tên loại câu hỏi</th>
                <th className="text-left px-6 py-3 font-medium text-gray-600 whitespace-nowrap">Mô tả</th>
                <th className="text-center px-6 py-3 font-medium text-gray-600 whitespace-nowrap">Số câu hỏi</th>
                <th className="text-right px-6 py-3 font-medium text-gray-600 whitespace-nowrap">Thao tác</th>
              </tr>
            </thead>
            <tbody className="divide-y">
              {filteredTypes.length === 0 ? (
                <tr>
                  <td colSpan={4} className="text-center py-12 text-gray-400">
                    Chưa có loại câu hỏi nào
                  </td>
                </tr>
              ) : (
                filteredTypes.map((t) => (
                  <tr key={t.id} className="hover:bg-gray-50/80 transition-colors">
                    <td className="px-6 py-4">
                      <div className="flex items-center gap-3">
                        <div className="p-2 bg-indigo-50 rounded-lg text-indigo-600 shrink-0">
                          <Layers className="h-4 w-4" />
                        </div>
                        <span className="font-semibold text-gray-800">{t.tenLoai}</span>
                      </div>
                    </td>
                    <td className="px-6 py-4 text-gray-500 max-w-md truncate" title={t.moTa || ''}>
                      {t.moTa || '—'}
                    </td>
                    <td className="px-6 py-4 text-center">
                      <span className="inline-flex items-center justify-center px-2.5 py-1 text-xs font-semibold rounded-full bg-blue-50 text-blue-700 border border-blue-100">
                        {t.soCauHoi ?? 0}
                      </span>
                    </td>
                    <td className="px-6 py-4">
                      <div className="flex items-center gap-1 justify-end">
                        <Button variant="ghost" size="icon" title="Sửa" onClick={() => handleEdit(t)}>
                          <Edit className="h-4 w-4 text-gray-500 hover:text-gray-700" />
                        </Button>
                        <Button variant="ghost" size="icon" title="Xóa" onClick={() => handleDelete(t)}>
                          <Trash2 className="h-4 w-4 text-red-500 hover:text-red-700" />
                        </Button>
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      )}

      {/* Modal Form Overlay */}
      {showForm && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 animate-in fade-in duration-200">
          <div className="bg-white rounded-xl shadow-xl w-full max-w-md p-6 space-y-4 transform scale-95 transition-all">
            <h2 className="text-lg font-bold text-gray-900">
              {editing ? 'Cập nhật Loại câu hỏi' : 'Thêm Loại câu hỏi mới'}
            </h2>
            <form onSubmit={handleSubmit} className="space-y-4">
              <div>
                <label className="text-sm font-medium text-gray-700">Tên loại câu hỏi *</label>
                <Input
                  value={form.tenLoai}
                  onChange={(e) => setForm((f) => ({ ...f, tenLoai: e.target.value }))}
                  placeholder="VD: Trắc nghiệm nhiều lựa chọn"
                  className="mt-1"
                  required
                />
              </div>
              <div>
                <label className="text-sm font-medium text-gray-700">Mô tả</label>
                <textarea
                  value={form.moTa}
                  onChange={(e) => setForm((f) => ({ ...f, moTa: e.target.value }))}
                  placeholder="Mô tả chi tiết về loại câu hỏi..."
                  rows={3}
                  className="mt-1 flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring resize-none"
                />
              </div>
              <div className="flex gap-3 justify-end pt-2 border-t">
                <Button type="button" variant="outline" onClick={() => setShowForm(false)}>
                  Hủy
                </Button>
                <Button type="submit">
                  {editing ? 'Cập nhật' : 'Tạo mới'}
                </Button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}
