import { useEffect, useState } from 'react'
import { useAppDispatch, useAppSelector } from '@/app/hooks'
import { fetchActiveExams } from '../slice'
import { examsApi } from '../api'
import { ExamTable } from '../components/exam-table'
import { ExamFormDialog } from '../components/exam-form-dialog'
import { AssignUsersDialog } from '../components/assign-users-dialog'
import type { DethiDto } from '../types'
import type { CreateExamFormData } from '../schemas'
import { Button } from '@/components/ui/button'
import { Plus } from 'lucide-react'

export function ExamsPage() {
  const dispatch = useAppDispatch()
  const { exams, isLoading } = useAppSelector((state) => state.exams)

  const [formOpen, setFormOpen] = useState(false)
  const [assignOpen, setAssignOpen] = useState(false)
  const [selectedExam, setSelectedExam] = useState<DethiDto | null>(null)
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    dispatch(fetchActiveExams())
  }, [dispatch])

  const handleCreateExam = async (data: CreateExamFormData) => {
    setSubmitting(true)
    try {
      await examsApi.create(data)
      setFormOpen(false)
      dispatch(fetchActiveExams())
    } catch {
      // handle error
    } finally {
      setSubmitting(false)
    }
  }

  const handleViewAssignments = (exam: DethiDto) => {
    setSelectedExam(exam)
    setAssignOpen(true)
  }

  const handleAssignUsers = async (examId: number, userIds: number[], note?: string) => {
    setSubmitting(true)
    try {
      await examsApi.assignUsers({ examId, userIds, note })
      setAssignOpen(false)
    } catch {
      // handle error
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Quản lý đề thi</h1>
        <Button onClick={() => setFormOpen(true)}>
          <Plus className="h-4 w-4 mr-1" />
          Tạo đề thi
        </Button>
      </div>

      <ExamTable
        exams={exams}
        isLoading={isLoading}
        onViewAssignments={handleViewAssignments}
      />

      <ExamFormDialog
        open={formOpen}
        onClose={() => setFormOpen(false)}
        onSubmit={handleCreateExam}
        isLoading={submitting}
      />

      <AssignUsersDialog
        open={assignOpen}
        exam={selectedExam}
        onClose={() => setAssignOpen(false)}
        onSubmit={handleAssignUsers}
        isLoading={submitting}
      />
    </div>
  )
}
