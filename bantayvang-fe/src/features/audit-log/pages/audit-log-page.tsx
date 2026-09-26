import { useEffect, useState, useCallback, useMemo } from 'react'
import { auditLogApi, type AuditLogEntry } from '../api'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { RefreshCw, Download, ChevronLeft, ChevronRight, Search, FilterX, Activity, AlertCircle, Users, FileText, Calendar, Clock, Globe, X } from 'lucide-react'
import { formatDate } from '@/lib/utils'
import { translateAction } from '../action-translator'

const STATUS_COLOR = (code?: number) => {
  if (!code) return 'text-gray-400 bg-gray-50 border-gray-200'
  if (code < 300) return 'text-emerald-700 bg-emerald-50 border-emerald-200'
  if (code < 400) return 'text-blue-700 bg-blue-50 border-blue-200'
  if (code < 500) return 'text-amber-700 bg-amber-50 border-amber-200'
  return 'text-red-700 bg-red-50 border-red-200'
}

const PAGE_SIZE = 50

export function AuditLogPage() {
  const [logs, setLogs] = useState<AuditLogEntry[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [actionFilter, setActionFilter] = useState('')
  const [userFilter, setUserFilter] = useState('')
  const [dateFrom, setDateFrom] = useState('')
  const [dateTo, setDateTo] = useState('')
  const [page, setPage] = useState(1)
  const [total, setTotal] = useState(0)
  const [totalPages, setTotalPages] = useState(1)
  const [selectedLog, setSelectedLog] = useState<AuditLogEntry | null>(null)
  // BUG FIX: "Có lỗi (4xx, 5xx)" used to call setActionFilter('ERROR'), which the backend matches
  // against ActionType (e.g. "POST /api/auth/login") via a Contains() - the literal string "ERROR"
  // never appears there, so the button always returned an empty/unfiltered result. There's no
  // server-side status-code filter to send instead, so filter client-side on the already-fetched
  // page (same statusCode field the stats widget below already uses).
  const [errorsOnly, setErrorsOnly] = useState(false)

  const loadLogs = useCallback(async (pg = page) => {
    setIsLoading(true)
    try {
      const res = await auditLogApi.getList({
        actionType: actionFilter || undefined,
        username: userFilter || undefined,
        from: dateFrom || undefined,
        to: dateTo || undefined,
        page: pg,
        pageSize: PAGE_SIZE,
      })
      if (res.data.success !== false) {
        setLogs(res.data.data || [])
        setTotal(res.data.total || 0)
        setTotalPages(res.data.totalPages || 1)
      }
    } catch {
      // fallback to recent
      try {
        const res2 = await auditLogApi.getRecent(500)
        if (res2.data.success && res2.data.data) {
          setLogs(res2.data.data)
          setTotal(res2.data.data.length)
          setTotalPages(Math.ceil(res2.data.data.length / PAGE_SIZE))
        }
      } catch { /* silent */ }
    } finally {
      setIsLoading(false)
    }
  }, [page, actionFilter, userFilter, dateFrom, dateTo])

  useEffect(() => { loadLogs(1) }, []) // eslint-disable-line

  const handleSearch = () => { setPage(1); loadLogs(1) }
  const handleClear = () => {
    setActionFilter(''); setUserFilter(''); setDateFrom(''); setDateTo(''); setErrorsOnly(false)
    setPage(1); setTimeout(() => loadLogs(1), 50)
  }

  // Quick filters
  const applyQuickFilter = (type: 'error' | 'login' | 'today') => {
    handleClear()
    setTimeout(() => {
      if (type === 'error') setErrorsOnly(true)
      if (type === 'login') setActionFilter('/api/auth/login')
      if (type === 'today') {
        const today = new Date().toISOString().split('T')[0]
        setDateFrom(today)
        setDateTo(today)
      }
      setPage(1); loadLogs(1)
    }, 100)
  }

  const handleExport = () => {
    const headers = ['ID', 'Thời gian', 'User', 'Method', 'Path', 'Status', 'Khoa/Phòng', 'IP', 'Chi tiết']
    const rows = logs.map(l => [
      l.id || '',
      l.timestamp ? new Date(l.timestamp).toLocaleString('vi-VN') : '',
      l.username || l.userId || '',
      l.method || l.action || '',
      l.path || '',
      l.statusCode || '',
      l.department || '',
      l.ipAddress || '',
      l.description || '',
    ])
    const csv = [headers, ...rows]
      .map(r => r.map(v => `"${String(v).replace(/"/g, '""')}"`).join(','))
      .join('\n')
    const blob = new Blob(['\ufeff' + csv], { type: 'text/csv;charset=utf-8' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `audit_log_${new Date().toISOString().slice(0, 10)}.csv`
    a.click()
    URL.revokeObjectURL(url)
  }

  // Calculate quick stats from current page logs
  const stats = useMemo(() => {
    const errors = logs.filter(l => (l.statusCode || 0) >= 400).length
    const logins = logs.filter(l => l.path?.toLowerCase().includes('/auth/login')).length
    return { errors, logins, total: logs.length }
  }, [logs])

  const pagedLogs = errorsOnly ? logs.filter(l => (l.statusCode || 0) >= 400) : logs

  return (
    <div className="p-4 md:p-6 space-y-6 max-w-[1600px] mx-auto bg-gray-50/30 min-h-screen">
      {/* Header */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-extrabold text-gray-900 tracking-tight flex items-center gap-2">
            <Activity className="h-6 w-6 text-blue-600" />
            Giám sát Hoạt động (Audit Log)
          </h1>
          <p className="text-sm text-gray-500 mt-1">
            Lịch sử thao tác, truy cập và thay đổi dữ liệu hệ thống
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" size="sm" onClick={handleExport} className="bg-white hover:bg-gray-50">
            <Download className="h-4 w-4 mr-1.5 text-gray-500" /> Xuất CSV
          </Button>
          <Button size="sm" onClick={() => loadLogs(page)} disabled={isLoading} className="bg-blue-600 hover:bg-blue-700 text-white shadow-sm">
            <RefreshCw className={`h-4 w-4 mr-1.5 ${isLoading ? 'animate-spin' : ''}`} /> 
            Làm mới
          </Button>
        </div>
      </div>

      {/* Dashboard Widgets */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
        <div className="bg-white p-5 rounded-2xl border border-gray-100 shadow-sm flex items-center gap-4">
          <div className="p-3 bg-blue-50 text-blue-600 rounded-xl">
            <Activity className="h-6 w-6" />
          </div>
          <div>
            <p className="text-sm font-medium text-gray-500">Lượt tương tác (trang này)</p>
            <h3 className="text-2xl font-bold text-gray-900">{stats.total}</h3>
          </div>
        </div>
        <div className="bg-white p-5 rounded-2xl border border-gray-100 shadow-sm flex items-center gap-4">
          <div className="p-3 bg-red-50 text-red-600 rounded-xl">
            <AlertCircle className="h-6 w-6" />
          </div>
          <div>
            <p className="text-sm font-medium text-gray-500">Lỗi / Cảnh báo (trang này)</p>
            <h3 className="text-2xl font-bold text-gray-900">{stats.errors}</h3>
          </div>
        </div>
        <div className="bg-white p-5 rounded-2xl border border-gray-100 shadow-sm flex items-center gap-4">
          <div className="p-3 bg-emerald-50 text-emerald-600 rounded-xl">
            <Users className="h-6 w-6" />
          </div>
          <div>
            <p className="text-sm font-medium text-gray-500">Lượt đăng nhập (trang này)</p>
            <h3 className="text-2xl font-bold text-gray-900">{stats.logins}</h3>
          </div>
        </div>
      </div>

      {/* Smart Filters */}
      <div className="bg-white rounded-2xl border border-gray-100 shadow-sm p-5 space-y-4">
        <div className="flex flex-wrap gap-2 mb-2">
          <span className="text-xs font-semibold text-gray-400 uppercase tracking-wider mr-2 self-center">Lọc nhanh:</span>
          <Button variant="outline" size="sm" className="h-7 text-xs rounded-full border-gray-200" onClick={() => applyQuickFilter('today')}>
            Hôm nay
          </Button>
          <Button variant="outline" size="sm" className="h-7 text-xs rounded-full border-gray-200 text-red-600 hover:text-red-700 hover:bg-red-50" onClick={() => applyQuickFilter('error')}>
            Có lỗi (4xx, 5xx)
          </Button>
          <Button variant="outline" size="sm" className="h-7 text-xs rounded-full border-gray-200 text-emerald-600 hover:text-emerald-700 hover:bg-emerald-50" onClick={() => applyQuickFilter('login')}>
            Đăng nhập
          </Button>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-12 gap-3">
          <div className="md:col-span-4 relative">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400" />
            <Input
              placeholder="Tìm theo API Path hoặc mô tả..."
              value={actionFilter}
              onChange={e => setActionFilter(e.target.value)}
              onKeyDown={e => e.key === 'Enter' && handleSearch()}
              className="pl-9 bg-gray-50/50 border-gray-200"
            />
          </div>
          <div className="md:col-span-3">
            <Input
              placeholder="Tên người dùng..."
              value={userFilter}
              onChange={e => setUserFilter(e.target.value)}
              onKeyDown={e => e.key === 'Enter' && handleSearch()}
              className="bg-gray-50/50 border-gray-200"
            />
          </div>
          <div className="md:col-span-2">
            <Input type="date" value={dateFrom} onChange={e => setDateFrom(e.target.value)} className="bg-gray-50/50 border-gray-200 text-gray-600" />
          </div>
          <div className="md:col-span-2">
            <Input type="date" value={dateTo} onChange={e => setDateTo(e.target.value)} className="bg-gray-50/50 border-gray-200 text-gray-600" />
          </div>
          <div className="md:col-span-1 flex gap-2 justify-end">
            <Button variant="ghost" size="icon" onClick={handleClear} className="text-gray-400 hover:text-red-500">
              <FilterX className="h-4 w-4" />
            </Button>
            <Button size="icon" onClick={handleSearch} className="bg-blue-600 hover:bg-blue-700 text-white shadow-sm shrink-0">
              <Search className="h-4 w-4" />
            </Button>
          </div>
        </div>
      </div>

      {/* Modern List View */}
      <div className="bg-white rounded-2xl border border-gray-100 shadow-sm overflow-hidden flex flex-col">
        <div className="px-5 py-3 border-b border-gray-100 bg-gray-50/50 flex justify-between items-center">
          <h2 className="font-semibold text-gray-700 text-sm">Danh sách Sự kiện</h2>
          <span className="text-xs font-medium text-gray-500 bg-white px-2.5 py-1 rounded-full border border-gray-200 shadow-sm">
            Tổng cộng: {total.toLocaleString('vi-VN')} bản ghi
          </span>
        </div>

        {errorsOnly && (
          <div className="px-4 py-2 bg-amber-50 border-b border-amber-100 text-xs text-amber-700 flex items-center justify-between gap-2">
            {/* BUG FIX: errorsOnly only filters the 50 logs already fetched for the CURRENT page -
                pagination (page/totalPages, Next/Prev) still reflects the server's unfiltered
                total, so an admin could see "0 kết quả, Trang 1/40" and wrongly conclude there are
                no errors at all, when errors may simply be on other pages. Make the limitation
                explicit instead of implying this is a full search across all logs. */}
            <span>Chỉ lọc trong {logs.length} bản ghi của trang hiện tại (Trang {page}/{totalPages}) - không phải toàn bộ nhật ký.</span>
            <button type="button" className="underline shrink-0" onClick={() => setErrorsOnly(false)}>Bỏ lọc</button>
          </div>
        )}
        <div className="divide-y divide-gray-50 overflow-x-auto min-h-[400px]">
          {isLoading ? (
            <div className="flex flex-col items-center justify-center h-64 text-gray-400">
              <RefreshCw className="h-8 w-8 animate-spin mb-3 text-gray-300" />
              <p>Đang tải dữ liệu...</p>
            </div>
          ) : pagedLogs.length === 0 ? (
            <div className="flex flex-col items-center justify-center h-64 text-gray-400">
              <FileText className="h-10 w-10 mb-3 text-gray-200" />
              <p>{errorsOnly ? 'Không có lỗi nào trong trang hiện tại - thử xem trang khác.' : 'Không tìm thấy bản ghi nào phù hợp.'}</p>
            </div>
          ) : (
            pagedLogs.map((log) => {
              const trans = translateAction(log.method || log.action || '', log.path || log.actionType || '')
              const Icon = trans.icon
              const hasError = (log.statusCode || 0) >= 400

              return (
                <div 
                  key={log.id} 
                  className={`group flex items-center p-4 hover:bg-blue-50/30 transition-colors cursor-pointer ${selectedLog?.id === log.id ? 'bg-blue-50/50' : ''}`}
                  onClick={() => setSelectedLog(log)}
                >
                  {/* Icon */}
                  <div className={`p-2.5 rounded-xl mr-4 shrink-0 transition-transform group-hover:scale-105 ${trans.color} ${hasError ? 'ring-2 ring-red-200 ring-offset-1' : ''}`}>
                    <Icon className="h-5 w-5" />
                  </div>

                  {/* Main Info */}
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 mb-1">
                      <h4 className={`text-sm font-bold truncate ${hasError ? 'text-red-700' : 'text-gray-900'}`}>
                        {trans.name}
                      </h4>
                      {hasError && (
                        <span className="inline-flex items-center px-2 py-0.5 rounded text-[10px] font-bold bg-red-100 text-red-700 border border-red-200">
                          Thất bại
                        </span>
                      )}
                      <span className={`inline-flex items-center px-2 py-0.5 rounded text-[10px] font-bold border ${STATUS_COLOR(log.statusCode)}`}>
                        {log.statusCode || 'N/A'}
                      </span>
                    </div>
                    
                    <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-gray-500">
                      <div className="flex items-center gap-1.5">
                        <Users className="h-3.5 w-3.5 text-gray-400" />
                        <span className="font-medium text-gray-700">{log.username || 'Hệ thống'}</span>
                        {log.department && <span className="text-gray-400">({log.department})</span>}
                      </div>
                      <div className="flex items-center gap-1.5 hidden sm:flex">
                        <Globe className="h-3.5 w-3.5 text-gray-400" />
                        <span className="font-mono text-gray-500">{log.ipAddress || 'Unknown IP'}</span>
                      </div>
                      <div className="flex items-center gap-1.5 truncate max-w-xs md:max-w-md hidden lg:flex">
                        <span className="font-mono text-gray-400 bg-gray-100 px-1.5 rounded">{log.method || log.action}</span>
                        <span className="truncate">{log.path || log.actionType}</span>
                      </div>
                    </div>
                  </div>

                  {/* Timestamp */}
                  <div className="text-right ml-4 shrink-0">
                    <div className="text-xs font-semibold text-gray-700 mb-0.5">
                      {log.timestamp ? formatDate(log.timestamp).split(' ')[1] : '—'}
                    </div>
                    <div className="text-[11px] text-gray-400">
                      {log.timestamp ? formatDate(log.timestamp).split(' ')[0] : ''}
                    </div>
                  </div>
                </div>
              )
            })
          )}
        </div>

        {/* Pagination */}
        {totalPages > 1 && (
          <div className="flex items-center justify-between px-5 py-3 border-t border-gray-100 bg-white">
            <span className="text-sm font-medium text-gray-500">Trang <span className="text-gray-900">{page}</span> / {totalPages}</span>
            <div className="flex gap-1.5">
              <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => { setPage(p => p - 1); loadLogs(page - 1) }} className="rounded-lg">
                <ChevronLeft className="h-4 w-4" />
              </Button>
              <Button variant="outline" size="sm" disabled={page >= totalPages} onClick={() => { setPage(p => p + 1); loadLogs(page + 1) }} className="rounded-lg">
                <ChevronRight className="h-4 w-4" />
              </Button>
            </div>
          </div>
        )}
      </div>

      {/* Slide-over Detail Panel */}
      {selectedLog && (
        <>
          {/* Overlay */}
          <div 
            className="fixed inset-0 bg-black/20 backdrop-blur-sm z-40 transition-opacity" 
            onClick={() => setSelectedLog(null)} 
          />
          
          {/* Panel */}
          <div className="fixed inset-y-0 right-0 z-50 w-full max-w-md bg-white shadow-2xl border-l border-gray-100 transform transition-transform animate-in slide-in-from-right duration-300 flex flex-col">
            <div className="flex items-center justify-between p-5 border-b border-gray-100 bg-gray-50/50">
              <div className="flex items-center gap-3">
                <div className="p-2 bg-white rounded-lg shadow-sm border border-gray-100 text-blue-600">
                  <Activity className="h-5 w-5" />
                </div>
                <div>
                  <h3 className="font-bold text-gray-900">Chi tiết Sự kiện</h3>
                  <p className="text-xs text-gray-500 font-mono">ID: {selectedLog.id}</p>
                </div>
              </div>
              <Button variant="ghost" size="icon" onClick={() => setSelectedLog(null)} className="rounded-full hover:bg-gray-200">
                <X className="h-5 w-5 text-gray-500" />
                <span className="sr-only">Đóng</span>
              </Button>
            </div>

            <div className="flex-1 overflow-y-auto p-6 space-y-6">
              
              {/* Meta info */}
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-1">
                  <p className="text-xs font-semibold text-gray-500 uppercase tracking-wider flex items-center gap-1.5"><Calendar className="h-3.5 w-3.5"/> Ngày thực hiện</p>
                  <p className="text-sm font-medium text-gray-900">{selectedLog.timestamp ? formatDate(selectedLog.timestamp).split(' ')[0] : '—'}</p>
                </div>
                <div className="space-y-1">
                  <p className="text-xs font-semibold text-gray-500 uppercase tracking-wider flex items-center gap-1.5"><Clock className="h-3.5 w-3.5"/> Giờ thực hiện</p>
                  <p className="text-sm font-medium text-gray-900">{selectedLog.timestamp ? formatDate(selectedLog.timestamp).split(' ')[1] : '—'}</p>
                </div>
                <div className="space-y-1 col-span-2">
                  <p className="text-xs font-semibold text-gray-500 uppercase tracking-wider flex items-center gap-1.5"><Users className="h-3.5 w-3.5"/> Tài khoản</p>
                  <div className="bg-gray-50 p-3 rounded-lg border border-gray-100 flex items-center justify-between">
                    <div>
                      <p className="text-sm font-bold text-gray-900">{selectedLog.username || 'Không xác định'}</p>
                      <p className="text-xs text-gray-500 mt-0.5">{selectedLog.department || 'Không thuộc khoa phòng'}</p>
                    </div>
                    {selectedLog.userId && <span className="text-xs font-mono text-gray-400">ID: {selectedLog.userId}</span>}
                  </div>
                </div>
              </div>

              <div className="h-px bg-gray-100" />

              {/* Request info */}
              <div className="space-y-4">
                <h4 className="text-sm font-bold text-gray-900 flex items-center gap-2">
                  <Globe className="h-4 w-4 text-gray-400" /> Thông tin Request
                </h4>
                
                <div className="bg-slate-50 p-4 rounded-xl border border-slate-200/60 font-mono text-xs space-y-3">
                  <div className="flex flex-col gap-1">
                    <span className="text-slate-500 font-semibold">Trạng thái (Status):</span>
                    <span className={`inline-flex w-fit items-center px-2 py-0.5 rounded font-bold border ${STATUS_COLOR(selectedLog.statusCode)}`}>
                      {selectedLog.statusCode || 'N/A'}
                    </span>
                  </div>
                  
                  <div className="flex flex-col gap-1">
                    <span className="text-slate-500 font-semibold">Method & Endpoint:</span>
                    <div className="text-slate-800 break-all bg-white p-2 rounded border border-slate-200">
                      <span className="font-bold text-blue-600 mr-2">{selectedLog.method || selectedLog.action}</span>
                      {selectedLog.path || selectedLog.actionType}
                    </div>
                  </div>

                  <div className="flex flex-col gap-1">
                    <span className="text-slate-500 font-semibold">IP Address:</span>
                    <span className="text-slate-800">{selectedLog.ipAddress || '—'}</span>
                  </div>
                </div>
              </div>

              <div className="h-px bg-gray-100" />

              {/* Payload / Details */}
              <div className="space-y-3">
                <h4 className="text-sm font-bold text-gray-900 flex items-center gap-2">
                  <FileText className="h-4 w-4 text-gray-400" /> Chi tiết Ghi chú / Dữ liệu
                </h4>
                
                <div className="bg-gray-900 p-4 rounded-xl shadow-inner overflow-x-auto">
                  <pre className="text-xs text-green-400 font-mono whitespace-pre-wrap break-words leading-relaxed">
                    {selectedLog.description || 'Không có mô tả chi tiết.'}
                  </pre>
                </div>
              </div>

            </div>
            
            <div className="p-4 border-t border-gray-100 bg-gray-50 flex justify-end">
              <Button onClick={() => setSelectedLog(null)} className="rounded-lg">Đóng chi tiết</Button>
            </div>
          </div>
        </>
      )}

    </div>
  )
}
