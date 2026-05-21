import { useEffect, useState } from 'react'
import { useAppDispatch, useAppSelector } from '@/app/hooks'
import { fetchActiveExams } from '@/features/exams/slice'
import { gradingApi } from '../api'
import { ExamResultsTable } from '../components/exam-results-table'
import { ResultDetailDialog } from '../components/result-detail-dialog'
import type { ExamResultDetailDto } from '../types'
import { Button } from '@/components/ui/button'
import { Download, Wand2 } from 'lucide-react'

export function GradingPage() {
  const dispatch = useAppDispatch()
  const { exams } = useAppSelector((state) => state.exams)

  const [selectedExamId, setSelectedExamId] = useState<number | null>(null)
  const [results, setResults] = useState<ExamResultDetailDto[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [detailBaiThiId, setDetailBaiThiId] = useState<number | null>(null)

  useEffect(() => {
    dispatch(fetchActiveExams())
  }, [dispatch])

  useEffect(() => {
    if (selectedExamId) {
      loadResults(selectedExamId)
    }
  }, [selectedExamId])

  const loadResults = async (examId: number) => {
    setIsLoading(true)
    try {
      const response = await gradingApi.getResultsByExam(examId)
      if (response.data.success && response.data.data) {
        setResults(response.data.data)
      }
    } catch {
      // handle error
    } finally {
      setIsLoading(false)
    }
  }

  const handleRegrade = async (baiThiId: number) => {
    if (!window.confirm('Chấm lại bài thi này?')) return
    try {
      await gradingApi.regrade(baiThiId)
      if (selectedExamId) loadResults(selectedExamId)
    } catch {
      // handle error
    }
  }

  const handleAutoGradeAll = async () => {
    if (!window.confirm('Tự động chấm tất cả bài thi chưa chấm?')) return
    try {
      const response = await gradingApi.autoGradeAll()
      if (response.data.success) {
        alert(`Đã chấm ${response.data.data} bài thi`)
        if (selectedExamId) loadResults(selectedExamId)
      }
    } catch {
      // handle error
    }
  }

  const handleExportResults = async () => {
    if (!selectedExamId) return
    try {
      const response = await gradingApi.exportResults(selectedExamId)
      const url = window.URL.createObjectURL(new Blob([response.data]))
      const link = document.createElement('a')
      link.href = url
      link.download = `KetQua_DeThi_${selectedExamId}.xlsx`
      link.click()
      window.URL.revokeObjectURL(url)
    } catch {
      // handle error
    }
  }

  const handleExportRanking = async () => {
    if (!selectedExamId) return
    try {
      const response = await gradingApi.exportRanking(selectedExamId)
      const url = window.URL.createObjectURL(new Blob([response.data]))
      const link = document.createElement('a')
      link.href = url
      link.download = `BangXepHang_DeThi_${selectedExamId}.xlsx`
      link.click()
      window.URL.revokeObjectURL(url)
    } catch {
      // handle error
    }
  }

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Kết quả & Chấm điểm</h1>
        <Button variant="outline" onClick={handleAutoGradeAll}>
          <Wand2 className="h-4 w-4 mr-1" />
          Auto-grade tất cả
        </Button>
      </div>

      {/* Exam selector */}
      <div className="flex flex-wrap items-center gap-3 mb-4">
        <select
          className="h-10 rounded-md border border-input bg-background px-3 text-sm min-w-[300px]"
          value={selectedExamId ?? ''}
          onChange={(e) => setSelectedExamId(e.target.value ? Number(e.target.value) : null)}
        >
          <option value="">-- Chọn đề thi --</option>
          {exams.map((exam) => (
            <option key={exam.id} value={exam.id}>
              {exam.maDeThi} - {exam.tenDeThi}
            </option>
          ))}
        </select>

        {selectedExamId && (
          <div className="flex gap-2 ml-auto">
            <Button variant="outline" size="sm" onClick={handleExportResults}>
              <Download className="h-4 w-4 mr-1" />
              Export kết quả
            </Button>
            <Button variant="outline" size="sm" onClick={handleExportRanking}>
              <Download className="h-4 w-4 mr-1" />
              Export xếp hạng
            </Button>
          </div>
        )}
      </div>

      {/* Results table */}
      {selectedExamId ? (
        <ExamResultsTable
          results={results}
          isLoading={isLoading}
          onViewDetail={(r) => setDetailBaiThiId(r.baiThiId)}
          onRegrade={handleRegrade}
        />
      ) : (
        <div className="text-center py-12 text-gray-500">
          Vui lòng chọn đề thi để xem kết quả
        </div>
      )}

      {/* Detail dialog */}
      <ResultDetailDialog
        open={!!detailBaiThiId}
        baiThiId={detailBaiThiId}
        onClose={() => setDetailBaiThiId(null)}
      />
    </div>
  )
}
