import { useEffect, useState } from 'react'
import { gradingApi } from '../api'
import type { ExamResultDetailDto } from '../types'
import { Button } from '@/components/ui/button'
import { X, CheckCircle2, XCircle, AlertTriangle, ThumbsUp, ThumbsDown, Loader2, ListFilter, RotateCcw } from 'lucide-react'

type QuestionFilter = 'all' | 'tracnghiem' | 'tuLuan'

interface ResultDetailDialogProps {
  open: boolean
  baiThiId: number | null
  onClose: () => void
  isAdmin?: boolean
}

export function ResultDetailDialog({ open, baiThiId, onClose, isAdmin = false }: ResultDetailDialogProps) {
  const [detail, setDetail] = useState<ExamResultDetailDto | null>(null)
  const [loading, setLoading] = useState(false)
  const [gradingId, setGradingId] = useState<number | null>(null)
  const [filter, setFilter] = useState<QuestionFilter>('all')

  useEffect(() => {
    if (open && baiThiId) {
      loadDetail(baiThiId)
      setFilter('all')
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, baiThiId])

  const loadDetail = async (id: number) => {
    setLoading(true)
    try {
      const response = await gradingApi.getResultDetail(id)
      if (response.data.success && response.data.data) {
        setDetail(response.data.data)
      }
    } catch {
      // handle error
    } finally {
      setLoading(false)
    }
  }

  const handleManualGrade = async (chiTietLamBaiId: number, isCorrect: boolean | null) => {
    if (!baiThiId) return
    setGradingId(chiTietLamBaiId)
    try {
      const response = await gradingApi.manualGrade({ chiTietLamBaiId, isCorrect })
      if (response.data.success) {
        // Reload detail để cập nhật điểm mới
        await loadDetail(baiThiId)
      }
    } catch {
      // silent
    } finally {
      setGradingId(null)
    }
  }

  if (!open) return null

  // Kiểm tra xem bài thi có câu tự luận chưa chấm không
  const hasUngradedEssay = detail?.answers?.some(
    a => (a.questionCategory === 'Tự luận' || a.questionCategory === 'TuLuan' || a.questionCategory === 'TL') && a.scoreObtained == null
  ) ?? false

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-2xl max-h-[85vh] overflow-y-auto">
        <div className="flex items-center justify-between p-4 border-b sticky top-0 bg-white z-10">
          <h2 className="text-lg font-semibold">Chi tiết bài thi</h2>
          <Button variant="ghost" size="icon" onClick={onClose}>
            <X className="h-4 w-4" />
          </Button>
        </div>

        {loading ? (
          <div className="p-8 text-center text-gray-500">Đang tải...</div>
        ) : detail ? (
          <div className="p-4 space-y-4">
            {/* Summary */}
            <div className="grid grid-cols-2 gap-3 text-center">
              <div className="p-3 bg-green-50 rounded-lg">
                <p className="text-2xl font-bold text-green-700">
                  {detail.correctAnswers ?? '—'}/{detail.tongSoCau ?? '—'}
                </p>
                <p className="text-xs text-green-600">Số câu đúng / Tổng câu</p>
              </div>
              <div className="p-3 bg-gray-50 rounded-lg">
                <p className="text-xl font-bold text-gray-700">{detail.durationMinutes ?? 0} phút</p>
                <p className="text-xs text-gray-600">Thời gian làm bài</p>
              </div>
            </div>

            {detail.soCauDungToiThieu !== undefined && detail.soCauDungToiThieu !== null && (
              <div className={`p-3 rounded-lg border text-center font-semibold text-sm flex items-center justify-center gap-2 ${
                detail.pass
                  ? 'bg-emerald-50 border-emerald-200 text-emerald-800'
                  : 'bg-rose-50 border-rose-200 text-rose-800'
              }`}>
                {detail.pass ? (
                  <>
                    <CheckCircle2 className="h-4.5 w-4.5 text-emerald-600" />
                    <span>Kết quả: ĐẠT</span>
                  </>
                ) : (
                  <>
                    <XCircle className="h-4.5 w-4.5 text-rose-600" />
                    <span>Kết quả: KHÔNG ĐẠT</span>
                  </>
                )}
                <span className="text-xs font-normal opacity-85 ml-1">
                  (Yêu cầu tối thiểu để đạt: {detail.soCauDungToiThieu} câu đúng)
                </span>
              </div>
            )}

            {/* Thông báo câu tự luận chưa chấm */}
            {hasUngradedEssay && isAdmin && (
              <div className="flex items-center gap-2 bg-yellow-50 border border-yellow-300 text-yellow-800 rounded-lg px-3 py-2 text-sm">
                <AlertTriangle className="h-4 w-4 shrink-0" />
                <span>Còn câu hỏi tự luận chưa được chấm. Vui lòng chấm trước khi công bố điểm.</span>
              </div>
            )}

            {/* Metadata (Warnings / Attempt details) */}
            <div className="flex flex-wrap gap-4 text-sm justify-between px-1 bg-gray-50/50 p-2.5 rounded-lg border">
              <div className="text-gray-500">
                Lần thi: <span className="font-semibold text-gray-800">{detail.soLanThi ?? 1}</span>
                {(detail.soLanThi ?? 1) > 1 && <span className="text-xs text-blue-500 ml-1">({detail.soLanThiLai ?? ((detail.soLanThi ?? 1) - 1)} lần thi lại)</span>}
              </div>
              {((detail.soCanhBao ?? 0) > 0 || (detail.soLanGianLan ?? 0) > 0) ? (
                <div className="text-orange-600 flex items-center gap-1.5">
                  <AlertTriangle className="h-4 w-4 shrink-0" />
                  <span>
                    Số lần vi phạm: <strong className="font-bold">{detail.soCanhBao ?? 0}</strong>
                    {(detail.soLanGianLan ?? 0) > (detail.soCanhBao ?? 0) && (
                      <> (Tổng tích lũy: <strong>{detail.soLanGianLan}</strong>)</>
                    )}
                  </span>
                </div>
              ) : (
                <div className="text-green-600 font-medium">Không có vi phạm</div>
              )}
            </div>

            {/* Answers */}
            {detail.answers && detail.answers.length > 0 && (() => {
              // Gán số thứ tự gốc rồi lọc theo filter
              const indexed = detail.answers.map((a, idx) => ({ a, origIdx: idx }))

              const filtered = indexed.filter(({ a }) => {
                const isTuLuan = a.questionCategory === 'Tự luận' || a.questionCategory === 'TuLuan' || a.questionCategory === 'TL'
                if (filter === 'tracnghiem') return !isTuLuan
                if (filter === 'tuLuan') return isTuLuan
                return true
              })

              // Sắp xếp: tự luận chưa chấm lên trên, giữ nguyên thứ tự gốc trong từng nhóm
              const sorted = [...filtered].sort((x, y) => {
                const xTuLuan = x.a.questionCategory === 'Tự luận' || x.a.questionCategory === 'TuLuan' || x.a.questionCategory === 'TL'
                const yTuLuan = y.a.questionCategory === 'Tự luận' || y.a.questionCategory === 'TuLuan' || y.a.questionCategory === 'TL'
                const xUngraded = xTuLuan && x.a.scoreObtained == null
                const yUngraded = yTuLuan && y.a.scoreObtained == null
                if (xUngraded && !yUngraded) return -1
                if (!xUngraded && yUngraded) return 1
                return x.origIdx - y.origIdx
              })

              const tracNghiemCount = indexed.filter(({ a }) => !(a.questionCategory === 'Tự luận' || a.questionCategory === 'TuLuan' || a.questionCategory === 'TL')).length
              const tuLuanCount = indexed.filter(({ a }) => a.questionCategory === 'Tự luận' || a.questionCategory === 'TuLuan' || a.questionCategory === 'TL').length
              const ungradedEssayCount = indexed.filter(({ a }) => (a.questionCategory === 'Tự luận' || a.questionCategory === 'TuLuan' || a.questionCategory === 'TL') && a.scoreObtained == null).length

              return (
                <div className="space-y-3">
                  {/* Header + filter tabs */}
                  <div className="flex items-center justify-between flex-wrap gap-2">
                    <h3 className="text-sm font-medium text-gray-700 flex items-center gap-1.5">
                      <ListFilter className="h-4 w-4 text-gray-400" />
                      Chi tiết từng câu
                    </h3>
                    <div className="flex items-center bg-gray-100 rounded-lg p-0.5 gap-0.5">
                      {([
                        { key: 'all' as QuestionFilter, label: 'Tất cả', count: indexed.length },
                        { key: 'tracnghiem' as QuestionFilter, label: 'Trắc nghiệm', count: tracNghiemCount },
                        { key: 'tuLuan' as QuestionFilter, label: 'Tự luận', count: tuLuanCount },
                      ]).map(tab => (
                        <button
                          key={tab.key}
                          onClick={() => setFilter(tab.key)}
                          className={`px-2.5 py-1 rounded-md text-xs font-medium transition-all ${
                            filter === tab.key
                              ? 'bg-white text-gray-800 shadow-sm'
                              : 'text-gray-500 hover:text-gray-700'
                          }`}
                        >
                          {tab.label}
                          <span className={`ml-1 text-[10px] ${
                            filter === tab.key ? 'text-gray-500' : 'text-gray-400'
                          }`}>({tab.count})</span>
                        </button>
                      ))}
                    </div>
                  </div>

                  {/* Thông báo nhỏ khi đang xem tự luận có câu chưa chấm */}
                  {filter === 'tuLuan' && ungradedEssayCount > 0 && (
                    <p className="text-xs text-amber-600 bg-amber-50 border border-amber-200 rounded-lg px-3 py-1.5">
                      ↑ {ungradedEssayCount} câu chưa chấm được đẩy lên đầu danh sách
                    </p>
                  )}
                  {filter === 'all' && ungradedEssayCount > 0 && (
                    <p className="text-xs text-amber-600 bg-amber-50 border border-amber-200 rounded-lg px-3 py-1.5">
                      ↑ {ungradedEssayCount} câu tự luận chưa chấm được đẩy lên đầu danh sách
                    </p>
                  )}

                  {sorted.length === 0 ? (
                    <div className="text-center text-sm text-gray-400 py-6">Không có câu hỏi nào</div>
                  ) : (
                    sorted.map(({ a, origIdx }) => {
                      const isTuLuan = a.questionCategory === 'Tự luận' || a.questionCategory === 'TuLuan' || a.questionCategory === 'TL'
                      const isGraded = a.scoreObtained != null
                      const isEssayCorrect = isTuLuan && a.scoreObtained === 1
                      const isGrading = gradingId === a.chiTietLamBaiId
                      const isUngradedEssay = isTuLuan && !isGraded

                      return (
                        <div
                          key={origIdx}
                          className={`border rounded-lg p-3 transition-colors ${
                            isUngradedEssay
                              ? 'bg-amber-50/60 border-amber-200'
                              : isTuLuan
                                ? 'bg-blue-50/30 border-blue-100'
                                : ''
                          }`}
                        >
                          <div className="flex items-start gap-2">
                            {/* Icon kết quả */}
                            {isTuLuan ? (
                              isGraded ? (
                                isEssayCorrect
                                  ? <CheckCircle2 className="h-5 w-5 text-green-500 shrink-0 mt-0.5" />
                                  : <XCircle className="h-5 w-5 text-red-400 shrink-0 mt-0.5" />
                              ) : (
                                <AlertTriangle className="h-5 w-5 text-yellow-400 shrink-0 mt-0.5" />
                              )
                            ) : (
                              a.isCorrect
                                ? <CheckCircle2 className="h-5 w-5 text-green-500 shrink-0 mt-0.5" />
                                : <XCircle className="h-5 w-5 text-red-400 shrink-0 mt-0.5" />
                            )}

                            <div className="flex-1 min-w-0">
                              <div className="flex items-center gap-2 flex-wrap">
                                <p className="text-sm font-medium">Câu {origIdx + 1}: {a.noiDungCauHoi}</p>
                                {isTuLuan && (
                                  <span className="text-xs bg-blue-100 text-blue-700 px-1.5 py-0.5 rounded">Tự luận</span>
                                )}
                                {isUngradedEssay && (
                                  <span className="text-xs bg-amber-100 text-amber-700 px-1.5 py-0.5 rounded font-medium">Chưa chấm</span>
                                )}
                              </div>

                              {/* Câu trả lời */}
                              <p className="text-xs text-gray-500 mt-1">
                                Trả lời:{' '}
                                <span className={
                                  isTuLuan
                                    ? 'text-gray-700'
                                    : a.isCorrect ? 'text-green-600' : 'text-red-600'
                                }>
                                  {a.noiDungDapAn || a.cauTraLoiTuLuan || '(Không trả lời)'}
                                </span>
                              </p>

                              {/* Đáp án đúng cho trắc nghiệm */}
                              {!isTuLuan && !a.isCorrect && a.noiDungDapAnDung && (
                                <p className="text-xs text-green-600 mt-0.5">
                                  Đáp án đúng: {a.noiDungDapAnDung}
                                </p>
                              )}

                              {/* Đáp án chuẩn cho tự luận */}
                              {isTuLuan && a.noiDungDapAnDung && (
                                <div className="mt-2 bg-green-50/50 border border-green-100 p-2 rounded-lg text-xs">
                                  <span className="font-semibold text-green-800">💡 Đáp án chuẩn / Hướng dẫn chấm:</span>
                                  <p className="text-green-700 whitespace-pre-wrap mt-0.5">{a.noiDungDapAnDung}</p>
                                </div>
                              )}

                              {/* Chấm điểm tự luận - chỉ admin mới thấy nút */}
                              {isTuLuan && isAdmin && (
                                <div className="mt-2 flex items-center gap-2">
                                  {!isGraded && (
                                    <span className="text-xs text-yellow-600 font-medium">Chưa chấm</span>
                                  )}
                                  {isGraded && (
                                    <span className={`text-xs font-medium ${isEssayCorrect ? 'text-green-600' : 'text-red-500'}`}>
                                      {isEssayCorrect ? '✓ Đúng' : '✗ Sai'}
                                    </span>
                                  )}
                                  <button
                                    disabled={isGrading}
                                    onClick={() => a.chiTietLamBaiId != null && handleManualGrade(a.chiTietLamBaiId, true)}
                                    className={`inline-flex items-center gap-1 px-2.5 py-1 rounded-lg text-xs font-medium border transition-colors ${
                                      isEssayCorrect
                                        ? 'bg-green-500 text-white border-green-500'
                                        : 'bg-white text-green-600 border-green-300 hover:bg-green-50'
                                    }`}
                                  >
                                    {isGrading ? <Loader2 className="h-3 w-3 animate-spin" /> : <ThumbsUp className="h-3 w-3" />}
                                    Đúng
                                  </button>
                                  <button
                                    disabled={isGrading}
                                    onClick={() => a.chiTietLamBaiId != null && handleManualGrade(a.chiTietLamBaiId, false)}
                                    className={`inline-flex items-center gap-1 px-2.5 py-1 rounded-lg text-xs font-medium border transition-colors ${
                                      isGraded && !isEssayCorrect
                                        ? 'bg-red-500 text-white border-red-500'
                                        : 'bg-white text-red-500 border-red-300 hover:bg-red-50'
                                    }`}
                                  >
                                    {isGrading ? <Loader2 className="h-3 w-3 animate-spin" /> : <ThumbsDown className="h-3 w-3" />}
                                    Sai
                                  </button>
                                  {isGraded && (
                                    <button
                                      disabled={isGrading}
                                      onClick={() => a.chiTietLamBaiId != null && handleManualGrade(a.chiTietLamBaiId, null)}
                                      className="inline-flex items-center gap-1 px-2.5 py-1 rounded-lg text-xs font-medium border transition-colors bg-white text-gray-600 border-gray-300 hover:bg-gray-50 ml-2"
                                    >
                                      {isGrading ? <Loader2 className="h-3 w-3 animate-spin" /> : <RotateCcw className="h-3 w-3" />}
                                      Chấm lại
                                    </button>
                                  )}
                                </div>
                              )}

                              {/* Trạng thái câu tự luận cho non-admin */}
                              {isTuLuan && !isAdmin && isGraded && (
                                <p className={`text-xs mt-1 font-medium ${isEssayCorrect ? 'text-green-600' : 'text-red-500'}`}>
                                  Kết quả: {isEssayCorrect ? '✓ Đúng' : '✗ Sai'}
                                </p>
                              )}
                            </div>
                          </div>
                        </div>
                      )
                    })
                  )}
                </div>
              )
            })()}
          </div>
        ) : (
          <div className="p-8 text-center text-gray-500">Không có dữ liệu</div>
        )}
      </div>
    </div>
  )
}
