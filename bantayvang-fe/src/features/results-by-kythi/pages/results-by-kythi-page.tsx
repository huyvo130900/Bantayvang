import { useEffect, useState, useMemo, forwardRef, useRef } from 'react'
import { useSearchParams } from 'react-router-dom'
import apiClient from '@/lib/axios'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { departmentApi } from '@/features/departments/api'
import { useAppSelector } from '@/app/hooks'
import { ROLES } from '@/lib/constants'
import {
  CalendarDays, Users, Trophy,
  AlertTriangle, RefreshCw, Download, Eye, EyeOff, Search, CheckCircle2, XCircle,
  ChevronDown, ChevronRight
} from 'lucide-react'
import { getClassification } from '@/lib/constants'
import DatePicker from 'react-datepicker'
import 'react-datepicker/dist/react-datepicker.css'

// eslint-disable-next-line @typescript-eslint/no-unused-vars
const CustomDateInput = forwardRef<HTMLInputElement, Record<string, unknown>>(({ value, onChange: dpOnChange, onClick, rawDateValue, onRawChange, ...props }: any, ref) => {
  const [internalValue, setInternalValue] = useState(rawDateValue || 'dd/mm/yyyy');
  const inputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (rawDateValue && rawDateValue !== internalValue) {
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setInternalValue(rawDateValue);
    }
    if (!rawDateValue) {
      setInternalValue('dd/mm/yyyy');
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [rawDateValue]);

  const setHighlight = (pos: number) => {
    let nextPos = pos;
    if (nextPos === 2 || nextPos === 5) nextPos++;
    if (nextPos > 9) nextPos = 9;
    
    setTimeout(() => {
      if (inputRef.current) {
        inputRef.current.setSelectionRange(nextPos, nextPos + 1);
      }
    }, 0);
  };

  const handleFocus = () => {
    let firstPlaceholder = internalValue.search(/[dmy]/);
    if (firstPlaceholder === -1) firstPlaceholder = 9;
    setHighlight(firstPlaceholder);
  };

  const handleClick = (e: React.MouseEvent<HTMLInputElement>) => {
    if (inputRef.current) {
      let pos = inputRef.current.selectionStart || 0;
      if (pos === 2 || pos === 5) pos++;
      setHighlight(pos);
    }
    if (onClick) onClick(e);
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Backspace' || e.key === 'Delete') {
      e.preventDefault();
      if (inputRef.current) {
        let pos = inputRef.current.selectionStart || 0;
        const end = inputRef.current.selectionEnd || 0;
        
        if (end - pos > 1) {
           setInternalValue('dd/mm/yyyy');
           onRawChange('');
           if (dpOnChange) dpOnChange({ target: { value: '' } } as any);
           setHighlight(0);
           return;
        }

        if (e.key === 'Backspace') {
          if (pos > 0) {
            let prevPos = pos - 1;
            if (prevPos === 2 || prevPos === 5) prevPos--;
            
            const arr = internalValue.split('');
            if (prevPos < 2) arr[prevPos] = 'd';
            else if (prevPos < 5) arr[prevPos] = 'm';
            else arr[prevPos] = 'y';
            
            const newVal = arr.join('');
            setInternalValue(newVal);
            onRawChange(newVal);
            if (/[dmy]/.test(newVal) && dpOnChange) dpOnChange({ target: { value: '' } } as any);
            setHighlight(prevPos);
          }
        } else if (e.key === 'Delete') {
            if (pos < 10) {
              if (pos === 2 || pos === 5) pos++;
              if (pos < 10) {
                const arr = internalValue.split('');
                if (pos < 2) arr[pos] = 'd';
                else if (pos < 5) arr[pos] = 'm';
                else arr[pos] = 'y';
                
                const newVal = arr.join('');
                setInternalValue(newVal);
                onRawChange(newVal);
                if (/[dmy]/.test(newVal) && dpOnChange) dpOnChange({ target: { value: '' } } as any);
                setHighlight(pos);
              }
            }
        }
      }
    } else if (e.key === 'ArrowLeft' || e.key === 'ArrowRight') {
      e.preventDefault();
      let pos = inputRef.current?.selectionStart || 0;
      if (e.key === 'ArrowLeft') {
         pos--;
         if (pos === 2 || pos === 5) pos--;
         if (pos < 0) pos = 0;
      } else {
         pos++;
         if (pos === 2 || pos === 5) pos++;
         if (pos > 9) pos = 9;
      }
      setHighlight(pos);
    }
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (!inputRef.current) return;
    const val = e.target.value;
    const pos = inputRef.current.selectionStart || 0;
    
    if (Math.abs(val.length - internalValue.length) > 1) {
       const numbers = val.replace(/[^0-9]/g, '');
       const newStr = 'dd/mm/yyyy'.split('');
       let numIdx = 0;
       for (let i = 0; i < 10 && numIdx < numbers.length; i++) {
         if (i === 2 || i === 5) continue;
         newStr[i] = numbers[numIdx++];
       }
       const newVal = newStr.join('');
       setInternalValue(newVal);
       onRawChange(newVal);
       if (!/[dmy]/.test(newVal) && dpOnChange) dpOnChange({ target: { value: newVal } } as any);
       setHighlight(9);
       return;
    }

    const charTyped = val.charAt(pos - 1);
    
    if (/[0-9]/.test(charTyped)) {
       let insertPos = pos - 1;
       if (insertPos === 2 || insertPos === 5) insertPos--;
       if (insertPos < 0) insertPos = 0;

       const arr = internalValue.split('');
       arr[insertPos] = charTyped;
       const newVal = arr.join('');
       setInternalValue(newVal);
       onRawChange(newVal);
       
       if (!/[dmy]/.test(newVal) && dpOnChange) {
          dpOnChange({ target: { value: newVal } } as any);
       }
       setHighlight(insertPos + 1);
    } else {
       setHighlight(pos - 1);
    }
  };

  const handleRef = (node: HTMLInputElement) => {
    (inputRef as React.MutableRefObject<HTMLInputElement | null>).current = node;
    if (typeof ref === 'function') ref(node);
    else if (ref) (ref as React.MutableRefObject<HTMLInputElement | null>).current = node;
  };

  return (
    <input
      ref={handleRef}
      type="text"
      value={internalValue}
      onChange={handleChange}
      onFocus={handleFocus}
      onClick={handleClick}
      onKeyDown={handleKeyDown}
      {...props}
    />
  );
});
CustomDateInput.displayName = 'CustomDateInput';

interface KyThiItem {
  id: number; campaignName: string; campaignCode: string
  startTime?: string; endTime?: string
  status: string; soCaThi?: number; organizedBy?: string
  minPassQuestions?: number | null
}

// Match actual backend ExamResultDetailDto camelCase fields
interface ThiSinhResult {
  examSubmissionId: number
  userId?: number
  username?: string
  fullName?: string
  employeeCode?: string
  department?: string
  examId?: number
  examPaperId?: number
  examPaperCode?: string
  examPaperName?: string
  startTime?: string
  submitTime?: string
  totalScore?: number
  correctAnswers?: number
  totalQuestions?: number
  status?: string
  pass?: boolean
  minPassQuestions?: number | null
  warningCount?: number
  attemptCount?: number
  cheatingCount?: number
  retakeCount?: number
  isResultPublished?: boolean
  departmentEvaluation?: string
}

export function ResultsByKyThiPage() {
  const [searchParams] = useSearchParams()
  const currentUser = useAppSelector((state) => state.auth.user)

  const isDeptManager = currentUser?.role === ROLES.DEPT_MANAGER || currentUser?.roleName === 'DeptManager'
  const isAdmin = !isDeptManager
  const myKhoa = currentUser?.deptManagerDeptName || currentUser?.department || null

  const [kyThiList, setKyThiList] = useState<KyThiItem[]>([])
  const [selectedKyThi, setSelectedKyThi] = useState<KyThiItem | null>(null)
  const [results, setResults] = useState<ThiSinhResult[]>([])
  const [loading, setLoading] = useState(false)
  const [search, setSearch] = useState('')
  const [filterXepLoai, setFilterXepLoai] = useState('')

  // Search & Filter states for exams
  const [filterKyThiName, setFilterKyThiName] = useState('')
  const [filterKyThiKhoa, setFilterKyThiKhoa] = useState('')
  const [filterKyThiStartDate, setFilterKyThiStartDate] = useState('')
  const [filterKyThiEndDate, setFilterKyThiEndDate] = useState('')
  const [expandedGroups, setExpandedGroups] = useState<Record<string, boolean>>({})

  const parseDateToMs = (dateStr: string, isEndOfDay: boolean = false): number | null => {
    if (!dateStr || dateStr.length !== 10) return null;
    if (/[dmy]/.test(dateStr)) return null;
    const parts = dateStr.split('/')
    if (parts.length !== 3) return null;
    const d = parseInt(parts[0], 10)
    const m = parseInt(parts[1], 10)
    const y = parseInt(parts[2], 10)
    if (isNaN(d) || isNaN(m) || isNaN(y)) return null;
    if (m < 1 || m > 12 || d < 1 || d > 31 || y < 1900 || y > 2100) return null;
    
    const date = new Date(y, m - 1, d)
    if (date.getFullYear() !== y || date.getMonth() !== m - 1 || date.getDate() !== d) return null;
    
    if (isEndOfDay) {
      date.setHours(23, 59, 59, 999)
    } else {
      date.setHours(0, 0, 0, 0)
    }
    return date.getTime()
  }

  // Báo lỗi khoảng thời gian
  const dateError = useMemo(() => {
    const isStartEmpty = !filterKyThiStartDate || filterKyThiStartDate === 'dd/mm/yyyy';
    const isEndEmpty = !filterKyThiEndDate || filterKyThiEndDate === 'dd/mm/yyyy';

    if (!isStartEmpty && isEndEmpty) {
      return 'Vui lòng nhập ngày kết thúc'
    }
    if (isStartEmpty && !isEndEmpty) {
      return 'Vui lòng nhập ngày bắt đầu'
    }
    if (!isStartEmpty && !isEndEmpty) {
      if (/[dmy]/.test(filterKyThiStartDate) || /[dmy]/.test(filterKyThiEndDate)) {
        return 'Vui lòng nhập đủ định dạng dd/mm/yyyy'
      }
      const start = parseDateToMs(filterKyThiStartDate)
      const end = parseDateToMs(filterKyThiEndDate, true)
      
      if (start === null || end === null) {
        return 'Ngày không hợp lệ (định dạng dd/mm/yyyy)'
      }
      
      if (start > end) {
        return 'Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu'
      }
    }
    return null
  }, [filterKyThiStartDate, filterKyThiEndDate])

  const [visibility, setVisibility] = useState<Record<number, boolean>>({})
  const [togglingId, setTogglingId] = useState<number | null>(null)
  const [msg, setMsg] = useState<string | null>(null)
  const [selectedexamSubmissionIdByUser, setSelectedexamSubmissionIdByUser] = useState<Record<string, number>>({})
  const [selectedexamPaperId, setSelectedexamPaperId] = useState<number | null>(null)
  const [isExportModalOpen, setIsExportModalOpen] = useState(false)
  const [selectedExportexamPaperId, setSelectedExportexamPaperId] = useState<number | null>(null)


  async function loadResults(examCampaignId: number) {
    setLoading(true)
    try {
      const res = await apiClient.get(`/Grading/by-exam-campaign/${examCampaignId}`)
      const data: ThiSinhResult[] = res.data?.data || []
      setResults(data)
      // Build visibility map: examPaperId -> isResultPublished
      const vis: Record<number, boolean> = {}
      data.forEach((r) => {
        if (r.examPaperId !== undefined) vis[r.examPaperId] = r.isResultPublished ?? false
      })
      setVisibility(vis)
    } catch { setResults([]) }
    finally { setLoading(false) }
  }

  async function loadKyThiList() {
    setLoading(true)
    try {
      const res = await apiClient.get('/ExamCampaign')
      let list: KyThiItem[] = res.data?.data || []

      // DeptManager chỉ thấy kỳ thi của khoa mình
      if (isDeptManager && myKhoa) {
        list = list.filter((k) => k.organizedBy === myKhoa)
      }

      setKyThiList(list)
      const preselect = searchParams.get('examCampaignId')
      if (preselect) {
        const found = list.find((k) => k.id === parseInt(preselect))
        if (found) { setSelectedKyThi(found); loadResults(found.id) }
      }
    } catch { /* ignore */ }
    finally { setLoading(false) }
  }

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect
    loadKyThiList()
  }, []) // eslint-disable-line

  useEffect(() => {
    if (selectedKyThi) {
      const dept = selectedKyThi.organizedBy || 'Tất cả các khoa'
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setExpandedGroups(prev => ({
        ...prev,
        [dept]: true
      }))
    }
  }, [selectedKyThi])



  const handleSelectKyThi = (kt: KyThiItem) => {
    setSelectedKyThi(kt)
    setSearch('')
    setFilterXepLoai('')
    setSelectedexamSubmissionIdByUser({})
    setSelectedexamPaperId(null)
    loadResults(kt.id)
  }

  async function handleToggleVisibility(examPaperId: number, current: boolean) {
    setTogglingId(examPaperId)
    try {
      await departmentApi.toggleExamVisibility(examPaperId, { isResultPublished: !current })
      setVisibility(v => ({ ...v, [examPaperId]: !current }))
      setMsg(!current ? 'Đã bật công bố kết quả' : 'Đã tắt công bố kết quả')
      setTimeout(() => setMsg(null), 3000)
    } catch { setMsg('Không thể cập nhật') }
    finally { setTogglingId(null) }
  }

  async function handleExportResults(examPaperId: number | null) {
    if (!selectedKyThi) return
    try {
      const targetResults = examPaperId
        ? results.filter(r => r.examPaperId === examPaperId)
        : results

      const map: Record<string, ThiSinhResult[]> = {}
      targetResults.forEach((r) => {
        const key = r.username || r.userId?.toString() || ''
        if (!map[key]) {
          map[key] = []
        }
        map[key].push(r)
      })

      const grouped = Object.entries(map).map(([key, attempts]) => {
        const sortedAttempts = [...attempts].sort((a, b) => a.examSubmissionId - b.examSubmissionId)
        let selectedAttempt = sortedAttempts.find((a) => a.examSubmissionId === selectedexamSubmissionIdByUser[key])
        if (!selectedAttempt) {
          selectedAttempt = sortedAttempts[sortedAttempts.length - 1]
        }
        return {
          attempts: sortedAttempts,
          selectedAttempt,
        }
      })

      const exportItems = grouped.map(c => ({
        examSubmissionId: c.selectedAttempt.examSubmissionId,
        lanThi: c.attempts.findIndex(a => a.examSubmissionId === c.selectedAttempt.examSubmissionId) + 1
      }))

      if (exportItems.length === 0) {
        setMsg("Không có dữ liệu để xuất Excel")
        setTimeout(() => setMsg(null), 3000)
        return
      }

      const response = await apiClient.post('/Grading/export-selected', exportItems, { responseType: 'blob' })
      
      const url = window.URL.createObjectURL(new Blob([response.data]))
      const link = document.createElement('a')
      link.href = url
      
      let suffix = ""
      if (examPaperId) {
        const deThiObj = uniqueDeThis.find(d => d.id === examPaperId)
        if (deThiObj) {
          suffix = "_" + (deThiObj.examPaperName || deThiObj.examPaperCode)
        }
      }
      const rawName = `${selectedKyThi.campaignName}${suffix}`
      const safeName = rawName.replace(/[^a-zA-Z0-9\s_]/g, '').replace(/\s+/g, '_')
      link.setAttribute('download', `KetQua_KyThi_${safeName}.xlsx`)
      document.body.appendChild(link)
      link.click()
      link.remove()
      
      setMsg("Đã xuất file Excel thành công")
      setTimeout(() => setMsg(null), 3000)
    } catch {
      setMsg("Không thể xuất file Excel")
      setTimeout(() => setMsg(null), 3000)
    }
  }

  // Lấy tất cả các khoa từ danh sách kỳ thi để hiển thị trong bộ lọc
  const uniqueDeptsFilter = useMemo(() => {
    const depts = new Set(kyThiList.map(k => k.organizedBy).filter(Boolean) as string[])
    return Array.from(depts).sort()
  }, [kyThiList])

  // Lọc kỳ thi theo tên, khoa và khoảng thời gian
  const filteredKyThiList = useMemo(() => {
    return kyThiList.filter(kt => {
      const matchName = !filterKyThiName.trim() ||
        kt.campaignName?.toLowerCase().includes(filterKyThiName.toLowerCase()) ||
        kt.campaignCode?.toLowerCase().includes(filterKyThiName.toLowerCase())

      const matchKhoa = !filterKyThiKhoa || (
        filterKyThiKhoa === 'Tất cả các khoa'
          ? !kt.organizedBy
          : kt.organizedBy === filterKyThiKhoa
      )

      // Lọc theo khoảng thời gian nếu người dùng nhập cả 2 mốc và mốc 2 >= mốc 1
      let matchDate = true
      if (filterKyThiStartDate && filterKyThiEndDate) {
        const startMs = parseDateToMs(filterKyThiStartDate)
        const endMs = parseDateToMs(filterKyThiEndDate, true)
        if (startMs !== null && endMs !== null && startMs <= endMs) {
          if (!kt.startTime) {
            matchDate = false
          } else {
            try {
              const examTime = new Date(kt.startTime).getTime()
              matchDate = examTime >= startMs && examTime <= endMs
            } catch {
              matchDate = false
            }
          }
        }
      }

      return matchName && matchKhoa && matchDate
    })
  }, [kyThiList, filterKyThiName, filterKyThiKhoa, filterKyThiStartDate, filterKyThiEndDate])

  // Nhóm các kỳ thi đã lọc theo khoa
  const groupedExams = useMemo(() => {
    const groups: Record<string, KyThiItem[]> = {}

    filteredKyThiList.forEach(kt => {
      const deptName = kt.organizedBy || 'Tất cả các khoa'
      if (!groups[deptName]) {
        groups[deptName] = []
      }
      groups[deptName].push(kt)
    })

    // Sắp xếp các kỳ thi trong mỗi khoa theo thời gian bắt đầu giảm dần (gần đây nhất trước)
    // và chỉ lấy tối đa 10 kỳ thi gần đây nhất
    const sortedGroups: Record<string, KyThiItem[]> = {}
    Object.keys(groups).forEach(deptName => {
      const sortedList = [...groups[deptName]].sort((a, b) => {
        const timeA = a.startTime ? new Date(a.startTime).getTime() : 0
        const timeB = b.startTime ? new Date(b.startTime).getTime() : 0
        if (timeA !== timeB) {
          return timeB - timeA
        }
        return b.id - a.id
      })
      sortedGroups[deptName] = sortedList.slice(0, 10)
    })

    return sortedGroups
  }, [filteredKyThiList])

  // Sắp xếp các khoa: 'Tất cả các khoa' lên đầu, sau đó theo bảng chữ cái tiếng Việt
  const sortedGroupKeys = useMemo(() => {
    const keys = Object.keys(groupedExams)
    return keys.sort((a, b) => {
      if (a === 'Tất cả các khoa') return -1
      if (b === 'Tất cả các khoa') return 1
      return a.localeCompare(b, 'vi')
    })
  }, [groupedExams])


  const hasThreshold = selectedKyThi?.minPassQuestions !== undefined && selectedKyThi?.minPassQuestions !== null;

  // Extract unique exams that have submissions in this campaign
  const uniqueDeThis = (() => {
    const map = new Map<number, { id: number; examPaperName: string; examPaperCode: string }>()
    results.forEach((r) => {
      if (r.examPaperId) {
        map.set(r.examPaperId, {
          id: r.examPaperId,
          examPaperName: r.examPaperName || '',
          examPaperCode: r.examPaperCode || ''
        })
      }
    })
    return Array.from(map.values())
  })()

  // Filter raw results by selected exam (if any) before grouping and stats
  const resultsFilteredByDeThi = (() => {
    if (!selectedexamPaperId) return results
    return results.filter((r) => r.examPaperId === selectedexamPaperId)
  })()

  // Group attempts by candidate
  const groupedCandidates = (() => {
    const map: Record<string, ThiSinhResult[]> = {}
    resultsFilteredByDeThi.forEach((r) => {
      const key = r.username || r.userId?.toString() || ''
      if (!map[key]) {
        map[key] = []
      }
      map[key].push(r)
    })

    return Object.entries(map).map(([key, attempts]) => {
      const sortedAttempts = [...attempts].sort((a, b) => a.examSubmissionId - b.examSubmissionId)
      return {
        userKey: key,
        attempts: sortedAttempts,
      }
    })
  })()

  // Get currently selected attempt for each candidate
  const candidatesWithSelectedAttempt = useMemo(() => {
    return groupedCandidates.map((c) => {
      let selectedAttempt = c.attempts.find((a) => a.examSubmissionId === selectedexamSubmissionIdByUser[c.userKey])
      if (!selectedAttempt) {
        selectedAttempt = c.attempts[c.attempts.length - 1] // Default to latest attempt
      }
      return {
        ...c,
        selectedAttempt,
      }
    })
  }, [groupedCandidates, selectedexamSubmissionIdByUser])

  // Filter and search based on selected attempt
  const filtered = useMemo(() => {
    return candidatesWithSelectedAttempt.filter(c => {
      const r = c.selectedAttempt
      const name = r.fullName || r.username || ''
      const matchSearch = !search ||
        name.toLowerCase().includes(search.toLowerCase()) ||
        r.employeeCode?.toLowerCase().includes(search.toLowerCase())
      const diem = r.totalScore
      const matchFilter = !filterXepLoai || (
        hasThreshold
          ? (filterXepLoai === 'Đạt' ? (r.correctAnswers ?? 0) >= selectedKyThi!.minPassQuestions! : (r.correctAnswers ?? 0) < selectedKyThi!.minPassQuestions!)
          : (diem !== undefined && getClassification(diem) === filterXepLoai)
      )
      return matchSearch && matchFilter
    })
  }, [candidatesWithSelectedAttempt, search, filterXepLoai, hasThreshold, selectedKyThi])

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

  const validCandidates = candidatesWithSelectedAttempt.map(c => c.selectedAttempt).filter(r => r.status !== 'BiBHuyGianLan' && r.totalScore !== undefined)
  const avgScore = validCandidates.length ? (validCandidates.reduce((s, r) => s + (r.totalScore ?? 0), 0) / validCandidates.length) : 0
  const cheatingCount = candidatesWithSelectedAttempt.filter(c => (c.selectedAttempt.warningCount ?? 0) > 0 || (c.selectedAttempt.cheatingCount ?? 0) > 0).length
  const retakeCount = candidatesWithSelectedAttempt.reduce((s, c) => s + (c.selectedAttempt.retakeCount ?? 0), 0)
  const passCount = hasThreshold
    ? candidatesWithSelectedAttempt.map(c => c.selectedAttempt).filter(r => r.status !== 'BiBHuyGianLan' && (r.correctAnswers ?? 0) >= selectedKyThi!.minPassQuestions!).length
    : candidatesWithSelectedAttempt.map(c => c.selectedAttempt).filter(r => r.pass).length
  const failCount = hasThreshold
    ? candidatesWithSelectedAttempt.map(c => c.selectedAttempt).filter(r => r.status !== 'BiBHuyGianLan' && (r.correctAnswers ?? 0) < selectedKyThi!.minPassQuestions!).length
    : 0;
  const passRate = candidatesWithSelectedAttempt.length ? (passCount / candidatesWithSelectedAttempt.length) * 100 : 0;

  return (
    <div className="flex flex-col lg:flex-row h-full lg:h-[calc(100vh-6rem)] min-h-0 overflow-hidden">
      {/* Left: ExamCampaign list */}
      <div className="w-full lg:w-80 border-b lg:border-b-0 lg:border-r bg-white flex flex-col shrink-0 h-[480px] lg:h-full">
        <div className="px-4 py-4 border-b">
          <h2 className="font-semibold text-gray-800 flex items-center gap-2">
            <CalendarDays className="h-4 w-4 text-blue-500" /> Kỳ thi
          </h2>
          {isDeptManager && myKhoa && (
            <p className="text-xs text-blue-500 mt-1">📋 {myKhoa}</p>
          )}
        </div>

        {/* Search & Filters */}
        <div className="px-4 py-3 border-b space-y-3 bg-gray-50/30">
          {/* Exam Name Search */}
          <div className="space-y-1">
            <label className="text-[10px] font-semibold text-gray-500 uppercase tracking-wider">Tên kỳ thi</label>
            <div className="relative">
              <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-gray-400" />
              <input
                type="text"
                placeholder="Tìm tên hoặc mã kỳ thi..."
                value={filterKyThiName}
                onChange={(e) => setFilterKyThiName(e.target.value)}
                className="w-full pl-8 pr-3 py-1.5 border border-gray-200 rounded-lg text-xs placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all bg-white"
              />
            </div>
          </div>

          {/* Department Filter (Admin only) */}
          {isAdmin && (
            <div className="space-y-1">
              <label className="text-[10px] font-semibold text-gray-500 uppercase tracking-wider">Khoa / Phòng</label>
              <select
                value={filterKyThiKhoa}
                onChange={(e) => setFilterKyThiKhoa(e.target.value)}
                className="w-full border border-gray-200 rounded-lg text-xs px-2 py-1.5 bg-white focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all"
              >
                <option value="">Tất cả các khoa/phòng</option>
                <option value="Tất cả các khoa">Các kỳ thi chung (tất cả các khoa)</option>
                {uniqueDeptsFilter.map(khoa => (
                  <option key={khoa} value={khoa}>{khoa}</option>
                ))}
              </select>
            </div>
          )}

          {/* Date Range Filter */}
          <div className="space-y-2">
            <label className="text-[10px] font-semibold text-gray-500 uppercase tracking-wider block">Khoảng thời gian thi</label>
            <div className="grid grid-cols-2 gap-2">
              <div className="space-y-1">
                <span className="text-[9px] text-gray-400 font-medium">Từ ngày</span>
                <DatePicker
                  selected={parseDateToMs(filterKyThiStartDate) ? new Date(parseDateToMs(filterKyThiStartDate)!) : null}
                  onChange={(date: Date | null) => {
                    if (date) {
                      const d = date.getDate().toString().padStart(2, '0')
                      const m = (date.getMonth() + 1).toString().padStart(2, '0')
                      const y = date.getFullYear()
                      setFilterKyThiStartDate(`${d}/${m}/${y}`)
                    } else {
                      setFilterKyThiStartDate('')
                    }
                  }}
                  dateFormat="dd/MM/yyyy"
                  customInput={
                    <CustomDateInput
                      rawDateValue={filterKyThiStartDate}
                      onRawChange={setFilterKyThiStartDate}
                      placeholder="dd/mm/yyyy"
                      maxLength={10}
                      className="w-full border border-gray-200 rounded-lg text-[11px] px-2 py-1 bg-white focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all cursor-text font-mono"
                    />
                  }
                />
              </div>
              <div className="space-y-1">
                <span className="text-[9px] text-gray-400 font-medium">Đến ngày</span>
                <DatePicker
                  selected={parseDateToMs(filterKyThiEndDate) ? new Date(parseDateToMs(filterKyThiEndDate)!) : null}
                  onChange={(date: Date | null) => {
                    if (date) {
                      const d = date.getDate().toString().padStart(2, '0')
                      const m = (date.getMonth() + 1).toString().padStart(2, '0')
                      const y = date.getFullYear()
                      setFilterKyThiEndDate(`${d}/${m}/${y}`)
                    } else {
                      setFilterKyThiEndDate('')
                    }
                  }}
                  dateFormat="dd/MM/yyyy"
                  customInput={
                    <CustomDateInput
                      rawDateValue={filterKyThiEndDate}
                      onRawChange={setFilterKyThiEndDate}
                      placeholder="dd/mm/yyyy"
                      maxLength={10}
                      className="w-full border border-gray-200 rounded-lg text-[11px] px-2 py-1 bg-white focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all cursor-text font-mono"
                    />
                  }
                />
              </div>
            </div>
            {dateError && (
              <p className="text-[10px] text-red-500 font-medium mt-1 leading-tight">{dateError}</p>
            )}
          </div>

          {/* Reset Filters button */}
          {(filterKyThiName || filterKyThiKhoa || filterKyThiStartDate || filterKyThiEndDate) && (
            <button
              onClick={() => {
                setFilterKyThiName('')
                setFilterKyThiKhoa('')
                setFilterKyThiStartDate('')
                setFilterKyThiEndDate('')
              }}
              className="text-[11px] text-blue-600 hover:text-blue-800 font-medium hover:underline block ml-auto transition-all"
            >
              Xóa bộ lọc
            </button>
          )}
        </div>

        {/* Grouped Exams List */}
        <div className="flex-1 overflow-y-auto divide-y divide-gray-100">
          {sortedGroupKeys.length === 0 ? (
            <div className="text-center py-12 text-gray-400 text-sm">
              Không tìm thấy kỳ thi nào
            </div>
          ) : (
            sortedGroupKeys.map(deptName => {
              const isExpanded = !!expandedGroups[deptName]
              const exams = groupedExams[deptName]
              return (
                <div key={deptName} className="flex flex-col">
                  {/* Group Header */}
                  <button
                    onClick={() => {
                      setExpandedGroups(prev => ({
                        ...prev,
                        [deptName]: !isExpanded
                      }))
                    }}
                    className="w-full flex items-center justify-between px-4 py-3 bg-gray-50 hover:bg-gray-100/80 transition-colors border-b text-left select-none"
                  >
                    <div className="flex items-center gap-2 min-w-0">
                      {isExpanded ? (
                        <ChevronDown className="h-4 w-4 text-gray-500 shrink-0" />
                      ) : (
                        <ChevronRight className="h-4 w-4 text-gray-500 shrink-0" />
                      )}
                      <span className="font-semibold text-xs text-gray-700 truncate">
                        {deptName}
                      </span>
                    </div>
                    <span className="text-[10px] bg-gray-200 text-gray-600 px-2 py-0.5 rounded-full font-medium shrink-0">
                      {exams.length}
                    </span>
                  </button>

                  {/* Group Body */}
                  {isExpanded && (
                    <div className="bg-white flex flex-col">
                      {exams.map(kt => (
                        <button
                          key={kt.id}
                          onClick={() => handleSelectKyThi(kt)}
                          className={`w-full text-left px-6 py-3 border-b hover:bg-gray-50 transition-colors ${
                            selectedKyThi?.id === kt.id ? 'bg-blue-50 border-l-4 border-l-blue-500' : ''
                          }`}
                        >
                          <p className="font-medium text-sm text-gray-800 truncate">{kt.campaignName}</p>
                          {kt.startTime && (
                            <div className="flex items-center gap-1.5 min-w-[120px] text-[11px] text-gray-400 mt-0.5">
                              <CalendarDays className="w-3 h-3" />
                              Ngày thi: {new Date(kt.startTime).toLocaleDateString('vi-VN')}
                            </div>
                          )}
                          <div className="flex items-center gap-2 mt-1">
                            <span className={`text-[10px] px-1.5 py-0.5 rounded-full ${trangThaiColor(kt.status)}`}>
                              {trangThaiLabel(kt.status)}
                            </span>
                          </div>
                        </button>
                      ))}
                    </div>
                  )}
                </div>
              )
            })
          )}
        </div>
      </div>

      {/* Right: Results */}
      <div className="flex-1 overflow-auto">
        {!selectedKyThi ? (
          <div className="flex flex-col items-center justify-center h-full text-gray-400 gap-3 py-12">
            <CalendarDays className="h-12 w-12 opacity-40" />
            <p>Chọn một kỳ thi để xem kết quả</p>
          </div>
        ) : (
          <div className="p-4 sm:p-6 space-y-5">
            <div className="flex flex-col sm:flex-row sm:items-start justify-between gap-4">
              <div>
                <div className="flex items-center gap-3">
                  <h1 className="text-xl font-bold text-gray-900">{selectedKyThi.campaignName}</h1>
                  {hasThreshold && (
                    <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold bg-emerald-50 text-emerald-700 border border-emerald-200">
                      Yêu cầu đạt: ≥ {selectedKyThi.minPassQuestions} câu đúng
                    </span>
                  )}
                </div>
                {selectedKyThi.organizedBy && (
                  <p className="text-xs text-blue-600 mt-0.5">{selectedKyThi.organizedBy}</p>
                )}
                <p className="text-sm text-gray-500 mt-0.5">
                  {groupedCandidates.length} thí sinh ({resultsFilteredByDeThi.length} lượt thi)
                </p>
              </div>
              <Button
                variant="outline"
                size="sm"
                onClick={() => {
                  setSelectedExportexamPaperId(null)
                  setIsExportModalOpen(true)
                }}
              >
                <Download className="h-4 w-4 mr-2" /> Xuất file Excel
              </Button>
            </div>

            {msg && <div className="bg-blue-50 text-blue-700 px-4 py-2 rounded-lg text-sm">{msg}</div>}

            {/* Stats */}
            <div className="grid grid-cols-4 gap-4">
              {(hasThreshold
                ? [
                    { label: 'Tổng lượt thi', value: resultsFilteredByDeThi.length, icon: Users, color: 'text-blue-500', bg: 'bg-blue-50', sub: 'Toàn bộ kỳ thi' },
                    { label: 'Thí sinh Đạt', value: passCount, icon: CheckCircle2, color: 'text-emerald-500', bg: 'bg-emerald-50', sub: 'Số thí sinh đạt' },
                    { label: 'Thí sinh Không đạt', value: failCount, icon: XCircle, color: 'text-rose-500', bg: 'bg-rose-50', sub: 'Số thí sinh không đạt' },
                    { label: 'Tỷ lệ đạt', value: `${passRate.toFixed(1)}%`, icon: Trophy, color: 'text-indigo-500', bg: 'bg-indigo-50', sub: 'Tỷ lệ phần trăm đạt' },
                  ]
                : [
                    { label: 'Tổng lượt thi', value: resultsFilteredByDeThi.length, icon: Users, color: 'text-blue-500', bg: 'bg-blue-50', sub: 'Toàn bộ kỳ thi' },
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

              {uniqueDeThis.length > 0 && (
                <select
                  value={selectedexamPaperId || ''}
                  onChange={(e) => {
                    const val = e.target.value ? Number(e.target.value) : null
                    setSelectedexamPaperId(val)
                    setSelectedexamSubmissionIdByUser({})
                  }}
                  className="border rounded-lg px-3 py-2 text-sm text-gray-600 bg-white"
                >
                  <option value="">Tất cả đề thi</option>
                  {uniqueDeThis.map((d) => (
                    <option key={d.id} value={d.id}>
                      {d.examPaperName || d.examPaperCode}
                    </option>
                  ))}
                </select>
              )}
              {hasThreshold ? (
                <select value={filterXepLoai} onChange={e => setFilterXepLoai(e.target.value)}
                  className="border rounded-lg px-3 py-2 text-sm text-gray-600 bg-white">
                  <option value="">Tất cả kết quả</option>
                  <option value="Đạt">Đạt (≥ {selectedKyThi.minPassQuestions} câu)</option>
                  <option value="Không đạt">Không đạt (&lt; {selectedKyThi.minPassQuestions} câu)</option>
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
              <div className="bg-white rounded-xl border overflow-x-auto">
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
                      <th className="text-center px-4 py-3 font-medium text-gray-600 whitespace-nowrap">Công bố điểm</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y">
                    {filtered.length === 0 ? (
                      <tr><td colSpan={7} className="text-center py-12 text-gray-400">Chưa có kết quả</td></tr>
                    ) : filtered.map(({ userKey, attempts, selectedAttempt: r }) => (
                      <tr key={r.examSubmissionId} className="hover:bg-gray-50">
                        <td className="px-4 py-3">
                          <p className="font-medium text-gray-800">{r.fullName || r.username || '—'}</p>
                          {r.employeeCode && <p className="text-xs text-gray-400">{r.employeeCode}</p>}
                          {r.department && <p className="text-xs text-blue-400">{r.department}</p>}
                        </td>
                        <td className="px-4 py-3">
                          <p className="text-gray-700">{r.examPaperName || r.examPaperCode || '—'}</p>
                        </td>
                        <td className="px-4 py-3 text-center">
                          <span className={`text-lg ${xepLoaiColor(r.totalScore)}`}>
                            {r.totalScore !== undefined ? r.totalScore.toFixed(1) : '—'}
                          </span>
                          {r.correctAnswers !== undefined && r.totalQuestions && (
                            <p className="text-xs text-gray-400">{r.correctAnswers}/{r.totalQuestions} câu</p>
                          )}
                        </td>
                        <td className="px-4 py-3 text-center font-medium">
                          {r.totalScore !== undefined ? (
                            hasThreshold ? (
                              (r.correctAnswers ?? 0) >= selectedKyThi.minPassQuestions! ? (
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
                                r.totalScore >= 9 ? 'bg-emerald-100 text-emerald-700' :
                                r.totalScore >= 8 ? 'bg-blue-100 text-blue-700' :
                                r.totalScore >= 6.5 ? 'bg-indigo-100 text-indigo-700' :
                                r.totalScore >= 5 ? 'bg-yellow-100 text-yellow-700' :
                                'bg-red-100 text-red-700'
                              }`}>{getClassification(r.totalScore)}</span>
                            )
                          ) : <span className="text-gray-400">—</span>}
                        </td>
                        <td className="px-4 py-3 text-center">
                          {(r.warningCount ?? 0) > 0 || (r.cheatingCount ?? 0) > 0 ? (
                            <div>
                              <span className={r.warningCount ? "text-red-600 font-semibold" : "text-gray-500 font-medium"}>
                                {r.warningCount ?? 0} lần
                              </span>
                              {(r.cheatingCount ?? 0) > (r.warningCount ?? 0) && (
                                <p className="text-xs text-gray-400">tổng: {r.cheatingCount}</p>
                              )}
                            </div>
                          ) : <span className="text-gray-400">0 lần</span>}
                        </td>
                        <td className="px-4 py-3 text-center">
                          {attempts.length > 1 ? (
                            <select
                              className="h-8 rounded-lg border border-gray-200 bg-gray-50/50 hover:bg-white hover:border-blue-400 px-2.5 py-1 text-xs font-semibold text-gray-700 shadow-sm transition-all duration-200 cursor-pointer focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500"
                              value={r.examSubmissionId}
                              onChange={(e) => {
                                const examSubmissionId = Number(e.target.value)
                                setSelectedexamSubmissionIdByUser((prev) => ({
                                  ...prev,
                                  [userKey]: examSubmissionId,
                                }))
                              }}
                            >
                              {attempts.map((attempt, index) => (
                                <option key={attempt.examSubmissionId} value={attempt.examSubmissionId}>
                                  Lần {index + 1}
                                </option>
                              ))}
                            </select>
                          ) : (
                            <span className="inline-flex items-center px-2 py-0.5 rounded bg-gray-100 text-xs font-semibold text-gray-600 border border-gray-200">
                              Lần 1
                            </span>
                          )}
                        </td>
                        <td className="px-4 py-3 text-center">
                          <button
                            className={`inline-flex items-center gap-1 text-xs px-2 py-1 rounded-full border transition-colors ${
                              r.examPaperId && visibility[r.examPaperId]
                                ? 'bg-green-50 text-green-700 border-green-200'
                                : 'bg-gray-50 text-gray-500 border-gray-200'
                            }`}
                            disabled={togglingId === r.examPaperId}
                            onClick={() => r.examPaperId && handleToggleVisibility(r.examPaperId, visibility[r.examPaperId] ?? false)}
                          >
                            {r.examPaperId && visibility[r.examPaperId]
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

      {/* Export Selection Modal */}
      {isExportModalOpen && selectedKyThi && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-sm animate-in fade-in duration-200">
          <div className="bg-white rounded-xl shadow-xl border max-w-md w-full overflow-hidden transform transition-all animate-in zoom-in-95 duration-200">
            <div className="px-6 py-4 border-b flex justify-between items-center bg-gray-50/50">
              <h3 className="font-bold text-gray-900 text-lg flex items-center gap-2">
                <Download className="h-5 w-5 text-blue-500" /> Xuất kết quả ra Excel
              </h3>
              <button
                onClick={() => setIsExportModalOpen(false)}
                className="text-gray-400 hover:text-gray-600 transition-colors p-1 rounded-lg hover:bg-gray-100"
              >
                <svg className="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>
            
            <div className="p-6 space-y-4">
              <p className="text-sm text-gray-500">
                Chọn đề thi bạn muốn xuất kết quả Excel cho kỳ thi <span className="font-semibold text-gray-700">"{selectedKyThi.campaignName}"</span>.
              </p>
              
              <div className="space-y-1.5">
                <label className="text-xs font-semibold text-gray-500 uppercase tracking-wider">Chọn đề thi</label>
                <select
                  value={selectedExportexamPaperId || ''}
                  onChange={(e) => {
                    const val = e.target.value ? Number(e.target.value) : null
                    setSelectedExportexamPaperId(val)
                  }}
                  className="w-full border rounded-lg px-3 py-2 text-sm text-gray-700 bg-white focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 focus:outline-none transition-all"
                >
                  <option value="">Tất cả đề thi ({uniqueDeThis.length})</option>
                  {uniqueDeThis.map((d) => (
                    <option key={d.id} value={d.id}>
                      {d.examPaperName || d.examPaperCode}
                    </option>
                  ))}
                </select>
              </div>
            </div>
            
            <div className="px-6 py-4 bg-gray-50 flex justify-end gap-3 border-t">
              <Button
                variant="outline"
                onClick={() => setIsExportModalOpen(false)}
              >
                Hủy
              </Button>
              <Button
                onClick={() => {
                  handleExportResults(selectedExportexamPaperId)
                  setIsExportModalOpen(false)
                }}
                className="bg-blue-600 text-white hover:bg-blue-700"
              >
                Xuất Excel
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
