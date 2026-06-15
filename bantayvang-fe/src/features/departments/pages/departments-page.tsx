import { useEffect, useState, useRef } from 'react'
import { departmentApi } from '../api'
import { usersApi } from '@/features/users/api'
import type { DepartmentDto } from '../types'
import type { UserDto } from '@/features/users/types'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Plus, Search, Building2, User, Edit, Trash2, UserPlus, UserX, RefreshCw, Upload, Download, FileSpreadsheet, X, CheckCircle, AlertTriangle, ChevronLeft, ChevronRight } from 'lucide-react'

export function DepartmentsPage() {
  const [departments, setDepartments] = useState<DepartmentDto[]>([])
  const [search, setSearch] = useState('')
  const [loading, setLoading] = useState(false)

  // Form tạo/sửa khoa
  const [showForm, setShowForm] = useState(false)
  const [editing, setEditing] = useState<DepartmentDto | null>(null)
  const [form, setForm] = useState({ maKhoa: '', tenKhoa: '', moTa: '', trangThai: true })

  // Dialog gán quản lý
  const [assignTarget, setAssignTarget] = useState<DepartmentDto | null>(null)
  const [deptManagers, setDeptManagers] = useState<UserDto[]>([])
  const [loadingManagers, setLoadingManagers] = useState(false)
  const [selectedManagerId, setSelectedManagerId] = useState<number | null>(null)
  const [assigning, setAssigning] = useState(false)

  // ✨ Import Excel
  const [importing, setImporting] = useState(false)
  const importRef = useRef<HTMLInputElement>(null)
  const [importResult, setImportResult] = useState<{
    success: number
    failed: number
    errors: string[]
  } | null>(null)

  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)

  // Phân trang
  const [page, setPage] = useState(1)
  const [pageSize] = useState(10)
  const [totalPages, setTotalPages] = useState(1)
  const [totalRecords, setTotalRecords] = useState(0)

  useEffect(() => { load(1) }, []) // eslint-disable-line

  // Auto clear messages
  useEffect(() => {
    if (success || error) {
      const t = setTimeout(() => { setSuccess(null); setError(null) }, 4000)
      return () => clearTimeout(t)
    }
  }, [success, error])

  const load = async (pageNum = page) => {
    setLoading(true)
    try {
      const res = await departmentApi.getAll({ search, page: pageNum, pageSize })
      setDepartments(res.data.data || [])
      if (res.data.pagination) {
        setPage(res.data.pagination.pageNumber)
        setTotalPages(res.data.pagination.totalPages)
        setTotalRecords(res.data.pagination.totalRecords)
      }
    } catch { setError('Không thể tải danh sách khoa') }
    finally { setLoading(false) }
  }

  const handleSubmit = async () => {
    if (!form.tenKhoa.trim()) { setError('Vui lòng nhập tên khoa'); return }
    try {
      if (editing) {
        await departmentApi.update(editing.id, { tenKhoa: form.tenKhoa, moTa: form.moTa, trangThai: form.trangThai })
        setSuccess('Cập nhật khoa thành công')
      } else {
        if (!form.maKhoa.trim()) { setError('Vui lòng nhập mã khoa'); return }
        await departmentApi.create(form)
        setSuccess('Tạo khoa thành công')
      }
      setShowForm(false)
      setEditing(null)
      load()
    } catch (err: unknown) {
      setError((err as { response?: { data?: { message?: string } } })?.response?.data?.message || 'Có lỗi xảy ra')
    }
  }

  const handleEdit = (d: DepartmentDto) => {
    setEditing(d)
    setForm({ maKhoa: d.maKhoa, tenKhoa: d.tenKhoa, moTa: d.moTa || '', trangThai: d.trangThai })
    setShowForm(true)
  }

  const handleDelete = async (id: number) => {
    if (!window.confirm('Vô hiệu hóa khoa này?')) return
    try {
      await departmentApi.delete(id)
      setSuccess('Đã vô hiệu hóa khoa')
      load()
    } catch { setError('Không thể vô hiệu hóa khoa') }
  }

  const openAssign = async (d: DepartmentDto) => {
    setAssignTarget(d)
    setSelectedManagerId(d.deptManagerId ?? null)
    setLoadingManagers(true)
    try {
      const res = await usersApi.list({ pageNumber: 1, pageSize: 100, idVaiTro: 5 })
      setDeptManagers(res.data.data || [])
    } catch { setError('Không thể tải danh sách quản lý') }
    finally { setLoadingManagers(false) }
  }

  const handleAssign = async () => {
    if (!assignTarget) return
    if (!selectedManagerId) { setError('Vui lòng chọn quản lý'); return }
    setAssigning(true)
    try {
      await departmentApi.assignManager(assignTarget.id, { deptManagerId: selectedManagerId })
      setSuccess(`Đã gán quản lý cho ${assignTarget.tenKhoa}`)
      setAssignTarget(null)
      load()
    } catch (err: unknown) {
      setError((err as { response?: { data?: { message?: string } } })?.response?.data?.message || 'Gán quản lý thất bại')
    } finally { setAssigning(false) }
  }

  const handleRemoveManager = async (d: DepartmentDto) => {
    if (!window.confirm(`Xóa quản lý khỏi ${d.tenKhoa}?`)) return
    try {
      await departmentApi.assignManager(d.id, { deptManagerId: 0 })
      setSuccess('Đã xóa quản lý')
      load()
    } catch {
      setError('Không thể xóa quản lý')
    }
  }

  // ✨ Download template
  const handleDownloadTemplate = async () => {
    try {
      const response = await departmentApi.downloadImportTemplate()
      const url = window.URL.createObjectURL(new Blob([response.data]))
      const link = document.createElement('a')
      link.href = url
      link.download = 'template_import_khoaphong.xlsx'
      link.click()
      window.URL.revokeObjectURL(url)
    } catch {
      setError('Không thể tải template')
    }
  }

  // ✨ Import từ Excel
  const handleImportFile = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file) return
    if (!window.confirm(`Import khoa/phòng từ file "${file.name}"?`)) return

    setImporting(true)
    setError(null)
    setSuccess(null)
    try {
      const res = await departmentApi.importDepartments(file)
      const data = res.data
      setImportResult({
        success: data.created || 0,
        failed: data.skipped || 0,
        errors: data.errors || []
      })
      load()
    } catch (err: unknown) {
      setError((err as { response?: { data?: { message?: string } } })?.response?.data?.message || 'Import thất bại')
    } finally {
      setImporting(false)
      // Reset input để có thể chọn lại cùng file
      if (importRef.current) importRef.current.value = ''
    }
  }

  return (
    <div className="p-6 space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Quản lý Khoa/Phòng ban</h1>
          <p className="text-sm text-gray-500 mt-1">Phân quyền quản lý theo khoa</p>
        </div>
        <div className="flex gap-2 flex-wrap justify-end">
          <Button variant="outline" size="sm" onClick={() => load(1)}>
            <RefreshCw className="h-4 w-4 mr-1" /> Làm mới
          </Button>
          {/* ✨ Template download */}
          <Button variant="outline" size="sm" onClick={handleDownloadTemplate} title="Tải file mẫu Excel nhập khoa">
            <Download className="h-4 w-4 mr-1 text-green-600" /> Tải file mẫu
          </Button>
          {/* ✨ Import Excel */}
          <Button
            variant="outline" size="sm"
            disabled={importing}
            onClick={() => importRef.current?.click()}
            className="border-blue-200 text-blue-700 hover:bg-blue-50"
          >
            <Upload className="h-4 w-4 mr-1" />
            {importing ? 'Đang nhập...' : 'Nhập từ Excel'}
          </Button>
          <input
            ref={importRef}
            type="file"
            accept=".xlsx,.xls,.xlsm,.xlsb,.csv,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet,application/vnd.ms-excel,application/vnd.ms-excel.sheet.macroEnabled.12,application/vnd.ms-excel.sheet.binary.macroEnabled.12,text/csv"
            className="hidden"
            onChange={handleImportFile}
          />
          <Button onClick={() => { setEditing(null); setForm({ maKhoa: '', tenKhoa: '', moTa: '', trangThai: true }); setShowForm(true) }}>
            <Plus className="h-4 w-4 mr-2" /> Thêm Khoa
          </Button>
        </div>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg text-sm flex items-center gap-2">
          ⚠ {error}
        </div>
      )}
      {success && (
        <div className="bg-green-50 border border-green-200 text-green-700 px-4 py-3 rounded-lg text-sm flex items-center gap-2">
          ✓ {success}
        </div>
      )}

      {/* Search */}
      <div className="flex gap-3">
        <div className="relative flex-1 max-w-sm">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400" />
          <Input placeholder="Tìm kiếm khoa..." value={search} onChange={e => setSearch(e.target.value)}
            onKeyDown={e => e.key === 'Enter' && load(1)} className="pl-9" />
        </div>
        <Button variant="outline" onClick={() => load(1)}>Tìm</Button>
      </div>

      {/* ===== Form Tạo/Sửa Khoa ===== */}
      {showForm && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
          <div className="bg-white rounded-xl shadow-xl w-full max-w-md p-6 space-y-4">
            <h2 className="text-lg font-semibold">{editing ? 'Cập nhật Khoa' : 'Thêm Khoa mới'}</h2>
            <div className="space-y-3">
              {!editing && (
                <div>
                  <label className="text-sm font-medium text-gray-700">Mã Khoa *</label>
                  <Input value={form.maKhoa} onChange={e => setForm(f => ({ ...f, maKhoa: e.target.value.toUpperCase() }))}
                    placeholder="VD: KHOA_NOI" className="mt-1 font-mono" />
                  <p className="text-xs text-gray-400 mt-1">Mã khoa không thể thay đổi sau khi tạo</p>
                </div>
              )}
              <div>
                <label className="text-sm font-medium text-gray-700">Tên Khoa *</label>
                <Input value={form.tenKhoa} onChange={e => setForm(f => ({ ...f, tenKhoa: e.target.value }))}
                  placeholder="VD: Khoa Nội" className="mt-1" />
              </div>
              <div>
                <label className="text-sm font-medium text-gray-700">Mô tả</label>
                <Input value={form.moTa} onChange={e => setForm(f => ({ ...f, moTa: e.target.value }))}
                  placeholder="Mô tả khoa..." className="mt-1" />
              </div>
              <label className="flex items-center gap-2 cursor-pointer">
                <input type="checkbox" checked={form.trangThai}
                  onChange={e => setForm(f => ({ ...f, trangThai: e.target.checked }))} className="rounded" />
                <span className="text-sm text-gray-700">Đang hoạt động</span>
              </label>
            </div>
            <div className="flex gap-3 justify-end pt-2">
              <Button variant="outline" onClick={() => setShowForm(false)}>Hủy</Button>
              <Button onClick={handleSubmit}>{editing ? 'Cập nhật' : 'Tạo Khoa'}</Button>
            </div>
          </div>
        </div>
      )}

      {/* ===== Dialog Gán Quản lý ===== */}
      {assignTarget && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50">
          <div className="bg-white rounded-xl shadow-xl w-full max-w-md p-6 space-y-4">
            <div>
              <h2 className="text-lg font-semibold">Gán Quản lý Khoa</h2>
              <p className="text-sm text-gray-500 mt-0.5">
                Khoa: <strong>{assignTarget.tenKhoa}</strong>
              </p>
            </div>

            {assignTarget.tenQuanLy && (
              <div className="flex items-center gap-2 p-3 bg-blue-50 rounded-lg text-sm text-blue-700 border border-blue-200">
                <User className="h-4 w-4 shrink-0" />
                Quản lý hiện tại: <strong>{assignTarget.tenQuanLy}</strong>
              </div>
            )}

            <div className="space-y-2">
              <label className="text-sm font-medium text-gray-700">Chọn tài khoản Quản lý Khoa *</label>
              {loadingManagers ? (
                <div className="text-sm text-gray-400 py-4 text-center">Đang tải danh sách...</div>
              ) : deptManagers.length === 0 ? (
                <div className="text-sm text-amber-600 bg-amber-50 border border-amber-200 rounded-lg p-3">
                  ⚠ Chưa có tài khoản nào có role <strong>Quản lý Khoa</strong>. Hãy tạo tài khoản với vai trò này trước.
                </div>
              ) : (
                <select
                  value={selectedManagerId ?? ''}
                  onChange={e => setSelectedManagerId(e.target.value ? Number(e.target.value) : null)}
                  className="w-full h-10 rounded-md border border-input bg-background px-3 text-sm"
                >
                  <option value="">-- Chọn quản lý --</option>
                  {deptManagers.map(u => (
                    <option key={u.id} value={u.id}>
                      {u.hoTen} ({u.tenDangNhap || u.email})
                      {u.khoaPhong ? ` — Hiện quản lý: ${u.khoaPhong}` : ' — Chưa phân khoa'}
                    </option>
                  ))}
                </select>
              )}
              <p className="text-xs text-gray-400">
                Chỉ hiển thị tài khoản có vai trò "Quản lý Khoa". Quản lý này sẽ chỉ thấy câu hỏi, đề thi và điểm của khoa mình.
              </p>
            </div>

            <div className="flex gap-3 justify-end pt-2 border-t">
              <Button variant="outline" onClick={() => setAssignTarget(null)}>Hủy</Button>
              <Button
                onClick={handleAssign}
                disabled={assigning || !selectedManagerId || deptManagers.length === 0}
              >
                {assigning ? 'Đang gán...' : 'Xác nhận gán'}
              </Button>
            </div>
          </div>
        </div>
      )}

      {/* ===== Table ===== */}
      {loading ? (
        <div className="text-center py-12 text-gray-500">Đang tải...</div>
      ) : (
        <div className="bg-white rounded-xl border overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b">
              <tr>
                <th className="text-left px-4 py-3 font-medium text-gray-600 whitespace-nowrap">Mã Khoa</th>
                <th className="text-left px-4 py-3 font-medium text-gray-600 whitespace-nowrap">Tên Khoa</th>
                <th className="text-left px-4 py-3 font-medium text-gray-600 whitespace-nowrap">Quản lý Khoa</th>
                <th className="text-left px-4 py-3 font-medium text-gray-600 whitespace-nowrap">Trạng thái</th>
                <th className="text-right px-4 py-3 font-medium text-gray-600 whitespace-nowrap">Thao tác</th>
              </tr>
            </thead>
            <tbody className="divide-y">
              {departments.length === 0 ? (
                <tr><td colSpan={5} className="text-center py-12 text-gray-400">Chưa có khoa nào</td></tr>
              ) : departments.map(d => (
                <tr key={d.id} className="hover:bg-gray-50">
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-2">
                      <Building2 className="h-4 w-4 text-blue-500 shrink-0" />
                      <span className="font-mono text-xs bg-gray-100 px-2 py-0.5 rounded">{d.maKhoa}</span>
                    </div>
                  </td>
                  <td className="px-4 py-3 font-medium">{d.tenKhoa}</td>
                  <td className="px-4 py-3">
                    {d.tenQuanLy ? (
                      <div className="flex items-center gap-2">
                        <div className="flex items-center gap-1.5 text-green-700">
                          <User className="h-3.5 w-3.5" />
                          <span className="font-medium">{d.tenQuanLy}</span>
                        </div>
                        <button
                          onClick={() => openAssign(d)}
                          className="text-xs text-blue-500 hover:text-blue-700 underline"
                          title="Thay đổi quản lý"
                        >
                          thay đổi
                        </button>
                      </div>
                    ) : (
                      <button
                        onClick={() => openAssign(d)}
                        className="flex items-center gap-1 text-xs text-amber-600 hover:text-amber-800 border border-amber-200 bg-amber-50 hover:bg-amber-100 px-2 py-1 rounded-lg transition-colors"
                      >
                        <UserPlus className="h-3.5 w-3.5" />
                        Chưa có quản lý — Gán ngay
                      </button>
                    )}
                  </td>
                  <td className="px-4 py-3">
                    <span className={`inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${
                      d.trangThai ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'
                    }`}>
                      {d.trangThai ? 'Hoạt động' : 'Vô hiệu'}
                    </span>
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-1 justify-end">
                      <Button variant="ghost" size="icon" title="Gán quản lý" onClick={() => openAssign(d)}>
                        <UserPlus className="h-4 w-4 text-blue-500" />
                      </Button>
                      {d.tenQuanLy && (
                        <Button variant="ghost" size="icon" title="Xóa quản lý" onClick={() => handleRemoveManager(d)}>
                          <UserX className="h-4 w-4 text-orange-500" />
                        </Button>
                      )}
                      <Button variant="ghost" size="icon" title="Sửa thông tin khoa" onClick={() => handleEdit(d)}>
                        <Edit className="h-4 w-4" />
                      </Button>
                      <Button variant="ghost" size="icon" title="Xóa/Vô hiệu khoa" onClick={() => handleDelete(d.id)}>
                        <Trash2 className="h-4 w-4 text-red-500" />
                      </Button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex items-center justify-between mt-4 bg-white p-4 rounded-xl border">
          <p className="text-sm text-gray-500">
            Trang {page} / {totalPages} · Tổng số {totalRecords} khoa/phòng
          </p>
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => load(page - 1)}
              disabled={page <= 1 || loading}
            >
              <ChevronLeft className="h-4 w-4 mr-1" />
              Trước
            </Button>
            <span className="text-sm font-medium w-8 text-center">{page}</span>
            <Button
              variant="outline"
              size="sm"
              onClick={() => load(page + 1)}
              disabled={page >= totalPages || loading}
            >
              Sau
              <ChevronRight className="h-4 w-4 ml-1" />
            </Button>
          </div>
        </div>
      )}

      {/* ===== Dialog Kết quả Import ===== */}
      {importResult && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-xl shadow-xl w-full max-w-md flex flex-col max-h-[90vh] overflow-hidden">
            {/* Header */}
            <div className="flex items-center justify-between p-4 border-b shrink-0 bg-gray-50/50">
              <div className="flex items-center gap-2">
                <FileSpreadsheet className="h-5 w-5 text-green-600" />
                <h2 className="text-lg font-semibold text-gray-800">Kết quả nhập Khoa/Phòng</h2>
              </div>
              <Button variant="ghost" size="icon" onClick={() => setImportResult(null)} className="rounded-full hover:bg-gray-100">
                <X className="h-4 w-4" />
              </Button>
            </div>

            {/* Content body */}
            <div className="p-5 space-y-4 overflow-y-auto flex-1">
              <div className={`rounded-xl p-4 border ${importResult.failed === 0 ? 'bg-green-50 border-green-200' : 'bg-yellow-50/75 border-yellow-200'}`}>
                <div className="flex items-center gap-2 mb-3">
                  {importResult.failed === 0 ? (
                    <CheckCircle className="h-5 w-5 text-green-600" />
                  ) : (
                    <AlertTriangle className="h-5 w-5 text-yellow-600" />
                  )}
                  <p className="text-sm font-semibold text-gray-800">Thông tin import</p>
                </div>
                <div className="grid grid-cols-2 gap-3 text-sm mb-3">
                  <div className="bg-white rounded-lg p-3 text-center border border-green-100 shadow-sm">
                    <p className="text-2xl font-bold text-green-600">{importResult.success}</p>
                    <p className="text-xs font-medium text-gray-500 mt-0.5">Thành công</p>
                  </div>
                  <div className="bg-white rounded-lg p-3 text-center border border-red-100 shadow-sm">
                    <p className="text-2xl font-bold text-red-500">{importResult.failed}</p>
                    <p className="text-xs font-medium text-gray-500 mt-0.5">Thất bại/Bỏ qua</p>
                  </div>
                </div>

                {importResult.failed > 0 && importResult.errors.length > 0 && (
                  <div className="mt-3 space-y-1.5">
                    <p className="text-xs font-semibold text-red-700">Chi tiết lỗi các dòng thất bại:</p>
                    <div className="bg-white border border-red-100 rounded-lg p-2.5 max-h-48 overflow-y-auto space-y-1 shadow-inner">
                      {importResult.errors.map((e, i) => (
                        <div key={i} className="text-xs flex items-start gap-1.5 text-red-600 py-0.5 border-b border-red-50 last:border-0 last:pb-0">
                          <span className="inline-block w-1.5 h-1.5 rounded-full bg-red-400 mt-1.5 shrink-0" />
                          <span className="leading-relaxed">{e}</span>
                        </div>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            </div>

            {/* Footer */}
            <div className="flex justify-end p-4 border-t shrink-0 bg-gray-50/50">
              <Button onClick={() => setImportResult(null)}>Đóng</Button>
            </div>
          </div>
        </div>
      )}

      {/* ✨ Import hint */}
      <div className="flex items-center gap-2 text-xs text-gray-400 bg-gray-50 rounded-lg px-4 py-2.5 border border-dashed">
        <FileSpreadsheet className="h-4 w-4 shrink-0" />
        <span>
          Dùng <strong>Tải file mẫu</strong> để tải file mẫu Excel, sau đó chỉnh sửa và dùng <strong>Nhập từ Excel</strong> để nhập hàng loạt khoa/phòng.
        </span>
      </div>
    </div>
  )
}
