export interface ExamPaperDto {
  id: number
  examPaperCode: string | null
  examPaperName: string | null
  durationMinutes: number | null
  totalScore: number | null
  startTime: string | null
  linkTruyCap: string | null
  status: string | null
  createdAt: string | null
  totalQuestions: number
  department?: string | null
  randomQuestionCount?: number | null
  isResultPublished?: boolean
  publishedAt?: string | null
  examCampaignId?: number | null
  minPassQuestions?: number | null
}

export interface CreateExamPaperDto {
  examPaperCode: string
  examPaperName?: string
  durationMinutes: number
  startTime?: string
  status?: string
  // Mới: chọn câu hỏi theo khoa + số câu random
  department?: string
  randomQuestionCount?: number
  // Legacy: chọn tay (không dùng nữa nhưng giữ tương thích)
  questionIds: number[]
  examCampaignId?: number
  minPassQuestions?: number | null
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
  totalQuestions: number | null
  ngayHoanThanh: string | null
  datYeuCau: boolean | null
  startTime: string | null
  endTime: string | null
  durationMinutes: number | null
}

export interface CreateExamAssignmentDto {
  examId: number
  userIds: number[]
  customStartTime?: string
  note?: string
}

export interface ExtendExamTimeDto {
  examSubmissionId: number
  additionalMinutes: number
  reason?: string
}

// Extended assignment type with exam session info (legacy, kept for compatibility)
export interface MyExamDto {
  id: number
  examId: number
  examPaperCode: string | null
  examPaperName: string | null
  startTime: string | null
  endTime: string | null
  durationMinutes: number | null
  status: string | null
  examSubmissionId: number | null
  note: string | null
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
  imageUrl?: string | null
  chuDe: string | null
  questionOptions: ChoicePreviewFE[]
}

export interface ChoicePreviewFE {
  id: number
  content: string | null
  isCorrect: boolean | null
}
