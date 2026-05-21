import { useEffect, useState, useCallback } from 'react'
import { useAppDispatch, useAppSelector } from '@/app/hooks'
import { fetchQuestions, fetchCategories, fetchQuestionTypes, setFilter } from '../slice'
import { questionsApi } from '../api'
import { QuestionFilter } from '../components/question-filter'
import { QuestionTable } from '../components/question-table'
import { QuestionFormDialog } from '../components/question-form-dialog'
import { ImportExcelDialog } from '../components/import-excel-dialog'
import type { CauhoiDto, QuestionFilterDto } from '../types'
import type { CreateQuestionFormData } from '../schemas'

export function QuestionsPage() {
  const dispatch = useAppDispatch()
  const { questions, pagination, categories, questionTypes, isLoading, filter } =
    useAppSelector((state) => state.questions)

  const [formOpen, setFormOpen] = useState(false)
  const [importOpen, setImportOpen] = useState(false)
  const [editingQuestion, setEditingQuestion] = useState<CauhoiDto | null>(null)
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    dispatch(fetchCategories())
    dispatch(fetchQuestionTypes())
  }, [dispatch])

  useEffect(() => {
    dispatch(fetchQuestions(filter))
  }, [dispatch, filter])

  const handleFilterChange = useCallback(
    (changes: Partial<QuestionFilterDto>) => {
      dispatch(setFilter(changes))
    },
    [dispatch]
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
    if (!window.confirm(`Xóa câu hỏi "${(question.noiDung || '').slice(0, 50)}..."?`)) return
    try {
      await questionsApi.delete(question.id)
      dispatch(fetchQuestions(filter))
    } catch {
      // handle error
    }
  }

  const handleFormSubmit = async (data: CreateQuestionFormData) => {
    setSubmitting(true)
    try {
      if (editingQuestion) {
        await questionsApi.update(editingQuestion.id, { ...data, id: editingQuestion.id })
      } else {
        await questionsApi.create(data)
      }
      setFormOpen(false)
      dispatch(fetchQuestions(filter))
    } catch {
      // handle error
    } finally {
      setSubmitting(false)
    }
  }

  const handlePageChange = (page: number) => {
    dispatch(setFilter({ pageNumber: page }))
  }

  return (
    <div>
      <h1 className="text-2xl font-bold text-gray-900 mb-6">Ngân hàng câu hỏi</h1>

      <QuestionFilter
        filter={filter}
        categories={categories}
        questionTypes={questionTypes}
        onFilterChange={handleFilterChange}
        onCreateClick={handleCreate}
        onImportClick={() => setImportOpen(true)}
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
        categories={categories}
        questionTypes={questionTypes}
        onClose={() => setFormOpen(false)}
        onSubmit={handleFormSubmit}
        isLoading={submitting}
      />

      <ImportExcelDialog
        open={importOpen}
        onClose={() => setImportOpen(false)}
        onSuccess={() => dispatch(fetchQuestions(filter))}
      />
    </div>
  )
}
