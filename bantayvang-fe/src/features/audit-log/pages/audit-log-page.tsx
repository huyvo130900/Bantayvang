import React, { useEffect, useState, useCallback } from 'react'
import { auditLogApi, type AuditLogEntry } from '../api'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { RefreshCw, Download, ChevronLeft, ChevronRight, Search } from 'lucide-react'
import { formatDate } from '@/lib/utils'

const METHOD_COLORS: Record<string, string> = {
  POST:   'bg-green-100 text-green-700',
  PUT:    'bg-yellow-100 text-yellow-700',
  PATCH:  'bg-orange-100 text-orange-700',
  DELETE: 'bg-red-100 text-red-700',
  GET:    'bg-blue-100 text-blue-700',
}

const STATUS_COLOR = (code?: number) => {
  if (!code) return 'text-gray-400'
  if (code < 300) return 'text-green-600'
  if (code < 400) return 'text-yellow-600'
  if (code < 500) return 'text-orange-600'
  return 'text-red-600'
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
  const [expandedIdx, setExpandedIdx] = useState<number | null>(null)

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
    setActionFilter(''); setUserFilter(''); setDateFrom(''); setDateTo('')
    setPage(1); setTimeout(() => loadLogs(1), 50)
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

  const pagedLogs = logs

  return (
    <div className="p-6 space-y-5">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Audit Log</h1>
          <p className="text-sm text-gray-500">Lịch sử hoạt động hệ thống</p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" size="sm" onClick={handleExport}>
            <Download className="h-4 w-4 mr-1" /> Xuất CSV
          </Button>
          <Button variant="outline" size="sm" onClick={() => loadLogs(page)} disabled={isLoading}>
            <RefreshCw className={`h-4 w-4 mr-1 ${isLoading ? 'animate-spin' : ''}`} /> Làm mới
          </Button>
        </div>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-xl border p-4 space-y-3">
        <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
          <Input
            placeholder="Action type (POST, DELETE...)"
            value={actionFilter}
            onChange={e => setActionFilter(e.target.value)}
            onKeyDown={e => e.key === 'Enter' && handleSearch()}
          />
          <Input
            placeholder="Tên người dùng"
            value={userFilter}
            onChange={e => setUserFilter(e.target.value)}
            onKeyDown={e => e.key === 'Enter' && handleSearch()}
          />
          <Input type="date" value={dateFrom} onChange={e => setDateFrom(e.target.value)} />
          <Input type="date" value={dateTo} onChange={e => setDateTo(e.target.value)} />
        </div>
        <div className="flex gap-2 justify-end">
          <Button variant="outline" size="sm" onClick={handleClear}>Xóa bộ lọc</Button>
          <Button size="sm" onClick={handleSearch}>
            <Search className="h-4 w-4 mr-1" /> Tìm kiếm
          </Button>
        </div>
      </div>

      {/* Stats */}
      <p className="text-sm text-gray-500">
        {total.toLocaleString('vi-VN')} bản ghi &nbsp;·&nbsp; Trang {page}/{totalPages}
      </p>

      {/* Table */}
      <div className="bg-white rounded-xl border overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b">
              <tr>
                <th className="text-left px-3 py-2.5 font-medium text-gray-600 w-8 whitespace-nowrap">ID</th>
                <th className="text-left px-3 py-2.5 font-medium text-gray-600 whitespace-nowrap">Thời gian</th>
                <th className="text-left px-3 py-2.5 font-medium text-gray-600 whitespace-nowrap">User</th>
                <th className="text-left px-3 py-2.5 font-medium text-gray-600 w-16 whitespace-nowrap">Method</th>
                <th className="text-left px-3 py-2.5 font-medium text-gray-600 whitespace-nowrap">Path</th>
                <th className="text-center px-3 py-2.5 font-medium text-gray-600 w-14 whitespace-nowrap">Status</th>
                <th className="text-left px-3 py-2.5 font-medium text-gray-600 whitespace-nowrap">IP</th>
                <th className="text-left px-3 py-2.5 font-medium text-gray-600 whitespace-nowrap">Chi tiết / Ghi chú</th>
              </tr>
            </thead>
            <tbody className="divide-y">
              {isLoading ? (
                <tr><td colSpan={8} className="text-center py-12 text-gray-400">Đang tải...</td></tr>
              ) : pagedLogs.length === 0 ? (
                <tr><td colSpan={8} className="text-center py-12 text-gray-400">Không có dữ liệu</td></tr>
              ) : pagedLogs.map((log, idx) => (
                <React.Fragment key={log.id ?? idx}>
                  <tr
                    className="hover:bg-gray-50 cursor-pointer"
                    onClick={() => setExpandedIdx(expandedIdx === idx ? null : idx)}
                  >
                    <td className="px-3 py-2.5 text-gray-400 font-mono text-xs">{log.id}</td>
                    <td className="px-3 py-2.5 whitespace-nowrap text-gray-600 text-xs">
                      {log.timestamp ? formatDate(log.timestamp) : '—'}
                    </td>
                    <td className="px-3 py-2.5">
                      <div className="font-medium text-gray-800">{log.username || '—'}</div>
                      {log.userId && <div className="text-xs text-gray-400">ID: {log.userId}</div>}
                    </td>
                    <td className="px-3 py-2.5">
                      {(log.method || log.action) ? (
                        <span className={`inline-flex items-center px-1.5 py-0.5 rounded text-xs font-semibold ${
                          METHOD_COLORS[log.method?.toUpperCase() || log.action?.toUpperCase() || ''] || 'bg-gray-100 text-gray-600'
                        }`}>
                          {log.method || log.action}
                        </span>
                      ) : <span className="text-gray-300">—</span>}
                    </td>
                    <td className="px-3 py-2.5 font-mono text-xs text-gray-600 max-w-[220px] truncate">
                      {log.path || log.actionType || '—'}
                    </td>
                    <td className="px-3 py-2.5 text-center">
                      <span className={`font-semibold ${STATUS_COLOR(log.statusCode)}`}>
                        {log.statusCode || '—'}
                      </span>
                    </td>
                    <td className="px-3 py-2.5 font-mono text-xs text-gray-500">{log.ipAddress || '—'}</td>
                    <td className="px-3 py-2.5 text-gray-600 text-xs max-w-[200px] truncate">
                      {log.description || '—'}
                    </td>
                  </tr>
                  {expandedIdx === idx && (
                    <tr className="bg-blue-50">
                      <td colSpan={8} className="px-6 py-4">
                        <div className="grid grid-cols-2 md:grid-cols-3 gap-3 text-xs">
                          <div><span className="text-gray-500">Action Type:</span> <span className="font-medium">{log.actionType || log.action || '—'}</span></div>
                          <div><span className="text-gray-500">Khoa/Phòng:</span> <span className="font-medium">{log.department || '—'}</span></div>
                          <div><span className="text-gray-500">User ID:</span> <span className="font-medium">{log.userId || '—'}</span></div>
                          <div className="col-span-2 md:col-span-3">
                            <span className="text-gray-500">Chi tiết:</span>
                            <p className="font-medium mt-1 whitespace-pre-wrap break-all">{log.description || '—'}</p>
                          </div>
                        </div>
                      </td>
                    </tr>
                  )}
              </React.Fragment>
              ))}
            </tbody>
          </table>
        </div>

        {/* Pagination */}
        {totalPages > 1 && (
          <div className="flex items-center justify-between px-4 py-3 border-t bg-gray-50">
            <span className="text-sm text-gray-500">Trang {page} / {totalPages}</span>
            <div className="flex gap-1">
              <Button variant="outline" size="sm" disabled={page <= 1}
                onClick={() => { setPage(p => p - 1); loadLogs(page - 1) }}>
                <ChevronLeft className="h-4 w-4" />
              </Button>
              <Button variant="outline" size="sm" disabled={page >= totalPages}
                onClick={() => { setPage(p => p + 1); loadLogs(page + 1) }}>
                <ChevronRight className="h-4 w-4" />
              </Button>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
