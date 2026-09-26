export interface ExamQuestionDto {
  id: number
  content: string | null
  imageUrl: string | null
  questionOrder: number
  options: ExamChoiceDto[]
  selectedOptionId: number | null
  selectedOptionIdList?: number[]
  essayAnswer: string | null
  isSaved: boolean
  allowMultipleSelection?: boolean
}

export interface ExamChoiceDto {
  id: number
  content: string | null
  orderIndex: number
}

export interface ExamSubmissionDto {
  id: number
  userId: number
  examPaperId: number
  examCampaignId?: number | null
  status: string | null
  submitTime: string | null
  totalScore: number | null
  calculatedScore?: number | null  // computed: (correctAnswers/totalQuestions)*10
  correctAnswers: number | null
  totalQuestions: number | null
  totalWarnings: number | null
  examPaperName: string | null
  examPaperCode: string | null
  durationMinutes: number | null
  startTime: string | null
  remainingTimeSeconds: number | null // seconds
  isResultPublished?: boolean
  pass?: boolean | null
}

export interface StartExamDto {
  examPaperCode?: string
  examCampaignId?: number
}

export interface SubmitAnswerDto {
  examSubmissionId: number
  questionId: number
  selectedOptionId: number | null
  essayAnswer?: string
  essayImageUrl?: string | null
  isSaved: boolean
}

export interface SubmitExamDto {
  examSubmissionId: number
  answers: SubmitAnswerDto[]
}

export interface SubmitMultipleAnswerDto {
  examSubmissionId: number
  questionId: number
  selectedOptionId: number[]
  essayAnswer?: string
  essayImageUrl?: string | null
  isSaved: boolean
}

export interface CheatingWarningDto {
  examSubmissionId: number
  warningType: string
  description?: string
}
