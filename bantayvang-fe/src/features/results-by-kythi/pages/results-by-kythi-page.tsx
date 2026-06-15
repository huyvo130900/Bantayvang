import { useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import apiClient from '@/lib/axios'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { departmentApi } from '@/features/departments/api'
import { useAppSelector } from '@/app/hooks'
import { ROLES } from '@/lib/constants'
import {
  CalendarDays, Users, Trophy,
  AlertTriangle, RefreshCw, Download, Eye, EyeOff, Search, Building2, CheckCircle2, XCircle
} from 'lucide-react'
import { getXepLoai } from '@/lib/constants'

interface KyThiItem {
  id: number; tenKyThi: string; maKyThi: string
  thoiGianBatDau?: string; thoiGianKetThuc?: string
  trangThai: string; soCaThi?: number; donViToChuc?: string
  soCauDungToiThieu?: number | null
}

// Match actual backend ExamResultDetailDto camelCase fields
interface ThiSinhResult {
  baiThiId: number
  userId?: number
  username?: string
  fullName?: string
  maNhanVien?: string
  khoaPhong?: string
  examId?: number
  idDeThi?: number
  maDeThi?: string
  tenDeThi?: string
  thoiGianBatDau?: string
  thoiGianNop?: string
  tongDiem?: number
  soCauDung?: number
  tongSoCau?: number
  trangThai?: string
  pass?: boolean
  soCauDungToiThieu?: number | null
  soCanhBao?: number
  soLanThi?: number
  soLanGianLan?: number
  soLanThiLai?: number
  congBoKetQua?: boolean
  danhGiaKhoa?: string
}

export function ResultsByKyThiPage() {
  const [searchParams] = useSearchParams()
  const currentUser = useAppSelector((state) => state.auth.user)

  const isDeptManager = currentUser?.role === ROLES.DEPT_MANAGER || currentUser?.tenVaiTro === 'DeptManager'
  const isAdmin = !isDeptManager
  const myKhoa = currentUser?.tenKhoaQuanLy || currentUser?.khoaPhong || null

  const [kyThiList, setKyThiList] = useState<KyThiItem[]>([])
  const [selectedKyThi, setSelectedKyThi] = useState<KyThiItem | null>(null)
  const [results, setResults] = useState<ThiSinhResult[]>([])
  const [loading, setLoading] = useState(false)
  const [search, setSearch] = useState('')
  const [filterXepLoai, setFilterXepLoai] = useState('')
  // Admin: lọc theo khoa
  const [khoaFilter, setKhoaFilter] = useState<string | null>(isDeptManager && myKhoa ? myKhoa : null)
  const [visibility, setVisibility] = useState<Record<number, boolean>>({})
  const [togglingId, setTogglingId] = useState<number | null>(null)
  const [msg, setMsg] = useState<string | null>(null)

  useEffect(() => {
    loadKyThiList()
  }, []) // eslint-disable-line

  const loadKyThiList = async () => {
    setLoading(true)
    try {
      const res = await apiClient.get('/KyThi')
      let list: KyThiItem[] = res.data?.data || []

      // DeptManager chỉ thấy kỳ thi của khoa mình
      if (isDeptManager && myKhoa) {
        list = list.filter((k) => k.donViToChuc === myKhoa)
      }

      setKyThiList(list)
      const preselect = searchParams.get('kyThiId')
      if (preselect) {
        const found = list.find((k) => k.id === parseInt(preselect))
        if (found) { setSelectedKyThi(found); loadResults(found.id) }
      }
    } catch { /* ignore */ }
    finally { setLoading(false) }
  }

  const loadResults = async (kyThiId: number) => {
    setLoading(true)
    try {
      const res = await apiClient.get(`/Grading/by-kythi/${kyThiId}`)
      const data: ThiSinhResult[] = res.data?.data || []
      setResults(data)
      // Build visibility map: idDeThi -> congBoKetQua
      const vis: Record<number, boolean> = {}
      data.forEach((r) => {
        if (r.idDeThi !== undefined) vis[r.idDeThi] = r.congBoKetQua ?? false
      })
      setVisibility(vis)
    } catch { setResults([]) }
    finally { setLoading(false) }
  }

  const handleSelectKyThi = (kt: KyThiItem) => {
    setSelectedKyThi(kt)
    setSearch('')
    setFilterXepLoai('')
    loadResults(kt.id)
  }

  const handleToggleVisibility = async (deThiId: number, current: boolean) => {
    setTogglingId(deThiId)
    try {
      await departmentApi.toggleExamVisibility(deThiId, { congBoKetQua: !current })
      setVisibility(v => ({ ...v, [deThiId]: !current }))
      setMsg(!current ? 'Đã bật công bố kết quả' : 'Đã tắt công bố kết quả')
      setTimeout(() => setMsg(null), 3000)
    } catch { setMsg('Không thể cập nhật') }
    finally { setTogglingId(null) }
  }

  // Lấy danh sách khoa từ kỳ thi (admin)
  const khoaList = isAdmin
    ? Array.from(new Set(kyThiList.map(k => k.donViToChuc).filter(Boolean) as string[])).sort()
    : []

  // Lọc kỳ thi theo khoa (admin)
  const filteredKyThiList = isAdmin && khoaFilter
    ? kyThiList.filter(k => k.donViToChuc === khoaFilter)
    : kyThiList

  const hasThreshold = selectedKyThi?.soCauDungToiThieu !== undefined && selectedKyThi?.soCauDungToiThieu !== null;

  const filtered = results.filter(r => {
    const name = r.fullName || r.username || ''
    const matchSearch = !search ||
      name.toLowerCase().includes(search.toLowerCase()) ||
      r.maNhanVien?.toLowerCase().includes(search.toLowerCase())
    const diem = r.tongDiem
    const matchFilter = !filterXepLoai || (
      hasThreshold
        ? (filterXepLoai === 'Đạt' ? (r.soCauDung ?? 0) >= selectedKyThi!.soCauDungToiThieu! : (r.soCauDung ?? 0) < selectedKyThi!.soCauDungToiThieu!)
        : (diem !== undefined && getXepLoai(diem) === filterXepLoai)
    )
    return matchSearch && matchFilter
  })

  const trangThaiColor = (t: string) => ({
    'DangDienRa': 'bg-green-100 text-green-700',
    'DaKetThuc': 'bg-gray-100 text-gray-500',
    'DangChuanBi': 'bg-yellow-100 text-yellow-700',
  }[t] || 'bg-gray-100 text-gray-500')

  const trangThaiLabel = (t: string) => ({
    'DangDienRa': 'Đang diễn ra', 'DaKetThuc': 'Đã kết thúc', 'DangChuanBi': 'Chuẩn bị'
  }[t] || t)

  const xepLoaiColor = (d?: number) => {
    if (d === undefined) return 'text-gray-400'
    if (d >= 9) return 'text-emerald-600 font-semibold'
    if (d >= 8) return 'text-blue-600 font-semibold'
    if (d >= 6.5) return 'text-indigo-600'
    if (d >= 5) return 'text-yellow-600'
    return 'text-red-600 font-semibold'
  }

  const validResults = results.filter(r => r.trangThai !== 'BiBHuyGianLan' && r.tongDiem !== undefined)
  const avgScore = validResults.length ? (validResults.reduce((s, r) => s + (r.tongDiem ?? 0), 0) / validResults.length) : 0
  const cheatingCount = results.filter(r => (r.soCanhBao ?? 0) > 0 || (r.soLanGianLan ?? 0) > 0).length
  const retakeCount = results.filter(r => (r.soLanThiLai ?? 0) > 0).length
  const passCount = hasThreshold
    ? results.filter(r => r.trangThai !== 'BiBHuyGianLan' && (r.soCauDung ?? 0) >= selectedKyThi!.soCauDungToiThieu!).length
    : 0;
  const failCount = hasThreshold
    ? results.filter(r => r.trangThai !== 'BiBHuyGianLan' && (r.soCauDung ?? 0) < selectedKyThi!.soCauDungToiThieu!).length
    : 0;
  const passRate = results.length ? (passCount / results.length) * 100 : 0;

  return (
    <div className="flex h-full">
      {/* Left: KyThi list */}
      <div className="w-72 border-r bg-white flex flex-col shrink-0">
        <div className="px-4 py-4 border-b">
          <h2 className="font-semibold text-gray-800 flex items-center gap-2">
            <CalendarDays className="h-4 w-4 text-blue-500" /> Kỳ thi
          </h2>
          {isDeptManager && myKhoa && (
            <p className="text-xs text-blue-500 mt-1">📋 {myKhoa}</p>
          )}
        </div>

        {/* Admin: filter khoa */}
        {isAdmin && khoaList.length > 0 && (
          <div className="px-3 py-2 border-b bg-gray-50">
            <div className="flex items-center gap-1 mb-1.5">
              <Building2 className="h-3.5 w-3.5 text-gray-400" />
              <span className="text-xs text-gray-500 font-medium">Lọc theo khoa</span>
            </div>
            <div className="flex flex-col gap-1">
              <button
                onClick={() => setKhoaFilter(null)}
                className={`text-left text-xs px-2 py-1 rounded-md transition-colors ${
                  khoaFilter === null
                    ? 'bg-blue-600 text-white'
                    : 'text-gray-600 hover:bg-gray-200'
                }`}
              >
                Tất cả ({kyThiList.length})
              </button>
              {khoaList.map(khoa => (
                <button
                  key={khoa}
                  onClick={() => { setKhoaFilter(khoa); setSelectedKyThi(null); setResults([]) }}
                  className={`text-left text-xs px-2 py-1 rounded-md transition-colors ${
                    khoaFilter === khoa
                      ? 'bg-blue-600 text-white'
                      : 'text-gray-600 hover:bg-gray-200'
                  }`}
                >
                  {khoa} ({kyThiList.filter(k => k.donViToChuc === khoa).length})
                </button>
              ))}
            </div>
          </div>
        )}

        <div className="flex-1 overflow-y-auto">
          {filteredKyThiList.length === 0 ? (
            <div className="text-center py-12 text-gray-400 text-sm">
              {khoaFilter ? `Không có kỳ thi của "${khoaFilter}"` : 'Chưa có kỳ thi'}
            </div>
          ) : filteredKyThiList.map(kt => (
            <button key={kt.id} onClick={() => handleSelectKyThi(kt)}
              className={`w-full text-left px-4 py-3 border-b hover:bg-gray-50 transition-colors ${selectedKyThi?.id === kt.id ? 'bg-blue-50 border-l-4 border-l-blue-500' : ''}`}>
              <p className="font-medium text-sm text-gray-800 truncate">{kt.tenKyThi}</p>
              {kt.donViToChuc && isAdmin && !khoaFilter && (
                <p className="text-xs text-blue-500 mt-0.5 truncate">{kt.donViToChuc}</p>
              )}
              <div className="flex items-center gap-2 mt-1">
                <span className={`text-[11px] px-1.5 py-0.5 rounded-full ${trangThaiColor(kt.trangThai)}`}>
                  {trangThaiLabel(kt.trangThai)}
                </span>
              </div>
            </button>
          ))}
        </div>
      </div>

      {/* Right: Results */}
      <div className="flex-1 overflow-auto">
        {!selectedKyThi ? (
          <div className="flex flex-col items-center justify-center h-full text-gray-400 gap-3">
            <CalendarDays className="h-12 w-12 opacity-40" />
            <p>Chọn một kỳ thi để xem kết quả</p>
          </div>
        ) : (
          <div className="p-6 space-y-5">
            <div className="flex items-start justify-between">
              <div>
                <div className="flex items-center gap-3">
                  <h1 className="text-xl font-bold text-gray-900">{selectedKyThi.tenKyThi}</h1>
                  {hasThreshold && (
                    <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold bg-emerald-50 text-emerald-700 border border-emerald-200">
                      Yêu cầu đạt: ≥ {selectedKyThi.soCauDungToiThieu} câu đúng
                    </span>
                  )}
                </div>
                {selectedKyThi.donViToChuc && (
                  <p className="text-xs text-blue-600 mt-0.5">{selectedKyThi.donViToChuc}</p>
                )}
                <p className="text-sm text-gray-500 mt-0.5">{results.length} lượt thi</p>
              </div>
              <Button variant="outline" size="sm">
                <Download className="h-4 w-4 mr-2" /> Xuất file Excel
              </Button>
            </div>

            {msg && <div className="bg-blue-50 text-blue-700 px-4 py-2 rounded-lg text-sm">{msg}</div>}

            {/* Stats */}
            <div className="grid grid-cols-4 gap-4">
              {(hasThreshold
                ? [
                    { label: 'Tổng lượt thi', value: results.length, icon: Users, color: 'text-blue-500', bg: 'bg-blue-50', sub: 'Toàn bộ kỳ thi' },
                    { label: 'Thí sinh Đạt', value: passCount, icon: CheckCircle2, color: 'text-emerald-500', bg: 'bg-emerald-50', sub: 'Số thí sinh đạt' },
                    { label: 'Thí sinh Không đạt', value: failCount, icon: XCircle, color: 'text-rose-500', bg: 'bg-rose-50', sub: 'Số thí sinh không đạt' },
                    { label: 'Tỷ lệ đạt', value: `${passRate.toFixed(1)}%`, icon: Trophy, color: 'text-indigo-500', bg: 'bg-indigo-50', sub: 'Tỷ lệ phần trăm đạt' },
                  ]
                : [
                    { label: 'Tổng lượt thi', value: results.length, icon: Users, color: 'text-blue-500', bg: 'bg-blue-50', sub: 'Toàn bộ kỳ thi' },
                    { label: 'Điểm TB', value: avgScore.toFixed(2), icon: Trophy, color: 'text-green-500', bg: 'bg-green-50', sub: 'Điểm số trung bình' },
                    { label: 'Có gian lận', value: cheatingCount, icon: AlertTriangle, color: 'text-red-500', bg: 'bg-red-50', sub: 'Số ca cảnh báo' },
                    { label: 'Thi lại', value: retakeCount, icon: RefreshCw, color: 'text-orange-500', bg: 'bg-orange-50', sub: 'Lượt thi lại' },
                  ]
              ).map(s => (
                <div key={s.label} className="bg-white rounded-xl border p-4 shadow-sm hover:shadow-md transition-shadow">
                  <div className={`inline-flex p-1.5 rounded-lg ${s.bg} mb-2`}>
                    <s.icon className={`h-4 w-4 ${s.color}`} />
                  </div>
                  <p className="text-xl font-bold text-gray-900">{s.value}</p>
                  <p className="text-xs text-gray-500">{s.label}</p>
                  <p className="text-[10px] text-gray-400 mt-0.5">{s.sub}</p>
                </div>
              ))}
            </div>

            {/* Filters */}
            <div className="flex gap-3">
              <div className="relative flex-1 max-w-sm">
                <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400" />
                <Input placeholder="Tìm thí sinh..." value={search} onChange={e => setSearch(e.target.value)} className="pl-9" />
              </div>
              {hasThreshold ? (
                <select value={filterXepLoai} onChange={e => setFilterXepLoai(e.target.value)}
                  className="border rounded-lg px-3 py-2 text-sm text-gray-600 bg-white">
                  <option value="">Tất cả kết quả</option>
                  <option value="Đạt">Đạt (≥ {selectedKyThi.soCauDungToiThieu} câu)</option>
                  <option value="Không đạt">Không đạt (&lt; {selectedKyThi.soCauDungToiThieu} câu)</option>
                </select>
              ) : (
                <select value={filterXepLoai} onChange={e => setFilterXepLoai(e.target.value)}
                  className="border rounded-lg px-3 py-2 text-sm text-gray-600 bg-white">
                  <option value="">Tất cả xếp loại</option>
                  <option value="Xuất sắc">Xuất sắc (≥9.0)</option>
                  <option value="Giỏi">Giỏi (≥8.0)</option>
                  <option value="Khá">Khá (≥6.5)</option>
                  <option value="Trung bình">Trung bình (≥5.0)</option>
                  <option value="Không đạt">Không đạt (&lt;5.0)</option>
                </select>
              )}
            </div>

            {loading ? (
              <div className="text-center py-12 text-gray-400">Đang tải...</div>
            ) : (
              <div className="bg-white rounded-xl border overflow-hidden">
                <table className="w-full text-sm">
                  <thead className="bg-gray-50 border-b">
                    <tr>
                      <th className="text-left px-4 py-3 font-medium text-gray-600 whitespace-nowrap">Thí sinh</th>
                      <th className="text-left px-4 py-3 font-medium text-gray-600 whitespace-nowrap">Đề thi</th>
                      <th className="text-center px-4 py-3 font-medium text-gray-600 whitespace-nowrap">Điểm</th>
                      <th className="text-center px-4 py-3 font-medium text-gray-600 whitespace-nowrap">
                        {hasThreshold ? 'Kết quả' : 'Xếp loại'}
                      </th>
                      <th className="text-center px-4 py-3 font-medium text-gray-600 whitespace-nowrap">⚠ Gian lận</th>
                      <th className="text-center px-4 py-3 font-medium text-gray-600 whitespace-nowrap">Lần thi</th>
                      <th className="text-left px-4 py-3 font-medium text-gray-600 whitespace-nowrap">Đánh giá khoa</th>
                      <th className="text-center px-4 py-3 font-medium text-gray-600 whitespace-nowrap">Công bố điểm</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y">
                    {filtered.length === 0 ? (
                      <tr><td colSpan={8} className="text-center py-12 text-gray-400">Chưa có kết quả</td></tr>
                    ) : filtered.map(r => (
                      <tr key={r.baiThiId} className="hover:bg-gray-50">
                        <td className="px-4 py-3">
                          <p className="font-medium text-gray-800">{r.fullName || r.username || '—'}</p>
                          {r.maNhanVien && <p className="text-xs text-gray-400">{r.maNhanVien}</p>}
                          {r.khoaPhong && <p className="text-xs text-blue-400">{r.khoaPhong}</p>}
                        </td>
                        <td className="px-4 py-3">
                          <p className="text-gray-700">{r.tenDeThi || r.maDeThi || '—'}</p>
                        </td>
                        <td className="px-4 py-3 text-center">
                          <span className={`text-lg ${xepLoaiColor(r.tongDiem)}`}>
                            {r.tongDiem !== undefined ? r.tongDiem.toFixed(1) : '—'}
                          </span>
                          {r.soCauDung !== undefined && r.tongSoCau && (
                            <p className="text-xs text-gray-400">{r.soCauDung}/{r.tongSoCau} câu</p>
                          )}
                        </td>
                        <td className="px-4 py-3 text-center font-medium">
                          {r.tongDiem !== undefined ? (
                            hasThreshold ? (
                              (r.soCauDung ?? 0) >= selectedKyThi.soCauDungToiThieu! ? (
                                <span className="text-xs px-2 py-0.5 rounded-full font-semibold bg-emerald-100 text-emerald-800 border border-emerald-200">
                                  ✓ Đạt
                                </span>
                              ) : (
                                <span className="text-xs px-2 py-0.5 rounded-full font-semibold bg-rose-100 text-rose-800 border border-rose-200">
                                  ✗ Không đạt
                                </span>
                              )
                            ) : (
                              <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${
                                r.tongDiem >= 9 ? 'bg-emerald-100 text-emerald-700' :
                                r.tongDiem >= 8 ? 'bg-blue-100 text-blue-700' :
                                r.tongDiem >= 6.5 ? 'bg-indigo-100 text-indigo-700' :
                                r.tongDiem >= 5 ? 'bg-yellow-100 text-yellow-700' :
                                'bg-red-100 text-red-700'
                              }`}>{getXepLoai(r.tongDiem)}</span>
                            )
                          ) : <span className="text-gray-400">—</span>}
                        </td>
                        <td className="px-4 py-3 text-center">
                          {(r.soCanhBao ?? 0) > 0 || (r.soLanGianLan ?? 0) > 0 ? (
                            <div>
                              <span className={r.soCanhBao ? "text-red-600 font-semibold" : "text-gray-500 font-medium"}>
                                {r.soCanhBao ?? 0} lần
                              </span>
                              {(r.soLanGianLan ?? 0) > (r.soCanhBao ?? 0) && (
                                <p className="text-xs text-gray-400">tổng: {r.soLanGianLan}</p>
                              )}
                            </div>
                          ) : <span className="text-gray-400">0 lần</span>}
                        </td>
                        <td className="px-4 py-3 text-center">
                          <div className="flex flex-col items-center">
                            <span className="font-semibold text-gray-700">{r.soLanThi ?? 1}</span>
                            {(r.soLanThi ?? 1) > 1 && (
                              <span className="text-xs text-blue-500">({(r.soLanThi ?? 1) - 1} thi lại)</span>
                            )}
                          </div>
                        </td>
                        <td className="px-4 py-3">
                          <span className={`text-xs ${r.danhGiaKhoa ? 'text-gray-700' : 'text-gray-300 italic'}`}>
                            {r.danhGiaKhoa || 'Chưa đánh giá'}
                          </span>
                        </td>
                        <td className="px-4 py-3 text-center">
                          <button
                            className={`inline-flex items-center gap-1 text-xs px-2 py-1 rounded-full border transition-colors ${
                              r.idDeThi && visibility[r.idDeThi]
                                ? 'bg-green-50 text-green-700 border-green-200'
                                : 'bg-gray-50 text-gray-500 border-gray-200'
                            }`}
                            disabled={togglingId === r.idDeThi}
                            onClick={() => r.idDeThi && handleToggleVisibility(r.idDeThi, visibility[r.idDeThi] ?? false)}
                          >
                            {r.idDeThi && visibility[r.idDeThi]
                              ? <><Eye className="h-3 w-3" /> Đã bật</>
                              : <><EyeOff className="h-3 w-3" /> Chưa bật</>
                            }
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  )
}
