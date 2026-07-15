export interface ExamPaperDto {
  id: number
  examPaperCode: string | null
  examPaperName: string | null
  durationMinutes: number | null
  totalScore: number | null
  thoiGianBatDau: string | null
  linkTruyCap: string | null
  status: string | null
  createdAt: string | null
  totalQuestions: number
  department?: string | null
  soCauRandom?: number | null
  isResultPublished?: boolean
  thoiGianCongBo?: string | null
  examCampaignId?: number | null
  soCauDungToiThieu?: number | null
}

export interface CreateExamPaperDto {
  examPaperCode: string
  examPaperName?: string
  durationMinutes: number
  thoiGianBatDau?: string
  status?: string
  // Mới: chọn câu hỏi theo khoa + số câu random
  department?: string
  soCauRandom?: number
  // Legacy: chọn tay (không dùng nữa nhưng giữ tương thích)
  danhSachIdCauHoi: number[]
  examCampaignId?: number
  soCauDungToiThieu?: number | null
}

export interface ExamAssignmentDto {
  id: number
  examId: number
  examPaperCode: string | null
  examPaperName: string | null
  userId: number
  username: string | null
  fullName: string | null
  assignedAt: string
  customStartTime: string | null
  extraMinutes: number | null
  isActive: boolean
  note: string | null
  // Trạng thái bài thi
  status: string  // Pending | InProgress | Completed | AutoSubmitted
  // Kết quả nếu đã thi xong
  examSubmissionId: number | null
  diemSo: number | null
  totalScore: number | null
  correctAnswers: number | null
  tongSoCau: number | null
  ngayHoanThanh: string | null
  datYeuCau: boolean | null
  thoiGianBatDau: string | null
  thoiGianKetThuc: string | null
  durationMinutes: number | null
}

export interface CreateExamAssignmentDto {
  examId: number
  userIds: number[]
  customStartTime?: string
  note?: string
}

export interface ExtendExamTimeDto {
  baiThiId: number
  additionalMinutes: number
  reason?: string
}

// Extended assignment type with exam session info (legacy, kept for compatibility)
export interface MyExamDto {
  id: number
  examId: number
  examPaperCode: string | null
  examPaperName: string | null
  thoiGianBatDau: string | null
  thoiGianKetThuc: string | null
  durationMinutes: number | null
  status: string | null
  examSubmissionId: number | null
  ghiChu: string | null
  extraMinutes: number | null
}

export interface ExamPreviewDtoFE {
  id: number
  examPaperCode: string | null
  examPaperName: string | null
  durationMinutes: number | null
  status: string | null
  department: string | null
  isResultPublished: boolean
  questions: QuestionPreviewFE[]
}

export interface QuestionPreviewFE {
  id: number
  content: string | null
  chuDe: string | null
  questionOptions: ChoicePreviewFE[]
}

export interface ChoicePreviewFE {
  id: number
  content: string | null
  isCorrect: boolean | null
}
