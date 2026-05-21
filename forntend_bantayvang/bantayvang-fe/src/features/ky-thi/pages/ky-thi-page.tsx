import { useEffect, useState } from 'react'
import { kyThiApi } from '../api'
import { KyThiTable } from '../components/ky-thi-table'
import { KyThiFormDialog } from '../components/ky-thi-form-dialog'
import { CaThiList } from '../components/ca-thi-list'
import { CaThiFormDialog } from '../components/ca-thi-form-dialog'
import type { KyThiDto, CaThiDto, CreateKyThiDto } from '../types'
import type { CreateKyThiFormData, CreateCaThiFormData } from '../schemas'
import { Button } from '@/components/ui/button'
import { Plus, ArrowLeft } from 'lucide-react'
import { useAppDispatch } from '@/app/hooks'
import { fetchActiveExams } from '@/features/exams/slice'

export function KyThiPage() {
  const dispatch = useAppDispatch()
  const [kyThis, setKyThis] = useState<KyThiDto[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [formOpen, setFormOpen] = useState(false)
  const [editingKyThi, setEditingKyThi] = useState<KyThiDto | null>(null)
  const [submitting, setSubmitting] = useState(false)

  // Detail view
  const [selectedKyThi, setSelectedKyThi] = useState<KyThiDto | null>(null)
  const [caThis, setCaThis] = useState<CaThiDto[]>([])
  const [caThiFormOpen, setCaThiFormOpen] = useState(false)

  useEffect(() => {
    loadKyThis()
    dispatch(fetchActiveExams())
  }, [dispatch])

  const loadKyThis = async () => {
    setIsLoading(true)
    try {
      const response = await kyThiApi.getAll()
      if (response.data.success && response.data.data) {
        setKyThis(response.data.data)
      }
    } catch {
      // handle error
    } finally {
      setIsLoading(false)
    }
  }

  const loadCaThis = async (kyThiId: number) => {
    try {
      const response = await kyThiApi.getCaThi(kyThiId)
      if (response.data.success && response.data.data) {
        setCaThis(response.data.data)
      }
    } catch {
      // handle error
    }
  }

  const handleCreateKyThi = async (data: CreateKyThiFormData) => {
    setSubmitting(true)
    try {
      if (editingKyThi) {
        await kyThiApi.update(editingKyThi.id, { ...data, trangThai: editingKyThi.trangThai || 'DangChuanBi' })
      } else {
        await kyThiApi.create(data as CreateKyThiDto)
      }
      setFormOpen(false)
      setEditingKyThi(null)
      loadKyThis()
    } catch {
      // handle error
    } finally {
      setSubmitting(false)
    }
  }

  const handleDeleteKyThi = async (kyThi: KyThiDto) => {
    if (!window.confirm(`Xóa kỳ thi "${kyThi.tenKyThi}"?`)) return
    try {
      await kyThiApi.delete(kyThi.id)
      loadKyThis()
    } catch {
      // handle error
    }
  }

  const handleViewKyThi = (kyThi: KyThiDto) => {
    setSelectedKyThi(kyThi)
    loadCaThis(kyThi.id)
  }

  const handleCreateCaThi = async (data: CreateCaThiFormData) => {
    setSubmitting(true)
    try {
      await kyThiApi.createCaThi(data)
      setCaThiFormOpen(false)
      if (selectedKyThi) loadCaThis(selectedKyThi.id)
    } catch {
      // handle error
    } finally {
      setSubmitting(false)
    }
  }

  const handleDeleteCaThi = async (caThi: CaThiDto) => {
    if (!window.confirm(`Xóa ca thi "${caThi.tenCa}"?`)) return
    try {
      await kyThiApi.deleteCaThi(caThi.id)
      if (selectedKyThi) loadCaThis(selectedKyThi.id)
    } catch {
      // handle error
    }
  }

  // Detail view
  if (selectedKyThi) {
    return (
      <div>
        <div className="flex items-center gap-3 mb-6">
          <Button variant="ghost" size="icon" onClick={() => setSelectedKyThi(null)}>
            <ArrowLeft className="h-4 w-4" />
          </Button>
          <div>
            <h1 className="text-2xl font-bold text-gray-900">{selectedKyThi.tenKyThi}</h1>
            <p className="text-sm text-gray-500">{selectedKyThi.maKyThi} • {selectedKyThi.loaiKyThi || 'Chưa phân loại'}</p>
          </div>
        </div>

        <CaThiList
          caThis={caThis}
          onAdd={() => setCaThiFormOpen(true)}
          onDelete={handleDeleteCaThi}
        />

        <CaThiFormDialog
          open={caThiFormOpen}
          kyThiId={selectedKyThi.id}
          onClose={() => setCaThiFormOpen(false)}
          onSubmit={handleCreateCaThi}
          isLoading={submitting}
        />
      </div>
    )
  }

  // List view
  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Kỳ thi & Ca thi</h1>
        <Button onClick={() => { setEditingKyThi(null); setFormOpen(true) }}>
          <Plus className="h-4 w-4 mr-1" />
          Tạo kỳ thi
        </Button>
      </div>

      <KyThiTable
        kyThis={kyThis}
        isLoading={isLoading}
        onView={handleViewKyThi}
        onDelete={handleDeleteKyThi}
      />

      <KyThiFormDialog
        open={formOpen}
        kyThi={editingKyThi}
        onClose={() => setFormOpen(false)}
        onSubmit={handleCreateKyThi}
        isLoading={submitting}
      />
    </div>
  )
}
