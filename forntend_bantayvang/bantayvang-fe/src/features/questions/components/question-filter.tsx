import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Search, Plus, Upload } from 'lucide-react'
import type { QuestionFilterDto, DanhmucauhoiDto, LoaicauhoiDto } from '../types'

interface QuestionFilterProps {
  filter: QuestionFilterDto
  categories: DanhmucauhoiDto[]
  questionTypes: LoaicauhoiDto[]
  onFilterChange: (filter: Partial<QuestionFilterDto>) => void
  onCreateClick: () => void
  onImportClick: () => void
}

const DIFFICULTY = [
  { value: '', label: 'Tất cả độ khó' },
  { value: 'De', label: 'Dễ' },
  { value: 'TrungBinh', label: 'Trung bình' },
  { value: 'Kho', label: 'Khó' },
]

export function QuestionFilter({
  filter,
  categories,
  questionTypes,
  onFilterChange,
  onCreateClick,
  onImportClick,
}: QuestionFilterProps) {
  return (
    <div className="flex flex-wrap items-center gap-3 mb-4">
      <div className="relative flex-1 min-w-[200px] max-w-sm">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400" />
        <Input
          placeholder="Tìm kiếm câu hỏi..."
          className="pl-9"
          value={filter.searchKeyword || ''}
          onChange={(e) => onFilterChange({ searchKeyword: e.target.value, pageNumber: 1 })}
        />
      </div>

      <select
        className="h-10 rounded-md border border-input bg-background px-3 text-sm"
        value={filter.idDanhMuc ?? ''}
        onChange={(e) =>
          onFilterChange({ idDanhMuc: e.target.value ? Number(e.target.value) : undefined, pageNumber: 1 })
        }
      >
        <option value="">Tất cả danh mục</option>
        {categories.map((c) => (
          <option key={c.id} value={c.id}>{c.tenDanhMuc}</option>
        ))}
      </select>

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

      <select
        className="h-10 rounded-md border border-input bg-background px-3 text-sm"
        value={filter.doKho ?? ''}
        onChange={(e) => onFilterChange({ doKho: e.target.value || undefined, pageNumber: 1 })}
      >
        {DIFFICULTY.map((d) => (
          <option key={d.value} value={d.value}>{d.label}</option>
        ))}
      </select>

      <div className="ml-auto flex gap-2">
        <Button variant="outline" onClick={onImportClick}>
          <Upload className="h-4 w-4 mr-1" />
          Import Excel
        </Button>
        <Button onClick={onCreateClick}>
          <Plus className="h-4 w-4 mr-1" />
          Thêm câu hỏi
        </Button>
      </div>
    </div>
  )
}
