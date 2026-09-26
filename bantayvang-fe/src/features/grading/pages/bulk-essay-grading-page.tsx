import { useEffect, useState, useRef } from 'react'
import { useSearchParams } from 'react-router-dom'
import { gradingApi } from '../api'
import { Button } from '@/components/ui/button'
import { MessageSquare, Loader2, Save, Sparkles } from 'lucide-react'

import type { BulkEssayItem } from '../types'
export function BulkEssayGradingPage() {
  const [searchParams] = useSearchParams()
  const examCampaignId = searchParams.get('examCampaignId')
  const examId = searchParams.get('examId')
  const [ungradedOnly, setUngradedOnly] = useState(true)
  const [autoFinalize, setAutoFinalize] = useState(() => {
    try {
      return localStorage.getItem('aiGrading_autoFinalize') !== 'false'
    } catch {
      return true
    }
  })

  function handleAutoFinalizeChange(value: boolean) {
    setAutoFinalize(value)
    try {
      localStorage.setItem('aiGrading_autoFinalize', String(value))
    } catch {
      // localStorage co the bi chan (private mode...) - khong sao, chi mat nho lua chon lan sau
    }
  }

  const [items, setItems] = useState<BulkEssayItem[]>([])
  const [loading, setLoading] = useState(false)
  
  // Track open comment boxes
  const [openComments, setOpenComments] = useState<Record<number, boolean>>({})
  const [comments, setComments] = useState<Record<number, string>>({})
  const [gradingIds, setGradingIds] = useState<Record<number, boolean>>({})
  const [aiSubmitting, setAiSubmitting] = useState(false)
  
  // Ref array for scrolling
  const itemRefs = useRef<Record<number, HTMLDivElement | null>>({})

  async function loadItems() {
    setLoading(true)
    try {
      const res = await gradingApi.getPendingEssayAnswers(
        examCampaignId ? Number(examCampaignId) : undefined,
        examId ? Number(examId) : undefined,
        ungradedOnly
      )
      
      if (res.data?.success && res.data.data) {
        setItems(res.data.data)
        // Initialize comments
        const initComments: Record<number, string> = {}
        res.data.data.forEach((item: BulkEssayItem) => {
          if (item.teacherComment) {
            initComments[item.submissionDetailId] = item.teacherComment
          }
        })
        setComments(initComments)
      }
    } catch {
      alert('Lỗi khi tải danh sách bài chấm')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadItems()
  }, [examCampaignId, examId, ungradedOnly])

  // Lang nghe ket qua AI cham tra ve realtime qua SignalR (su kien "AiGradingDone" duoc
  // use-realtime-notifications.ts phat lai thanh custom event "ai-grading:done").
  // Cap nhat ngay item tuong ung thay vi phai cho polling.
  useEffect(() => {
    function handleAiGradingDone(e: Event) {
      const detail = (e as CustomEvent).detail as {
        submissionDetailId?: number
        aiScore?: number
        aiComment?: string
        scoreObtained?: number | null
      } | undefined
      if (!detail?.submissionDetailId) return

      setItems(prev => prev.map(item =>
        item.submissionDetailId === detail.submissionDetailId
          ? {
              ...item,
              aiGradingStatus: 'Done',
              aiScore: detail.aiScore ?? item.aiScore,
              aiComment: detail.aiComment ?? item.aiComment,
              // Dung dung gia tri ScoreObtained thuc te backend vua tra ve - KHONG tu doan bang
              // aiScore nua, vi che do "AI goi y, nguoi cham duyet lai" (autoFinalize=false) co chu
              // dich de ScoreObtained = null cho toi khi nguoi cham bam chon diem tay.
              scoreObtained: detail.scoreObtained !== undefined ? detail.scoreObtained : item.scoreObtained,
            }
          : item
      ))
    }

    window.addEventListener('ai-grading:done', handleAiGradingDone)
    return () => window.removeEventListener('ai-grading:done', handleAiGradingDone)
  }, [])

  // Du phong cho truong hop SignalR bi mat ket noi tam thoi / bo lo su kien: neu con cau nao
  // dang o trang thai Pending/Processing, tu dong tai lai dinh ky de khong bi "ket" mai mai
  // (truoc day chi co 1 lan setTimeout 15s duy nhat sau khi bam "AI Cham").
  useEffect(() => {
    const hasPending = items.some(i => i.aiGradingStatus === 'Pending' || i.aiGradingStatus === 'Processing')
    if (!hasPending) return

    const intervalId = setInterval(() => {
      loadItems()
    }, 5000)

    return () => clearInterval(intervalId)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [items])

  async function handleGrade(id: number, score: number | null, index: number) {
    setGradingIds(prev => ({ ...prev, [id]: true }))
    try {
      const res = await gradingApi.manualGrade({
        submissionDetailId: id,
        score,
        comment: comments[id] || undefined
      })
      if (res.data.success) {
        setItems(prev => prev.map(item => item.submissionDetailId === id ? { ...item, scoreObtained: score } : item))
        // Không alert — đổi màu nút là feedback đủ
        // Auto-scroll to next ungraded item
        if (score !== null) {
          const nextItemIndex = items.findIndex((item, idx) => idx > index && item.scoreObtained === null)
          if (nextItemIndex !== -1) {
            const nextId = items[nextItemIndex].submissionDetailId
            itemRefs.current[nextId]?.scrollIntoView({ behavior: 'smooth', block: 'center' })
          }
        }
      } else {
        alert(res.data.message || 'Lỗi lưu điểm')
      }
    } catch {
      alert('Lỗi kết nối')
    } finally {
      setGradingIds(prev => ({ ...prev, [id]: false }))
    }
  }

  async function handleSaveComment(id: number, score: number | null) {
    setGradingIds(prev => ({ ...prev, [id]: true }))
    try {
      const res = await gradingApi.manualGrade({
        submissionDetailId: id,
        score,
        comment: comments[id] || undefined
      })
      if (res.data.success) {
        // Không alert khi thành công — nười chấm thấy textarea giữ nguyên là đủ feedback
      } else {
        alert(res.data.message || 'Lỗi lưu nhận xét')
      }
    } catch {
      alert('Lỗi kết nối')
    } finally {
      setGradingIds(prev => ({ ...prev, [id]: false }))
    }
  }

  const handleKeyDown = (e: React.KeyboardEvent, id: number, index: number) => {
    const target = e.target as HTMLElement
    if (target.tagName === 'TEXTAREA' || target.tagName === 'INPUT') return

    if (e.key === '0') {
      handleGrade(id, 0, index)
    } else if (e.key === '1') {
      handleGrade(id, 1, index)
    } else if (e.key === 'z' || e.key === 'Z') {
      handleGrade(id, 0.5, index)
    }
  }

  const gradedCount = items.filter(i => i.scoreObtained !== null).length
  const totalCount = items.length
  const ungradedItems = items.filter(i => i.scoreObtained === null)
  const hasPendingAiItems = items.some(i => i.aiGradingStatus === 'Pending' || i.aiGradingStatus === 'Processing')

  async function handleAiGradeAll() {
    const ungradedIds = ungradedItems.map(i => i.submissionDetailId)
    if (ungradedIds.length === 0) {
      alert('Không có câu nào cần chấm AI')
      return
    }
    setAiSubmitting(true)
    try {
      const res = await gradingApi.aiGradeBatch(ungradedIds, autoFinalize)
      if (res.data.success) {
        // Cập nhật trạng thái local ngay lập tức để FE hiện spinner.
        // Kết quả thật sẽ về qua sự kiện realtime "ai-grading:done" hoặc qua polling dự phòng
        // (xem các useEffect ở trên) — không còn phụ thuộc vào 1 lần setTimeout cố định 15s nữa.
        setItems(prev => prev.map(item =>
          ungradedIds.includes(item.submissionDetailId)
            ? { ...item, aiGradingStatus: 'Pending' }
            : item
        ))
      } else {
        alert(res.data.message || 'Lỗi khi gửi yêu cầu AI')
      }
    } catch {
      alert('Lỗi kết nối khi gửi yêu cầu AI')
    } finally {
      setAiSubmitting(false)
    }
  }

  return (
    <div className="p-6 max-w-5xl mx-auto">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold">Chấm tự luận hàng loạt</h1>
          <p className="text-gray-500 mt-1">Đã chấm {gradedCount} / {totalCount} câu</p>
        </div>
        <div className="flex items-center gap-3">
          <div
            className="flex items-center bg-gray-100 rounded-lg p-0.5 gap-0.5 text-xs"
            title="Chế độ chấm AI: chọn AI tự chốt điểm ngay, hoặc chỉ đưa gợi ý để người chấm duyệt lại"
          >
            <button
              type="button"
              onClick={() => handleAutoFinalizeChange(true)}
              className={`px-2.5 py-1.5 rounded-md font-medium transition-all ${
                autoFinalize ? 'bg-white text-purple-700 shadow-sm' : 'text-gray-500 hover:text-gray-700'
              }`}
            >
              AI tự chốt điểm
            </button>
            <button
              type="button"
              onClick={() => handleAutoFinalizeChange(false)}
              className={`px-2.5 py-1.5 rounded-md font-medium transition-all ${
                !autoFinalize ? 'bg-white text-purple-700 shadow-sm' : 'text-gray-500 hover:text-gray-700'
              }`}
            >
              AI gợi ý, tôi duyệt lại
            </button>
          </div>
          <label className="flex items-center gap-2 text-sm">
            <input
              type="checkbox"
              checked={ungradedOnly}
              onChange={e => setUngradedOnly(e.target.checked)}
              className="rounded"
            />
            Chỉ hiện chưa chấm
          </label>
          {ungradedItems.length > 0 && (
            <Button
              variant="outline"
              className="border-purple-300 text-purple-700 hover:bg-purple-50"
              onClick={handleAiGradeAll}
              disabled={loading || aiSubmitting || hasPendingAiItems}
              title={
                hasPendingAiItems
                  ? 'AI đang chấm các câu đã gửi trước đó, vui lòng đợi...'
                  : autoFinalize
                    ? `Gửi ${ungradedItems.length} câu cho AI chấm - điểm AI đưa ra sẽ được chốt luôn`
                    : `Gửi ${ungradedItems.length} câu cho AI chấm - chỉ ra gợi ý, bạn cần duyệt lại từng câu`
              }
            >
              {aiSubmitting || hasPendingAiItems ? (
                <Loader2 className="h-4 w-4 mr-2 animate-spin" />
              ) : (
                <Sparkles className="h-4 w-4 mr-2" />
              )}
              AI Chấm ({ungradedItems.length} câu)
            </Button>
          )}
          <Button variant="outline" onClick={loadItems} disabled={loading}>
            {loading ? <Loader2 className="h-4 w-4 animate-spin mr-2" /> : null}
            Tải lại
          </Button>
        </div>
      </div>

      <div className="space-y-6">
        {items.length === 0 && !loading && (
          <div className="text-center py-12 text-gray-500 bg-white rounded-xl shadow-sm border">
            Không có câu tự luận nào cần chấm.
          </div>
        )}

        {items.map((item, index) => {
          const isGrading = gradingIds[item.submissionDetailId]
          const isCommentOpen = openComments[item.submissionDetailId] || !!item.teacherComment

          return (
            <div 
              key={item.submissionDetailId} 
              ref={el => { itemRefs.current[item.submissionDetailId] = el }}
              tabIndex={0}
              onKeyDown={(e) => handleKeyDown(e, item.submissionDetailId, index)}
              className="bg-white p-5 rounded-xl shadow-sm border focus-within:ring-2 focus-within:ring-blue-500 outline-none transition-shadow"
            >
              <div className="flex justify-between items-start mb-3">
                <div className="flex-1">
                  <div className="flex items-center gap-2 mb-1 flex-wrap">
                    <span className="font-semibold text-blue-800">{item.fullName}</span>
                    <span className="text-xs text-gray-500">({item.employeeCode})</span>
                    {item.submitTime && (
                      <span
                        className="text-xs text-amber-700 bg-amber-50 border border-amber-200 rounded px-1.5 py-0.5"
                        title={`Bài này nộp lúc ${new Date(item.submitTime).toLocaleString('vi-VN')} - nếu thí sinh thi lại nhiều lần, mỗi lần là 1 lượt nộp riêng, kiểm tra đúng lượt trước khi chấm.`}
                      >
                        Nộp lúc: {new Date(item.submitTime).toLocaleString('vi-VN')}
                      </span>
                    )}
                  </div>
                  <p className="text-sm font-medium text-gray-900 line-clamp-2" title={item.questionContent}>
                    Q: {item.questionContent}
                  </p>
                </div>
              </div>

              <div className="bg-gray-50 p-4 rounded-lg mb-4 text-sm text-gray-800 whitespace-pre-wrap">
                {item.essayAnswer || <span className="text-gray-400 italic">Không có câu trả lời</span>}
              </div>

              {item.suggestedAnswer && (
                <div className="bg-green-50/50 p-3 rounded-lg mb-4 text-xs text-green-800 border border-green-100">
                  <span className="font-semibold">Đáp án chuẩn: </span> {item.suggestedAnswer}
                </div>
              )}

              {/* AI Grading Status & Suggestion */}
              {(item.aiGradingStatus === 'Pending' || item.aiGradingStatus === 'Processing') && (
                <div className="flex items-center gap-2 text-xs text-blue-600 bg-blue-50 p-2 rounded-lg mb-3 border border-blue-100">
                  <Loader2 className="h-3 w-3 animate-spin" />
                  AI đang chấm bài này...
                </div>
              )}
              {item.aiGradingStatus === 'Done' && item.aiScore != null && (
                <div className="text-xs bg-purple-50 p-2 rounded-lg mb-3 border border-purple-100">
                  <span className="text-purple-700 font-semibold">🤖 AI gợi ý: </span>
                  <span className={`font-bold ${
                    item.aiScore === 1 ? 'text-green-700' : item.aiScore === 0.5 ? 'text-yellow-700' : 'text-red-700'
                  }`}>{item.aiScore} điểm</span>
                  {item.aiComment && <span className="text-purple-600"> — {item.aiComment.replace(/^\[AI\]\s*/, '')}</span>}
                </div>
              )}
              {item.aiGradingStatus === 'Error' && (
                <div className="text-xs text-red-600 bg-red-50 p-2 rounded-lg mb-3 border border-red-100">
                  ⚠️ AI không thể chấm câu này — vui lòng chấm tay
                  {item.aiComment && (
                    <div className="mt-1 text-red-500 italic">Chi tiết lỗi: {item.aiComment}</div>
                  )}
                </div>
              )}

              <div className="flex items-center justify-between mt-4 border-t pt-4">
                <div className="flex items-center gap-3">
                  <Button
                    variant={item.scoreObtained === 0 ? "default" : "outline"}
                    className={item.scoreObtained === 0 ? "bg-red-600 hover:bg-red-700 text-white" : ""}
                    onClick={() => handleGrade(item.submissionDetailId, 0, index)}
                    disabled={isGrading || item.aiGradingStatus === 'Processing' || item.aiGradingStatus === 'Pending'}
                  >
                    0
                  </Button>
                  <Button
                    variant={item.scoreObtained === 0.5 ? "default" : "outline"}
                    className={item.scoreObtained === 0.5 ? "bg-yellow-600 hover:bg-yellow-700 text-white" : ""}
                    onClick={() => handleGrade(item.submissionDetailId, 0.5, index)}
                    disabled={isGrading || item.aiGradingStatus === 'Processing' || item.aiGradingStatus === 'Pending'}
                  >
                    0.5
                  </Button>
                  <Button
                    variant={item.scoreObtained === 1 ? "default" : "outline"}
                    className={item.scoreObtained === 1 ? "bg-green-600 hover:bg-green-700 text-white" : ""}
                    onClick={() => handleGrade(item.submissionDetailId, 1, index)}
                    disabled={isGrading || item.aiGradingStatus === 'Processing' || item.aiGradingStatus === 'Pending'}
                  >
                    1.0
                  </Button>
                  {isGrading && <Loader2 className="h-4 w-4 animate-spin text-gray-400" />}
                </div>

                <Button 
                  variant="ghost" 
                  size="sm" 
                  className="text-gray-500"
                  onClick={() => setOpenComments(prev => ({ ...prev, [item.submissionDetailId]: !prev[item.submissionDetailId] }))}
                >
                  <MessageSquare className="h-4 w-4 mr-2" />
                  Nhận xét
                </Button>
              </div>

              {isCommentOpen && (
                <div className="mt-4 flex gap-2">
                  <textarea
                    className="flex-1 text-sm rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 p-2 border"
                    rows={2}
                    placeholder="Nhập nhận xét (không bắt buộc)..."
                    value={comments[item.submissionDetailId] || ''}
                    onChange={(e) => setComments(prev => ({ ...prev, [item.submissionDetailId]: e.target.value }))}
                  />
                  <Button 
                    variant="secondary" 
                    className="self-end"
                    onClick={() => handleSaveComment(item.submissionDetailId, item.scoreObtained ?? null)}
                    disabled={isGrading}
                  >
                    <Save className="h-4 w-4" />
                  </Button>
                </div>
              )}
            </div>
          )
        })}
      </div>
    </div>
  )
}
