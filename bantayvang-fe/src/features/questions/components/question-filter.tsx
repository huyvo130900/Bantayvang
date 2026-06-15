import { useState, useRef, useEffect } from 'react'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Search, Plus, Upload, Building2, ChevronDown, Check } from 'lucide-react'
import type { QuestionFilterDto, LoaicauhoiDto } from '../types'

interface QuestionFilterProps {
  filter: QuestionFilterDto
  questionTypes: LoaicauhoiDto[]
  onFilterChange: (filter: Partial<QuestionFilterDto>) => void
  onCreateClick: () => void
  onImportClick: () => void
  hideKhoaFilter?: boolean
  // Admin truyền vào danh sách khoa động (lấy từ dữ liệu thực tế)
  khoaList?: string[]
}

export function QuestionFilter({
  filter,
  questionTypes,
  onFilterChange,
  onCreateClick,
  onImportClick,
  hideKhoaFilter = false,
  khoaList = [],
}: QuestionFilterProps) {
  const [isOpen, setIsOpen] = useState(false)
  const [searchQuery, setSearchQuery] = useState('')
  const dropdownRef = useRef<HTMLDivElement>(null)

  // Đóng dropdown khi click ra ngoài
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsOpen(false)
      }
    }
    document.addEventListener('mousedown', handleClickOutside)
    return () => {
      document.removeEventListener('mousedown', handleClickOutside)
    }
  }, [])

  const filteredKhoa = khoaList.filter((khoa) =>
    khoa.toLowerCase().includes(searchQuery.toLowerCase())
  )

  const selectedKhoa = filter.khoaPhong

  return (
    <div className="space-y-3 mb-4">
      {/* Dòng filter + nút tạo/import */}
      <div className="flex flex-wrap items-center gap-3">
        {/* Tìm kiếm */}
        <div className="relative flex-1 min-w-[200px] max-w-sm">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400" />
          <Input
            placeholder="Tìm kiếm câu hỏi..."
            className="pl-9"
            value={filter.searchKeyword || ''}
            onChange={(e) => onFilterChange({ searchKeyword: e.target.value, pageNumber: 1 })}
          />
        </div>

        {/* Lọc theo khoa (dropdown search) */}
        {!hideKhoaFilter && khoaList.length > 0 && (
          <div className="relative" ref={dropdownRef}>
            <button
              type="button"
              onClick={() => {
                setIsOpen(!isOpen)
                setSearchQuery('')
              }}
              className="h-10 px-3 py-2 flex items-center justify-between gap-2 rounded-md border border-input bg-background text-sm text-gray-700 hover:bg-accent hover:text-accent-foreground min-w-[180px] max-w-[220px] text-left transition-all"
            >
              <div className="flex items-center gap-2 truncate">
                <Building2 className="h-4 w-4 text-gray-400 shrink-0" />
                <span className="truncate">{selectedKhoa || 'Tất cả khoa'}</span>
              </div>
              <ChevronDown className="h-4 w-4 text-gray-400 shrink-0" />
            </button>

            {isOpen && (
              <div className="absolute left-0 mt-1 z-50 w-64 bg-white rounded-md border shadow-lg max-h-60 overflow-hidden flex flex-col">
                <div className="p-2 border-b">
                  <div className="relative">
                    <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-gray-400" />
                    <input
                      type="text"
                      placeholder="Tìm khoa..."
                      className="w-full h-8 pl-8 pr-2 rounded-sm border border-input bg-background text-xs focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
                      value={searchQuery}
                      onChange={(e) => setSearchQuery(e.target.value)}
                      autoFocus
                    />
                  </div>
                </div>

                <div className="overflow-y-auto flex-1 py-1 max-h-48 text-sm">
                  {/* Option: Tất cả khoa */}
                  <button
                    type="button"
                    onClick={() => {
                      onFilterChange({ khoaPhong: undefined, showDuplicatesOnly: false, pageNumber: 1 })
                      setIsOpen(false)
                    }}
                    className={`w-full px-3 py-1.5 flex items-center justify-between text-left hover:bg-gray-100 transition-colors ${
                      !selectedKhoa ? 'bg-blue-50 text-blue-600 font-medium' : 'text-gray-700'
                    }`}
                  >
                    <span>Tất cả khoa</span>
                    {!selectedKhoa && <Check className="h-4 w-4 text-blue-600" />}
                  </button>

                  {/* Options: Danh sách khoa đã lọc */}
                  {filteredKhoa.map((khoa) => (
                    <button
                      key={khoa}
                      type="button"
                      onClick={() => {
                        onFilterChange({ khoaPhong: khoa, pageNumber: 1 })
                        setIsOpen(false)
                      }}
                      className={`w-full px-3 py-1.5 flex items-center justify-between text-left hover:bg-gray-100 transition-colors truncate ${
                        selectedKhoa === khoa ? 'bg-blue-50 text-blue-600 font-medium' : 'text-gray-700'
                      }`}
                    >
                      <span className="truncate">{khoa}</span>
                      {selectedKhoa === khoa && <Check className="h-4 w-4 text-blue-600 shrink-0" />}
                    </button>
                  ))}

                  {filteredKhoa.length === 0 && (
                    <div className="px-3 py-2 text-xs text-gray-400 text-center">
                      Không tìm thấy khoa phù hợp
                    </div>
                  )}
                </div>
              </div>
            )}
          </div>
        )}

        {/* Lọc theo loại câu hỏi */}
        <select
          className="h-10 rounded-md border border-input bg-background px-3 text-sm"
          value={filter.idLoaiCauHoi ?? ''}
          onChange={(e) =>
            onFilterChange({ idLoaiCauHoi: e.target.value ? Number(e.target.value) : undefined, pageNumber: 1 })
          }
        >
          <option value="">Tất cả loại</option>
          {questionTypes.map((t) => (
            <option key={t.id} value={t.id}>{t.tenLoai}</option>
          ))}
        </select>

        {/* Lọc theo mức độ khó */}
        <select
          className="h-10 rounded-md border border-input bg-background px-3 text-sm"
          value={filter.doKho ?? ''}
          onChange={(e) =>
            onFilterChange({ doKho: e.target.value || undefined, pageNumber: 1 })
          }
        >
          <option value="">Tất cả mức độ</option>
          <option value="Dễ">Dễ</option>
          <option value="Trung bình">Trung bình</option>
          <option value="Khó">Khó</option>
        </select>

        {/* Lọc trùng lặp - Chỉ hiển thị khi chọn 1 khoa nhất định */}
        {!!filter.khoaPhong && (
          <label className="flex items-center gap-2 cursor-pointer text-sm text-gray-700 bg-gray-50/50 border px-3 h-10 rounded-md hover:bg-gray-100 transition-colors select-none">
            <input
              type="checkbox"
              className="rounded border-gray-300 text-blue-600 focus:ring-blue-500 h-4 w-4 cursor-pointer"
              checked={filter.showDuplicatesOnly || false}
              onChange={(e) => onFilterChange({ showDuplicatesOnly: e.target.checked, pageNumber: 1 })}
            />
            <span className="font-medium">Chỉ hiển thị trùng lặp</span>
          </label>
        )}

        <div className="ml-auto flex gap-2">
          <Button variant="outline" onClick={onImportClick}>
            <Upload className="h-4 w-4 mr-1" />
            Nhập từ Excel
          </Button>
          <Button onClick={onCreateClick}>
            <Plus className="h-4 w-4 mr-1" />
            Thêm câu hỏi
          </Button>
        </div>
      </div>
    </div>
  )
}
