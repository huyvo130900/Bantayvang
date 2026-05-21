import { useEffect, useState } from 'react'
import { auditLogApi, type AuditLogEntry } from '../api'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { RefreshCw } from 'lucide-react'

export function AuditLogPage() {
  const [logs, setLogs] = useState<AuditLogEntry[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [actionFilter, setActionFilter] = useState('')

  useEffect(() => {
    loadLogs()
  }, [])

  const loadLogs = async () => {
    setIsLoading(true)
    try {
      const response = actionFilter
        ? await auditLogApi.search(actionFilter)
        : await auditLogApi.getRecent(200)
      if (response.data.success && response.data.data) {
        setLogs(response.data.data)
      }
    } catch {
      // handle error
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Audit Log</h1>
        <Button variant="outline" size="sm" onClick={loadLogs}>
          <RefreshCw className="h-4 w-4 mr-1" />
          Làm mới
        </Button>
      </div>

      <div className="flex gap-3 mb-4">
        <Input
          placeholder="Lọc theo action type..."
          value={actionFilter}
          onChange={(e) => setActionFilter(e.target.value)}
          onKeyDown={(e) => e.key === 'Enter' && loadLogs()}
          className="max-w-xs"
        />
        <Button variant="outline" onClick={loadLogs}>Lọc</Button>
      </div>

      {isLoading ? (
        <div className="text-center py-12 text-gray-500">Đang tải...</div>
      ) : logs.length === 0 ? (
        <div className="text-center py-12 text-gray-500">Không có log</div>
      ) : (
        <div className="overflow-x-auto rounded-lg border">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b">
              <tr>
                <th className="px-4 py-3 text-left font-medium text-gray-600">Thời gian</th>
                <th className="px-4 py-3 text-left font-medium text-gray-600">User</th>
                <th className="px-4 py-3 text-left font-medium text-gray-600">Action</th>
                <th className="px-4 py-3 text-left font-medium text-gray-600">Path</th>
                <th className="px-4 py-3 text-left font-medium text-gray-600">IP</th>
              </tr>
            </thead>
            <tbody className="divide-y">
              {logs.map((log, idx) => (
                <tr key={idx} className="hover:bg-gray-50">
                  <td className="px-4 py-2 text-xs text-gray-500">
                    {log.timestamp ? new Date(log.timestamp).toLocaleString('vi-VN') : '—'}
                  </td>
                  <td className="px-4 py-2 text-gray-600">{log.username || log.userId || '—'}</td>
                  <td className="px-4 py-2">
                    <span className="font-mono text-xs bg-gray-100 px-1.5 py-0.5 rounded">
                      {log.action || log.method || '—'}
                    </span>
                  </td>
                  <td className="px-4 py-2 text-xs text-gray-500 max-w-xs truncate">{log.path || log.details || '—'}</td>
                  <td className="px-4 py-2 text-xs text-gray-400">{log.ipAddress || '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
