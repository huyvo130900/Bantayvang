export interface ExamResultDetailDto {
  examSubmissionId: number
  userId: number | null
  username: string | null
  fullName: string | null
  employeeCode?: string | null
  department: string | null
  examId: number
  examPaperId?: number | null
  examPaperCode: string | null
  examPaperName: string | null
  startTime: string | null
  submitTime?: string
  durationMinutes?: number
  durationSeconds?: number
  totalScore?: number | null
  correctAnswers: number | null
  totalQuestions: number | null
  status: string | null
  pass: boolean
  minPassQuestions?: number | null
  warningCount: number | null
  questionsGraded?: number
  attemptCount?: number
  cheatingCount?: number
  retakeCount?: number
  isResultPublished?: boolean
  totalMultipleChoiceQuestions?: number
  multipleChoiceQuestionsGraded?: number
  totalEssayQuestions?: number
  essayQuestionsGraded?: number
  campaignEndTime?: string | null  // thời gian kết thúc ca thi để kiểm tra còn hạn không
  departmentEvaluation?: string    // nhận xét của quản lý khoa
  answers?: AnswerDetailDto[]
}

export interface AnswerDetailDto {
  questionId: number
  questionContent: string | null
  questionCategory?: string | null
  selectedOptionId: number | null
  answerContent: string | null
  essayAnswer: string | null
  essayImageUrl?: string | null
  suggestedAnswer?: string | null
  isCorrect: boolean
  scoreObtained: number | null
  correctOptionId: number | null
  correctAnswerContent: string | null
  submissionDetailId?: number | null
  teacherComment?: string | null
  // AI Grading fields
  aiScore?: number | null
  aiComment?: string | null
  aiGradingStatus?: string | null  // null | 'Pending' | 'Processing' | 'Done' | 'Error'
}

export interface ManualGradingDto {
  submissionDetailId: number
  score: number | null
  comment?: string
}

export interface PendingEssayDto {
  examSubmissionId: number
  userId: number | null
  username: string | null
  fullName: string | null
  employeeCode?: string | null
  department: string | null
  examPaperCode: string | null
  examPaperName: string | null
  submitTime: string | null
  totalScore?: number | null
  correctAnswers: number | null
  totalQuestions: number | null
  status: string | null
  ungradedEssayQuestions: number
  totalEssayQuestions: number
  campaignName?: string | null
}

export interface BulkEssayItem {
  submissionDetailId: number
  examSubmissionId: number
  userId?: number | null
  username?: string | null
  fullName?: string | null
  employeeCode?: string | null
  department?: string | null
  // Thoi diem nop bai cua chinh luot thi (ExamSubmission) chua cau nay - giup phan biet khi
  // thi sinh thi lai nhieu lan (moi lan la 1 examSubmissionId rieng, khong the nhan ra qua ten).
  submitTime?: string | null
  questionId: number
  questionContent: string
  essayAnswer?: string | null
  suggestedAnswer?: string | null
  scoreObtained?: number | null
  teacherComment?: string | null
  isGraded: boolean
  // AI Grading fields
  aiScore?: number | null
  aiComment?: string | null
  aiGradingStatus?: string | null  // null | 'Pending' | 'Processing' | 'Done' | 'Error'
}
