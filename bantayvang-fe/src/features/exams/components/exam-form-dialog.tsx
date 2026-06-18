import { useState, useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { createExamSchema, type CreateExamFormData } from '../schemas'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { X, BookOpen, Search, ChevronDown, ChevronUp, Download, AlertTriangle, CheckCircle2 } from 'lucide-react'
import { questionsApi } from '@/features/questions/api'
import type { CauhoiDto } from '@/features/questions/types'
import { useAppSelector } from '@/app/hooks'
import { ROLES } from '@/lib/constants'
import { kyThiApi } from '@/features/ky-thi/api'
import type { KyThiDto } from '@/features/ky-thi/types'
import { examsApi } from '../api'

interface ExamFormDialogProps {
  open: boolean
  onClose: () => void
  onSubmit: (data: CreateExamFormData) => void
  isLoading: boolean
  lockedKhoa?: string
  kyThiList: { id: number; tenKyThiText: string }[]
  defaultKyThiId?: number
}

const KHOA_PHONG_OPTIONS = [
  { value: '', label: '-- Chọn khoa/phòng --' },
  { value: 'Khoa KSNK', label: 'Khoa KSNK' },
  { value: 'Khoa Nội', label: 'Khoa Nội' },
  { value: 'Khoa Ngoại', label: 'Khoa Ngoại' },
  { value: 'Khoa Sản', label: 'Khoa Sản' },
  { value: 'Khoa Nhi', label: 'Khoa Nhi' },
  { value: 'Khoa Cấp cứu', label: 'Khoa Cấp cứu' },
  { value: 'Khoa ICU', label: 'Khoa ICU' },
  { value: 'Khoa Dược', label: 'Khoa Dược' },
  { value: 'Khoa Xét nghiệm', label: 'Khoa Xét nghiệm' },
  { value: 'Khoa Chẩn đoán hình ảnh', label: 'Khoa Chẩn đoán hình ảnh' },
  { value: 'Phòng Hành chính', label: 'Phòng Hành chính' },
  { value: 'Lập trình C#', label: 'Lập trình C#' },
  { value: 'CNTT', label: 'CNTT' },
]

// Chế độ chọn câu hỏi
type QuestionMode = 'random' | 'manual' | 'import'

const generateDeThiCode = () => {
  const now = new Date()
  const year = now.getFullYear()
  const month = String(now.getMonth() + 1).padStart(2, '0')
  const day = String(now.getDate()).padStart(2, '0')
  const hours = String(now.getHours()).padStart(2, '0')
  const minutes = String(now.getMinutes()).padStart(2, '0')
  const seconds = String(now.getSeconds()).padStart(2, '0')
  const rand = Math.random().toString(36).substring(2, 6).toUpperCase()
  return `DT_${year}${month}${day}_${hours}${minutes}${seconds}_${rand}`
}

export function ExamFormDialog({ open, onClose, onSubmit, isLoading, lockedKhoa, kyThiList, defaultKyThiId }: ExamFormDialogProps) {
  const currentUser = useAppSelector((state) => state.auth.user)
  const isAdmin = currentUser?.role === ROLES.ADMIN || currentUser?.tenVaiTro === 'Admin'

  const [selectedKhoa, setSelectedKhoa] = useState(lockedKhoa || '')
  const [questionMode, setQuestionMode] = useState<QuestionMode>('manual')

  // Manual selection state
  const [questionPool, setQuestionPool] = useState<CauhoiDto[]>([])
  const [loadingPool, setLoadingPool] = useState(false)
  const [poolSearch, setPoolSearch] = useState('')
  const [poolKhoa, setPoolKhoa] = useState(lockedKhoa || '')
  const [selectedIds, setSelectedIds] = useState<number[]>([])
  const [selectedKyThiDetails, setSelectedKyThiDetails] = useState<KyThiDto | null>(null)
  const [poolPage, setPoolPage] = useState(1)
  const POOL_PAGE_SIZE = 20

  // Excel import state
  const [configMode, setConfigMode] = useState<'bank' | 'excel'>('bank')
  const [excelFile, setExcelFile] = useState<File | null>(null)
  const [isUploadingExcel, setIsUploadingExcel] = useState(false)
  const [excelErrors, setExcelErrors] = useState<string[]>([])
  const [importedCount, setImportedCount] = useState<number>(0)
  const [themVaoNganHang, setThemVaoNganHang] = useState(false)
  const [bankKhoa, setBankKhoa] = useState('')

  const form = useForm<CreateExamFormData>({
    resolver: zodResolver(createExamSchema) as any,
    defaultValues: {
      maDeThi: '',
      tenDeThi: '',
      thoiGianLamBai: 60,
      thoiGianBatDau: '',
      trangThai: 'Active',
      khoaPhong: lockedKhoa || '',
      soCauRandom: 30,
      danhSachIdCauHoi: [],
      kyThiId: defaultKyThiId || '' as any,
      soCauDungToiThieu: '' as any,
    },
  })

  useEffect(() => {
    if (open) {
      form.reset({
        maDeThi: generateDeThiCode(),
        tenDeThi: '',
        thoiGianLamBai: 60,
        thoiGianBatDau: '',
        trangThai: 'Active',
        khoaPhong: lockedKhoa || '',
        soCauRandom: 30,
        danhSachIdCauHoi: [],
        kyThiId: defaultKyThiId || '' as any,
        soCauDungToiThieu: '' as any,
      })
      setSelectedKhoa(lockedKhoa || '')
      setPoolKhoa(lockedKhoa || '')
      form.setValue('khoaPhong', lockedKhoa || '')
      setQuestionMode('manual')
      setSelectedIds([])
      setPoolSearch('')
      setConfigMode('bank')
      setExcelFile(null)
      setIsUploadingExcel(false)
      setExcelErrors([])
      setImportedCount(0)
      setThemVaoNganHang(false)
      setBankKhoa(lockedKhoa || 'Tất cả các khoa')
      setSelectedKyThiDetails(null)
    }
  }, [open, form, lockedKhoa, defaultKyThiId])

  const handleExcelFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (file) {
      setExcelFile(file)
      setExcelErrors([])
      setImportedCount(0)
    }
  }

  const handleDownloadTemplate = async () => {
    try {
      const response = await questionsApi.downloadTemplate(1, true) // 1 = Trắc nghiệm, isExamImport = true
      const url = window.URL.createObjectURL(new Blob([response.data]))
      const link = document.createElement('a')
      link.href = url
      link.download = `template_import_de_thi.xlsx`
      link.click()
      window.URL.revokeObjectURL(url)
    } catch {
      alert('Không thể tải template')
    }
  }

  useEffect(() => {
    if (lockedKhoa) {
      setSelectedKhoa(lockedKhoa)
      setPoolKhoa(lockedKhoa)
      form.setValue('khoaPhong', lockedKhoa)
      setBankKhoa(lockedKhoa)
    }
  }, [lockedKhoa, form])

  const selectedKyThiId = form.watch('kyThiId')
  useEffect(() => {
    if (open && selectedKyThiId) {
      kyThiApi.getById(selectedKyThiId).then((res) => {
        if (res.data.success && res.data.data) {
          const kyThi = res.data.data
          setSelectedKyThiDetails(kyThi)
          const tenKhoa = kyThi.tenKhoa || ''
          setSelectedKhoa(tenKhoa)
          setPoolKhoa(tenKhoa)
          form.setValue('khoaPhong', tenKhoa)
          setBankKhoa(tenKhoa || 'Tất cả các khoa')
        }
      }).catch(() => {})
    } else if (open && !selectedKyThiId) {
      setSelectedKyThiDetails(null)
      setSelectedKhoa(lockedKhoa || '')
      setPoolKhoa(lockedKhoa || '')
      form.setValue('khoaPhong', lockedKhoa || '')
      setBankKhoa(lockedKhoa || 'Tất cả các khoa')
    }
  }, [selectedKyThiId, open, lockedKhoa, form])

  useEffect(() => {
    form.clearErrors('danhSachIdCauHoi')
  }, [selectedIds, selectedKyThiId, configMode, form])

  // Load question pool khi chuyển sang manual và có khoa
  useEffect(() => {
    if (questionMode === 'manual' && (selectedKhoa || poolKhoa || !isAdmin)) {
      loadQuestionPool()
    }
  }, [questionMode, selectedKhoa, poolKhoa, poolPage, isAdmin]) // eslint-disable-line react-hooks/exhaustive-deps

  const loadQuestionPool = async () => {
    setLoadingPool(true)
    try {
      let khoaFilter = poolKhoa || selectedKhoa
      if (!isAdmin) {
        khoaFilter = currentUser?.tenKhoaQuanLy || currentUser?.khoaPhong || ''
      }
      const response = await questionsApi.list({
        pageNumber: poolPage,
        pageSize: POOL_PAGE_SIZE,
        khoaPhong: khoaFilter || undefined,
        searchKeyword: poolSearch || undefined,
      })
      if (response.data.success && response.data.data) {
        setQuestionPool(response.data.data.items || [])
      }
    } catch {
      // silent
    } finally {
      setLoadingPool(false)
    }
  }

  const toggleSelectQuestion = (id: number) => {
    setSelectedIds(prev =>
      prev.includes(id) ? prev.filter(i => i !== id) : [...prev, id]
    )
  }

  const handleSubmit = async (data: CreateExamFormData) => {
    let finalIds = selectedIds

    if (configMode === 'excel') {
      if (!excelFile) return
      
      setIsUploadingExcel(true)
      setExcelErrors([])
      setImportedCount(0)

      // 1. Pre-validate exam code duplication (before calling Excel import)
      try {
        const checkRes = await examsApi.getByCode(data.maDeThi)
        if (checkRes.data.success) {
          form.setError('maDeThi', {
            type: 'manual',
            message: 'Mã đề thi đã tồn tại trong hệ thống',
          })
          setIsUploadingExcel(false)
          return
        }
      } catch (err) {
        // If it throws an error/404, it means it doesn't exist yet, which is what we want.
      }

      // 2. Pre-validate soCauDungToiThieu against selectedKyThiDetails.tongSoCauHoi
      if (selectedKyThiDetails?.tongSoCauHoi) {
        const requiredCount = selectedKyThiDetails.tongSoCauHoi
        if (data.soCauDungToiThieu != null && data.soCauDungToiThieu > requiredCount) {
          form.setError('soCauDungToiThieu', {
            type: 'manual',
            message: `Số câu đúng tối thiểu (${data.soCauDungToiThieu}) không được lớn hơn tổng số câu hỏi yêu cầu (${requiredCount})`,
          })
          setIsUploadingExcel(false)
          return
        }
      }

      try {
        let targetKhoa = themVaoNganHang 
          ? (bankKhoa || 'Tất cả các khoa') 
          : 'Không thuộc ngân hàng'

        if (!isAdmin && themVaoNganHang) {
          targetKhoa = currentUser?.tenKhoaQuanLy || currentUser?.khoaPhong || ''
        }

        // 1 = Trắc nghiệm, isExamImport = true, expectedCount
        const response = await questionsApi.importExcel(excelFile, targetKhoa, 1, true, selectedKyThiDetails?.tongSoCauHoi ?? undefined)
        if (response.data.success && response.data.data) {
          const importedQuestions = response.data.data
          finalIds = importedQuestions.map((q: any) => q.id)
          setImportedCount(importedQuestions.length)
        } else {
          const apiErrors = (response.data as any).errors
          const errors = apiErrors && apiErrors.length > 0
            ? apiErrors
            : [response.data.message || 'Lỗi không xác định khi tải câu hỏi']
          setExcelErrors(errors)
          setIsUploadingExcel(false)
          return
        }
      } catch (err: any) {
        const apiErrors = err?.response?.data?.errors
        const errors = apiErrors && apiErrors.length > 0
          ? apiErrors
          : [err?.response?.data?.message || 'Lỗi hệ thống khi tải file excel']
        setExcelErrors(errors)
        setIsUploadingExcel(false)
        return
      } finally {
        setIsUploadingExcel(false)
      }
    }

    // Client-side validation: check if selected questions match the required count for KyThi
    if (selectedKyThiDetails?.tongSoCauHoi) {
      const requiredCount = selectedKyThiDetails.tongSoCauHoi
      if (finalIds.length < requiredCount) {
        const missing = requiredCount - finalIds.length
        const errorMsg = `Số lượng câu hỏi chưa đủ, còn thiếu ${missing} câu hỏi`
        if (configMode === 'excel') {
          setExcelErrors([errorMsg])
        } else {
          form.setError('danhSachIdCauHoi', {
            type: 'manual',
            message: errorMsg,
          })
        }
        return
      } else if (finalIds.length > requiredCount) {
        const excess = finalIds.length - requiredCount
        const errorMsg = `Số lượng câu hỏi vượt quá yêu cầu, thừa ${excess} câu hỏi (Yêu cầu: ${requiredCount} câu)`
        if (configMode === 'excel') {
          setExcelErrors([errorMsg])
        } else {
          form.setError('danhSachIdCauHoi', {
            type: 'manual',
            message: errorMsg,
          })
        }
        return
      }
    }

    // Client-side validation: soCauDungToiThieu cannot exceed selected questions
    if (data.soCauDungToiThieu != null && data.soCauDungToiThieu > finalIds.length) {
      form.setError('soCauDungToiThieu', {
        type: 'manual',
        message: `Số câu đúng tối thiểu (${data.soCauDungToiThieu}) không được lớn hơn tổng số câu hỏi đã chọn (${finalIds.length})`,
      })
      return
    }
    onSubmit({ ...data, danhSachIdCauHoi: finalIds, soCauRandom: finalIds.length })
  }

  const filteredPool = poolSearch
    ? questionPool.filter(q =>
        q.noiDung?.toLowerCase().includes(poolSearch.toLowerCase()) ||
        q.khoaPhong?.toLowerCase().includes(poolSearch.toLowerCase())
      )
    : questionPool

  if (!open) return null

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-3xl max-h-[92vh] overflow-y-auto">
        <div className="flex items-center justify-between p-4 border-b sticky top-0 bg-white z-10">
          <h2 className="text-lg font-semibold">Tạo đề thi mới</h2>
          <Button variant="ghost" size="icon" onClick={onClose}>
            <X className="h-4 w-4" />
          </Button>
        </div>

        <form onSubmit={form.handleSubmit(handleSubmit)} className="p-4 space-y-4">
          {/* Kỳ thi liên kết */}
          <div className="space-y-1">
            <label className="text-sm font-medium text-gray-700">Kỳ thi liên kết *</label>
            <select
              {...form.register('kyThiId', { 
                valueAsNumber: true,
                onChange: (e) => {
                  const val = Number(e.target.value)
                  if (val) {
                    localStorage.setItem('bantayvang_last_kythi_id', String(val))
                  }
                }
              })}
              className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm focus:outline-none focus:ring-2 focus:ring-primary/30"
            >
              <option value="">-- Chọn kỳ thi --</option>
              {kyThiList.map((ky) => (
                <option key={ky.id} value={ky.id}>
                  {ky.tenKyThiText}
                </option>
              ))}
            </select>
            {form.formState.errors.kyThiId && (
              <p className="text-xs text-red-500">{form.formState.errors.kyThiId.message}</p>
            )}
          </div>

          {/* Basic info */}
          <div className="space-y-1">
            <label className="text-sm font-medium text-gray-700">Mã đề thi *</label>
            <Input {...form.register('maDeThi')} placeholder="DETHI_001" readOnly className="bg-gray-100 cursor-not-allowed" />
            {form.formState.errors.maDeThi && (
              <p className="text-xs text-red-500">{form.formState.errors.maDeThi.message}</p>
            )}
          </div>

          {/* Điểm đạt section removed as requested */}

          {/* ===== CẤU HÌNH CÂU HỎI ===== */}
          <div className="border rounded-lg p-4 space-y-4 bg-purple-50 border-purple-200">
            <div className="flex items-center gap-2">
              <BookOpen className="h-4 w-4 text-purple-600" />
              <h3 className="text-sm font-semibold text-purple-800">Cấu hình câu hỏi</h3>
            </div>

            <div className="space-y-3">
              {/* Tab selector */}
              <div className="flex border-b border-purple-200 mb-2">
                <button
                  type="button"
                  onClick={() => { setConfigMode('bank'); setSelectedIds([]); }}
                  className={`flex-1 py-1.5 text-xs font-semibold border-b-2 transition-all ${
                    configMode === 'bank'
                      ? 'border-purple-600 text-purple-700'
                      : 'border-transparent text-gray-400 hover:text-gray-600'
                  }`}
                >
                  Chọn từ ngân hàng câu hỏi
                </button>
                <button
                  type="button"
                  onClick={() => { setConfigMode('excel'); setSelectedIds([]); setExcelFile(null); setExcelErrors([]); setImportedCount(0); }}
                  className={`flex-1 py-1.5 text-xs font-semibold border-b-2 transition-all ${
                    configMode === 'excel'
                      ? 'border-purple-600 text-purple-700'
                      : 'border-transparent text-gray-400 hover:text-gray-600'
                  }`}
                >
                  Nhập từ file Excel
                </button>
              </div>

              {configMode === 'bank' && (
                <>
                  <p className="text-xs text-purple-600">
                    Tích chọn từng câu hỏi cụ thể. Hệ thống chỉ dùng đúng các câu bạn chọn.
                  </p>

                  {/* Filter bar for pool */}
                  <div className="flex gap-2">
                    {/* Department filter selection or read-only text */}
                    {!isAdmin ? (
                      <input
                        type="text"
                        value={currentUser?.tenKhoaQuanLy || currentUser?.khoaPhong || ''}
                        readOnly
                        disabled
                        className="h-8 w-40 rounded border border-gray-200 bg-gray-50 text-gray-500 px-2 text-xs cursor-not-allowed"
                      />
                    ) : selectedKhoa ? (
                      <input
                        type="text"
                        value={selectedKhoa}
                        readOnly
                        disabled
                        className="h-8 w-40 rounded border border-gray-200 bg-gray-50 text-gray-500 px-2 text-xs cursor-not-allowed"
                      />
                    ) : (
                      isAdmin && (
                        <select
                          value={poolKhoa}
                          onChange={(e) => { setPoolKhoa(e.target.value); setPoolPage(1); setSelectedIds([]) }}
                          className="h-8 rounded border border-input bg-white px-2 text-xs"
                        >
                          {KHOA_PHONG_OPTIONS.map((o) => (
                            <option key={o.value} value={o.value}>{o.label || 'Tất cả khoa'}</option>
                          ))}
                        </select>
                      )
                    )}
                    <div className="relative flex-1">
                      <Search className="absolute left-2 top-1/2 -translate-y-1/2 h-3 w-3 text-gray-400" />
                      <input
                        type="text"
                        placeholder="Tìm câu hỏi..."
                        value={poolSearch}
                        onChange={(e) => setPoolSearch(e.target.value)}
                        className="w-full pl-7 pr-3 h-8 border rounded text-xs focus:outline-none focus:ring-1 focus:ring-purple-300"
                      />
                    </div>
                    <Button type="button" size="sm" variant="outline" onClick={loadQuestionPool} className="h-8 text-xs">
                      Tải
                    </Button>
                  </div>

                  {/* Selected count */}
                  <div className="flex items-center justify-between bg-white rounded-lg px-3 py-2 border border-purple-200">
                    <span className="text-xs text-gray-600">
                      Đã chọn: <strong className="text-purple-700">{selectedIds.length} câu</strong>
                    </span>
                    {selectedIds.length > 0 && (
                      <button type="button" onClick={() => setSelectedIds([])} className="text-xs text-red-400 hover:text-red-600">
                        Bỏ chọn tất cả
                      </button>
                    )}
                  </div>

                  {/* Question list */}
                  <div className="border rounded-lg bg-white max-h-64 overflow-y-auto divide-y">
                    {loadingPool ? (
                      <div className="py-8 text-center text-xs text-gray-400">Đang tải...</div>
                    ) : filteredPool.length === 0 ? (
                      <div className="py-8 text-center text-xs text-gray-400">
                        {poolKhoa || selectedKhoa ? 'Không có câu hỏi' : 'Chọn khoa để xem câu hỏi'}
                      </div>
                    ) : (
                      filteredPool.map((q) => {
                        const isSelected = selectedIds.includes(q.id)
                        return (
                          <label
                            key={q.id}
                            className={`flex items-start gap-3 px-3 py-2.5 cursor-pointer transition-colors ${
                              isSelected ? 'bg-purple-50' : 'hover:bg-gray-50'
                            }`}
                          >
                            <input
                              type="checkbox"
                              checked={isSelected}
                              onChange={() => toggleSelectQuestion(q.id)}
                              className="mt-0.5 shrink-0 accent-purple-600"
                            />
                            <div className="flex-1 min-w-0">
                              <p className="text-xs text-gray-800 line-clamp-2">{q.noiDung}</p>
                              <div className="flex gap-2 mt-0.5">
                                {q.khoaPhong && (
                                  <span className="text-xs text-purple-500">{q.khoaPhong}</span>
                                )}
                                {q.doKho && (
                                  <span className={`text-xs px-1 rounded ${
                                    q.doKho === 'De' ? 'text-green-500' :
                                    q.doKho === 'Kho' ? 'text-red-500' : 'text-yellow-600'
                                  }`}>{q.doKho}</span>
                                )}
                              </div>
                            </div>
                          </label>
                        )
                      })
                    )}
                  </div>

                  {/* Pagination */}
                  <div className="flex items-center justify-between text-xs text-gray-500">
                    <span>{filteredPool.length} câu hỏi</span>
                    <div className="flex gap-1">
                      <button
                        type="button"
                        disabled={poolPage === 1}
                        onClick={() => setPoolPage(p => p - 1)}
                        className="px-2 py-1 border rounded disabled:opacity-40 hover:bg-gray-50"
                      >
                        <ChevronDown className="h-3 w-3 rotate-90" />
                      </button>
                      <span className="px-2 py-1">Trang {poolPage}</span>
                      <button
                        type="button"
                        disabled={filteredPool.length < POOL_PAGE_SIZE}
                        onClick={() => setPoolPage(p => p + 1)}
                        className="px-2 py-1 border rounded disabled:opacity-40 hover:bg-gray-50"
                      >
                        <ChevronUp className="h-3 w-3 rotate-90" />
                      </button>
                    </div>
                  </div>

                  {selectedKyThiDetails?.tongSoCauHoi && (
                    <p className="text-xs text-purple-600">
                      Yêu cầu của kỳ thi: <strong>{selectedKyThiDetails.tongSoCauHoi} câu hỏi</strong>.
                    </p>
                  )}
                  {selectedIds.length === 0 && (
                    <p className="text-xs text-red-400">⚠ Chưa chọn câu hỏi nào</p>
                  )}
                  {form.formState.errors.danhSachIdCauHoi && (
                    <p className="text-xs text-red-500 font-semibold mt-1">
                      ⚠ {form.formState.errors.danhSachIdCauHoi.message as string}
                    </p>
                  )}
                </>
              )}

              {configMode === 'excel' && (
                <div className="space-y-4 pt-1">
                  <p className="text-xs text-purple-600">
                    Tải file Excel câu hỏi lên. Hệ thống sẽ import câu hỏi và gán vào đề thi này.
                  </p>

                  <div className="flex flex-col sm:flex-row gap-3 items-end">
                    <div className="flex-1 space-y-1 w-full">
                      <label className="text-[11px] font-bold text-purple-800 uppercase tracking-wider block">
                        Chọn tệp câu hỏi (Excel)
                      </label>
                      <input
                        type="file"
                        accept=".xlsx,.xls"
                        onChange={handleExcelFileChange}
                        className="text-xs border p-1 rounded w-full bg-white border-purple-200 file:mr-2 file:py-1 file:px-2 file:rounded file:border-0 file:text-[11px] file:font-semibold file:bg-purple-100 file:text-purple-700 hover:file:bg-purple-200"
                      />
                    </div>
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      onClick={handleDownloadTemplate}
                      className="h-8 border-purple-200 text-purple-700 bg-white hover:bg-purple-100/30 text-xs flex gap-1 justify-center items-center shrink-0 w-full sm:w-auto"
                    >
                      <Download className="h-3.5 w-3.5" />
                      Tải file mẫu Excel
                    </Button>
                  </div>

                  <div className="space-y-3 pt-2 border-t border-purple-100">
                    <label className="flex items-center gap-2 cursor-pointer text-xs font-semibold text-purple-800 select-none">
                      <input
                        type="checkbox"
                        checked={themVaoNganHang}
                        onChange={(e) => setThemVaoNganHang(e.target.checked)}
                        className="rounded border-purple-300 text-purple-600 focus:ring-purple-500 h-4 w-4 cursor-pointer"
                      />
                      <span>Thêm đề vào ngân hàng câu hỏi</span>
                    </label>

                    {themVaoNganHang && (
                      <div className="space-y-1 pl-6 max-w-xs animate-in fade-in slide-in-from-top-1 duration-200">
                        <label className="text-[11px] font-bold text-purple-800 uppercase tracking-wider block">
                          Chọn khoa/phòng
                        </label>
                        {!isAdmin ? (
                          <input
                            type="text"
                            value={currentUser?.tenKhoaQuanLy || currentUser?.khoaPhong || ''}
                            disabled
                            className="h-8 w-full rounded border border-gray-200 bg-gray-50 px-2 text-xs text-gray-500 cursor-not-allowed"
                          />
                        ) : (
                          <select
                            value={bankKhoa}
                            onChange={(e) => setBankKhoa(e.target.value)}
                            disabled={!!lockedKhoa}
                            className="h-8 w-full rounded border border-purple-200 bg-white px-2 text-xs focus:outline-none focus:ring-1 focus:ring-purple-300 disabled:bg-gray-100 disabled:text-gray-500 disabled:cursor-not-allowed"
                          >
                            <option value="Tất cả các khoa">Tất cả các khoa</option>
                            {KHOA_PHONG_OPTIONS.filter((o) => o.value !== '').map((o) => (
                              <option key={o.value} value={o.value}>{o.label}</option>
                            ))}
                          </select>
                        )}
                      </div>
                    )}
                  </div>

                  {excelErrors.length > 0 && (
                    <div className="bg-red-50 border border-red-200 text-red-700 rounded-lg p-3 text-xs space-y-1 max-h-32 overflow-y-auto">
                      <p className="font-bold flex items-center gap-1"><AlertTriangle className="h-3.5 w-3.5 text-red-500" /> Tải file thất bại:</p>
                      <ul className="list-disc pl-5 space-y-0.5">
                        {excelErrors.map((err, idx) => (
                          <li key={idx}>{err}</li>
                        ))}
                      </ul>
                    </div>
                  )}

                  {importedCount > 0 && (
                    <div className="bg-green-50 border border-green-200 text-green-700 rounded-lg p-3 text-xs flex items-center gap-2">
                      <CheckCircle2 className="h-4 w-4 text-green-600 shrink-0" />
                      <span>Đã tải thành công <strong>{importedCount}</strong> câu hỏi từ Excel.</span>
                    </div>
                  )}
                </div>
              )}
            </div>
          </div>

          {/* Số câu đúng tối thiểu */}
          <div className="space-y-1 border rounded-lg p-3 bg-amber-50 border-amber-200">
            <label className="text-sm font-medium text-amber-800 flex items-center gap-1.5">
              🏆 Số câu đúng tối thiểu để ĐẠT
              <span className="text-xs font-normal text-amber-600">(tùy chọn)</span>
            </label>
            <input
              type="number"
              min={0}
              placeholder="Để trống nếu không xét đạt/không đạt"
              {...form.register('soCauDungToiThieu')}
              className="h-9 w-full rounded-md border border-amber-200 bg-white px-3 text-sm focus:outline-none focus:ring-2 focus:ring-amber-300"
            />
            {form.formState.errors.soCauDungToiThieu && (
              <p className="text-xs text-red-500">{form.formState.errors.soCauDungToiThieu.message as string}</p>
            )}
            {configMode === 'bank' && (
              <div className="text-xs text-amber-600 space-y-1">
                <p>
                  Tổng số câu đã chọn: <strong>{selectedIds.length}</strong> câu.
                  {selectedKyThiDetails?.tongSoCauHoi ? ` Yêu cầu: ${selectedKyThiDetails.tongSoCauHoi} câu.` : ''}
                  {selectedIds.length > 0 && ` Số câu đúng tối thiểu phải ≤ ${selectedIds.length}.`}
                </p>
                {selectedKyThiDetails?.tongSoCauHoi && selectedIds.length < selectedKyThiDetails.tongSoCauHoi && (
                  <p className="text-red-500 font-semibold">
                    ⚠ Số lượng câu hỏi chưa đủ, còn thiếu {selectedKyThiDetails.tongSoCauHoi - selectedIds.length} câu hỏi.
                  </p>
                )}
                {selectedKyThiDetails?.tongSoCauHoi && selectedIds.length > selectedKyThiDetails.tongSoCauHoi && (
                  <p className="text-red-500 font-semibold">
                    ⚠ Số lượng câu hỏi vượt quá yêu cầu, thừa {selectedIds.length - selectedKyThiDetails.tongSoCauHoi} câu hỏi.
                  </p>
                )}
              </div>
            )}
            {configMode === 'excel' && (
              <div className="text-xs text-amber-600 space-y-1">
                {importedCount > 0 && (
                  <p>
                    Tổng số câu tải lên: <strong>{importedCount}</strong> câu. Số câu đúng tối thiểu phải ≤ {importedCount}.
                  </p>
                )}
                {selectedKyThiDetails?.tongSoCauHoi && importedCount > 0 && importedCount < selectedKyThiDetails.tongSoCauHoi && (
                  <p className="text-red-500 font-semibold">
                    ⚠ Số lượng câu hỏi chưa đủ, còn thiếu {selectedKyThiDetails.tongSoCauHoi - importedCount} câu hỏi.
                  </p>
                )}
                {selectedKyThiDetails?.tongSoCauHoi && importedCount > 0 && importedCount > selectedKyThiDetails.tongSoCauHoi && (
                  <p className="text-red-500 font-semibold">
                    ⚠ Số lượng câu hỏi vượt quá yêu cầu, thừa {importedCount - selectedKyThiDetails.tongSoCauHoi} câu hỏi.
                  </p>
                )}
              </div>
            )}
          </div>

          {/* Actions */}
          <div className="flex justify-end gap-2 pt-4 border-t">
            <Button type="button" variant="outline" onClick={onClose}>Hủy</Button>
            <Button
              type="submit"
              disabled={
                isLoading ||
                isUploadingExcel ||
                (configMode === 'bank' && selectedIds.length === 0) ||
                (configMode === 'excel' && !excelFile)
              }
            >
              {isLoading || isUploadingExcel ? 'Đang tạo...' : 'Tạo đề thi'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  )
}
