import { useEffect, useState, useCallback, useRef } from 'react'
import { useAppDispatch, useAppSelector } from '@/app/hooks'
import { fetchQuestions, fetchQuestionTypes, setFilter } from '../slice'
import { questionsApi } from '../api'
import { departmentApi } from '@/features/departments/api'
import type { DepartmentDto } from '@/features/departments/types'
import { QuestionFilter } from '../components/question-filter'
import { QuestionTable } from '../components/question-table'
import { QuestionFormDialog } from '../components/question-form-dialog'
import { ImportExcelDialog } from '../components/import-excel-dialog'
import type { CauhoiDto, QuestionFilterDto } from '../types'
import type { CreateQuestionFormData } from '../schemas'
import { ROLES } from '@/lib/constants'
import { kyThiApi } from '@/features/ky-thi/api'
import { examsApiExtended } from '@/features/exams/api'
import type { KyThiDto } from '@/features/ky-thi/types'
import type { DethiDto } from '@/features/exams/types'

export function QuestionsPage() {
  const dispatch = useAppDispatch()
  const { questions, pagination, questionTypes, isLoading, filter } =
    useAppSelector((state) => state.questions)
  const currentUser = useAppSelector((state) => state.auth.user)

  const isDeptManager = currentUser?.role === ROLES.DEPT_MANAGER || currentUser?.tenVaiTro === 'DeptManager'
  const isAdmin = !isDeptManager
  // Khoa của DeptManager: ưu tiên tenKhoaQuanLy, fallback khoaPhong
  const myKhoa = currentUser?.tenKhoaQuanLy || currentUser?.khoaPhong || null

  const [khoaList, setKhoaList] = useState<string[]>([])
  const [kyThiList, setKyThiList] = useState<KyThiDto[]>([])
  const [deThiList, setDeThiList] = useState<DethiDto[]>([])
  const [formOpen, setFormOpen] = useState(false)
  const [importOpen, setImportOpen] = useState(false)
  const [editingQuestion, setEditingQuestion] = useState<CauhoiDto | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const [toast, setToast] = useState<{ msg: string; ok: boolean } | null>(null)

  // BUG FIX #3: filterRef luôn giữ giá trị filter mới nhất,
  // tránh stale closure trong onSuccess của ImportExcelDialog.
  const filterRef = useRef(filter)
  useEffect(() => { filterRef.current = filter }, [filter])

  useEffect(() => {
    dispatch(fetchQuestionTypes())
    
    // Fetch all active departments to populate the department filters dynamically
    if (isAdmin) {
      departmentApi.getAll({ trangThai: true, pageSize: 100 })
        .then((res) => {
          const list = (res.data?.data as DepartmentDto[] | undefined)?.map((d) => d.tenKhoa).filter(Boolean) as string[]
          setKhoaList(Array.from(new Set(list)).sort())
        })
        .catch(() => {})
    }

    // Fetch exams and tests
    kyThiApi.getAll()
      .then((res) => {
        if (res.data?.success && res.data.data) {
          setKyThiList(res.data.data)
        }
      })
      .catch(() => {})

    examsApiExtended.getAll()
      .then((res) => {
        if (res.data?.success && res.data.data) {
          setDeThiList(res.data.data)
        }
      })
      .catch(() => {})
  }, [dispatch, isAdmin])

  useEffect(() => {
    // Nếu là DeptManager, bắt buộc filter theo khoa của họ
    if (isDeptManager && myKhoa) {
      dispatch(setFilter({ khoaPhong: myKhoa, pageNumber: 1 }))
    }
  }, [isDeptManager, myKhoa]) // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    dispatch(fetchQuestions(filter))
  }, [dispatch, filter])

  const showToast = (msg: string, ok = true) => {
    setToast({ msg, ok })
    setTimeout(() => setToast(null), 3500)
  }

  const handleFilterChange = useCallback(
    (changes: Partial<QuestionFilterDto>) => {
      // DeptManager không được thay đổi khoaPhong
      if (isDeptManager && myKhoa && 'khoaPhong' in changes) {
        changes = { ...changes, khoaPhong: myKhoa }
      }
      // Reset deThiId if kyThiId is explicitly changed/cleared
      if ('kyThiId' in changes) {
        changes.deThiId = undefined
      }
      dispatch(setFilter(changes))
    },
    [dispatch, isDeptManager, myKhoa]
  )

  const handleCreate = () => {
    setEditingQuestion(null)
    setFormOpen(true)
  }

  const handleEdit = (question: CauhoiDto) => {
    setEditingQuestion(question)
    setFormOpen(true)
  }

  const handleDelete = async (question: CauhoiDto) => {
    const preview = (question.noiDung || '').slice(0, 60)
    if (!window.confirm(`Xóa câu hỏi:\n"${preview}..."\n\nHành động này không thể hoàn tác.`)) return
    try {
      await questionsApi.delete(question.id)
      showToast('Đã xóa câu hỏi thành công')
      dispatch(fetchQuestions(filter))
    } catch {
      showToast('Không thể xóa câu hỏi này', false)
    }
  }

  const handleFormSubmit = async (data: CreateQuestionFormData) => {
    setSubmitting(true)
    try {
      // DeptManager: tự động gán khoa của mình vào câu hỏi
      const submitData = isDeptManager && myKhoa
        ? { ...data, khoaPhong: myKhoa }
        : data

      if (editingQuestion) {
        await questionsApi.update(editingQuestion.id, { ...submitData, id: editingQuestion.id })
        showToast('Đã cập nhật câu hỏi')
      } else {
        await questionsApi.create(submitData)
        showToast('Đã tạo câu hỏi mới')
      }
      setFormOpen(false)
      dispatch(fetchQuestions(filter))
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message
        || 'Có lỗi khi lưu câu hỏi'
      showToast(msg, false)
    } finally {
      setSubmitting(false)
    }
  }

  const handlePageChange = (page: number) => {
    dispatch(setFilter({ pageNumber: page }))
  }

  return (
    <div>
      {/* Toast */}
      {toast && (
        <div className={`fixed top-4 right-4 z-50 flex items-center gap-2 px-4 py-3 rounded-xl shadow-lg text-sm font-medium transition-all ${toast.ok ? 'bg-green-600 text-white' : 'bg-red-600 text-white'}`}>
          {toast.ok ? '✓' : '⚠'} {toast.msg}
        </div>
      )}

      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Ngân hàng câu hỏi</h1>
          {isDeptManager && myKhoa && (
            <p className="text-sm text-blue-600 mt-0.5">
              📚 Khoa: <strong>{myKhoa}</strong> — Chỉ hiển thị câu hỏi của khoa bạn
            </p>
          )}
          {pagination && (
            <p className="text-sm text-gray-400 mt-0.5">
              {pagination.totalRecords} câu hỏi · Trang {pagination.pageNumber}/{pagination.totalPages}
            </p>
          )}
        </div>
      </div>

      <QuestionFilter
        filter={filter}
        questionTypes={questionTypes}
        onFilterChange={handleFilterChange}
        onCreateClick={handleCreate}
        onImportClick={() => setImportOpen(true)}
        hideKhoaFilter={isDeptManager}
        khoaList={khoaList}
        kyThiList={kyThiList}
        deThiList={deThiList}
      />

      <QuestionTable
        questions={questions}
        pagination={pagination}
        isLoading={isLoading}
        onEdit={handleEdit}
        onDelete={handleDelete}
        onPageChange={handlePageChange}
      />

      <QuestionFormDialog
        open={formOpen}
        question={editingQuestion}
        questionTypes={questionTypes}
        onClose={() => setFormOpen(false)}
        onSubmit={handleFormSubmit}
        isLoading={submitting}
        defaultKhoaPhong={isDeptManager && myKhoa ? myKhoa : undefined}
      />

      <ImportExcelDialog
        open={importOpen}
        onClose={() => setImportOpen(false)}
        onSuccess={() => {
          dispatch(fetchQuestions(filterRef.current))
          showToast('Nhập câu hỏi thành công')
        }}
      />
    </div>
  )
}
