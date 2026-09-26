import { useEffect, useState, useCallback } from 'react'
import { examsApiExtended } from '../api'
import type { ExamPaperDto, ExamPreviewDtoFE } from '../types'
import { Button } from '@/components/ui/button'
import { X, Printer, CheckCircle2, Clock, FileQuestion, RefreshCw } from 'lucide-react'

interface Props {
  exam: ExamPaperDto | null
  onClose: () => void
}

export function ExamPreviewModal({ exam, onClose }: Props) {
  const [preview, setPreview] = useState<ExamPreviewDtoFE | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [printStatus, setPrintStatus] = useState<'idle' | 'success' | 'error'>('idle')

  const loadPreview = useCallback(() => {
    if (!exam) return
    setLoading(true)
    setError(null)
    setPreview(null)
    examsApiExtended.preview(exam.id)
      .then(res => {
        if (res.data.success && res.data.data) setPreview(res.data.data)
        else setError(res.data.message || 'Không thể tải preview')
      })
      .catch(() => setError('Lỗi kết nối khi tải preview'))
      .finally(() => setLoading(false))
  }, [exam])

  useEffect(() => {
    loadPreview()
    setPrintStatus('idle')
  }, [loadPreview])

  if (!exam) return null

  const handleReload = () => {
    loadPreview()
  }

  const handlePrint = () => {
    try {
      const printWindow = window.open('', '_blank', 'width=900,height=700')
      if (!printWindow || !preview) {
        setPrintStatus('error')
        setTimeout(() => setPrintStatus('idle'), 3000)
        return
      }

      const totalQuestions = preview.questions.length
      const tenParts = (exam.examPaperName ?? '').split(' - ')
      const examName = tenParts[0] ?? ''
      const paperLabel = tenParts.slice(1).join(' - ') || (exam.examPaperCode ?? '')

      const html = `<!DOCTYPE html>
<html lang="vi">
<head>
  <meta charset="UTF-8" />
  <title> </title>
  <style>
    @page {
      size: A4;
      margin: 2cm 2cm 2cm 3cm;
    }
    * { box-sizing: border-box; }
    body {
      font-family: Arial, sans-serif;
      font-size: 13px;
      color: #111;
      margin: 0;
      padding: 0;
      position: relative;
      line-height: 1.5;
    }

    /* Print Header */
    .print-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: 24px;
      border-bottom: 2px solid #000;
      padding-bottom: 12px;
    }
    .hospital-brand {
      display: flex;
      align-items: center;
      gap: 12px;
      width: 48%;
    }
    .hospital-logo {
      width: 55px;
      height: 55px;
      object-fit: contain;
      flex-shrink: 0;
    }
    .hospital-title {
      text-align: left;
      line-height: 1.3;
    }
    .hospital-title .line-parent {
      font-size: 11px;
      font-weight: normal;
      text-transform: uppercase;
      color: #444;
      margin: 0;
    }
    .hospital-title .line-child {
      font-size: 13px;
      font-weight: bold;
      text-transform: uppercase;
      color: #000;
      margin: 0;
    }
    .exam-info {
      width: 48%;
      text-align: center;
      line-height: 1.4;
    }
    .exam-info h1 {
      font-size: 14px;
      font-weight: bold;
      margin: 0 0 4px;
      text-transform: uppercase;
    }
    .exam-info .meta {
      font-size: 11px;
      color: #333;
    }

    hr { border: none; border-top: 1px solid #bbb; margin: 20px 0 14px; }

    /* Candidate info – no border, no background */
    .candidate-info {
      display: flex;
      flex-wrap: wrap;
      gap: 8px 28px;
      margin: 0 0 28px;
      font-size: 13px;
    }
    .candidate-info .field {
      display: flex;
      align-items: center;
      gap: 6px;
    }
    .candidate-info .field label {
      font-weight: bold;
      white-space: nowrap;
    }
    .candidate-info .field .line {
      border-bottom: 1px solid #333;
      min-width: 130px;
      height: 18px;
      display: inline-block;
    }
    .score-field .line {
      min-width: 55px !important;
    }
    .score-suffix {
      font-weight: normal;
      white-space: nowrap;
    }

    /* Questions */
    .question { margin-bottom: 16px; page-break-inside: avoid; }
    .question p { margin: 0 0 5px; font-weight: bold; }
    .choice { margin-left: 22px; margin-bottom: 3px; }
    .essay-lines { margin-top: 6px; }
    .essay-line {
      border-bottom: 1px dotted #555;
      margin-bottom: 10px;
      height: 18px;
      letter-spacing: 3px;
      color: #888;
      font-size: 12px;
      padding-left: 4px;
    }
  </style>
</head>
<body>
  <!-- Header with logo and hospital details -->
  <div class="print-header">
    <div class="hospital-brand">
      <img src="${window.location.origin}/logoBVND2.png" alt="Logo" class="hospital-logo" />
      <div class="hospital-title">
        <div class="line-parent">SỞ Y TẾ TP. HỒ CHÍ MINH</div>
        <div class="line-child">BỆNH VIỆN NHI ĐỒNG 2</div>
      </div>
    </div>
    <div class="exam-info">
      <h1>${examName}</h1>
      <div class="meta">Mã đề: <strong>${paperLabel}</strong> &nbsp;|&nbsp; Thời gian: ${exam.durationMinutes} phút &nbsp;|&nbsp; Số câu: ${totalQuestions}</div>
    </div>
  </div>

  <!-- Candidate info fields (no border) -->
  <div class="candidate-info">
    <div class="field">
      <label>Khoa:</label>
      <span class="line" style="min-width:160px;"></span>
    </div>
    <div class="field">
      <label>Mã nhân viên:</label>
      <span class="line"></span>
    </div>
    <div class="field">
      <label>Họ và tên:</label>
      <span class="line" style="min-width:190px;"></span>
    </div>
    <div class="field score-field">
      <label>Điểm:</label>
      <span class="line"></span>
      <span class="score-suffix">/ ${totalQuestions}</span>
    </div>
  </div>

  <!-- Questions -->
  ${preview.questions.map((q, i) => `
    <div class="question">
      <p>Câu ${i + 1}: ${q.content ?? ''}</p>
      ${q.imageUrl ? `<img src="${q.imageUrl}" style="max-height: 200px; max-width: 100%; display: block; margin: 10px 0; border: 1px solid #ccc; border-radius: 4px;" />` : ''}
      ${q.questionOptions.length === 0
        ? `<div class="essay-lines">${Array(5).fill('<div class="essay-line"></div>').join('')}</div>`
        : q.questionOptions.map((c, ci) => `<div class="choice">${String.fromCharCode(65 + ci)}. ${c.content ?? ''}</div>`).join('')
      }
    </div>
  `).join('')}

  <script>window.onload = function(){ window.print(); }; window.onafterprint = function(){ window.close(); };<\/script>
</body>
</html>`

      printWindow.document.write(html)
      printWindow.document.close()
      setPrintStatus('success')
      setTimeout(() => setPrintStatus('idle'), 4000)
    } catch {
      setPrintStatus('error')
      setTimeout(() => setPrintStatus('idle'), 3000)
    }
  }

  return (
    <div className="fixed inset-0 z-50 bg-black/50 flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl shadow-2xl w-full max-w-3xl max-h-[90vh] flex flex-col">
        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b shrink-0">
          <div>
            <h2 className="text-lg font-bold text-gray-900">{exam.examPaperName}</h2>
            <div className="flex items-center gap-4 mt-1 text-xs text-gray-500">
              <span className="font-mono bg-gray-100 px-2 py-0.5 rounded">{exam.examPaperCode}</span>
              <span className="flex items-center gap-1"><Clock className="h-3 w-3" /> {exam.durationMinutes} phút</span>
              <span className="flex items-center gap-1"><FileQuestion className="h-3 w-3" /> {preview?.questions?.length ?? exam.totalQuestions} câu</span>
              {exam.department && <span className="text-blue-600">{exam.department}</span>}
            </div>
          </div>
          <div className="flex items-center gap-2">
            {/* Reload button */}
            <Button
              variant="outline"
              size="sm"
              onClick={handleReload}
              disabled={loading}
              title="Random lại bộ câu hỏi"
            >
              <RefreshCw className={`h-4 w-4 mr-1 ${loading ? 'animate-spin' : ''}`} />
              Reload
            </Button>

            {/* Print button */}
            <Button
              variant="outline"
              size="sm"
              onClick={handlePrint}
              disabled={loading || !preview}
              className={
                printStatus === 'success'
                  ? 'border-green-400 bg-green-50 text-green-700 hover:bg-green-100'
                  : printStatus === 'error'
                  ? 'border-red-400 bg-red-50 text-red-700'
                  : ''
              }
            >
              {printStatus === 'success' ? (
                <><CheckCircle2 className="h-4 w-4 mr-1 text-green-600" /> In thành công!</>
              ) : printStatus === 'error' ? (
                <><Printer className="h-4 w-4 mr-1" /> Lỗi in</>
              ) : (
                <><Printer className="h-4 w-4 mr-1" /> In đề</>
              )}
            </Button>

            <Button variant="ghost" size="icon" onClick={onClose}><X className="h-5 w-5" /></Button>
          </div>
        </div>

        {/* Print status banner */}
        {printStatus === 'success' && (
          <div className="px-6 py-2 bg-green-50 border-b border-green-200 text-sm text-green-700 flex items-center gap-2">
            <CheckCircle2 className="h-4 w-4 shrink-0" />
            Đã mở cửa sổ in thành công. Nếu không thấy, hãy cho phép pop-up của trình duyệt.
          </div>
        )}
        {printStatus === 'error' && (
          <div className="px-6 py-2 bg-red-50 border-b border-red-200 text-sm text-red-700">
            ⚠ Không thể mở cửa sổ in. Vui lòng cho phép pop-up và thử lại.
          </div>
        )}

        {/* Body */}
        <div className="overflow-y-auto flex-1 p-6">
          {loading && (
            <div className="text-center py-16 text-gray-400">
              <div className="animate-spin h-8 w-8 border-2 border-primary border-t-transparent rounded-full mx-auto mb-3" />
              Đang tải câu hỏi...
            </div>
          )}
          {error && <div className="text-center py-12 text-red-500">{error}</div>}
          {!loading && preview && (
            <div className="space-y-5">
              {preview.questions.length === 0 ? (
                <div className="text-center py-12 text-gray-400">
                  Đề thi này dùng ngân hàng câu hỏi random — câu hỏi sẽ được chọn ngẫu nhiên khi thí sinh bắt đầu thi.
                </div>
              ) : preview.questions.map((q, idx) => (
                <div key={q.id} className="border rounded-xl p-4">
                  <p className="font-medium text-gray-900 mb-3">
                    <span className="text-primary font-bold mr-2">Câu {idx + 1}.</span>
                    {q.content}
                  </p>
                  {q.imageUrl && (
                    <img src={q.imageUrl} alt="Minh họa câu hỏi" className="max-h-48 rounded-md border object-contain mb-3" />
                  )}
                  {q.chuDe && (
                    <span className="text-xs bg-blue-50 text-blue-600 px-2 py-0.5 rounded-full mb-2 inline-block">{q.chuDe}</span>
                  )}
                  <div className="space-y-1.5 mt-2">
                    {q.questionOptions.map((c, ci) => (
                      <div key={c.id} className={`flex items-center gap-2 p-2 rounded-lg text-sm ${
                        c.isCorrect ? 'bg-green-50 border border-green-200' : 'bg-gray-50'
                      }`}>
                        <span className={`w-6 h-6 rounded-full flex items-center justify-center text-xs font-bold shrink-0 ${
                          c.isCorrect ? 'bg-green-500 text-white' : 'bg-gray-200 text-gray-600'
                        }`}>
                          {String.fromCharCode(65 + ci)}
                        </span>
                        <span className={c.isCorrect ? 'text-green-800 font-medium' : 'text-gray-700'}>
                          {c.content}
                        </span>
                        {c.isCorrect && <CheckCircle2 className="h-4 w-4 text-green-500 ml-auto shrink-0" />}
                      </div>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="px-6 py-3 border-t bg-gray-50 text-xs text-gray-400 shrink-0 flex items-center justify-between">
          <span>Đáp án đúng được đánh dấu màu xanh lá · Chỉ hiển thị cho Admin và Quản lý Khoa</span>
          {preview && <span>{preview.questions.length} câu hỏi</span>}
        </div>
      </div>
    </div>
  )
}
